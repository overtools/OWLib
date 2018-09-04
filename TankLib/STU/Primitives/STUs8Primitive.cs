using System;

namespace TankLib.STU.Primitives {
    /// <summary>SByte/sbyte STU primitive</summary>
    public class STUs8Primitive : IStructuredDataPrimitiveFactory {
        public object Deserialize(teStructuredData data, STUField_Info field) {
            if (data.Format == teStructuredDataFormat.V1) {
                return (sbyte) data.Data.ReadInt32();
            }
            return data.Data.ReadSByte();
        }

        public object DeserializeArray(teStructuredData data, STUField_Info field) {
            if (data.Format == teStructuredDataFormat.V1) {
                return (sbyte) data.DynData.ReadInt32();
            }
            return data.DynData.ReadSByte();
        }

        public Type GetValueType() {
            return typeof(sbyte);
        }
    }
}