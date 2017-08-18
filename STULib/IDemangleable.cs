namespace STULib {
    public interface IDemangleable {
        ulong[] GetGUIDs();
        void SetGUIDs(ulong[] GUIDs);
        ulong[] GetGUIDXORs();
    }
}
