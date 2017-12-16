using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using OWLib;
using static STULib.Types.Generic.Common;
using static STULib.Types.Generic.Version1;

namespace STULib.Impl {
    public class Version1 : ISTU {
        public const uint Magic = 0x53545544;

        protected Stream Stream;
        private STUHeader _data;
        private readonly Dictionary<long, STUInstance> _instances;
        private readonly uint _buildVersion;
        public List<STUInstanceRecord> Records { get; private set; }
        public long Start;

        public override IEnumerable<STUInstance> Instances => _instances.Select(pair => pair.Value);
        public override uint Version => 1;

        public override void Dispose() {
            Stream?.Dispose();
        }

        public static bool IsValidVersion(BinaryReader reader) {
            return reader.ReadUInt32() == Magic;
        }

        public Version1(Stream stuStream, uint owVersion) {
            Stream = stuStream;
            _buildVersion = owVersion;
            _instances = new Dictionary<long, STUInstance>();
            ReadInstanceData(stuStream.Position);
        }

        internal object GetValueArray(Type type, BinaryReader reader, STUFieldAttribute element, FieldInfo fieldInfo) {
            long offset;
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
            if (size == -1) return null;

            Array array = Array.CreateInstance(type, size);
            reader.BaseStream.Position = offset+Start;
            for (int i = 0; i < size; ++i) {
                array.SetValue(GetValue(null, type, reader, element, fieldInfo, true, typeof(STUInstance).IsAssignableFrom(type)), i);
                if (typeof(STUInstance).IsAssignableFrom(type)) reader.ReadUInt32();
                // array.SetValue(GetValue(null, type, reader, element, fieldInfo, true), i);
            }

            return array;
        }

        internal object GetValue(object fieldOwner, Type type, BinaryReader reader, STUFieldAttribute element, FieldInfo fieldInfo, bool isArray=false, bool isInlineInstance=false) {
            if (type.IsArray) {
                long offset = reader.ReadInt64();
                if (offset == 0) {
                    return Array.CreateInstance(type.GetElementType(), 0);
                }
                long position = reader.BaseStream.Position;
                reader.BaseStream.Position = offset+Start;
                if (element.EmbeddedInstance) {
                    throw new NotImplementedException();
                }
                object value = GetValueArray(type.GetElementType(), reader, element, fieldInfo);
                reader.BaseStream.Position = position + 8;
                return value;
            }
            switch (type.Name) {
                case "String": {
                    long offset = reader.ReadInt64();
                    if (offset == 0) return string.Empty;
                    long position = reader.BaseStream.Position;
                    reader.BaseStream.Position = offset+Start;
                    STUString stringData = reader.Read<STUString>();
                    if (stringData.Size == 0) {
                        return string.Empty;
                    }
                    reader.BaseStream.Position = stringData.Offset+Start;
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
                    if (type.GetInterfaces().Contains(typeof(ISTUCustomSerializable))) {
                        if (fieldInfo.FieldType.IsArray) {
                            return ((ISTUCustomSerializable) Activator.CreateInstance(type)).DeserializeArray(fieldOwner, this, fieldInfo,
                                reader, null);
                        }
                        return ((ISTUCustomSerializable) Activator.CreateInstance(type)).Deserialize(fieldOwner, this, fieldInfo,
                            reader, null);
                    }
                    if (type.IsEnum) {
                        return Enum.ToObject(type, GetValue(fieldOwner, type.GetEnumUnderlyingType(), reader, element, fieldInfo, isArray));
                    }
                    if (type.IsClass || type.IsValueType) {
                        if (!element.EmbeddedInstance)
                            return InitializeObject(Activator.CreateInstance(type), type, reader, isArray, isInlineInstance && typeof(STUInstance).IsAssignableFrom(type));
                        int index = reader.ReadInt32();
                        EmbedRequests.Add(new KeyValuePair<object, FieldInfo>(fieldOwner, fieldInfo), index);
                        return null;
                    }
                    return null;
            }
        }

        private object InitializeObject(object instance, Type type, BinaryReader reader, bool isArray, bool isInlineInstance) {
            FieldInfo[] fields = GetFields(type);
            foreach (FieldInfo field in fields) {
                STUFieldAttribute element = field.GetCustomAttribute<STUFieldAttribute>();
                bool skip = false;
                if (element?.STUVersionOnly != null) {
                    skip = element.STUVersionOnly.All(version => version != Version);
                }
                if (element != null) {
                    if (isArray && element.OnlyBuffer) {
                        skip = true;
                    }
                }
                if (isInlineInstance && (field.Name == nameof(STUInstance.InstanceChecksum) ||
                                         field.Name == nameof(STUInstance.NextInstanceOffset))) skip = true;
                if (skip) {
                    continue;
                }
                if (!CheckCompatVersion(field, _buildVersion)) {
                    continue;
                }
                if (field.FieldType.IsArray) {
                    long offset = reader.ReadInt64();
                    if (offset == 0 || offset <= -1) {
                        field.SetValue(instance, Array.CreateInstance(field.FieldType.GetElementType(), 0));
                        continue;
                    }
                    long position = reader.BaseStream.Position;
                    reader.BaseStream.Position = offset+Start;
                    if (element?.EmbeddedInstance == true) {
                        long[] offsets = (long[])GetValueArray(typeof(long), reader, element, field);
                        Array array = Array.CreateInstance(field.FieldType.GetElementType(), offsets.Length);
                        EmbedArrayRequests.Add(array, offsets.Select(x => (int) x).ToArray());
                        field.SetValue(instance, array);
                    } else {
                        field.SetValue(instance, GetValueArray(field.FieldType.GetElementType(), reader, element, field));
                    }
                    reader.BaseStream.Position = position + 8;
                } else {
                    long position = -1;
                    if (element?.ReferenceValue == true) {
                        long offset = reader.ReadInt64();
                        if (offset == 0) {
                            continue;
                        }
                        position = reader.BaseStream.Position;
                        reader.BaseStream.Position = offset+Start;
                    }
                    field.SetValue(instance, GetValue(instance, field.FieldType, reader, element, field, false, true));
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
            Records = new List<STUInstanceRecord>();
            Stream.Position = offset;
            TypeHashes = new HashSet<uint>();
            EmbedRequests = new Dictionary<KeyValuePair<object, FieldInfo>, int>();
            EmbedArrayRequests = new Dictionary<Array, int[]>();
            Start = offset;
            using (BinaryReader reader = new BinaryReader(Stream, Encoding.UTF8, true)) {
                if (_InstanceTypes == null) {
                    LoadInstanceTypes();
                }
                STUHeader instance = new STUHeader();
                _data = (STUHeader)InitializeObject(instance, typeof(STUHeader), reader, false, false);
                
                STUInstanceRecord[] instanceTable = new STUInstanceRecord[_data.InstanceCount];
                for (int i = 0; i < _data.InstanceCount; i++) {
                    instanceTable[i] = reader.Read<STUInstanceRecord>();
                    Records.Add(instanceTable[i]);
                }
                
                foreach (STUInstanceRecord record in instanceTable) {
                    ReadInstance(record.Offset, reader);
                }

                foreach (KeyValuePair<KeyValuePair<object,FieldInfo>,int> embedRequest in EmbedRequests) {
                    if (!_instances.ContainsKey(embedRequest.Value)) continue;
                    embedRequest.Key.Value.SetValue(embedRequest.Key.Key, _instances[embedRequest.Value]);
                    if (_instances[embedRequest.Value] == null) continue;
                    _instances[embedRequest.Value].Usage = InstanceUsage.Embed;
                }
                foreach (KeyValuePair<Array,int[]> request in EmbedArrayRequests) {
                    int arrayIndex = 0;
                    foreach (int i in request.Value) {
                        if (_instances.ContainsKey(i)) {
                            request.Key.SetValue(_instances[i], arrayIndex);
                            if (_instances[i] == null) continue;
                            _instances[i].Usage = InstanceUsage.EmbedArray;
                        }
                        arrayIndex++;
                    }
                }
            }
        }

        // ReSharper disable once UnusedMethodReturnValue.Local
        private STUInstance ReadInstance(long offset, BinaryReader reader) {
            if (_instances.ContainsKey(offset)) {
                return _instances[offset];
            }

            _instances[offset] = null;

            long position = reader.BaseStream.Position;
            reader.BaseStream.Position = offset + Start;
            uint checksum = reader.ReadUInt32();
            reader.BaseStream.Position = offset + Start;

            if (_InstanceTypes.ContainsKey(checksum)) {
                Type type = _InstanceTypes[checksum];
                object instance = Activator.CreateInstance(type);
                _instances[offset] = InitializeObject(instance, type, reader, false, false) as STUInstance;
            } else {
                // STUInstance instance = new STUInstance();
                // instances[offset] = InitializeObject(instance, typeof(STUInstance), reader) as STUInstance;
                Debugger.Log(0, "STU", $"[Version1]: Unhandled instance type: {checksum:X} (offset={offset})\n");
            }
            TypeHashes.Add(checksum);

            reader.BaseStream.Position = position;

            return _instances[offset];
        }
    }
}
