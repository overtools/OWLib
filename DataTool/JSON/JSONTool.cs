using System;
using System.IO;
using DataTool.ToolLogic.List;
using Utf8Json;
using Utf8Json.Resolvers;
using static DataTool.Helper.IO;
using static DataTool.Helper.Logger;

namespace DataTool.JSON {
    public class JSONTool {
        internal void OutputJSON(object jObj, ListFlags toolFlags) {
            CompositeResolver.RegisterAndSetAsDefault(new IJsonFormatter[] {
                new ResourceGUIDFormatter()
            }, new[] {
                StandardResolver.Default
            });
            byte[] json = JsonSerializer.Serialize(jObj);
            if (!string.IsNullOrWhiteSpace(toolFlags.Output)) {
                byte[] pretty = JsonSerializer.PrettyPrintByteArray(json);

                Log("Writing to {0}", toolFlags.Output);

                CreateDirectoryFromFile(toolFlags.Output);

                using (Stream file = File.OpenWrite(toolFlags.Output)) {
                    file.SetLength(0);
                    file.Write(pretty, 0, pretty.Length);
                }
            } else {
                Console.Error.WriteLine(JsonSerializer.PrettyPrint(json));
            }
        }
    }
}
