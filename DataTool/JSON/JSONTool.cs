using System;
using System.IO;
using DataTool.ToolLogic.List;
using Utf8Json;
using Utf8Json.Resolvers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static DataTool.Helper.IO;
using static DataTool.Helper.Logger;

namespace DataTool.JSON {
    public class JSONTool {
        public void OutputJSON(object jObj, ListFlags toolFlags, bool log = true) {
            try {
                CompositeResolver.RegisterAndSetAsDefault(new IJsonFormatter[] {
                    new ResourceGUIDFormatter()
                }, new[] {
                    StandardResolver.Default
                });
            } catch {
                // rip, already registered and set as default???
            }

            byte[] json = Utf8Json.JsonSerializer.NonGeneric.Serialize(jObj.GetType(), jObj);
            if (!string.IsNullOrWhiteSpace(toolFlags.Output)) {
                byte[] pretty = Utf8Json.JsonSerializer.PrettyPrintByteArray(json);

                if (log) Log("Writing to {0}", toolFlags.Output);

                CreateDirectoryFromFile(toolFlags.Output);

                var fileName = !toolFlags.Output.EndsWith(".json") ? $"{toolFlags.Output}.json" : toolFlags.Output;

                using (Stream file = File.OpenWrite(fileName)) {
                    file.SetLength(0);
                    file.Write(pretty, 0, pretty.Length);
                }
            } else {
                Console.Error.WriteLine(Utf8Json.JsonSerializer.PrettyPrint(json));
            }
        }

        // Outputs JSON using JSON.net
        // Might not output STUs and GUIDs the same as the other one but it supports object inheritance better
        public static void OutputJSONAlt(object jObj, ListFlags toolFlags, bool log = true) {
            var serializeSettings = new JsonSerializerSettings();
            serializeSettings.Converters.Add(new StringEnumConverter());
            serializeSettings.Converters.Add(new teResourceGUID_Newtonsoft());

            string json = JsonConvert.SerializeObject(jObj, Formatting.Indented, serializeSettings);

            if (!string.IsNullOrWhiteSpace(toolFlags.Output)) {
                if (log) Log("Writing to {0}", toolFlags.Output);

                CreateDirectoryFromFile(toolFlags.Output);

                var fileName = !toolFlags.Output.EndsWith(".json") ? $"{toolFlags.Output}.json" : toolFlags.Output;

                using (Stream file = File.OpenWrite(fileName)) {
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
