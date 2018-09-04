using System;

namespace TankLib.STU.Primitives {
    /// <summary>Byte\/byte STU primitive</summary>
    public class STUu8Primitive : IStructuredDataPrimitiveFactory {
        public object Deserialize(teStructuredData data, STUField_Info field) {
            if (data.Format == teStructuredDataFormat.V1) {
                return (byte) data.DynData.ReadUInt32();
            }
            return data.Data.ReadByte();
        }

        public object DeserializeArray(teStructuredData data, STUField_Info field) {
            if (data.Format == teStructuredDataFormat.V1) {
                return (byte) data.DynData.ReadUInt32();
            }
            return data.DynData.ReadByte();
        }

        public Type GetValueType() {
            return typeof(byte);
        }
    }
}