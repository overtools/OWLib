using System;

namespace TankLib.STU.Primitives {
    /// <summary>SByte/sbyte STU primitive</summary>
    public class STUs8Primitive : IStructuredDataPrimitiveFactory {
        public object Deserialize(teStructuredData data, STUField_Info field) {
            return data.Data.ReadSByte();
        }

        public object DeserializeArray(teStructuredData data, STUField_Info field) {
            return data.DynData.ReadSByte();
        }

        public Type GetValueType() {
            return typeof(sbyte);
        }
    }
}