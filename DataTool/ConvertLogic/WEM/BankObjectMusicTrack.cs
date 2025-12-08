using System.IO;

namespace DataTool.ConvertLogic.WEM {
    [BankObject(11)]
    public class BankObjectMusicTrack : IBankObject {
        public void Read(BinaryReader reader) {
            // we don't actually care about the data
        }
    }
}