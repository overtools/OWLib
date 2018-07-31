using System.IO;
using System.Text;
using TankLib.Helpers.DataSerializer;

namespace TankLib.Replay
{
    public sealed class tePlayerReplay : ReadableData
    {
        public byte FormatVersion;
        public uint BuildNumber;
        public teResourceGUID Map;
        public teResourceGUID GameMode;
        public byte Unknown1;
        public uint Unknown2;
        public uint Unknown3;
        public ReplayChecksum MapChecksum;
        public int ParamsBlockLength;
        public ReplayParams Params;
        [Logical.Conditional("(helper.BitwiseAnd(Unknown1, 4)) != 0", new[] { "Unknown1" })]
        public int HighlightInfoLength;
        [Logical.Conditional("(helper.BitwiseAnd(Unknown1, 4)) != 0", new[] { "Unknown1" })]
        public tePlayerHighlight.HighlightInfo HighlightInfo;
        [Logical.ZstdBuffer(Logical.ZstdBufferSize.StreamEnd)]
        public byte[] DecompressedBuffer;

        public class ReplayParams : ReadableData
        {
            public uint StartFrame;
            public uint EndFrame;
            public ulong ExpectedDurationMS;
            public ulong StartMS;
            public ulong EndMS;
            [Logical.DynamicSizeArrayAttribute(typeof(int), typeof(HeroData))]
            public HeroData[] Heroes;
        }

        [Logical.Skip]
        // ReSharper disable once InconsistentNaming
        public static readonly int MAGIC = Util.GetMagicBytesBE('p', 'r', 'p'); // Player RePlay

        public tePlayerReplay(Stream stream, bool leaveOpen = false)
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
