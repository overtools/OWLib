using System.IO;
using System.Runtime.InteropServices;
using OWReplayLib2.Types;
using static OWLib.Extensions;
using Common = OWReplayLib.Types.Common;

namespace OWReplayLib2 {    
    public class HighlightReader {
        private Stream _stream;
        public Highlight.HighlightHeader Header;

        public const uint MagicConstant = 0x036C6870; // phl3

        public HighlightReader(Stream stream) {
            _stream = stream;

            using (BinaryReader reader = new BinaryReader(stream)) {
                Marshal.SizeOf<Highlight.HighlightInfo>();
                Marshal.SizeOf<Highlight.HighlightHeader>();
                Header = reader.Read<Highlight.HighlightHeader>();

                if (Header.Magic != MagicConstant) {
                    throw new InvalidDataException("Data stream is not a highlight!");
                }

                for (int i = 0; i < Header.tempCount; i++) {
                    // char c = reader.ReadChar();
                    Highlight.HighlightInfo hero = reader.Read<Highlight.HighlightInfo>();
                }
            }
        }

        public static HighlightReader FromFile(string fileName) {
            using (Stream stream = File.OpenRead(fileName)) {
                return new HighlightReader(stream);
            }
        }
    }
}