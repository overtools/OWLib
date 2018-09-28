using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace TankLib.STU {
    /// <summary>Instance usage</summary>
    public enum TypeUsage {
        /// <summary>Root type</summary>
        Root,
        
        /// <summary>Embedded in another instance</summary>
        Embed,
        
        /// <summary>Embedded instance array element</summary>
        EmbedArray,
        
        /// <summary>Inline instance</summary>
        Inline,
        
        /// <summary>Inline instance array element</summary>
        InlineArray,
        
        /// <summary>HashMap element</summary>
        HashMap
    }
    
    /// <summary>Base STU instance class</summary>
    public class STUInstance : ISerializable_STU {
        /// <summary>Instance usage</summary>
        public TypeUsage Usage = TypeUsage.Root;

        /// <summary>Read a specified STU field</summary>
        protected void DeserializeField(teStructuredData assetFile, STUField_Info fieldInfo,
            Dictionary<uint, KeyValuePair<FieldInfo, STUFieldAttribute>> fields, STUAttribute stuAttribute) {
            if (!fields.TryGetValue(fieldInfo.Hash, out var field)) {
                string name = stuAttribute.Name ?? $"STU_{stuAttribute.Hash:X8}";  // todo: dis slow
                Debugger.Log(0, "STUInstance", $"Unhandled field: {name}:{fieldInfo.Hash:X8} (size: {fieldInfo.Size})\r\n");
                return;
            }

            if (assetFile.Format == teStructuredDataFormat.V1) {
                assetFile.DynData = assetFile.Data;
            }

            IStructuredDataFieldReader reader = teStructuredData.Manager.FieldReaders[field.Value.ReaderType];

            if (field.Key.FieldType.IsArray) {
                Type elementType = field.Key.FieldType.GetElementType();
                if (elementType == null) return;
                Array array;
                
                BinaryReader data = assetFile.Data;
                BinaryReader dynData = assetFile.DynData;
                
                if (assetFile.Format == teStructuredDataFormat.V2) {
                    if (fieldInfo.Size == 0 || reader is InlineInstanceFieldReader) {  // inline
                        int size = data.ReadInt32();
                        if (size == 0) return;
                        array = Array.CreateInstance(elementType, size);

                        for (int i = 0; i != size; ++i) {
                            reader.Deserialize_Array(teStructuredData.Manager, assetFile, fieldInfo, array, i);
                        }
                    } else {
                        int offset = data.ReadInt32();
                        if (offset == -1) return;
                        dynData.Seek(offset);
                        int size = dynData.ReadInt32();
                        if (size == 0) return;
                        array = Array.CreateInstance(elementType, size);
                        uint unknown = dynData.ReadUInt32();
                        dynData.Seek(dynData.ReadInt64()); // Seek to dataoffset
      
                        for (int i = 0; i != size; ++i) {
                            reader.Deserialize_Array(teStructuredData.Manager, assetFile, fieldInfo, array, i);
                        }
                    }
                } else {
                    long offset = data.ReadInt32();
                    data.ReadInt32(); // :kyaah:

                    long position = data.Position();
                    if (offset <= 0) {
                        array = null;
                    } else {
                        data.BaseStream.Position = offset + assetFile.StartPos;

                        long count = data.ReadInt64();
                        long dataOffset = data.ReadInt64();

                        if (count != -1 && dataOffset > 0) {
                            array = Array.CreateInstance(elementType, count);

                            data.BaseStream.Position = dataOffset + assetFile.StartPos;
                                
                            for (int i = 0; i != count; ++i) {
                                reader.Deserialize_Array(teStructuredData.Manager, assetFile, fieldInfo, array, i);
                            }
                        } else {
                            array = null;
                        }
                    }
                    data.BaseStream.Position = position + 8;
                }
                field.Key.SetValue(this, array);

            } else {
                reader.Deserialize(teStructuredData.Manager, assetFile, fieldInfo, this, field.Key);
            }
        }

        /// <summary>Deserialize instance</summary>
        public void Deserialize(teStructuredData assetFile, STUField_Info field = null) {
            BinaryReader data = assetFile.Data;

            uint instanceHash = teStructuredData.Manager.InstancesInverted[GetType()];
            STUAttribute stuAttribute = teStructuredData.Manager.InstanceAttributes[instanceHash];
            Dictionary<uint, KeyValuePair<FieldInfo, STUFieldAttribute>> fields = teStructuredData.Manager.FieldAttributes[instanceHash];

            if (assetFile.Format == teStructuredDataFormat.V2) {
                int fieldBagIdx = data.ReadInt32();
                if (fieldBagIdx == -1) return;
                if (fieldBagIdx >= assetFile.FieldInfoBags.Count)
                    throw new ArgumentOutOfRangeException($"FieldBag index is out of range. Id: {fieldBagIdx}, Type: '{GetType().Name}', Data offset: {data.BaseStream.Position - 4}");
                foreach (STUField_Info stuField in assetFile.FieldInfoBags[fieldBagIdx]) {
                    int fieldSize = stuField.Size;
                    if (fieldSize == 0) {
                        fieldSize = data.ReadInt32();
                    }
                    long startPosition = data.BaseStream.Position;
                
                    DeserializeField(assetFile, stuField, fields, stuAttribute);

                    data.BaseStream.Position = startPosition + fieldSize;

                    //long endPosition = data.BaseStream.Position;
                    //if (endPosition - startPosition != fieldSize)
                    //    throw new InvalidDataException($"read len != field size. Type: '{GetType().Name}', Field: {stuField.Hash:X8}, Data offset: {startPosition}");
                }
            } else if (assetFile.Format == teStructuredDataFormat.V1) {
                //if (instanceHash == 0xEA30C5E9 || instanceHash == 0x298FC27F || instanceHash == 0x75526BC2 ||
                //    instanceHash == 0x5C7059E || instanceHash == 0x646FF8C3 || instanceHash == 0xAD5967B3 || 
                //    instanceHash == 0x74F13350) {
                //    Debugger.Log(0, "STUInstnace", "HACK: SKIPPING DESERIALIZE\r\n");
                //    return;
                //    //Debugger.Break();
                //}
                uint[] fieldOrder = teStructuredData.Manager.InstanceFields[instanceHash];

                foreach (uint fieldHash in fieldOrder) {
                    //long fieldStart = data.BaseStream.Position;
                    
                    STUField_Info stuField = new STUField_Info {Hash = fieldHash, Size = -1};
                    
                    DeserializeField(assetFile, stuField, fields, stuAttribute);
                }
            }
        }

        public void Deserialize_Array(teStructuredData assetFile, STUField_Info field) => Deserialize(assetFile, field);
    }
}