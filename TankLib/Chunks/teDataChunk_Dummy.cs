using System.IO;

namespace TankLib.Chunks {
    public class teDataChunk_Dummy : IChunk {
        public string ID => "\0\0\0\0";

        public MemoryStream Data;
        
        public void Parse(Stream stream) {
            Data = new MemoryStream((int)stream.Length);
            stream.CopyTo(Data);
            Data.Position = 0;
        }
    }
}
