using System;
using System.IO;
using DataTool.Flag;
using DataTool.ToolLogic.List;
using Newtonsoft.Json;
using static DataTool.Helper.IO;
using static DataTool.Helper.Logger;

namespace DataTool {
    public interface ITool {
        void IntegrateView(object sender);

        void Parse(ICLIFlags toolFlags);
    }

    public class JSONTool {
        internal void ParseJSON(object jObj, ListFlags toolFlags) {
            string json = JsonConvert.SerializeObject(jObj, Formatting.Indented);
            if (!string.IsNullOrWhiteSpace(toolFlags.Output)) {
                Log("Writing to {0}", toolFlags.Output);

                CreateDirectoryFromFile(toolFlags.Output);

                using (Stream file = File.OpenWrite(toolFlags.Output)) {
                    file.SetLength(0);
                    using (TextWriter writer = new StreamWriter(file)) {
                        writer.WriteLine(json);
                    }
                }
            } else {
                Console.Error.WriteLine(json);
            }
        }
    }
}