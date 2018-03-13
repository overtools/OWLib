using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace TankLib.STU.DataTypes {
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
        protected void DeserializeField(teStructuredData assetFile, STUField_Info fieldInfo, Dictionary<uint, KeyValuePair<FieldInfo, STUFieldAttribute>> fields) {
            if (!fields.ContainsKey(fieldInfo.Hash)) {
                return;
            }

            KeyValuePair<FieldInfo, STUFieldAttribute> field = fields[fieldInfo.Hash];

            IStructuredDataFieldReader reader = teStructuredData.Manager.FieldReaders[field.Value.ReaderType];

            if (field.Key.FieldType.IsArray) {
                Type elementType = field.Key.FieldType.GetElementType();
                if (elementType == null) return;
                Array array;
                // todo: has to be hardcoded as Ver1 doesn't carry this info
                if (fieldInfo.Size == 0 || reader is InlineInstanceFieldReader) {  // inline
                    BinaryReader data = assetFile.Data;

                    int size = data.ReadInt32();
                    if (size == 0) return;
                    array = Array.CreateInstance(elementType, size);
      
                    for (int i = 0; i != size; ++i) {
                        reader.Deserialize_Array(teStructuredData.Manager, assetFile, fieldInfo, array, i);
                    }
                    
                } else {
                    BinaryReader data = assetFile.Data;
                    BinaryReader dynData = assetFile.DynData;

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
                field.Key.SetValue(this, array);

            } else {
                reader.Deserialize(teStructuredData.Manager, assetFile, fieldInfo, this, field.Key);
            }
        }

        /// <summary>Deserialize instance</summary>
        public void Deserialize(teStructuredData assetFile, STUField_Info field = null) {
            BinaryReader data = assetFile.Data;

            uint instanceHash = teStructuredData.Manager.InstancesInverted[GetType()];
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
                
                    DeserializeField(assetFile, stuField, fields);

                    data.BaseStream.Position = startPosition + fieldSize;

                    //long endPosition = data.BaseStream.Position;
                    //if (endPosition - startPosition != fieldSize)
                    //    throw new InvalidDataException($"read len != field size. Type: '{GetType().Name}', Field: {stuField.Hash:X8}, Data offset: {startPosition}");
                }
            } else if (assetFile.Format == teStructuredDataFormat.V1) {
                uint[] fieldOrder = teStructuredData.Manager.InstanceFields[instanceHash];

                foreach (uint fieldHash in fieldOrder) {
                    STUField_Info stuField = new STUField_Info {Hash = fieldHash, Size = -1};
                    
                    DeserializeField(assetFile, stuField, fields);
                }
            }
        }

        public void Deserialize_Array(teStructuredData assetFile, STUField_Info field) => Deserialize(assetFile, field);
    }
}