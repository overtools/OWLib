using System;
using System.IO;

namespace TankLib.STU.Primitives {
    /// <inheritdoc />
    /// <summary>teString STU primitive</summary>
    public class STUteStringPrimitive : IStructuredDataPrimitiveFactory {
        public object Deserialize(teStructuredData data, STUField_Info field) {
            if (data.Format == teStructuredDataFormat.V2) {
                int offset = data.Data.ReadInt32();
                if (offset == -1) return null;
                data.DynData.BaseStream.Position = offset;
                Deserialize(data, data.DynData, out string value);
            
                return new teString(value);
            }
            if (data.Format == teStructuredDataFormat.V1) {
                long infoOffset = data.Data.ReadInt64(); data.Data.ReadInt64(); // haHAAA 64-bit
                if (infoOffset == -1 || infoOffset == 0) {
                    return null;
                }

                long posAfter = data.Data.Position();
                data.Data.BaseStream.Position = infoOffset + data.StartPos;
                
                Deserialize(data, data.Data, out string value);
                data.Data.BaseStream.Position = posAfter;
            
                return new teString(value);
            }
            throw new NotImplementedException();
        }

        public object DeserializeArray(teStructuredData data, STUField_Info field) {
            BinaryReader dynData = data.DynData;
            long offset = dynData.ReadInt64();
            
            Enums.SDAM mutability = (Enums.SDAM)dynData.ReadInt64(); // SDAM_NONE = 0, SDAM_MUTABLE = 1, SDAM_IMMUTABLE = 2
            // Debug.Assert(Mutability == teEnums.SDAM.IMMUTABLE, "teString.unk != 2 (not immutable)");
            
            long pos = dynData.BaseStream.Position;
            dynData.Seek(offset);
            
            Deserialize(data, dynData, out string value);
            dynData.Seek(pos);
            
            return new teString(value, mutability);
        }

        private void Deserialize(teStructuredData data, BinaryReader reader, out string value) {
            int size = reader.ReadInt32();
            if (size != 0) {
                uint checksum = reader.ReadUInt32();
                long offset = reader.ReadInt64();
                reader.BaseStream.Position = offset + data.StartPos;
                value = reader.ReadString(size);
            } else {
                value = string.Empty;
            }
        }

        public Type GetValueType() {
            return typeof(teString);
        }
    }
}