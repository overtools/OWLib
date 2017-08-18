using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using OWLib;
using static STULib.Types.Generic.Common;
using static STULib.Types.Generic.Version2;

// ReSharper disable MemberCanBeMadeStatic.Local
namespace STULib.Impl {
    public class Version2 : ISTU {
        public override IEnumerable<STUInstance> Instances => instances.Select((pair => pair.Value));
        public override uint Version => 2;

        private Stream stream;
        private MemoryStream metadata;

        private Dictionary<long, STUInstance> instances;

        private STUHeader header;
        private STUInstanceRecord[] instanceInfo;
        private STUInstanceFieldList[] instanceFieldLists;
        private STUInstanceField[][] instanceFields;

        private uint buildVersion;

        private BinaryReader metadataReader;

        public override void Dispose() {
            stream?.Dispose();
            metadataReader?.Dispose();
        }

        internal static bool IsValidVersion(BinaryReader reader) {
            return reader.BaseStream.Length >= 36;
        }

        public Version2(Stream stuStream, uint owVersion) {
            stream = stuStream;
            buildVersion = owVersion;
            instances = new Dictionary<long, STUInstance>();
            ReadInstanceData(stuStream.Position);
        }

        private object GetValueArray(Type type, STUFieldAttribute element, uint count) {
            Array array = Array.CreateInstance(type, count);
            for (uint i = 0; i < count; ++i) {
                if (element != null) {
                    metadata.Position += element.Padding;
                }
                array.SetValue(GetValueArrayInner(type, element), i);
            }
            return array;
        }

        private object GetValueArrayInner(Type type, STUFieldAttribute element) {
            BinaryReader reader = metadataReader;
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
                        return GetValueArrayInner(type.GetEnumUnderlyingType(), element);
                    }
                    if (type.IsClass || type.IsValueType) {
                        return InitializeObject(Activator.CreateInstance(type), type, reader);
                    }
                    return null;
            }
        }

        // ReSharper disable once UnusedParameter.Local
        private object GetValue(Type type, BinaryReader reader, STUFieldAttribute element, int nestedOffset) {
            if (type.IsArray) {
                return InitializeObjectArray(type.GetElementType(), reader, element);
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
                        return GetValue(type.GetEnumUnderlyingType(), reader, element, nestedOffset);
                    }
                    if (type.IsClass) {
                        throw new Exception();
                    }
                    if (type.IsValueType) {
                        return InitializeObject(Activator.CreateInstance(type), type, CreateInstanceFields(type),
                            reader, nestedOffset);
                    }
                    return null;
            }
        }

        private STUInstanceField[] CreateInstanceFields(Type instanceType) {
            return instanceType.GetCustomAttributes<STUOverride>()
                .Select(@override => new STUInstanceField {
                    FieldChecksum = @override.Checksum,
                    FieldSize = @override.Size
                }).ToArray();
        }

        private void SetField(object instance, uint checksum, object value) {
            if (instance == null) {
                return;
            }

            Dictionary<uint, FieldInfo> fieldMap = CreateFieldMap(GetValidFields(instance.GetType()));

            if (fieldMap.ContainsKey(checksum)) {
                fieldMap[checksum].SetValue(instance, value);
            }
        }

        private static Dictionary<uint, FieldInfo> CreateFieldMap(IEnumerable<FieldInfo> info) {
            return info.ToDictionary(fieldInfo => fieldInfo.GetCustomAttribute<STUFieldAttribute>().Checksum);
        }

        private static IEnumerable<FieldInfo> GetValidFields(Type instanceType) {
            return instanceType.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance |
                                          BindingFlags.FlattenHierarchy)
                .Where(fieldInfo => fieldInfo.GetCustomAttribute<STUFieldAttribute>()?.Checksum > 0);
        }

        private object InitializeObjectArray(Type type, BinaryReader reader, STUFieldAttribute element) {
            int offset = reader.ReadInt32();
            metadata.Position = offset;
            STUArray arrayInfo = metadataReader.Read<STUArray>();
            metadata.Position = arrayInfo.Offset;
            return GetValueArray(type, element, arrayInfo.Count);
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
                    field.SetValue(instance, InitializeObjectArray(field.FieldType, reader, element));
                }
                else {
                    long position = -1;
                    if (element?.ReferenceValue == true) {
                        long offset = reader.ReadInt64();
                        if (offset == 0) {
                            continue;
                        }
                        position = reader.BaseStream.Position;
                        reader.BaseStream.Position = offset;
                    }
                    field.SetValue(instance, GetValue(field.FieldType, reader, element, 0));
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

        private object InitializeObject(object instance, Type type, STUInstanceField[] writtenFields,
            BinaryReader reader, int nestedOffset) {
            Dictionary<uint, FieldInfo> fieldMap = CreateFieldMap(GetValidFields(type));

            foreach (STUInstanceField writtenField in writtenFields) {
                if (!fieldMap.ContainsKey(writtenField.FieldChecksum)) {
                    if (writtenField.FieldSize == 0) {
                        STUNestedInfo nested = reader.Read<STUNestedInfo>();
                        int newNestedOffset = (int) reader.BaseStream.Position;
                        reader.BaseStream.Position = nestedOffset + nested.Offset;
                        InitializeObject(new STUInstance(), typeof(STUInstance), instanceFields[nested.FieldListIndex],
                            reader, newNestedOffset);
                        nestedOffset = (int) reader.BaseStream.Position;
                    }
                    reader.BaseStream.Position += writtenField.FieldSize;
                    continue;
                }
                FieldInfo field = fieldMap[writtenField.FieldChecksum];
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
                    field.SetValue(instance, InitializeObjectArray(field.FieldType.GetElementType(), reader, element));
                }
                else {
                    if (element != null) {
                        reader.BaseStream.Position += element.Padding;
                    }

                    if (writtenField.FieldSize == 0) {
                        if (type.IsClass || type.IsValueType) {
                            STUNestedInfo nested = reader.Read<STUNestedInfo>();
                            int newNestedOffset = (int) reader.BaseStream.Position;
                            object nestedInstance = Activator.CreateInstance(field.FieldType);
                            reader.BaseStream.Position = nestedOffset + nested.Offset;
                            field.SetValue(instance,
                                InitializeObject(nestedInstance, field.FieldType, instanceFields[nested.FieldListIndex],
                                    reader, newNestedOffset));
                            nestedOffset = (int) reader.BaseStream.Position;
                            continue;
                        }
                    }

                    long position = -1;
                    if (element?.ReferenceValue == true) {
                        long offset = reader.ReadInt64();
                        if (offset == 0) {
                            continue;
                        }
                        position = reader.BaseStream.Position;
                        reader.BaseStream.Position = offset;
                    }
                    field.SetValue(instance, GetValue(field.FieldType, reader, element, nestedOffset));
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

        private STUInstance ReadInstance(Type instanceType, STUInstanceField[] writtenFields, BinaryReader reader,
            int size) {
            MemoryStream sandbox = new MemoryStream(size);
            sandbox.Write(reader.ReadBytes(size), 0, size);
            sandbox.Position = 0;
            using (BinaryReader sandboxedReader = new BinaryReader(sandbox, Encoding.UTF8, false)) {
                object instance = Activator.CreateInstance(instanceType);
                return InitializeObject(instance, instanceType, writtenFields, sandboxedReader, 0) as STUInstance;
            }
        }

        // ReSharper disable once PossibleNullReferenceException
        private void ReadInstanceData(long offset) {
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
                    for (int i = 0; i < instanceInfo.Length; ++i) {
                        stream.Position = stuOffset;
                        stuOffset += instanceInfo[i].InstanceSize;

                        instances[i] = null;

                        if (instanceTypes.ContainsKey(instanceInfo[i].InstanceChecksum)) {
                            int fieldListIndex = reader.ReadInt32();
                            instances[i] = ReadInstance(instanceTypes[instanceInfo[i].InstanceChecksum],
                                instanceFields[fieldListIndex], reader, (int) instanceInfo[i].InstanceSize - 4);
                        }
                    }

                    for (int i = 0; i < instanceInfo.Length; ++i) {
                        if (instanceInfo[i].AssignInstanceIndex > -1 &&
                            instanceInfo[i].AssignInstanceIndex < instances.Count) {
                            SetField(instances[instanceInfo[i].AssignInstanceIndex],
                                instanceInfo[i].AssignFieldChecksum, instances[i]);
                        }
                    }
                }
            }
        }

        private bool ReadHeaderData(BinaryReader reader) {
            header = reader.Read<STUHeader>();
            if (header.InstanceCount == 0 || header.InstanceListOffset == 0) {
                return false;
            }

            instanceInfo = new STUInstanceRecord[header.InstanceCount];
            stream.Position = header.InstanceListOffset;
            for (uint i = 0; i < instanceInfo.Length; ++i) {
                instanceInfo[i] = reader.Read<STUInstanceRecord>();
            }

            instanceFieldLists = new STUInstanceFieldList[header.InstanceFieldListCount];
            stream.Position = header.InstanceFieldListOffset;
            for (uint i = 0; i < instanceFieldLists.Length; ++i) {
                instanceFieldLists[i] = reader.Read<STUInstanceFieldList>();
            }

            instanceFields = new STUInstanceField[header.InstanceFieldListCount][];
            for (uint i = 0; i < instanceFieldLists.Length; ++i) {
                instanceFields[i] = new STUInstanceField[instanceFieldLists[i].FieldCount];
                stream.Position = instanceFieldLists[i].ListOffset;
                for (uint j = 0; j < instanceFields[i].Length; ++j) {
                    instanceFields[i][j] = reader.Read<STUInstanceField>();
                }
            }

            metadata = new MemoryStream((int) header.MetadataSize);
            metadata.Write(reader.ReadBytes((int) header.MetadataSize), 0, (int) header.MetadataSize);
            metadata.Position = 0;
            metadataReader = new BinaryReader(metadata, Encoding.UTF8, false);

            return true;
        }
    }
}
