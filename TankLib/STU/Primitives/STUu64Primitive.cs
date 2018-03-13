using System;

namespace TankLib.STU.Primitives {
    /// <summary>UInt64/ulong STU primitive</summary>
    public class STUu64Primitive : IStructuredDataPrimitiveFactory {
        public object Deserialize(teStructuredData data, STUField_Info field) {
            return data.Data.ReadUInt64();
        }

        public object DeserializeArray(teStructuredData data, STUField_Info field) {
            return data.DynData.ReadUInt64();
        }

        public Type GetValueType() {
            return typeof(ulong);
        }
    }
}