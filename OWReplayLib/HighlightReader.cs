using System.IO;
using System.Linq;
using OWReplayLib.Types;

namespace OWReplayLib {    
    public class HighlightReader {
        public Highlight.HighlightHeaderNew Data;

        public HighlightReader(Stream stream) {
            using (BinaryReader reader = new BinaryReader(stream)) {
                Data = new Highlight.HighlightHeaderNew();
                Data.Read(reader);

                reader.BaseStream.Position = Data.GetFieldEndPos("DataLength");
                byte[] data = reader.ReadBytes((int)Data.DataLength);
                Checksum checksum = Checksum.Compute(data);

                if (!checksum.Data.SequenceEqual(Data.Checksum.Data)) {
                    throw new InvalidDataException("Checksum is invalid");
                }
                if (Data.DataLength != reader.BaseStream.Length - Data.GetFieldEndPos("DataLength")) {
                    throw new InvalidDataException("DataLength is wrong");
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