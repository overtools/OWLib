#nullable enable
using System;
using System.IO;
using DataTool.ToolLogic.List;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static DataTool.Helper.IO;
using static DataTool.Helper.Logger;

namespace DataTool.JSON {
    public class JSONTool {
        /// <summary>
        /// Serialize the object to JSON and writes it to the output path.
        /// By default, the JSON is indented, enums are serialized as strings, and teResourceGUIDs are serialized as strings.
        /// </summary>
        public static void OutputJSON(object? jObj, ListFlags toolFlags, JsonSerializerSettings? serializeSettings = null) {
            OutputJSON(jObj, toolFlags.Output, serializeSettings);
        }

        /// <inheritdoc cref="OutputJSON(object,DataTool.ToolLogic.List.ListFlags,Newtonsoft.Json.JsonSerializerSettings?)"/>
        public static void OutputJSON(object? jObj, string? outputFilePath, JsonSerializerSettings? serializeSettings = null) {
            if (serializeSettings == null) {
                serializeSettings = new JsonSerializerSettings {
                    Formatting = Formatting.Indented,
                };

                serializeSettings.Converters.Add(new StringEnumConverter());
                serializeSettings.Converters.Add(new NewtonsoftResourceGUIDFormatter());
            }

            string json = JsonConvert.SerializeObject(jObj, serializeSettings.Formatting, serializeSettings);

            if (!string.IsNullOrWhiteSpace(outputFilePath)) {
                Log("Writing to {0}", outputFilePath);
                CreateDirectoryFromFile(outputFilePath);

                var actualPath = !outputFilePath.EndsWith(".json") ? $"{outputFilePath}.json" : outputFilePath;
                File.WriteAllText(actualPath, json);
            } else {
                Console.Error.WriteLine(json);
            }
        }
    }
}