using System.IO;
using OWLib.Types;

namespace OWLib {
    public class Material {
        private MaterialHeader header;

        public MaterialHeader Header => header;

        public Material(Stream input, ulong streamid) {
            if (input == null || input.Length <= 0) { return; }
#if OUTPUT_MATERIAL
            long spos = input.Position;
            string outFilename = string.Format("./Materials/{0:X16}.mat", streamid);
            string putPathname = outFilename.Substring(0, outFilename.LastIndexOf('/'));
            Directory.CreateDirectory(putPathname);
            Stream OutWriter = File.Open(outFilename, FileMode.OpenOrCreate);
            input.Seek(0, SeekOrigin.Begin);
            input.CopyTo(OutWriter);
            OutWriter.Close();
            input.Seek(spos, SeekOrigin.Begin);
#endif
            using (BinaryReader reader = new BinaryReader(input)) {
                header = reader.Read<MaterialHeader>();
            }
        }
    }
}
