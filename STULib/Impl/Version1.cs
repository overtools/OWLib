using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using OWLib;
using static STULib.Types.Generic.Common;
using static STULib.Types.Generic.Version1;

namespace STULib.Impl {
    public class Version1 : ISTU {
        public const uint MAGIC = 0x53545544;

        private Stream stream;
        private STUHeader data;
        private Dictionary<long, STUInstance> instances;
        private uint buildVersion;
        
        public override IEnumerable<STUInstance> Instances => instances.Select((pair => pair.Value));
        public override uint Version => 1;

        public override void Dispose() {
            stream?.Dispose();
        }

        internal static bool IsValidVersion(BinaryReader reader) {
            return reader.ReadUInt32() == MAGIC;
        }

        public Version1(Stream stuStream, uint owVersion) {
            stream = stuStream;
            buildVersion = owVersion;
            instances = new Dictionary<long, STUInstance>();
            ReadInstanceData(stuStream.Position);
        }

        private object GetValueArray(Type type, BinaryReader reader, STUFieldAttribute element) {
            long offset = 0;
            int size = 0;

            if (element?.ReferenceArray == true) {
                STUReferenceArray referenceInfo = reader.Read<STUReferenceArray>();
                offset = referenceInfo.DataOffset;
                reader.BaseStream.Position = referenceInfo.SizeOffset;
                for (long i = 0; i < referenceInfo.EntryCount; ++i) {
                    size = Math.Max(size, reader.ReadInt32());
                }
            } else {
                STUArray arrayInfo = reader.Read<STUArray>();
                offset = arrayInfo.Offset;
                size = (int)arrayInfo.EntryCount;
            }

            Array array = Array.CreateInstance(type, size);
            reader.BaseStream.Position = offset;
            for (int i = 0; i < size; ++i) {
                array.SetValue(GetValue(type, reader, element), i);
            }

            return array;
        }

        private object GetValue(Type type, BinaryReader reader, STUFieldAttribute element) {
            if (type.IsArray) {
                long offset = reader.ReadInt64();
                if (offset == 0) {
                    return Array.CreateInstance(type.GetElementType(), 0);
                }
                long position = reader.BaseStream.Position;
                reader.BaseStream.Position = offset;
                object value = GetValueArray(type.GetElementType(), reader, element);
                reader.BaseStream.Position = position + 8;
                return value;
            }
            switch (type.Name) {
                case "String": {
                    long offset = reader.ReadInt64();
                    long position = reader.BaseStream.Position;
                    reader.BaseStream.Position = offset;
                    STUString stringData = reader.Read<STUString>();
                    if (stringData.Size == 0) {
                        return string.Empty;
                    }
                    reader.BaseStream.Position = stringData.Offset;
                    string @string = new string(reader.ReadChars((int) stringData.Size));
                    reader.BaseStream.Position = position;
                    return @string;
                }
                case "Single":
                    return reader.ReadSingle();
                case "Boolean":
                    return reader.ReadByte() != 0;
                case "Int16":
                    return reader.ReadInt16();
                case "UInt16":
                    return reader.ReadUInt16();
                case "Int32":
                    return reader.ReadInt32();
                case "UInt32":
                    return reader.ReadUInt32();
                case "Int64":
                    return reader.ReadInt64();
                case "UInt64":
                    return reader.ReadUInt64();
                case "Byte":
                    return reader.ReadByte();
                case "SByte":
                    return reader.ReadSByte();
                case "Char":
                    return reader.ReadChar();
                default:
                    if (type.IsEnum) {
                        return GetValue(type.GetEnumUnderlyingType(), reader, element);
                    }
                    if (type.IsClass || type.IsValueType) {
                        return InitializeObject(Activator.CreateInstance(type), type, reader);
                    }
                    return null;
            }
        }

        private object InitializeObject(object instance, Type type, BinaryReader reader) {
            FieldInfo[] fields = GetFields(type);
            foreach (FieldInfo field in fields) {
                STUFieldAttribute element = field.GetCustomAttribute<STUFieldAttribute>();
                bool skip = false;
                if (element?.STUVersionOnly != null) {
                    skip = element.STUVersionOnly.All(version => version != Version);
                }
                if (skip) {
                    continue;
                }
                if (!CheckCompatVersion(field, buildVersion)) {
                    continue;
                }
                if (field.FieldType.IsArray) {
                    long offset = reader.ReadInt64();
                    if (offset == 0) {
                        field.SetValue(instance, Array.CreateInstance(field.FieldType.GetElementType(), 0));
                        continue;
                    }
                    long position = reader.BaseStream.Position;
                    reader.BaseStream.Position = offset;
                    field.SetValue(instance, GetValueArray(field.FieldType.GetElementType(), reader, element));
                    reader.BaseStream.Position = position + 8;
                } else {
                    long position = -1;
                    if (element?.ReferenceValue == true) {
                        long offset = reader.ReadInt64();
                        if (offset == 0) {
                            continue;
                        }
                        position = reader.BaseStream.Position;
                        reader.BaseStream.Position = offset;
                    }
                    field.SetValue(instance, GetValue(field.FieldType, reader, element));
                    if (position > -1) {
                        reader.BaseStream.Position = position;
                    }
                }
                if (element?.Verify != null) {
                    if (!field.GetValue(instance).Equals(element.Verify)) {
                        throw new Exception("Verification failed");
                    }
                }
            }

            return instance;
        }

        private void ReadInstanceData(long offset) {
            stream.Position = offset;
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true)) {
                if (instanceTypes == null) {
                    LoadInstanceTypes();
                }
                STUHeader instance = new STUHeader();
                data = InitializeObject(instance, typeof(STUHeader), reader) as STUHeader;

                if (data?.InstanceTable != null) {
                    foreach (STUInstanceRecord record in data?.InstanceTable) {
                        ReadInstance(record.Offset, reader);
                    }
                }
            }
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private STUInstance ReadInstance(long offset, BinaryReader reader) {
            if (instances.ContainsKey(offset)) {
                return instances[offset];
            }

            instances[offset] = null;

            long position = reader.BaseStream.Position;
            reader.BaseStream.Position = offset;
            uint Id = reader.ReadUInt32();
            reader.BaseStream.Position = offset;

            if (instanceTypes.ContainsKey(Id)) {
                Type type = instanceTypes[Id];
                object instance = Activator.CreateInstance(type);
                instances[offset] = InitializeObject(instance, type, reader) as STUInstance;
            }

            reader.BaseStream.Position = position;

            return instances[offset];
        }
    }
}
