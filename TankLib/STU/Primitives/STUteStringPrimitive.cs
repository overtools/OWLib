using System;
using System.IO;

namespace TankLib.STU.Primitives {
    /// <summary>teString STU primitive</summary>
    public class STUteStringPrimitive : IStructuredDataPrimitiveFactory {
        public object Deserialize(teStructuredData data, STUField_Info field) {
            int offset = data.Data.ReadInt32();
            if (offset == -1) return null;
            data.DynData.BaseStream.Position = offset;
            Deserialize(data.DynData, out string value);
            
            return new teString(value);
        }

        public object DeserializeArray(teStructuredData data, STUField_Info field) {
            BinaryReader dynData = data.DynData;
            long offset = dynData.ReadInt64();
            
            teEnums.SDAM mutability = (teEnums.SDAM)dynData.ReadInt64(); // SDAM_NONE = 0, SDAM_MUTABLE = 1, SDAM_IMMUTABLE = 2
            // Debug.Assert(Mutability == teEnums.SDAM.IMMUTABLE, "teString.unk != 2 (not immutable)");
            
            long pos = dynData.BaseStream.Position;
            dynData.Seek(offset);
            
            Deserialize(dynData, out string value);
            dynData.Seek(pos);
            
            return new teString(value, mutability);
        }

        private void Deserialize(BinaryReader reader, out string value) {
            int size = reader.ReadInt32();
            if (size != 0) {
                uint checksum = reader.ReadUInt32();
                long offset = reader.ReadInt64();
                reader.BaseStream.Position = offset;
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