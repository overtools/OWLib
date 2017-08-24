using OWLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
    }

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

        protected override object InitializeObject(object instance, Type type, STUInstanceField[] writtenFields, BinaryReader reader) {
            Dictionary<uint, FieldInfo> fieldMap = CreateFieldMap(GetValidFields(type));
            FieldData[] output = new FieldData[writtenFields.Length];
            int i = 0;
            foreach (STUInstanceField writtenField in writtenFields) {
                // Console.Out.WriteLine("{0:X}", writtenField.FieldChecksum);
                STUFieldAttribute element = new STUFieldAttribute { Checksum = writtenField.FieldChecksum };  // hmm

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
                    output[i] = new FieldData { sha1 = sha1.ComputeHash(reader.ReadBytes((int)writtenField.FieldSize)), size = writtenField.FieldSize, hash = writtenField.FieldChecksum, demangle_sha1 = demangleSha1 };

                    // Console.Out.WriteLine($"{writtenField.FieldChecksum:X} : {GetByteString(output[i].sha1)} : {GetByteString(output[i].demangle_sha1)} ({writtenField.FieldSize} bytes)");
                }

                if (position > -1) {
                    reader.BaseStream.Position = position;
                }
                i++;
            }
            return output;
        }
    }
}
