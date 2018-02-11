using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using STUHashTool;
using STULib;
using STULib.Impl.Version2HashComparer;
using InstanceData = STULib.Impl.Version2HashComparer.InstanceData;

namespace STUClassFixer {
    internal class Program {
        public static List<string> InvalidTypes;
        public static Dictionary<uint, string> FieldNames;
        public static Dictionary<uint, string> EnumNames;
        public static Dictionary<uint, string> InstanceNames;
        public static string OutputDirectory;
        public static string InputDirectory;

        public static void CleanDirectory(string directory) {
            foreach (string subdirectory in Directory.EnumerateDirectories(directory)) {
                CleanDirectory(subdirectory);
            }

            foreach (string file in Directory.EnumerateFiles(directory)) {
                File.Delete(file);
            }
        }

        public static void CreateDirectory(string directory) {
            if (!Directory.Exists(directory)) {
                Directory.CreateDirectory(directory);
            }
        }
        
        
        public static void Main(string[] args) {
            InputDirectory = args[0];
            OutputDirectory = args[1];
            
            CreateDirectory(OutputDirectory);
            CleanDirectory(OutputDirectory);
            
            Dictionary<uint, STUInstanceJSON> instanceJson = STUHashTool.Program.LoadInstanceJson("RegisteredSTUTypes.json");
            STUHashTool.Program.LoadHashCSV("KnownFields.csv", out FieldNames);
            STUHashTool.Program.LoadHashCSV("KnownTypes.csv", out InstanceNames);
            STUHashTool.Program.LoadHashCSV("KnownEnums.csv", out EnumNames);
            Version2Comparer.InstanceJSON = instanceJson;

            ISTU.LoadInstanceTypes();
            
            InvalidTypes = STUHashTool.Program.LoadInvalidTypes("IgnoredBrokenSTUs.txt");
            
            // fix existing classes
            FixClasses();
            
            // add any classes that don't exist
            AddNewClasses(instanceJson);
        }

        public static void FixClasses() {
            foreach (string file in Directory.EnumerateFiles(InputDirectory, "*.cs", SearchOption.AllDirectories)) {
                FixClass(file);
            }
        }

        public class InstanceCode {
            public List<FieldCode> Fields;
            public List<string> Lines;

            public List<string> ExtraLines;

            // parsing
            public uint StartLine;
            public uint EndLine;
            public uint IndentLevel;
            public bool IsDone;
            
            // info
            public uint Hash;
            public string Comment;
            public string Name;
            
            public static Regex HashRegex = new Regex(@"0x(\w+)");

            public InstanceCode(uint indentLevel) {
                Lines = new List<string>();
                IndentLevel = indentLevel;
            }

            public void Feed(string line) {
                if (IsDone) return;
                Lines.Add(line);
            }

            public static bool Check(string line) {
                return (line.Contains("[STU(0x") || line.Contains("[STULib.STU(0x")) && !line.StartsWith(@"//");
            }

            public void ParseFedLines() {
                string prevLine = "";
                Fields = new List<FieldCode>();
                ExtraLines = new List<string>();

                int lineIndex = 0;
                
                foreach (string line in Lines) {
                    string realLine = line.TrimStart(' ');
                    if (lineIndex == 0) {
                        Match instanceMatch = HashRegex.Match(line);
                        if (instanceMatch.Success) {
                            Hash = uint.Parse(instanceMatch.Groups[1].Value, NumberStyles.HexNumber);
                        }
                    } else if (FieldCode.Check(prevLine, realLine)) {
                        Fields.Add(new FieldCode(prevLine, realLine));
                    } else if (lineIndex != 0 && lineIndex != 1 && !string.IsNullOrEmpty(realLine) && !FieldCode.Check(realLine, null) && !Check(realLine)) {
                        ExtraLines.Add(line);
                    }
                
                    prevLine = line;
                    lineIndex++;
                }
            }

            public void UpdateFields(Dictionary<uint, string> fieldNames) {
                if (Version2Comparer.InstanceJSON.ContainsKey(Hash)) {
                    var instance = Version2Comparer.InstanceJSON[Hash];

                    foreach (STUInstanceJSON.STUFieldJSON field in instance.Fields) {
                        if (Fields.All(x => x.Hash != field.Hash)) {
                            Fields.Add(new FieldCode(field));
                        }
                    }
                }
            }

            public void Write(StringBuilder output) {
                int fieldIndex = 0;

                STUInstanceJSON instanceData = Version2Comparer.InstanceJSON[Hash];

                const string indent = "        ";
                
                foreach (STUInstanceJSON.STUFieldJSON field in instanceData.Fields) {
                    FieldCode fieldCode = Fields.First(x => x.Hash == field.Hash);

                    output.AppendLine(indent+fieldCode.HeaderLine);
                    output.AppendLine(indent+fieldCode.ContentLine);
                    
                    if (fieldIndex != Fields.Count - 1) {
                        output.AppendLine();
                    }
                    
                    fieldIndex++;
                }

                if (ExtraLines.Count > 0) {
                    output.AppendLine();
                }
                foreach (string extraLine in ExtraLines) {
                    output.AppendLine(extraLine);
                }
            }
        }

        [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
        public class FieldCode {
            public uint Hash;
            public string Type;
            public string Name;
            public string AccessLevel;
            public string Comment;

            public string HeaderLine;
            public string ContentLine;
            
            internal string DebuggerDisplay => $"FieldCode: {Hash:X8}";
            
            public FieldCode(string headerLine, string contentLine) {
                InitFromLines(headerLine, contentLine);
            }

            private void InitFromLines(string headerLine, string contentLine) {
                HeaderLine = headerLine.TrimStart(' ');
                ContentLine = contentLine.TrimStart(' ');
                
                Match instanceMatch = InstanceCode.HashRegex.Match(HeaderLine);
                if (instanceMatch.Success) {
                    Hash = uint.Parse(instanceMatch.Groups[1].Value, NumberStyles.HexNumber);
                }

                string[] commentArray = ContentLine.Split(new [] {@"//"}, StringSplitOptions.None);

                if (commentArray.Length > 1) {
                    Comment = commentArray[1].TrimStart(' ');
                }

                string[] parts = ContentLine.Split(' ');

                AccessLevel = parts[0];
                Type = parts[1];
                Name = parts[2].TrimEnd(';');
            }

            public FieldCode(STUInstanceJSON.STUFieldJSON field) {
                FieldData wrappedField = new FieldData(field);
                
                ClassBuilder.WriteField(out string headerLine, out string contentLine, "    ", "STULib.Types.Dump", wrappedField, InstanceNames, FieldNames, EnumNames, true);
                
                InitFromLines(headerLine, contentLine);
            }

            public static bool Check(string line1, string line2) {
                if (line1.StartsWith(@"//") || (line2 != null && line2.StartsWith(@"//"))) return false;
                return line1.Contains("[STUField(0x") || line1.Contains("[STULib.STUField(0x");
            }
        }

        public static void FixClass(string file) {
            List<InstanceCode> instances = new List<InstanceCode>();
            uint bracketLevel = 0;

            string previousLine = "";
            uint lineIndex = 0;
            string[] lines = File.ReadAllLines(file);
            
            foreach (string line in lines) {
                string realLine = line.TrimStart(' ');
                if (realLine.Contains("{")) bracketLevel++;
                if (realLine.Contains("}")) bracketLevel--;
                
                if (InstanceCode.Check(previousLine)) {
                    InstanceCode instance = new InstanceCode(bracketLevel) {StartLine = lineIndex};
                    instance.Feed(previousLine);
                    instances.Add(instance);
                }

                foreach (InstanceCode instance in instances) {
                    if (bracketLevel < instance.IndentLevel && !instance.IsDone) {
                        instance.IsDone = true;
                        instance.EndLine = lineIndex-1;
                        continue;
                    }

                    if (bracketLevel >= instance.IndentLevel) {
                        instance.Feed(line);
                    }
                }

                previousLine = realLine;
                lineIndex++;
            }

            if (instances.Count == 0) return;

            StringBuilder output = new StringBuilder();
            
            foreach (InstanceCode instance in instances) {
                instance.ParseFedLines();
                instance.UpdateFields(FieldNames);
            }
            
            lineIndex = 0;
            foreach (string line in lines) {
                bool writeNormal = true;

                foreach (InstanceCode instance in instances) {
                    if (instance.StartLine < lineIndex && instance.EndLine >= lineIndex) {
                        writeNormal = false;
                    }

                    if (lineIndex == instance.StartLine+1) {
                        instance.Write(output);
                    }
                }

                if (writeNormal) {
                    output.AppendLine(line);
                }
                
                lineIndex++;
            }
            
            Console.Out.WriteLine(output);
            string outTest = OutputDirectory+file.Replace(InputDirectory, "");

            WriteStringToFile(output.ToString(), outTest);
        }

        public static void WriteStringToFile(string data, string file) {
            CreateDirectory(Path.GetDirectoryName(file));
            
            using (Stream classOutput = File.OpenWrite(file)) {
                classOutput.SetLength(0);
                using (TextWriter writer = new StreamWriter(classOutput)) {
                    writer.WriteLine(data);
                }
            }
        }

        public static void AddNewClasses(Dictionary<uint, STUInstanceJSON> instanceJson) {
            const string dumpNamespace = "Dump";
            const string enumNamespace = "Enums";
            
            CreateDirectory(Path.Combine(OutputDirectory, dumpNamespace));
            CreateDirectory(Path.Combine(OutputDirectory, dumpNamespace, "Enums"));
            foreach (KeyValuePair<uint, STUInstanceJSON> json in Version2Comparer.InstanceJSON) {
                if (InvalidTypes.Contains(json.Value.Name)) continue;
                if (ISTU.InstanceTypes.ContainsKey(json.Value.Hash)) continue;
                InstanceData instanceData = Version2Comparer.GetData(json.Key);
                ClassBuilder builder = new ClassBuilder(instanceData);
                string @class = builder.Build(InstanceNames, EnumNames, FieldNames, $"STULib.Types.{dumpNamespace}", true, false);
                string className = $"STU_{instanceData.Checksum:X8}";
                if (InstanceNames.ContainsKey(instanceData.Checksum)) {
                    className = InstanceNames[instanceData.Checksum];
                }

                WriteStringToFile(@class, Path.Combine(OutputDirectory, dumpNamespace, $"{className}.cs"));
                
                foreach (FieldData field in instanceData.Fields) {
                    if (!field.IsEnum && !field.IsEnumArray) continue;
                    if (ISTU.EnumTypes.ContainsKey(field.EnumChecksum)) continue;
                    EnumBuilder enumBuilder = new EnumBuilder(new STUEnumData {
                        Type = STUHashTool.Program.GetSizeType(field.Size),
                        Checksum = field.EnumChecksum
                    });
                    string @enum = enumBuilder.Build(new Dictionary<uint, string>(), $"STULib.Types.{dumpNamespace}.{enumNamespace}", true);
                    string enumName = $"STUEnum_{field.EnumChecksum:X8}";
                    if (EnumNames.ContainsKey(field.EnumChecksum)) {
                        enumName = EnumNames[field.EnumChecksum];
                    }
                    WriteStringToFile(@enum, Path.Combine(OutputDirectory, dumpNamespace, enumNamespace, $"{enumName}.cs"));
                }
            }
        }
    }
}