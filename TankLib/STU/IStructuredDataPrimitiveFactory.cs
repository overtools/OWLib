using System;

namespace TankLib.STU {
    /// <summary>STU primitive factory. Reads a primitive type from STU data</summary>
    public interface IStructuredDataPrimitiveFactory {
        object Deserialize(teStructuredData data, STUField_Info field);
        object DeserializeArray(teStructuredData data, STUField_Info field);
        Type GetValueType();
    }
}