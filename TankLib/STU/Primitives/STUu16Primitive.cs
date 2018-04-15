using System;

namespace TankLib.STU.Primitives {
    /// <summary>UInt16/ushort STU primitive</summary>
    public class STUu16Primitive : IStructuredDataPrimitiveFactory {
        public object Deserialize(teStructuredData data, STUField_Info field) {
            return data.Data.ReadUInt16();
        }

        public object DeserializeArray(teStructuredData data, STUField_Info field) {
            return data.DynData.ReadUInt16();
        }

        public Type GetValueType() {
            return typeof(ushort);
        }
    }
}