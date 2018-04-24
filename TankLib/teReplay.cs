using System.IO;
using System.Text;
using TankLib.DataSerializer;
using static TankLib.teHighlight;
using static TankLib.DataSerializer.Logical;

namespace TankLib
{
    public class teReplay : ReadableData
    {
        public byte FormatVersion;
        public uint BuildNumber;
        public teResourceGUID Map;
        public teResourceGUID Gamemode;
        public byte Unknown1;
        public uint Unknown2;
        public uint Unknown3;
        public teChecksum MapChecksum;
        public int ParamsBlockLength;
        public ReplayParams Params;
        [Conditional("(helper.BitwiseAnd(Unknown1, 4)) != 0", new[] { "Unknown1" })]
        public int HighlightInfoLength;
        [Conditional("(helper.BitwiseAnd(Unknown1, 4)) != 0", new[] { "Unknown1" })]
        public HighlightInfo HighlightInfo;
        [ZstdBuffer(ZstdBufferSize.StreamEnd)]
        public byte[] DecompressedBuffer;

        public class ReplayParams : ReadableData
        {
            public uint StartFrame;
            public uint EndFrame;
            public ulong ExpectedDurationMS;
            public ulong StartMS;
            public ulong EndMS;
            [DynamicSizeArray(typeof(int), typeof(teHeroData))]
            public teHeroData[] Heroes;
        }

        public static readonly int MAGIC = Util.GetMagicBytesBE('p', 'r', 'p'); // Player RePlay

        public teReplay(Stream stream, bool leaveOpen = false)
        {
            using (BinaryReader reader = new BinaryReader(stream, Encoding.Default, leaveOpen))
            {
                if ((reader.ReadInt32() & Util.BYTE_MASK_3) == MAGIC)
                {
                    stream.Position -= 1;
                    Read(reader);
                }
            }
        }
    }
}
