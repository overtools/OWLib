using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OWLib;
using STULib.Types.Generic;

namespace STULib.Impl.Version2HashComparer {
    public class InstanceData {
        public FieldData[] Fields;
        public uint Hash;
        public uint Size;
        public ChainedInstanceData[] ChainedInstances;
    }

    public class FieldData {
        public byte[] Sha1;
        public byte[] DemangleSha1;
        public uint Hash;
        public uint Size;
        public bool IsNestedStandard;
        public bool IsNestedArray;
        public FieldData[] NestedFields;
        public bool PossibleArray;
        public uint PossibleArrayItemSize;
    }

    public class ChainedInstanceData {
        public uint ParentField;
        public uint InstanceChecksum;
        public FieldData[] Fields;
    }

    public class FakeNestedInstance { }

    internal class FakeGUID : IDemangleable {
        private ulong Padding = ulong.MaxValue;

        public ulong Key;

        public static implicit operator long(FakeGUID i) {
            return (long)i.Key;
        }

        public static implicit operator ulong(FakeGUID i) {
            return i.Key;
        }

        public new string ToString() {
            return $"{GUID.LongKey(Key):X12}.{GUID.Type(Key):X3}";
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

        // ReSharper disable once InconsistentNaming
        public void SetGUIDs(ulong[] GUIDs) {
            if (GUIDs?.Length > 0) {
                Key = GUIDs[0];
            }
        }
    }

    internal class FakeInstanceType : Common.STUInstance { }

    public class Version2Comparer : Version2 {
        public InstanceData[] InstanceDiffData;

        public Version2Comparer(Stream stuStream, uint owVersion) : base(stuStream, owVersion) {
        }

        public string GetByteString(byte[] bytes) {
            return bytes != null ? Convert.ToBase64String(bytes) : "null";
        }

        protected FieldData[] FakeReadInstance(Type instanceType, Types.Generic.Version2.STUInstanceField[] writtenFields, BinaryReader reader,
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

                if (!ReadHeaderData(reader)) return;
                if (instanceTypes == null) {
                    LoadInstanceTypes();
                }

                long stuOffset = header.Offset;
                InstanceDiffData = new InstanceData[instanceInfo.Length];
                uint[] fakeInstances = new uint[instanceInfo.Length];
                int[] chainedCount = new int[fakeInstances.Length];
                int[] chainCounter = new int[fakeInstances.Length];
                for (int i = 0; i < instanceInfo.Length; ++i) {
                    stream.Position = stuOffset;
                    stuOffset += instanceInfo[i].InstanceSize;

                    int fieldListIndex = reader.ReadInt32();
                    
                    fakeInstances[i] = instanceInfo[i].InstanceChecksum;
                    chainedCount[i] = 0;
                    chainCounter[i] = 0;

                    if (instanceFields.Length <= fieldListIndex || fieldListIndex < 0) {
                        InstanceDiffData[i] = null;
                        continue;
                    }
                    FieldData[] fields = FakeReadInstance(typeof(FakeInstanceType), instanceFields[fieldListIndex], reader,
                        (int)instanceInfo[i].InstanceSize - 4);
                    InstanceDiffData[i] = new InstanceData { Size = instanceInfo[i].InstanceSize, 
                        Hash = instanceInfo[i].InstanceChecksum, Fields = fields };
                }
                
                for (int ci = 0; ci < fakeInstances.Length; ++ci) {
                    if (instanceInfo[ci].AssignInstanceIndex > -1 &&
                        instanceInfo[ci].AssignInstanceIndex < fakeInstances.Length) {
                        chainedCount[instanceInfo[ci].AssignInstanceIndex]++;
                    }
                }

                for (int fi = 0; fi < fakeInstances.Length; ++fi) {
                    if (InstanceDiffData[fi] == null) continue;
                    InstanceDiffData[fi].ChainedInstances = new ChainedInstanceData[chainedCount[fi]];
                }

                for (int i = 0; i < fakeInstances.Length; ++i) {
                    if (instanceInfo[i].AssignInstanceIndex <= -1 ||
                        instanceInfo[i].AssignInstanceIndex >= fakeInstances.Length) continue;
                    int parentIndex = instanceInfo[i].AssignInstanceIndex;
                    if (InstanceDiffData[parentIndex] == null) {
                        // Console.Out.WriteLine("Instance does not exist FeelsBadMan");
                        continue;
                    }
                    InstanceDiffData[parentIndex].ChainedInstances[chainCounter[parentIndex]] =
                        new ChainedInstanceData {
                            InstanceChecksum = fakeInstances[i],
                            ParentField = instanceInfo[i].AssignFieldChecksum,
                            Fields = InstanceDiffData[i] == null ? new FieldData[0] : InstanceDiffData[i].Fields
                        };
                    chainCounter[parentIndex]++;
                }
            }
        }

        protected override object InitializeObject(object instance, Type type, Types.Generic.Version2.STUInstanceField[] writtenFields, BinaryReader reader) {
            FieldData[] output = new FieldData[writtenFields.Length];
            int i = 0;
            foreach (Types.Generic.Version2.STUInstanceField writtenField in writtenFields) {
                STUFieldAttribute element = new STUFieldAttribute { Checksum = writtenField.FieldChecksum };  // hmm
                bool isNestedStandard = false;
                bool isNestedArray = false;
                bool possibleArray = false;
                int possibleArrayItemSize = 0;
                long afterNestedPosition = -1;

                FieldData[] nestedFields = null;

                long beforeArrayPos = reader.BaseStream.Position;

                try {
                    int offset = reader.ReadInt32();
                    metadata.Position = offset;
                    Types.Generic.Version2.STUArray array = metadataReader.Read<Types.Generic.Version2.STUArray>();
                    metadata.Position = array.Offset;

                    if (array.Count > 1000) {  // ewww, this is nasty, if we try and load lots (usually a sign that its not actually an array) the system will choke
                        throw new InvalidDataException("too much data?");
                    }

                    // arrayItemSize = GetArrayItemSize(array, element, metadataReader);
                    // ReSharper disable once ConvertToConstant.Local
                    int arrayItemSize = 1;
                    if (arrayItemSize > 0 && array.Count < 9999 && array.Count > 0) {
                        possibleArray = true;
                        possibleArrayItemSize = arrayItemSize;
                    }
                } catch (Exception) {
                    //Console.Out.WriteLine($"{writtenField.FieldChecksum:X8}: standard array fail: {e}");
                }
                reader.BaseStream.Position = beforeArrayPos;

                if (writtenField.FieldSize == 0) {

                    // todo: nested sha1, every item in array?

                    long nestStartPos = reader.BaseStream.Position;
                    Types.Generic.Version2.STUNestedInfo nested = reader.Read<Types.Generic.Version2.STUNestedInfo>();
                    long nestBeforeReadPos = reader.BaseStream.Position;

                    try {
                        Dictionary<uint, FieldData> nestedFieldCache = new Dictionary<uint, FieldData>();
                        foreach (FieldData nF in InitializeObject(instance, type, instanceFields[nested.FieldListIndex], reader) as FieldData[]) {
                            if (!nestedFieldCache.ContainsKey(nF.Hash)) {
                                nestedFieldCache[nF.Hash] = nF;
                            } else {
                                if (nF.Size > nestedFieldCache[nF.Hash].Size) {
                                    nestedFieldCache[nF.Hash].Size = nF.Size;
                                }
                            }
                        }
                        if (reader.BaseStream.Position == nestStartPos + nested.Size + 4) {
                            isNestedStandard = true;
                            nestedFields = nestedFieldCache.Values.ToArray();
                            afterNestedPosition = nestStartPos + nested.Size + 4;
                        }
                    } catch (Exception) {
                        //Console.Out.WriteLine($"Fail standard nest: {e}");
                    }

                    reader.BaseStream.Position = nestBeforeReadPos;

                    try {
                        Dictionary<uint, FieldData> nestedFieldCache = new Dictionary<uint, FieldData>();
                        for (uint nestI = 0; nestI < nested.FieldListIndex; ++nestI) {
                            stream.Position += element.Padding;
                            uint fieldIndex = reader.ReadUInt32();
                            foreach (FieldData nF in InitializeObject(instance, type, instanceFields[fieldIndex], reader) as FieldData[]) {
                                if (!nestedFieldCache.ContainsKey(nF.Hash)) {
                                    nestedFieldCache[nF.Hash] = nF;
                                } else {
                                    if (nF.Size > nestedFieldCache[nF.Hash].Size) {
                                        nestedFieldCache[nF.Hash].Size = nF.Size;
                                    }
                                }
                            }
                        }
                        if (reader.BaseStream.Position == nestStartPos + nested.Size + 4) {
                            nestedFields = nestedFieldCache.Values.ToArray();
                            isNestedArray = true;
                            afterNestedPosition = nestStartPos + nested.Size + 4;
                        }
                    } catch (Exception) {
                        //Console.Out.WriteLine($"Fail nest array: {writtenField.FieldChecksum:X8}: {e}");
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
                        demangleSha1 = sha1.ComputeHash(BitConverter.GetBytes(f.Key));  // is this bad?

                        reader.BaseStream.Position = startPosition;
                    }
                    output[i] = new FieldData {
                        Sha1 = sha1.ComputeHash(reader.ReadBytes((int)writtenField.FieldSize)),
                        Size = writtenField.FieldSize,
                        Hash = writtenField.FieldChecksum,
                        DemangleSha1 = demangleSha1,
                        IsNestedStandard = isNestedStandard,
                        IsNestedArray = isNestedArray,
                        NestedFields = nestedFields,
                        PossibleArray = possibleArray,
                        PossibleArrayItemSize = (uint)possibleArrayItemSize
                    };
                    // Console.Out.WriteLine($"{writtenField.FieldChecksum:X} : {GetByteString(output[i].sha1)} : {GetByteString(output[i].demangle_sha1)} ({writtenField.FieldSize} bytes)");
                }

                if (position > -1) {
                    reader.BaseStream.Position = position;
                }
                if (afterNestedPosition != -1) {
                    reader.BaseStream.Position = afterNestedPosition;
                }
                i++;
            }
            return output;
        }
    }
}