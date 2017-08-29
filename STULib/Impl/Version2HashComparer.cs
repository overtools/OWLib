using OWLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using static STULib.Types.Generic.Common;
using static STULib.Types.Generic.Version2;

namespace STULib.Impl.Version2HashComparer {
    public class InstanceData {
        public FieldData[] fields;
        public uint hash;
        public uint size;
    }
    public class FieldData {
        public byte[] sha1;
        public byte[] demangle_sha1;
        public uint hash;
        public uint size;
        public bool is_nested_standard;
        public bool is_nested_array;
        public FieldData[] nested_fields;
        public bool possible_array;
        public uint possible_array_item_size;

        public bool IsNested => is_nested_array && is_nested_standard;
    }
    //public class FieldClassData {
    //    public uint hash;
    //    public FieldClassField[] fields;
    //    public bool array;
    //}

    //public class FieldClassField {
    //    public uint hash;
    //    public uint size;
    //    public bool array;
    //}

    public class FakeNestedInstance { }


    class FakeGUID : IDemangleable {
        // [STUField(STUVersionOnly = new uint[] { 1 }, IgnoreVersion = new[] { 0xc41B27A5 })]
        private ulong Padding = ulong.MaxValue;

        // [STUField(0xDEADBEEF)] // DUMMY
        public ulong Key;

        public static implicit operator long(FakeGUID i) {
            return (long)i.Key;
        }

        public static implicit operator ulong(FakeGUID i) {
            return i.Key;
        }

        public new string ToString() {
            return $"{GUID.LongKey(Key):X12}.{GUID.Type(Key):X3}" + (GUID.IsMangled(Key) ? " (Mangled)" : "");
        }

        public ulong[] GetGUIDs() {
            return new[] {
                    Key
                };
        }

        public ulong[] GetGUIDXORs() {
            return new[] {
                    Padding
                };
        }

        public void SetGUIDs(ulong[] GUIDs) {
            if (GUIDs?.Length > 0) {
                Key = GUIDs[0];
            }
        }
    }
    class FakeInstanceType : STUInstance { }

    public class Version2Comparer : Version2 {
        public InstanceData[] instanceDiffData;

        public Version2Comparer(Stream stuStream, uint owVersion) : base(stuStream, owVersion) {
        }

        public string GetByteString(byte[] bytes) {
            if (bytes != null) {
                return Convert.ToBase64String(bytes);
            }
            return "null";
        }

        protected FieldData[] FakeReadInstance(Type instanceType, STUInstanceField[] writtenFields, BinaryReader reader,
            int size) {
            MemoryStream sandbox = new MemoryStream(size);
            sandbox.Write(reader.ReadBytes(size), 0, size);
            sandbox.Position = 0;
            using (BinaryReader sandboxedReader = new BinaryReader(sandbox, Encoding.UTF8, false)) {
                object instance = Activator.CreateInstance(instanceType);
                return InitializeObject(instance, instanceType, writtenFields, sandboxedReader) as FieldData[];
            }
        }

        protected override void ReadInstanceData(long offset) {
            stream.Position = offset;
            GetHeaderCRC();
            stream.Position = offset;
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true)) {
                if (instanceTypes == null) {
                    LoadInstanceTypes();
                }

                if (ReadHeaderData(reader)) {
                    if (instanceTypes == null) {
                        LoadInstanceTypes();
                    }

                    long stuOffset = header.Offset;
                    instanceDiffData = new InstanceData[instanceInfo.Length];
                    for (int i = 0; i < instanceInfo.Length; ++i) {
                        stream.Position = stuOffset;
                        stuOffset += instanceInfo[i].InstanceSize;

                        // instances[i] = null;

                        int fieldListIndex = reader.ReadInt32();


                        FieldData[] fields = FakeReadInstance(typeof(FakeInstanceType), instanceFields[fieldListIndex], reader,
                            (int)instanceInfo[i].InstanceSize - 4);
                        instanceDiffData[i] = new InstanceData { size = instanceInfo[i].InstanceSize, hash = instanceInfo[i].InstanceChecksum, fields = fields };

                        // }
                    }

                    //for (int i = 0; i < instanceInfo.Length; ++i) {
                    //    if (instanceInfo[i].AssignInstanceIndex > -1 &&
                    //        instanceInfo[i].AssignInstanceIndex < instances.Count) {
                    //        SetField(instances[instanceInfo[i].AssignInstanceIndex],
                    //            instanceInfo[i].AssignFieldChecksum, instances[i]);
                    //    }
                    //}
                }
            }
        }

        private bool TryReadArrayItems(STUArray array, STUFieldAttribute element, int byteCount, BinaryReader reader) {
            long start = reader.BaseStream.Position;
            bool output = false;
            try {
                for (uint i = 0; i < array.Count; ++i) {
                    if (element != null) {
                        metadata.Position += element.Padding;
                    }
                    reader.ReadBytes(byteCount);
                }
                output = true;
            } catch (Exception) {
                output = false;
            }
            reader.BaseStream.Position = start;
            return output;
        }

        private int GetArrayItemSize(STUArray array, STUFieldAttribute element, BinaryReader reader) {  // nasty
            // unfortunately there is no other way to do this
            int output = -1;
            // todo: currently only standard sizes, more in future?

            // todo: reading 12 and 16 is too much strain on the system.

            if (TryReadArrayItems(array, element, 8, reader)) { output = 16; }
            if (TryReadArrayItems(array, element, 8, reader)) { output = 12; }
            if (TryReadArrayItems(array, element, 8, reader)) { output = 8; }
            if (TryReadArrayItems(array, element, 4, reader)) { output = 4; }
            if (TryReadArrayItems(array, element, 2, reader)) { output = 2; }
            if (TryReadArrayItems(array, element, 1, reader)) { output = 1; }

            return output;
        }

        protected override object InitializeObject(object instance, Type type, STUInstanceField[] writtenFields, BinaryReader reader) {
            FieldData[] output = new FieldData[writtenFields.Length];
            int i = 0;
            foreach (STUInstanceField writtenField in writtenFields) {
                // Console.Out.WriteLine("{0:X}", writtenField.FieldChecksum);
                STUFieldAttribute element = new STUFieldAttribute { Checksum = writtenField.FieldChecksum };  // hmm
                bool isNestedStandard = false;
                bool isNestedArray = false;
                bool possibleArray = false;
                int possibleArrayItemSize = 0;
                long afterNestedPosition = -1;

                FieldData[] nestedFields = null;

                long beforeArrayPos = reader.BaseStream.Position;

                try {
                    int arrayItemSize = 0;
                    int offset = reader.ReadInt32();
                    metadata.Position = offset;
                    STUArray array = metadataReader.Read<STUArray>();
                    metadata.Position = array.Offset;

                    if (array.Count > 1000) {  // ewww, this is nasty, if we try and load lots (usually a sign that its not actually an array) the system will choke
                        throw new InvalidDataException("too much data?");
                    }

                    uint parent = 0;
                    uint parentSize = 0;
                    if (instanceArrayRef?.Length > array.InstanceIndex) {
                        parent = instanceArrayRef[array.InstanceIndex].Checksum;
                        parentSize = instanceArrayRef[array.InstanceIndex].Size;
                    }

                    arrayItemSize = GetArrayItemSize(array, element, metadataReader);
                    if (arrayItemSize > 0 && array.Count < 9999 && array.Count > 0) {
                        possibleArray = true;
                        possibleArrayItemSize = arrayItemSize;
                    }
                } catch (Exception) {
                    // Console.Out.WriteLine($"{writtenField.FieldChecksum:X8}: standard array fail: {e.GetType()}");
                }
                reader.BaseStream.Position = beforeArrayPos;

                if (writtenField.FieldSize == 0) {

                    // todo: nested sha1, every item in array?


                    long nestStartPos = reader.BaseStream.Position;
                    STUNestedInfo nested = reader.Read<STUNestedInfo>();
                    long nestBeforeReadPos = reader.BaseStream.Position;

                    try {
                        Dictionary<uint, FieldData> nestedFieldCache = new Dictionary<uint, FieldData>();
                        foreach (FieldData n_f in InitializeObject(instance, type, instanceFields[nested.FieldListIndex], reader) as FieldData[]) {
                            if (!nestedFieldCache.ContainsKey(n_f.hash)) {
                                nestedFieldCache[n_f.hash] = n_f;
                            } else {
                                if (n_f.size > nestedFieldCache[n_f.hash].size) {
                                    nestedFieldCache[n_f.hash].size = n_f.size;
                                }
                            }
                        }
                        //foreach (KeyValuePair<uint, FieldData> n_f in nestedFieldCache) {
                        //    // Console.Out.WriteLine($"{writtenField.FieldChecksum:X8}: found nest standard: {n_f.Value.hash:X8} ({n_f.Value.size} bytes)");
                        //}
                        isNestedStandard = true;
                        nestedFields = nestedFieldCache.Values.ToArray();
                        afterNestedPosition = reader.BaseStream.Position;
                    } catch (Exception) {
                        // Console.Out.WriteLine($"Fail standard nest: {e.GetType()}");
                    }

                    reader.BaseStream.Position = nestBeforeReadPos;

                    try {
                        Dictionary<uint, FieldData> nestedFieldCache = new Dictionary<uint, FieldData>();
                        for (uint nest_i = 0; nest_i < (uint)nested.FieldListIndex; ++nest_i) {
                            stream.Position += element.Padding;
                            uint fieldIndex = reader.ReadUInt32();
                            if (fieldIndex < 0) {
                                throw new FieldAccessException("invalid instance index");
                            }
                            foreach (FieldData n_f in InitializeObject(instance, type, instanceFields[fieldIndex], reader) as FieldData[]) {
                                if (!nestedFieldCache.ContainsKey(n_f.hash)) {
                                    nestedFieldCache[n_f.hash] = n_f;
                                } else {
                                    if (n_f.size > nestedFieldCache[n_f.hash].size) {
                                        nestedFieldCache[n_f.hash].size = n_f.size;
                                    }
                                }
                            }
                        }
                        //foreach (KeyValuePair<uint, FieldData> n_f in nestedFieldCache) {
                        //    // Console.Out.WriteLine($"{writtenField.FieldChecksum:X8}: found nest array: {n_f.Value.hash:X8} ({n_f.Value.size} bytes)");
                        //}
                        nestedFields = nestedFieldCache.Values.ToArray();
                        isNestedArray = true;
                        afterNestedPosition = reader.BaseStream.Position;
                    } catch (Exception e) {
                        // Console.Out.WriteLine($"Fail nest array: {writtenField.FieldChecksum:X8}: {e}");
                    }

                    reader.BaseStream.Position = nestStartPos;

                    //Console.Out.WriteLine($"{writtenField.FieldChecksum:X8}: a:{isNestedArray} na:{isNestedStandard}");
                }

                reader.BaseStream.Position += element.Padding;

                long position = -1;
                if (element?.ReferenceValue == true) {
                    long offset = reader.ReadInt64();
                    if (offset == 0) {
                        continue;
                    }
                    position = reader.BaseStream.Position;
                    reader.BaseStream.Position = offset;
                }
                long startPosition = reader.BaseStream.Position;
                using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider()) {
                    byte[] demangleSha1 = null;
                    if (writtenField.FieldSize == 8) {  // this might be a GUID, can't be sure
                        FakeGUID f = new FakeGUID { Key = reader.ReadUInt64() };
                        DemangleInstance(f, writtenField.FieldChecksum);
                        demangleSha1 = sha1.ComputeHash(new byte[1] { (byte)f.Key });

                        reader.BaseStream.Position = startPosition;
                    }
                    // Convert.ToBase64String(
                    output[i] = new FieldData {
                        sha1 = sha1.ComputeHash(reader.ReadBytes((int)writtenField.FieldSize)),
                        size = writtenField.FieldSize,
                        hash = writtenField.FieldChecksum,
                        demangle_sha1 = demangleSha1,
                        is_nested_standard = isNestedStandard,
                        is_nested_array = isNestedArray,
                        nested_fields = nestedFields,
                        possible_array = possibleArray,
                        possible_array_item_size = (uint)possibleArrayItemSize
                    };

                    // Console.Out.WriteLine($"{writtenField.FieldChecksum:X} : {GetByteString(output[i].sha1)} : {GetByteString(output[i].demangle_sha1)} ({writtenField.FieldSize} bytes)");
                }

                if (position > -1) {
                    reader.BaseStream.Position = position;
                }
                // }
                if (isNestedArray || isNestedStandard) {
                    if (reader.BaseStream.Length >= afterNestedPosition) {
                        reader.BaseStream.Position = afterNestedPosition;
                    }
                }
                i++;
            }
            return output;
        }
    }
}