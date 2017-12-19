using System;
using System.IO;
using DataTool.ConvertLogic;
using DataTool.Flag;
using static DataTool.Program;
using static DataTool.Helper.IO;

namespace DataTool.ToolLogic.List.Debug {
    [Tool("list-debug-soundbank", Description = "List soundbanks (debug)", TrackTypes = new ushort[] {0x43}, CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListSoundbank : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            GetSoundbanks();
        }

        public void GetSoundbanks() {
            foreach (ulong key in TrackedFiles[0x43]) {
                // using (FileStream fs = File.OpenRead("C:\\Users\\ZingBallyhoo\\Downloads\\Wood.bnk")) {
                //     Sound.WwiseBank tempBnk = new Sound.WwiseBank(fs);
                // }
                using (Stream stream = OpenFile(key)) {
                    Sound.WwiseBank wwiseBank = new Sound.WwiseBank(stream);
                }
            }
        }
    }
}