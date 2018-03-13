namespace TankLib.STU {
    // ReSharper disable once InconsistentNaming
    public interface ISerializable_STU {
        void Deserialize(teStructuredData data, STUField_Info field);
        void Deserialize_Array(teStructuredData data, STUField_Info field);
        //void Serialize();
        //void Serialize_Array();
    }
}