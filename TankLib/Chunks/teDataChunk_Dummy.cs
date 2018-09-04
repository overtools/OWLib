using System.IO;

namespace TankLib.Chunks {
    public class teDataChunk_Dummy : IChunk {
        public string ID => "\0\0\0\0";

        public MemoryStream Data;
        public string RealID;
        
        public teDataChunk_Dummy() {} // called by manager

        public teDataChunk_Dummy(string id) {
            RealID = id;
        }

        public void Parse(Stream stream) {
            Data = new MemoryStream((int)stream.Length);
            stream.CopyTo(Data);
            Data.Position = 0;
        }
    }
}
