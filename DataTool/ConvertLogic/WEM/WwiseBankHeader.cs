using System.Runtime.InteropServices;

namespace DataTool.ConvertLogic.WEM {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct WwiseBankHeader {
        public uint MagicNumber;
        public uint HeaderLength;
        public uint Version;
        public uint ID;
    }
}