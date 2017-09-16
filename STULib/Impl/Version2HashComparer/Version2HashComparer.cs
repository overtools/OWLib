using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using OWLib;
using STULib.Types.Generic;
using static STULib.Types.Generic.Version2;

namespace STULib.Impl.Version2HashComparer {
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class STUInstanceJSON {
        public uint Hash;
        public string Name;
        public string Parent;
        public STUFieldJSON[] Fields;
        internal string DebuggerDisplay => $"{Name}{(Parent == null ? "" : $" (Parent: {Parent})")}";

        public uint ParentChecksum => Parent != null ? uint.Parse(Parent.Split('_')[1], NumberStyles.HexNumber) : 0;

        [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
        public class STUFieldJSON {
            public uint Hash;
            public string Name;
            public string Type;
            public int SerializationType;
            public int Size = -1;
            internal string DebuggerDisplay => $"{Name} (Type: {Type})";
        }

        public STUFieldJSON[] GetFields(bool parent=false) {
            List<STUFieldJSON> fields = new List<STUFieldJSON>();
            fields.AddRange(Fields);
            if (Parent == null || Version2Comparer.InstanceJSON == null || !parent) return fields.ToArray();
            if (!Version2Comparer.InstanceJSON.ContainsKey(ParentChecksum)) {
                Debugger.Log(0, "STULib",
                    $"[Version2HashComparer]: Instance {Hash:X} parent {ParentChecksum:X} does not exist in the dataset\n");
            } else {
                fields.AddRange(Version2Comparer.InstanceJSON[ParentChecksum].GetFields());
            }
            return fields.ToArray();
        }

        public STUFieldJSON GetField(uint hash) {
            IEnumerable<STUFieldJSON> found = GetFields(true).Where(x => x.Hash == hash);
            
            IEnumerable<STUFieldJSON> stuFieldJsons = found as STUFieldJSON[] ?? found.ToArray();
            return !stuFieldJsons.Any() ? null : stuFieldJsons.First();
        }
    }
    
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class InstanceGuessData {
        public FieldGuessData[] Fields;
        public uint Checksum;
        public uint Size;

        public uint ParentChecksum;

        public bool IsChained;
        public ChainedInstanceInfo[] ChainInfo;

        internal string DebuggerDisplay => $"{Checksum:X} ({Size} bytes){(IsChained ? " Chained" : "")}";
    }

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class ArrayFieldDataHash {
        public FieldSHA1 SHA1;
        public FieldSHA1 DemangleSHA1;
        public uint ItemSize;

        internal string DebuggerDisplay => $"{ItemSize}: {SHA1}{(DemangleSHA1 == null ? "" : $" {DemangleSHA1}")}";
    }
    
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class FieldGuessData {
        public bool IsArray;
        public bool IsInlineStandard;
        public bool IsInlineArray;
        public bool IsChained;
        public bool IsUnknownInline;

        public uint Checksum;
        public uint Size;

        public FieldGuessData[] InlineFields;

        public FieldSHA1 SHA1;
        public FieldSHA1 DemangleSHA1;

        public STUArray ArrayInfo;
        public Dictionary<uint, ArrayFieldDataHash[]> ArraySHA1;

        public uint ChainedInstanceChecksum;

        public bool Is0Byte => Size == 0;
    
        internal string DebuggerDisplay => $"{Checksum:X} ({Size} bytes) {(IsArray ? "Array" : (IsInlineArray ? "InlineArray" : (IsInlineStandard ? "InlineStandard" : (IsChained ? $"(Chained to {ChainedInstanceChecksum:X})" : "Standard"))))}";
    }

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class InstanceData {
        public uint Checksum;
        public string ParentType;
        public uint ParentChecksum;
        
        public FieldData[] Fields;
        public WrittenFieldData[] WrittenFields;

        internal string DebuggerDisplay => $"{Checksum:X}{(ParentChecksum == 0 ? "" : $" (Parent: {ParentChecksum:X})")}";
    }
    
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class FieldSHA1 {
        public FieldSHA1(byte[] data) {
            Data = data;
        }
        
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public byte[] Data;

        public static implicit operator string(FieldSHA1 obj) {
            return obj.DebuggerDisplay;
        }
        
        public static implicit operator FieldSHA1(byte[] obj) {
            return new FieldSHA1(obj);
        }
        
        public static implicit operator byte[](FieldSHA1 obj) {
            return obj.Data;
        }
        
        private string DebuggerDisplay => Version2Comparer.GetByteString(Data);

        public override string ToString() {
            return DebuggerDisplay;
        }
    }
    
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class FieldData {
        public uint Checksum;
        public string Name;
        public int SerializationType;
        public int Size;
        public string Type;
        
        // helpers
        public bool IsPrimitive => SerializationType == 0;
        public bool IsPrimitiveArray => SerializationType == 1;
        
        public bool IsEmbed => SerializationType == 2;
        public bool IsEmbedArray => SerializationType == 3;
        
        public bool IsInline => SerializationType == 4;
        public bool IsInlineArray => SerializationType == 5;
        
        public bool IsHashMap => SerializationType == 7;
        
        public bool IsEnum => SerializationType == 8;
        public bool IsEnumArray => SerializationType == 9;

        public bool IsGUID => SerializationType == 10;
        public bool IsGUIDArray => SerializationType == 11;

        public bool IsGUIDOther => SerializationType == 12;
        public bool IsGUIDOtherArray => SerializationType == 13;

        public uint TypeInstanceChecksum => Type.StartsWith("STU_") ? uint.Parse(Type.Split('_')[1], NumberStyles.HexNumber) : 0;
        public uint InlineInstanceChecksum =>
            IsInline || IsInlineArray ? TypeInstanceChecksum : 0;
        public uint EmbedInstanceChecksum =>
            IsEmbed || IsEmbedArray ? TypeInstanceChecksum : 0;
        public uint EnumChecksum => IsEnum || IsEnumArray ? uint.Parse(Type, NumberStyles.HexNumber) : 0;
        
        public FieldData() { }

        public FieldData(STUInstanceJSON.STUFieldJSON jsonField) {
            Checksum = jsonField.Hash;
            Name = jsonField.Name;
            Type = jsonField.Type;
            SerializationType = jsonField.SerializationType;
            Size = jsonField.Size;
        }
        internal string DebuggerDisplay => $"{Checksum:X} (Type: {Type})";
    }
    
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class WrittenFieldData : FieldData {
        public FieldSHA1 SHA1;
        public FieldSHA1 SHA1Demangle;
        
        public Dictionary<uint, ArrayFieldDataHash[]> ArraySHA1;

        public WrittenFieldData(STUInstanceJSON.STUFieldJSON jsonField) : base(jsonField) { }
    }

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class FieldLocationInfo {
        public long StartPosition;
        public long EndPosition;
        public uint WrittenSize;
        public uint ActualSize;
        public uint Checksum;
        internal string DebuggerDisplay => $"{Checksum:X}: Start:{StartPosition} End:{EndPosition}";
    }
    
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class ChainedInstanceInfo {
        public uint Checksum;
        public uint OwnerChecksum;
        public uint OwnerField;
        internal string DebuggerDisplay => $"{Checksum:X} (Chained to {OwnerChecksum:X}:{OwnerField:X})";
    }

    public class Version2Comparer : Version2 {
        public InstanceGuessData[] InstanceGuessData;
        public InstanceData[] InstanceData;
        public Dictionary<uint, List<ChainedInstanceInfo>> ChainedInstances;
        public Dictionary<uint, InstanceData> InternalInstances = new Dictionary<uint, InstanceData>();
        public static Dictionary<uint, STUInstanceJSON> InstanceJSON;

        public bool IsInData(uint hash, FieldGuessData[] fields) {
            return InstanceGuessData.Any(data => data?.Checksum == hash && data.Fields == fields);
        }

        public Version2Comparer(Stream stuStream, uint owVersion) : base(stuStream, owVersion) { }

        public static string GetByteString(byte[] bytes) {
            return bytes != null ? Convert.ToBase64String(bytes) : "null";
        }

        protected override void ReadInstanceData(long offset) {
            if (_InstanceTypes == null) {
                LoadInstanceTypes();
            }
            stream.Position = offset;
            GetHeaderCRC();
            stream.Position = offset;

            ChainedInstances = new Dictionary<uint, List<ChainedInstanceInfo>>();
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true)) {
                if (!ReadHeaderData(reader)) return;
                long stuOffset = header.Offset;

                for (int chainedI = 0; chainedI < instanceInfo.Length; ++chainedI) {
                    if (instanceInfo[chainedI].AssignInstanceIndex <= -1 ||
                        instanceInfo[chainedI].AssignInstanceIndex >= instanceInfo.Length) continue;
                    if (!ChainedInstances.ContainsKey(instanceInfo[chainedI].InstanceChecksum))
                        ChainedInstances[instanceInfo[chainedI].InstanceChecksum] = new List<ChainedInstanceInfo>();
                    ChainedInstances[instanceInfo[chainedI].InstanceChecksum].Add(new ChainedInstanceInfo {
                        Checksum = instanceInfo[chainedI].InstanceChecksum,
                        OwnerChecksum = instanceInfo[instanceInfo[chainedI].AssignInstanceIndex].InstanceChecksum,
                        OwnerField = instanceInfo[chainedI].AssignFieldChecksum
                    });
                }

                InstanceGuessData = new InstanceGuessData[instanceInfo.Length];
                InstanceData = new InstanceData[instanceInfo.Length];
                for (int i = 0; i < instanceInfo.Length; ++i) {
                    stream.Position = stuOffset;
                    stuOffset += instanceInfo[i].InstanceSize;

                    int fieldListIndex = reader.ReadInt32();

                    if (fieldListIndex < 0) continue;

                    if (InstanceJSON == null) {
                        FieldGuessData[] fields = GuessInstance(instanceFields[fieldListIndex], reader,
                            (int) instanceInfo[i].InstanceSize - 4, instanceInfo[i].InstanceChecksum);

                        InstanceGuessData[i] = new InstanceGuessData {
                            Fields = fields,
                            IsChained = ChainedInstances.ContainsKey(instanceInfo[i].InstanceChecksum),
                            Checksum = instanceInfo[i].InstanceChecksum,
                            Size = instanceInfo[i].InstanceSize,
                            ChainInfo = ChainedInstances.ContainsKey(instanceInfo[i].InstanceChecksum)
                                ? ChainedInstances[instanceInfo[i].InstanceChecksum].ToArray()
                                : null
                        };
                    } else {
                        InstanceData[i] = GetInstanceData(instanceInfo[i].InstanceChecksum, reader,
                            instanceInfo[i].InstanceSize, fieldListIndex);        
                    }
                }
            }
        }

        public InstanceData GetInstanceData(uint instanceChecksum, BinaryReader reader, uint? instanceSize=null, int? fieldListIndex=null) {
            if (!InstanceJSON.ContainsKey(instanceChecksum)) {
                Debugger.Log(0, "STULib",
                    $"[Version2HashComparer]: Instance {instanceChecksum:X} does not exist in the dataset\n");
                return null;
            }
            STUInstanceJSON json = InstanceJSON[instanceChecksum];
            WrittenFieldData[] fields = null;

            if (json.Parent != null && !InternalInstances.ContainsKey(json.ParentChecksum)) {
                InternalInstances[json.ParentChecksum] = GetInstanceData(json.ParentChecksum, reader);
            }
            
            if (instanceSize != null && fieldListIndex != null && reader != null) {
                fields = FakeReadInstance(instanceFields[(int)fieldListIndex], reader,
                    (int) instanceSize - 4, instanceChecksum);
            }
            
            FieldData[] jsonFields = ProcessJSONFields(instanceChecksum);
            return new InstanceData {
                WrittenFields = fields,
                Fields = jsonFields,
                Checksum = instanceChecksum,
                ParentType = json.Parent,
                ParentChecksum = json.ParentChecksum
            };
        }

        protected MemoryStream GetSandbox(int size, BinaryReader reader) {
            MemoryStream sandbox = new MemoryStream(size);
            sandbox.Write(reader.ReadBytes(size), 0, size);
            sandbox.Position = 0;
            return sandbox;
        }

        protected FieldGuessData[] GuessInstance(STUInstanceField[] writtenFields, BinaryReader reader, int size,
            uint instanceChecksum) {
            MemoryStream sandbox = GetSandbox(size, reader);
            using (BinaryReader sandboxedReader = new BinaryReader(sandbox, Encoding.UTF8, false)) {
                return GuessFields(writtenFields, sandboxedReader, instanceChecksum);
            }
        }
        
        protected WrittenFieldData[] FakeReadInstance(STUInstanceField[] writtenFields, BinaryReader reader, int size,
            uint instanceChecksum) {
            MemoryStream sandbox = GetSandbox(size, reader);
            using (BinaryReader sandboxedReader = new BinaryReader(sandbox, Encoding.UTF8, false)) {
                return ReadFields(writtenFields, sandboxedReader, instanceChecksum);
            }
        }

        protected void TryReadArray(FieldGuessData output, STUInstanceField field, BinaryReader reader) {
            long beforePos = reader.BaseStream.Position;
            try {
                int offset = reader.ReadInt32();
                metadata.Position = offset;
                STUArray array = metadataReader.Read<STUArray>();

                if (array.Count < 99999 && array.Count > 0) {
                    output.IsArray = true;
                    output.ArrayInfo = array;
                }
                // uint parent = 0;
                // if (instanceArrayRef?.Length > array.InstanceIndex) {
                //     parent = instanceArrayRef[array.InstanceIndex].Checksum;
                // }
            }
            catch (Exception) {
                output.IsArray = false;
            }
            reader.BaseStream.Position = beforePos;
        }

        protected void TryReadInlineStandard(FieldGuessData output, BinaryReader reader) {
            if (output.IsInlineArray) return;
            long beforePos = reader.BaseStream.Position;
            STUInlineInfo inline = reader.Read<STUInlineInfo>();
            try {
                FieldGuessData[] inlineFields = GuessFields(instanceFields[inline.FieldListIndex], reader);

                output.IsInlineStandard = true;
                output.InlineFields = inlineFields;
            }
            catch (Exception) {
                output.IsInlineStandard = false;
            }

            reader.BaseStream.Position = beforePos;
        }

        protected void TryReadInlineArray(FieldGuessData output, BinaryReader reader) {
            if (output.IsInlineStandard) return;
            if (output.InlineFields?.Length >= 1) {
                return;
            }
            long beforePos = reader.BaseStream.Position;
            STUInlineArrayInfo inline = reader.Read<STUInlineArrayInfo>();

            try {
                Dictionary<uint, FieldGuessData> inlineFields = new Dictionary<uint, FieldGuessData>();
                for (uint i = 0; i < inline.Count; ++i) {
                    uint fieldIndex = reader.ReadUInt32();
                    foreach (FieldGuessData inlineField in GuessFields(instanceFields[fieldIndex], reader))
                        if (!inlineFields.ContainsKey(inlineField.Checksum)) {
                            inlineFields[inlineField.Checksum] = inlineField;
                        } else {
                            if (inlineFields[inlineField.Checksum].Size < inlineField.Size)
                                inlineFields[inlineField.Checksum].Size = inlineField.Size;
                        }
                }
                output.InlineFields = inlineFields.Values.ToArray();
                output.IsInlineArray = true;
            }
            catch (Exception) {
                output.IsInlineArray = false;
            }
            reader.BaseStream.Position = beforePos;
        }

        protected byte[] _ReadArrayItem(FieldGuessData output, uint itemSize) {
            try {
                using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider()) {
                    return sha1.ComputeHash(metadataReader.ReadBytes((int) itemSize));
                    }
                }
            catch (EndOfStreamException) {
                return null;
            }
        }

        protected void TryReadArrayItems(FieldGuessData output) {
            const uint max = 32;

            metadata.Position = output.ArrayInfo.Offset;
            
            output.ArraySHA1 = new Dictionary<uint, ArrayFieldDataHash[]>();

            for (uint i = 1; i <= max; i++) {
                // bool badSize = false;
                output.ArraySHA1[i] = new ArrayFieldDataHash[output.ArrayInfo.Count];
                
                long itemBeforePos = metadata.Position;
                
                for (uint ai = 0; ai < output.ArrayInfo.Count; ++ai) {
                    // long thisItemBeforePos = metadata.Position;
                    output.ArraySHA1[i][ai] = new ArrayFieldDataHash {SHA1 = _ReadArrayItem(output, i), ItemSize = i};
                    
                    // if (output.ArraySHA1[i][ai].SHA1 == null) {
                    //     badSize = true;
                    // }
                    // if (badSize) {
                    //     continue;
                    // }
                    
                    // if (i != 16 || output.ArraySHA1[i][ai].SHA1 == null) continue;
                    // todo: this is broken
                    //metadata.Position = thisItemBeforePos;
                    //ulong padding = metadataReader.ReadUInt64();
                    //ulong key = metadataReader.ReadUInt64();
                    ////Common.STUGUID guid = new Common.STUGUID(key, padding);
                    //Common.STUGUID guid = new Common.STUGUID(key);
                    //DemangleInstance(guid, output.Checksum);
                    //using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider()) {
                    //    output.ArraySHA1[i][ai].DemangleSHA1 = sha1.ComputeHash(BitConverter.GetBytes(guid));
                    //}
                }
                metadata.Position = itemBeforePos;
            }
        }

        protected FieldLocationInfo[] GetFieldSizes(STUInstanceField[] fields, BinaryReader reader) {
            long startPosition = reader.BaseStream.Position;
            List<FieldLocationInfo> output = new List<FieldLocationInfo>();

            foreach (STUInstanceField field in fields)
                if (field.FieldSize != 0) {
                    output.Add(new FieldLocationInfo {
                        ActualSize = field.FieldSize,
                        EndPosition = reader.BaseStream.Position + field.FieldSize,
                        StartPosition = reader.BaseStream.Position,
                        WrittenSize = field.FieldSize,
                        Checksum = field.FieldChecksum
                    });
                    reader.BaseStream.Position += field.FieldSize;
                } else {
                    output.Add(new FieldLocationInfo {
                        StartPosition = reader.BaseStream.Position,
                        WrittenSize = field.FieldSize,
                        Checksum = field.FieldChecksum
                    });
                    uint size = reader.ReadUInt32();
                    reader.BaseStream.Position += size;
                    output.Last().ActualSize = size;
                    output.Last().EndPosition = reader.BaseStream.Position;
                }
            reader.BaseStream.Position = startPosition;
            return output.ToArray();
        }

        protected FieldData[] ProcessJSONFields(uint instanceChecksum) {
            if (!InstanceJSON.ContainsKey(instanceChecksum)) return null;
            FieldData[] output = new FieldData[InstanceJSON[instanceChecksum].GetFields().Length];

            uint fieldIndex = 0;
            foreach (STUInstanceJSON.STUFieldJSON field in InstanceJSON[instanceChecksum].GetFields()) {
                output[fieldIndex] = new FieldData(field);
                if (output[fieldIndex].SerializationType >= 2 && output[fieldIndex].SerializationType <= 5) {
                    if (!InternalInstances.ContainsKey(output[fieldIndex].TypeInstanceChecksum)) {
                        InternalInstances[output[fieldIndex].TypeInstanceChecksum] =
                            GetInstanceData(output[fieldIndex].TypeInstanceChecksum, null);
                    }
                }
                fieldIndex++;
            }
            return output;
        }

        protected WrittenFieldData[] ReadFields(STUInstanceField[] definedFields, BinaryReader reader, uint instanceChecksum) {
            if (InstanceJSON == null) return null;
            if (!InstanceJSON.ContainsKey(instanceChecksum)) {
                Debugger.Log(0, "STULib", $"[Version2HashComparer]: Instance {instanceChecksum:X} does not exist in the dataset\n");
                return new WrittenFieldData[0];
            }
            
            WrittenFieldData[] output = new WrittenFieldData[definedFields.Length];

            for (int i = 0; i < definedFields.Length; i++) {
                STUInstanceField field = definedFields[i];
                STUInstanceJSON.STUFieldJSON jsonField = InstanceJSON[instanceChecksum].GetField(field.FieldChecksum);

                if (jsonField == null) {
                    Debugger.Log(0, "STULib",
                        $"[Version2HashComparer]: Field {instanceChecksum:X}:{field.FieldChecksum:X} does not exist in the dataset\n");
                    continue;
                }

                WrittenFieldData outputField = new WrittenFieldData(jsonField);

                switch (jsonField.SerializationType) {
                    // todo: move over all old sha1 logic
                    case 2:
                    case 3:
                    case 4:
                    case 5:
                        //Console.Out.WriteLine($"{jsonField.SerializationType}: {field.FieldSize} bytes");
                        break;
                    case 0:
                        reader.BaseStream.Position += field.FieldSize;
                        break;
                    case 1: // todo arrray
                        reader.BaseStream.Position += field.FieldSize;
                        break;
                    case 7: // todo hashmap
                        Debugger.Break();
                        break;
                    // case 8: // todo enum
                    // case 9: // todo enum array
                    // case 10: // todo guid
                    // case 11: // todo guid array
                    //     break;
                    
                    // case 0: Primitive
		            // case 1: STUArray<STU{stuField.Type}Primitive>
		            // case 2: STUEmbed<{stuField.Type}>    // ex chained
		            // case 3: STUArray<STUEmbed<{stuField.Type}>>
		            // case 4: STUInline<{stuField.Type}>  // ex nested
		            // case 5: STUArray<STUInline<{stuField.Type}>>
		            // case 7: STUHashMap<{stuField.Type}>
		            // case 8: Enums.{stuField.Type}?
		            // case 9: STUArray<Enums.{stuField.Type}?>
		            // case 10: STUAssetRef<ulong>
		            // case 11: STUArray<STUAssetRef<ulong>>
		            // case 12: STUAssetRef<{stuField.Type}>
		            // case 13: STUArray<STUAssetRef<{stuField.Type}>>
                }
                output[i] = outputField;
            }
            
            return output;
        }

        protected FieldGuessData[] GuessFields(STUInstanceField[] definedFields, BinaryReader reader,
            uint instanceChecksum = 0) {
            FieldGuessData[] output = new FieldGuessData[definedFields.Length];
            // FieldLocationInfo[] fieldSizes = GetFieldSizes(definedFields, reader); // todo: use this for something

            uint outputIndex = 0;
            foreach (STUInstanceField field in definedFields) {
                FieldGuessData fieldOutput = new FieldGuessData {Size = field.FieldSize, Checksum = field.FieldChecksum};

                if (instanceChecksum != 0)
                    foreach (KeyValuePair<uint, List<ChainedInstanceInfo>> pair in ChainedInstances
                    ) // if (pair.Key != instanceChecksum) continue;
                    foreach (ChainedInstanceInfo info in pair.Value) {
                        if (info.OwnerChecksum != instanceChecksum || info.OwnerField != field.FieldChecksum)
                            continue;
                        fieldOutput.IsChained = true;
                        fieldOutput.ChainedInstanceChecksum = info.Checksum;
                        break;
                    }

                if (field.FieldSize == 0) {
                    TryReadInlineStandard(fieldOutput, reader);
                    TryReadInlineArray(fieldOutput, reader);
                    
                    uint size = reader.ReadUInt32();
                    
                    reader.BaseStream.Position += size;
                } else {
                    TryReadArray(fieldOutput, field, reader);
                    if (fieldOutput.IsArray) {
                        TryReadArrayItems(fieldOutput);
                    }

                    long startPosition = reader.BaseStream.Position;

                    using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider()) {
                        byte[] fieldSHA1Demangle = null;
                        if (field.FieldSize == 8) {
                            Common.STUGUID f = new Common.STUGUID(reader.ReadUInt64());
                            DemangleInstance(f, field.FieldChecksum);
                            fieldSHA1Demangle = sha1.ComputeHash(BitConverter.GetBytes(f)); // is this bad?
                            reader.BaseStream.Position = startPosition;
                        }
                        byte[] fieldSHA1 = sha1.ComputeHash(reader.ReadBytes((int) field.FieldSize));

                        fieldOutput.SHA1 = fieldSHA1;
                        fieldOutput.DemangleSHA1 = fieldSHA1Demangle;
                    }
                }

                output[outputIndex] = fieldOutput;
                outputIndex++;
            }
            return output;
        }
    }
}