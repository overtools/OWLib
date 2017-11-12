using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using STULib.Impl.Version2HashComparer;
using static DataTool.Helper.IO;

namespace STUClassFixer {
    internal class Program {
        public static void Main(string[] args) {
            string inputDir = args[0];
            string outputDir = args[1];

            Dictionary<uint, STUInstanceJSON> instanceJson = STUHashTool.Program.LoadInstanceJson("RegisteredSTUTypes.json");
            Version2Comparer.InstanceJSON = instanceJson;

            Regex hashRegex = new Regex(@"0x(\w+)");
            
            foreach (string file in Directory.EnumerateFiles(inputDir, "*.cs", SearchOption.AllDirectories)) {
                // if (!file.Contains("STUHero")) continue;
                Console.WriteLine(file);

                int bracketLevel = 0;
                Dictionary<uint, int> classEnds = new Dictionary<uint, int>();

                string newFilePath = $"{outputDir}{file.Replace(inputDir, "")}";
                CreateDirectoryFromFile(newFilePath);
                using (StreamWriter newFileStream = new StreamWriter(newFilePath)) {
                    foreach (string line in File.ReadAllLines(file)) {
                        string newLine = line;
                        if (line.Contains("{")) bracketLevel++;
                        if (line.Contains("}")) bracketLevel--;
                        if (line.Contains("[STU(0x")) {
                            Match instanceMatch = hashRegex.Match(line);
                            if (instanceMatch.Success) {
                                uint checksum = uint.Parse(instanceMatch.Groups[1].Value, NumberStyles.HexNumber);
                                classEnds[checksum] = bracketLevel;
                            }
                        } else if (line.Contains("}")) {
                            foreach (KeyValuePair<uint,int> classEnd in new Dictionary<uint, int>(classEnds)) {
                                if (classEnd.Value == bracketLevel) {
                                    classEnds.Remove(classEnd.Key);
                                }
                            }
                        } else if (line.Contains("[STUField(0x")) {
                            if (classEnds.Count == 0) continue; // a field from non-stu
                            int biggestLevel = classEnds.Max(x => x.Value);
                            uint instanceChecksum = classEnds.FirstOrDefault(x => x.Value == biggestLevel).Key;
                            Match fieldMatch = hashRegex.Match(line);
                            uint fieldChecksum = uint.Parse(fieldMatch.Groups[1].Value, NumberStyles.HexNumber);
                            if (!instanceJson.ContainsKey(instanceChecksum)) {
                                Console.Out.WriteLine($"Instance {instanceChecksum} does not exist");
                                continue;
                            }
                            STUInstanceJSON instance = instanceJson[instanceChecksum];
                            STUInstanceJSON.STUFieldJSON field = instance.GetField(fieldChecksum);
                            if (field.SerializationType == 2 || field.SerializationType == 3) {
                                newLine = newLine.Substring(0, newLine.LastIndexOf(")]", StringComparison.InvariantCulture));
                                newLine = newLine + ", EmbeddedInstance = true)]";
                                Console.Out.WriteLine($"[STUField(0x{fieldChecksum}), EmbeddedInstance = true)]");
                            }
                        }
                        newFileStream.WriteLine(newLine.TrimEnd(' '));
                    }
                }
            }
        }
    }
}