using Newtonsoft.Json;
using TankLib.STU;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TankLib;
using System.Reflection;
using System.Threading.Tasks;

namespace STU2JSON
{
    class Program
    {
        public class GUIDConverter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                writer.WriteStartObject();

                writer.WritePropertyName("Key");
                Type type = value.GetType();
                ulong key;
                if (value is teResourceGUID guid)
                {
                    key = (ulong)guid;
                }
                else
                {
                    MethodInfo info = type.GetMethods().FirstOrDefault(x => x.Name == "op_Implicit" && x.ReturnType.FullName == "System.UInt64");
                    if (info == null)
                    {
                        key = (ulong)value;
                    }
                    else
                    {
                        key = (ulong)info.Invoke(null, new object[] { value });
                    }
                }
                writer.WriteValue($"{key:X16}");

                writer.WritePropertyName("String");
                writer.WriteValue($"{teResourceGUID.LongKey(key):X12}.{teResourceGUID.Type(key):X3}");

                writer.WriteEndObject();
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                return 0;
            }

            public override bool CanConvert(Type objectType)
            {
                return typeof(teStructuredDataAssetRef<>).Name == objectType.Name || typeof(teResourceGUID).IsAssignableFrom(objectType) || typeof(ulong).IsAssignableFrom(objectType);
            }
        }

        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Usage: STU2JSON stu_file/dir output_dir");
                return;
            }

            JsonConvert.DefaultSettings = () => new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new GUIDConverter() },
                PreserveReferencesHandling = PreserveReferencesHandling.All,
                Formatting = Formatting.Indented
            };

            MagicTheGathering(Path.GetFullPath(args[0]), Path.GetFullPath(args[1]));
        }

        // Decide if folder or file.
        static void MagicTheGathering(string path, string output)
        {
            if (Directory.Exists(path))
            {
                Pathfinder(path, output);
            }
            else
            {
                DungeonsNDragons(path, output);
            }
        }

        // Iterate folder.
        static void Pathfinder(string path, string output)
        {
            string[] files = Directory.GetFiles(path).Concat(Directory.GetDirectories(path)).ToArray();
            Console.Out.WriteLine($"Folder: {path}");
            output = Path.Combine(output, Path.GetFileName(path));
            Parallel.ForEach(files, file =>
            {
                MagicTheGathering(file, output);
            });
        }

        // Parse File
        static void DungeonsNDragons(string path, string output)
        {
            string filename = Path.GetFileNameWithoutExtension(path);
            string targetDir = Path.Combine(output, filename);
            Console.Out.WriteLine($"File: {path}; Target: {targetDir}");
            using (Stream stream = File.OpenRead(path))
            {
                try
                {
                    teStructuredData stu = new teStructuredData(stream, true);
                    string prefix = string.Empty;
                    IEnumerable<int> instances = stu.Instances.Select((x, i) => new KeyValuePair<int, STUInstance>(i, x)).Where(x => x.Value.Usage == TypeUsage.Root).Select(x => x.Key);
                    if (instances.Count() == 1)
                    {
                        prefix = $"{filename}_";
                        targetDir = output;
                    }
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }
                    foreach (int i in instances)
                    {
                        string target = Path.Combine(targetDir, $"{prefix}{i}_{stu.InstanceInfo[i].Hash:X8}.json");
                        File.WriteAllText(target, JsonConvert.SerializeObject(stu.Instances[i] as object, Formatting.Indented));
                    }
                }
                catch
                {
                    // lol jk not a stu-nami
                }
            }
        }
    }
}
