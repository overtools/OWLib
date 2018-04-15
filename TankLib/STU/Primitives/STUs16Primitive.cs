using System;

namespace TankLib.STU.Primitives {
    /// <summary>Int16/short STU primitive</summary>
    public class STUs16Primitive : IStructuredDataPrimitiveFactory {
        public object Deserialize(teStructuredData data, STUField_Info field) {
            return data.Data.ReadInt16();
        }

        public object DeserializeArray(teStructuredData data, STUField_Info field) {
            return data.DynData.ReadInt16();
        }

        public Type GetValueType() {
            return typeof(short);
        }
    }
}