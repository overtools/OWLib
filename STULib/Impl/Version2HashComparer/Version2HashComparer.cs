using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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
                fields.AddRange(Version2Comparer.InstanceJSON[ParentChecksum].GetFields(true));
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

        public STUArrayInfo ArrayInfo;
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
        public uint HashMapChecksum => IsHashMap ? TypeInstanceChecksum : 0;
        
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
        
        public FieldSHA1[] ArraySHA1;

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

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct FakeArrayGUID {
        public ulong Padding;
        public ulong Key;
    }

    public class Version2Comparer : Version2 {
        public InstanceGuessData[] InstanceGuessData;
        public InstanceData[] InstanceData;
        public Dictionary<uint, List<ChainedInstanceInfo>> ChainedInstances;
        public Dictionary<uint, InstanceData> InternalInstances = new Dictionary<uint, InstanceData>();
        public static Dictionary<uint, STUInstanceJSON> InstanceJSON;
        public static bool GetAllChildren = false;

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
            Stream.Position = offset;
            GetHeaderCRC();
            Stream.Position = offset;

            ChainedInstances = new Dictionary<uint, List<ChainedInstanceInfo>>();
            using (BinaryReader reader = new BinaryReader(Stream, Encoding.UTF8, true)) {
                if (Version1.IsValidVersion(reader)) {
                    Version1Comparer ver1 = new Version1Comparer(Stream, BuildVersion);
                    InstanceData = ver1.InstanceData;
                    InternalInstances = ver1.InternalInstances;
                    return;
                }
                Stream.Position = offset;
                if (!ReadHeaderData(reader)) return;
                long stuOffset = Header.Offset;

                for (int chainedI = 0; chainedI < InstanceInfo.Length; ++chainedI) {
                    if (InstanceInfo[chainedI].AssignInstanceIndex <= -1 ||
                        InstanceInfo[chainedI].AssignInstanceIndex >= InstanceInfo.Length) continue;
                    if (!ChainedInstances.ContainsKey(InstanceInfo[chainedI].InstanceChecksum))
                        ChainedInstances[InstanceInfo[chainedI].InstanceChecksum] = new List<ChainedInstanceInfo>();
                    ChainedInstances[InstanceInfo[chainedI].InstanceChecksum].Add(new ChainedInstanceInfo {
                        Checksum = InstanceInfo[chainedI].InstanceChecksum,
                        OwnerChecksum = InstanceInfo[InstanceInfo[chainedI].AssignInstanceIndex].InstanceChecksum,
                        OwnerField = InstanceInfo[chainedI].AssignFieldChecksum
                    });
                }

                InstanceGuessData = new InstanceGuessData[InstanceInfo.Length];
                InstanceData = new InstanceData[InstanceInfo.Length];
                for (int i = 0; i < InstanceInfo.Length; ++i) {
                    Stream.Position = stuOffset;
                    stuOffset += InstanceInfo[i].InstanceSize;

                    int fieldListIndex = reader.ReadInt32();

                    if (fieldListIndex < 0) {
                        Debugger.Log(0, "STU",
                            $"[Version2HashComparer:{InstanceInfo[i].InstanceChecksum:X}]: Instance field list was not valid ({fieldListIndex})\n");
                        continue;
                    }

                    if (InstanceJSON == null) {
                        FieldGuessData[] fields = GuessInstance(InstanceFields[fieldListIndex], reader,
                            (int) InstanceInfo[i].InstanceSize - 4, InstanceInfo[i].InstanceChecksum);

                        InstanceGuessData[i] = new InstanceGuessData {
                            Fields = fields,
                            IsChained = ChainedInstances.ContainsKey(InstanceInfo[i].InstanceChecksum),
                            Checksum = InstanceInfo[i].InstanceChecksum,
                            Size = InstanceInfo[i].InstanceSize,
                            ChainInfo = ChainedInstances.ContainsKey(InstanceInfo[i].InstanceChecksum)
                                ? ChainedInstances[InstanceInfo[i].InstanceChecksum].ToArray()
                                : null
                        };
                    } else {
                        InstanceData[i] = GetInstanceData(InstanceInfo[i].InstanceChecksum, reader,
                            InstanceInfo[i].InstanceSize, fieldListIndex);        
                    }
                }
            }
        }

        public static InstanceData GetData(uint checksum) {
            if (!InstanceJSON.ContainsKey(checksum)) {
                Debugger.Log(0, "STULib",
                    $"[Version2HashComparer]: Instance {checksum:X} does not exist in the dataset\n");
                return null;
            }
            STUInstanceJSON json = InstanceJSON[checksum];
            
            FieldData[] fields = new FieldData[InstanceJSON[checksum].GetFields().Length];
            uint fieldIndex = 0;
            foreach (STUInstanceJSON.STUFieldJSON field in InstanceJSON[checksum].GetFields()) {
                fields[fieldIndex] = new FieldData(field);
                fieldIndex++;
            }
            return new InstanceData {
                WrittenFields = null,
                Fields = fields,
                Checksum = checksum,
                ParentType = json.Parent,
                ParentChecksum = json.ParentChecksum
            };
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
                bool beforeGetAll = GetAllChildren;
                GetAllChildren = false;  // we do not need parent's children
                InternalInstances[json.ParentChecksum] = GetInstanceData(json.ParentChecksum, reader);
                GetAllChildren = beforeGetAll;
            }
            
            // get all children
            // WARNING: NOT THREAD SAFE
            // if (GetAllChildren) {
            //     foreach (KeyValuePair<uint,STUInstanceJSON> instanceJSON in InstanceJSON.Where(x => x.Value.ParentChecksum != 0 && x.Value.ParentChecksum == instanceChecksum)) {
            //         if (instanceJSON.Value.ParentChecksum != instanceChecksum) continue; // wat
            //         if (InternalInstances.ContainsKey(instanceJSON.Value.Hash)) continue;
            //         InternalInstances[instanceJSON.Value.Hash] = null;
            //         InternalInstances[instanceJSON.Value.Hash] = GetInstanceData(instanceJSON.Value.Hash, reader);
            //     }
            // }
            
            
            if (instanceSize != null && fieldListIndex != null && reader != null) {
                fields = FakeReadInstance(InstanceFields[(int)fieldListIndex], reader,
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
                Metadata.Position = offset;
                STUArrayInfo array = MetadataReader.Read<STUArrayInfo>();

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
                FieldGuessData[] inlineFields = GuessFields(InstanceFields[inline.FieldListIndex], reader);

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
                    foreach (FieldGuessData inlineField in GuessFields(InstanceFields[fieldIndex], reader))
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
                    return sha1.ComputeHash(MetadataReader.ReadBytes((int) itemSize));
                    }
                }
            catch (EndOfStreamException) {
                return null;
            }
        }

        protected void TryReadArrayItems(FieldGuessData output) {
            const uint max = 32;

            Metadata.Position = output.ArrayInfo.Offset;
            
            output.ArraySHA1 = new Dictionary<uint, ArrayFieldDataHash[]>();

            for (uint i = 1; i <= max; i++) {
                // bool badSize = false;
                output.ArraySHA1[i] = new ArrayFieldDataHash[output.ArrayInfo.Count];
                
                long itemBeforePos = Metadata.Position;
                
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
                Metadata.Position = itemBeforePos;
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
                if (output[fieldIndex].TypeInstanceChecksum != 0) {
                    if (!InternalInstances.ContainsKey(output[fieldIndex].TypeInstanceChecksum)) {
                        InternalInstances[output[fieldIndex].TypeInstanceChecksum] = null; // prevent stackoverflow
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
                using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider()) {
                    /*switch (jsonField.SerializationType) {
                        case 2:
                        case 3:
                            reader.BaseStream.Position += field.FieldSize;
                            break;
                        case 4:
                        case 5:
                            reader.BaseStream.Position += reader.ReadUInt32();
                            // outputField.SHA1 = sha1.ComputeHash(BitConverter.GetBytes(jsonField.Size));
                            break;
                        case 8:
                        case 0:
                            outputField.SHA1 = sha1.ComputeHash(reader.ReadBytes((int) field.FieldSize));
                            break;
                        case 9:
                        case 1:
                            int offset = reader.ReadInt32();
                            metadata.Position = offset;
                            STUArrayInfo array = metadataReader.Read<STUArrayInfo>();
                            outputField.ArraySHA1 = new FieldSHA1[array.Count];
                            int arrayItemSize = 0; // todo
                            if (arrayItemSize == 0) {
                                break;
                            }
                            for (int j = 0; j < array.Count; j++) {
                                outputField.ArraySHA1[i] = sha1.ComputeHash(metadataReader.ReadBytes(arrayItemSize));
                            }
                            break;
                        case 7: // todo hashmap
                            Debugger.Break();
                            break;
                        case 10:
                        case 12:
                            Common.STUGUID f = new Common.STUGUID(reader.ReadUInt64());
                            DemangleInstance(f, field.FieldChecksum);
                            outputField.SHA1Demangle = sha1.ComputeHash(BitConverter.GetBytes(f));
                            break;
                        case 11:
                        case 13:
                            int offset2 = reader.ReadInt32();
                            metadata.Position = offset2;
                            STUArrayInfo array2 = metadataReader.Read<STUArrayInfo>();
                            outputField.ArraySHA1 = new FieldSHA1[array2.Count];
                            for (int j = 0; j < array2.Count; j++) {
                                FakeArrayGUID fakeArrayGUID = metadataReader.Read<FakeArrayGUID>();
                                Common.STUGUID f2 = new Common.STUGUID(fakeArrayGUID.Key, fakeArrayGUID.Padding);
                                DemangleInstance(f2, field.FieldChecksum);
                                outputField.ArraySHA1[j] = sha1.ComputeHash(BitConverter.GetBytes(f2));
                            }
                            break;
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
                    }*/
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