using Newtonsoft.Json;
using STULib;
using System;
using System.IO;
using System.Linq;

namespace STU2JSON
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.Error.WriteLine("Usage: STUEHUEHUEHUE stu_file/dir output_dir");
                return;
            }

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
            foreach (string file in files)
            {
                MagicTheGathering(file, output);
            }
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
                    ISTU stu = ISTU.NewInstance(stream, uint.MaxValue, null);
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }
                    for (int i = 0; i < stu.Instances.Count(); ++i)
                    {
                        string target = Path.Combine(targetDir, $"{i}_{stu.Instances.ElementAt(i).InstanceChecksum}.json");
                        File.WriteAllText(target, JsonConvert.SerializeObject(stu.Instances.ElementAt(i) as object, Formatting.Indented));
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
