using System.Runtime.InteropServices;

namespace DataTool.ConvertLogic.WEM {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct WwiseBankWemDef {
        public uint FileID;
        public uint DataOffset;
        public int FileLength;
    }
}