using System;

namespace TankLib.STU.Primitives {
    /// <summary>UInt32/uint STU primitive</summary>
    public class STUu32Primitive : IStructuredDataPrimitiveFactory {
        public object Deserialize(teStructuredData data, STUField_Info field) {
            return data.Data.ReadUInt32();
        }

        public object DeserializeArray(teStructuredData data, STUField_Info field) {
            if (data.Format == teStructuredDataFormat.V2) {
                return data.DynData.ReadUInt32();
            } else {
                return data.Data.ReadUInt32();
            }
        }

        public Type GetValueType() {
            return typeof(uint);
        }
    }
}