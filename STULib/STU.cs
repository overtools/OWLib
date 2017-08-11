using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.HashFunction.CRCStandards;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using OWLib;
using static STULib.Types.Generic;

namespace STULib {
    public class STU {
        public const uint MAGIC = 0x53545544;

        private static Dictionary<uint, Type> instanceTypes;
        private static Dictionary<Type, string> instanceNames;

        private static void LoadInstanceTypes() {
            instanceTypes = new Dictionary<uint, Type>();
            instanceNames = new Dictionary<Type, string>();
            
            Assembly asm = typeof(STUInstance).Assembly;
            Type t = typeof(STUInstance);
            List<Type> types = asm.GetTypes().Where(type => type != t && t.IsAssignableFrom(type)).ToList();
            foreach (Type type in types) {
                STUAttribute attrib = type.GetCustomAttribute<STUAttribute>();
                if (attrib == null) {
                    continue;
                }
                if (attrib.Name == null) {
                    attrib.Name = type.Name;
                }
                if (!attrib.Name.StartsWith("STU") && type.Namespace != null) {
                    attrib.Name = type.Namespace.Split('.').Last() + attrib.Name;
                }
                if (attrib.Checksum == 0) {
                    attrib.Checksum =
                        BitConverter.ToUInt32(new CRC32().ComputeHash(Encoding.ASCII.GetBytes(attrib.Name)), 0);
                }
                instanceNames[type] = attrib.Name;
                instanceTypes[attrib.Checksum] = type;
            }
        }

        private Stream stream;
        private STUHeader data;
        private Dictionary<long, STUInstance> instances;

        public IEnumerator<STUInstance> Instances => instances.Values.GetEnumerator();

        public STU(Stream stuStream) {
            stream = stuStream;
            instances = new Dictionary<long, STUInstance>();
            ReadInstanceData(stuStream.Position);
        }

        private object GetValueArray(Type type, BinaryReader reader, STUElementAttribute element) {
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

        private object GetValue(Type type, BinaryReader reader, STUElementAttribute element) {
            if (type.IsArray) {
                long offset = reader.ReadInt64();
                long position = reader.BaseStream.Position;
                reader.BaseStream.Position = offset;
                object value = GetValueArray(type.GetElementType(), reader, element);
                reader.BaseStream.Position = position;
                return value;
            }
            switch (type.Name) {
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

        private FieldInfo[] GetFields(Type type) {
            FieldInfo[] parent = new FieldInfo[0];
            if (type.BaseType != null) {
                parent = GetFields(type.BaseType);
            }
            return parent.Concat(type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)).ToArray();
        }

        private object InitializeObject(object instance, Type type, BinaryReader reader) {
            FieldInfo[] fields = GetFields(type);
            foreach (FieldInfo field in fields) {
                STUElementAttribute element = field.GetCustomAttribute<STUElementAttribute>();
                if (field.FieldType.IsArray) {
                    long offset = reader.ReadInt64();
                    if (offset == 0) {
                        field.SetValue(instance, Array.CreateInstance(field.FieldType.GetElementType(), 0));
                        continue;
                    }
                    long position = reader.BaseStream.Position;
                    reader.BaseStream.Position = offset;
                    field.SetValue(instance, GetValueArray(field.FieldType.GetElementType(), reader, element));
                    reader.BaseStream.Position = position;
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
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, false)) {
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

        public static string GetName<T>() {
            return GetName(typeof(T));
        }

        public static string GetName(Type t) {
            if (instanceNames.ContainsKey(t)) {
                return instanceNames[t];
            }
            return null;
        }
    }
}
