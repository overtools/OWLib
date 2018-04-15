using System;

namespace TankLib.STU.Primitives {
    /// <summary>Int32/int STU primitive</summary>
    public class STUs32Primitive : IStructuredDataPrimitiveFactory {
        public object Deserialize(teStructuredData data, STUField_Info field) {
            return data.Data.ReadInt32();
        }

        public object DeserializeArray(teStructuredData data, STUField_Info field) {
            return data.DynData.ReadInt32();
        }

        public Type GetValueType() {
            return typeof(int);
        }
    }
}