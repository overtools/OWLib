using System;
using System.Runtime.InteropServices;
using System.Text;

namespace DataTool.ConvertLogic.WEM {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct WwiseBankChunkHeader {
        public uint MagicNumber;
        public uint ChunkLength;

        public string Name => Encoding.UTF8.GetString(BitConverter.GetBytes(MagicNumber));
    }
}