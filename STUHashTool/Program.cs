using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CASCLib;
using Newtonsoft.Json.Linq;
using OverTool;
using OWLib;
using STULib;
using STULib.Impl;
using STULib.Impl.Version2HashComparer;
using Console = System.Console;
using InstanceData = STULib.Impl.Version2HashComparer.InstanceData;
using Util = OverTool.Util;

namespace STUHashTool {
    public class InstanceTally {
        public uint Count;
        public Dictionary<uint, uint> FieldOccurrences;
        public Dictionary<uint, List<CompareResult>> ResultDict;
        public Dictionary<uint, List<FieldResult>> FieldDict;

        public FieldResult GetField(uint before, uint after) {
            return FieldDict[before].FirstOrDefault(f => f.AfterFieldHash == after);
        }
    }

    public class FieldResult {
        public uint BeforeFieldHash;
        public uint AfterFieldHash;
        public uint Count;
    }

    public class CompareResult {
        public uint BeforeInstanceHash;
        public uint AfterInstanceHash;
        public List<FieldCompareResult> Fields;
    }

    public class FieldCompareResult {
        public uint BeforeFieldHash;
        public uint AfterFieldHash;
    }

    public class STUInstanceInfo {
        public List<STUFieldInfo> Fields;
        public uint Checksum;
        public uint Occurrences;
        public bool IsChained;
        public List<ChainedInstanceInfo> ChainInfo;

        public bool ContainsChainInfo(ChainedInstanceInfo info) {
            return ChainInfo.Any(thisInfo => thisInfo.Checksum == info.Checksum && thisInfo.OwnerChecksum == info.OwnerChecksum && thisInfo.OwnerField == info.OwnerField);
        }

        public STUFieldInfo GetField(uint hash) {
            return Fields.FirstOrDefault(f => f.Checksum == hash);
        }

        public STUInstanceInfo Copy(uint occurrences=1) {
            List<STUFieldInfo> newFields = Fields.Select(field => field.Copy()).ToList();
            return new STUInstanceInfo { Fields = newFields, Checksum = Checksum, Occurrences = occurrences};
        }
    }
    
    public class STUEnumData {
        public string Type;
        public uint Checksum;
    }

    public class STUFieldInfo {
        public uint Checksum;
        public uint Size;
        public uint Occurrences;
        public bool IsArray;
        public bool IsInlineStandard;
        public bool IsInlineArray;
        public bool IsUnknownInline;

        public uint StandardOccurrences;

        public uint ArrayOccurrences;

        public uint InlineArrayOccurrences;
        public uint InlineStandardOccurrences;
        public uint UnknownInlineOccurrences;
        public List<STUFieldInfo> InlineFields;

        public bool IsChained;
        public uint ChainedInstanceChecksum;

        public STUFieldInfo Copy(uint occurrences = 1) {
            return new STUFieldInfo {
                Checksum = Checksum,
                Size = Size,
                Occurrences = occurrences,
                IsArray = IsArray,
                IsInlineStandard = IsInlineStandard,
                IsInlineArray = IsInlineArray,
                InlineFields = InlineFields?.Select(field => field.Copy(occurrences)).ToList(),
                IsChained = IsChained,
                ChainedInstanceChecksum = ChainedInstanceChecksum,

                ArrayOccurrences = IsArray ? occurrences : 0,
                InlineArrayOccurrences = IsInlineArray ? occurrences : 0,
                InlineStandardOccurrences = IsInlineStandard ? occurrences : 0,
                IsUnknownInline = IsUnknownInline,
                UnknownInlineOccurrences = IsUnknownInline ? occurrences : 0,
                StandardOccurrences = IsArray || IsInlineArray || IsInlineStandard || IsUnknownInline ? 0 : occurrences
            };
        }

        public bool ContainsInlineField(uint inlineField) {
            if (!IsInlineStandard && !IsInlineArray) return false;
            return InlineFields.Any(f => f.Checksum == inlineField);
        }

        public STUFieldInfo GetInlineField(uint inlineField) {
            if (!IsInlineStandard && !IsInlineArray) return null;
            return InlineFields.FirstOrDefault(f => f.Checksum == inlineField);
        }

        public bool Equals(STUFieldInfo obj) {
            bool firstPass = obj.Checksum == Checksum && obj.IsInlineArray == IsInlineArray
                             && obj.IsInlineStandard == IsInlineStandard && obj.IsArray == IsArray;
            if (!firstPass) return false;
            if (!IsInlineStandard && !IsInlineArray) return true;
            if (obj.InlineFields.Count != InlineFields.Count) return false;
            foreach (STUFieldInfo objF in obj.InlineFields)
                if (ContainsInlineField(objF.Checksum)) {
                    STUFieldInfo gotF = GetInlineField(objF.Checksum);
                    if (!objF.Equals(gotF)) return false;
                } else {
                    return false;
                }
            return true;
        }
    }

    public static class Extensions {
        public static string ProperName(this Type t) => t.FullName.Replace("+", ".");
    }

    public class Program {
        // ReSharper disable once SuggestBaseTypeForParameter
        
        
        public static Dictionary<uint, STUInstanceInfo> Instances = new Dictionary<uint, STUInstanceInfo>();
        public static Dictionary<uint, InstanceData> RealInstances = new Dictionary<uint, InstanceData>();
        public static Dictionary<uint, STUInstanceJSON> InstanceJSON = new Dictionary<uint, STUInstanceJSON>();
        public static Dictionary<uint, STUInstanceJSON> OldInstanceJSON = new Dictionary<uint, STUInstanceJSON>();

        public static Dictionary<uint, string> EnumNames = new Dictionary<uint, string>();
        public static Dictionary<uint, string> FieldNames = new Dictionary<uint, string>();
        public static Dictionary<uint, string> InstanceNames = new Dictionary<uint, string>();
        
        public static Dictionary<uint, STUEnumData> Enums = new Dictionary<uint, STUEnumData>();
        public static List<uint> TodoEnums = new List<uint>();
        
        private static bool ArraysEqual(byte[] a1, byte[] a2) {
            if (ReferenceEquals(a1, a2))
                return true;

            if (a1 == null || a2 == null)
                return false;

            if (a1.Length != a2.Length)
                return false;

            EqualityComparer<byte> comparer = EqualityComparer<byte>.Default;
            return !a1.Where((t, i) => !comparer.Equals(t, a2[i])).Any();
        }

        private static void LoadHashCSV(string filepath, out Dictionary<uint, string> dict) {
            if (string.IsNullOrEmpty(filepath)) {
                dict = new Dictionary<uint, string>(0);
                return;
            }
            string[] rows = File.ReadAllLines(filepath);
            if (rows.Length < 2) { // If it doesn't have at least 1 row after the header
                dict = new Dictionary<uint, string>(0);
                return;
            }
            dict = new Dictionary<uint, string>(rows.Length - 1);
            foreach (string row in rows.Skip(1)) {
                string[] split = row.Split(',');
                if (split.Length != 2) continue;

                string val = split[1].Trim();
                if (val != "N/A")
                    dict.Add(uint.Parse(split[0], NumberStyles.HexNumber), val);
            }
        }

        private static void PrintHelp() {
            Console.Out.WriteLine("Usage:");
            Console.Out.WriteLine("Single file: \"file {before file} {after file}\"");
            Console.Out.WriteLine(
                "Iter files in a single directory: \"dir {before files directory} {after files directory}\"");
            Console.Out.WriteLine("List instances in a directory of files: \"list {files directory}\"");
            Console.Out.WriteLine(
                "Auto generate instance class: \"class {files directory} {instance, \"*\" for all}\"");
            Console.Out.WriteLine(
                "Test classes: \"test {CASC dir} {file type} {instance, \"*\" for all}\"");
        }

        public static List<string> LoadInvalidTypes(string filename) {
            return File.Exists(filename)
                ? File.ReadAllLines(filename).Where(x => !string.IsNullOrEmpty(x)).Select(x => x.Split(' ')[0]).ToList()
                : null;
        }
        

        public static Dictionary<uint, STUInstanceJSON> LoadInstanceJson(string filename) {
            Dictionary<uint, STUInstanceJSON> output = new Dictionary<uint, STUInstanceJSON>();
            JObject stuTypesJson = JObject.Parse(File.ReadAllText(filename));
            foreach (KeyValuePair<string,JToken> pair in stuTypesJson) {
                uint checksum = uint.Parse(pair.Key.Split('_')[1], NumberStyles.HexNumber);
                output[checksum] = new STUInstanceJSON {
                    Fields = null,
                    Hash = checksum,
                    Parent = (string)pair.Value["parent"],
                    Name = pair.Key
                };
                if (pair.Value["fields"] == null) continue;
                output[checksum].Fields = new STUInstanceJSON.STUFieldJSON[pair.Value["fields"].Count()];
                uint fieldCounter = 0;
                foreach (JToken field in pair.Value["fields"]) {
                    output[checksum].Fields[fieldCounter] = new STUInstanceJSON.STUFieldJSON {
                        Hash = uint.Parse((string)field["name"], NumberStyles.HexNumber),
                        Name = (string)field["name"],
                        SerializationType = (int)field["serializationType"],
                        Size = field.Value<int>("size"),
                        Type = field.Value<string>("type")
                    };
                    fieldCounter++;
                }       
            }
            return output;
        }

        private static void Main(string[] args) {
            // Usage:
            // Single file: "file {before file} {after file}"
            // Iter files in a single directory: "dir {before files directory} {after files directory}"
            // List instances in a directory of files: "list {files directory}"
            // Auto generate instance class: "class {files directory} {instance, "*" for all}"
            // Test classes: "test {CASC dir} {file type} {instance, "*" for all}"

            // todo: cleanup

            if (args.Length > 1) {
                if (args[0] == "list" && args.Length < 2) {
                    PrintHelp();
                    return;
                }
                if (args[0] == "compare-debug" && args.Length == 2) {}
                else if (args[0] == "class" && args.Length == 2) {}
                else if (args[0] != "list" && args.Length < 3) {
                    PrintHelp();
                    return;
                }
            } else {
                PrintHelp();
                return;
            }

            List<string> files1 = new List<string>();
            List<string> files2 = new List<string>();
            string mode = args[0];
            string directory1;
            string directory2;
            string classInstance = "";
            string outputFolder = "";
            string cascDir = "";
            string testFileType = "";
            string testinstanceWildcard = null;
            
            switch (mode) {
                default:
                    PrintHelp();
                    return;
                case "file":
                    directory1 = Path.GetDirectoryName(args[1]);
                    directory2 = Path.GetDirectoryName(args[2]);
                    files1.Add(Path.GetFileName(args[1]));
                    files2.Add(Path.GetFileName(args[2]));
                    break;
                case "dir":
                    directory1 = args[1];
                    directory2 = args[2];
                    files1.AddRange(Directory.GetFiles(args[1], "*", SearchOption.TopDirectoryOnly).Select(
                        Path.GetFileName));
                    files2.AddRange(Directory.GetFiles(args[2], "*", SearchOption.TopDirectoryOnly).Select(
                        Path.GetFileName));
                    break;
                case "class":
                    directory1 = args[1];
                    directory2 = args[1];
                    classInstance = args.Length > 2 ? args[2] : "*";
                    if (args.Length > 3) {
                        outputFolder = args[3];
                        if (!Directory.Exists(outputFolder)) {
                            Directory.CreateDirectory(outputFolder);
                        }
                    }
                    foreach (string f in Directory.GetFiles(args[1], "*", SearchOption.TopDirectoryOnly)) {
                        files1.Add(Path.GetFileName(f));
                        files2.Add(Path.GetFileName(f));
                    }
                    if (!File.Exists("RegisteredSTUTypes.json")) break;
                    InstanceJSON = LoadInstanceJson("RegisteredSTUTypes.json");
                    Version2Comparer.InstanceJSON = InstanceJSON;
                    LoadHashCSV("KnownFields.csv", out FieldNames);
                    LoadHashCSV("KnownEnums.csv", out EnumNames);
                    LoadHashCSV("KnownTypes.csv", out InstanceNames);
                    break;
                case "list":
                    directory1 = args[1];
                    directory2 = args[1];
                    foreach (string f in Directory.GetFiles(args[1], "*", SearchOption.TopDirectoryOnly)) {
                        files1.Add(Path.GetFileName(f));
                        files2.Add(Path.GetFileName(f));
                    }
                    break;
                case "test":
                    directory1 = null;
                    directory2 = null;
                    cascDir = args[1];
                    testFileType = args[2];
                    if (args.Length >= 4) {
                        testinstanceWildcard = args[3];
                    }
                    break;
                case "compare-debug":
                    directory1 = args[1];
                    directory2 = args[1];
                    foreach (string f in Directory.GetFiles(args[1], "*", SearchOption.TopDirectoryOnly)) {
                        files1.Add(Path.GetFileName(f));
                        files2.Add(Path.GetFileName(f));
                    }
                    break;
                case "newhashes-test":
                    directory1 = null;
                    directory2 = null;

                    string oldHashes = args[1];
                    string oldTypesCSV = args[2];
                    string oldFieldsCSV = args[3];
                    
                    InstanceJSON = LoadInstanceJson("RegisteredSTUTypes.json");
                    OldInstanceJSON = LoadInstanceJson(oldHashes);
                    
                    LoadHashCSV(oldTypesCSV, out InstanceNames);
                    LoadHashCSV(oldFieldsCSV, out FieldNames);
                    break;
                case "dir-rec":
                    // todo: recurse over every type
                    throw new NotImplementedException();
            }

            List<string> both = files2.Intersect(files1).ToList();
            List<CompareResult> results = new List<CompareResult>();
            
            uint classCount = 0;

            foreach (string file in both) {
                if (directory1 == null || directory2 == null) {
                    break;
                }
                string file1 = Path.Combine(directory1, file);
                string file2 = Path.Combine(directory2, file);
                Debugger.Log(0, "STUHashTool", $"[STUHashTool]: Loading file: {file1}\n");

                Version2Comparer.GetAllChildren = true;

                Type type = typeof(Version2Comparer);
                if (directory1.EndsWith("0BC")) type = typeof(MapComparer); // not nice

                using (Stream file1Stream = File.Open(file1, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    using (Stream file2Stream = File.Open(file2, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        // if (classCount >= 5) continue;
                        
                        ISTU file1STU = ISTU.NewInstance(file1Stream, uint.MaxValue, type);
                        Version2Comparer file1STU2 = (Version2Comparer) file1STU;

                        Version2Comparer file2STU2;
                        if (mode == "class" || mode == "test") {
                            file2STU2 = file1STU2;
                        } else {
                            ISTU file2STU = ISTU.NewInstance(file2Stream, uint.MaxValue, type);
                            file2STU2 = (Version2Comparer) file2STU;
                            // if (mode == "compare-debug") {
                            //     continue;
                            // }
                        }
                        classCount++;
                        

                        foreach (InstanceData instanceData in file1STU2.InstanceData) {
                            if (instanceData == null) continue;
                            if (!RealInstances.ContainsKey(instanceData.Checksum)) {
                                RealInstances[instanceData.Checksum] = instanceData;
                            }
                            FindInternalInstances(instanceData, file1STU2.InternalInstances);
                        }

                        foreach (KeyValuePair<uint,InstanceData> instanceData in file1STU2.InternalInstances) {
                            if (instanceData.Value == null) continue;
                            if (!RealInstances.ContainsKey(instanceData.Value.Checksum)) {
                                RealInstances[instanceData.Value.Checksum] = instanceData.Value;
                            }
                            FindInternalInstances(instanceData.Value, file1STU2.InternalInstances);
                        }
                        
                        if (file1STU2.InstanceGuessData == null) continue;

                        foreach (InstanceGuessData instance1 in file1STU2.InstanceGuessData) {
                            if (instance1 == null) {
                                continue;
                            }
                            // IncrementInstance(instance1);
                            if (mode == "class" || mode == "compare-debug") {
                                continue;
                            }
                            foreach (InstanceGuessData instance2 in file2STU2
                                .InstanceGuessData) {
                                if (instance1 == null || instance2 == null) {
                                    continue;
                                }
                                // Console.Out.WriteLine($"Trying {instance1.hash:X}:{instance2.hash:X}");
                                if (instance1.Fields.Length != instance2.Fields.Length) {
                                    Debugger.Log(0, "STUHashTool",
                                        $"[STUHashTool] {file}: {instance1.Checksum:X8} != {instance2.Checksum:X8}, " +
                                        "different field count\n");
                                    continue;
                                }

                                if (instance1.Size != instance2.Size) {
                                    Debugger.Log(0, "STUHashTool",
                                        $"[STUHashTool] {file}: {instance1.Checksum:X8} != {instance2.Checksum:X8}, " +
                                        "different size\n");
                                    continue;
                                }

                                //if (file1STU2.instanceDiffData.Length != file2STU2.instanceDiffData.Length) {
                                //    Debugger.Log(0, "STUHashTool", $"[STUHashTool] {file}: {instance1.hash:X} != {instance2.hash:X}, different instance count\n");
                                //    Console.Out.WriteLine($"{instance1.hash:X} != {instance2.hash:X}, can't verify due to different instance count");
                                //    continue;
                                //}

                                if (file1STU2.InstanceGuessData.Length == 1 || file2STU2.InstanceGuessData.Length == 1) {
                                    Debugger.Log(0, "STUHashTool",
                                        $"[STUHashTool] {file}: {instance1.Checksum:X8} might be {instance2.Checksum:X8}, " +
                                        "only one instance\n");
                                } else {
                                    Debugger.Log(0, "STUHashTool",
                                        $"[STUHashTool] {file}: {instance1.Checksum:X8} might be {instance2.Checksum:X8}\n");
                                }

                                results.Add(new CompareResult {
                                    BeforeInstanceHash = instance1.Checksum,
                                    AfterInstanceHash = instance2.Checksum,
                                    Fields = new List<FieldCompareResult>()
                                });

                                foreach (FieldGuessData field1 in instance1.Fields) {
                                    foreach (FieldGuessData field2 in instance2.Fields) {
                                        if (field1.Size != field2.Size) {
                                            continue;
                                        }

                                        if (ArraysEqual(field1.SHA1, field2.SHA1)) {
                                            Debugger.Log(0, "STUHashTool",
                                                $"[STUHashTool] {file}: {instance1.Checksum:X8}:{field1.Checksum:X8} == " +
                                                $"{instance2.Checksum:X8}:{field2.Checksum:X8}, same SHA1\n");
                                            results.Last().Fields.Add(new FieldCompareResult {
                                                BeforeFieldHash = field1.Checksum,
                                                AfterFieldHash = field2.Checksum
                                            });
                                        }

                                        if (field1.DemangleSHA1 == null && field2.DemangleSHA1 == null) continue;
                                        if (!ArraysEqual(field1.DemangleSHA1, field2.DemangleSHA1)) continue;
                                        Debugger.Log(0, "STUHashTool",
                                            $"[STUHashTool] {file}: {instance1.Checksum:X8}:{field1.Checksum:X8} == " +
                                            $"{instance2.Checksum:X8}:{field2.Checksum:X8}, same demangled SHA1\n");
                                        results.Last().Fields.Add(new FieldCompareResult {
                                            BeforeFieldHash = field1.Checksum,
                                            AfterFieldHash = field2.Checksum
                                        });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            Dictionary<uint, InstanceTally> instanceChangeTally = new Dictionary<uint, InstanceTally>();
            foreach (CompareResult result in results) {
                if (!instanceChangeTally.ContainsKey(result.BeforeInstanceHash)) {
                    instanceChangeTally[result.BeforeInstanceHash] = new InstanceTally {
                        Count = 1,
                        ResultDict = new Dictionary<uint, List<CompareResult>>(),
                        FieldDict = new Dictionary<uint, List<FieldResult>>()
                    };
                    instanceChangeTally[result.BeforeInstanceHash].ResultDict[result.AfterInstanceHash] =
                        new List<CompareResult> {result};
                    instanceChangeTally[result.BeforeInstanceHash].FieldOccurrences = new Dictionary<uint, uint>();
                    foreach (FieldCompareResult d in result.Fields) {
                        if (instanceChangeTally[result.BeforeInstanceHash].FieldDict.ContainsKey(d.BeforeFieldHash)) {
                            instanceChangeTally[result.BeforeInstanceHash].FieldOccurrences[d.BeforeFieldHash]++;
                            FieldResult f = instanceChangeTally[result.BeforeInstanceHash]
                                .GetField(d.BeforeFieldHash, d.AfterFieldHash);
                            if (f != null) {
                                f.Count++;
                            } else {
                                instanceChangeTally[result.BeforeInstanceHash].FieldDict[d.BeforeFieldHash]
                                    .Add(new FieldResult {
                                        BeforeFieldHash = d.BeforeFieldHash,
                                        AfterFieldHash = d.AfterFieldHash,
                                        Count = 1
                                    });
                            }
                        } else {
                            instanceChangeTally[result.BeforeInstanceHash].FieldDict[d.BeforeFieldHash] =
                                new List<FieldResult> {
                                    new FieldResult {
                                        BeforeFieldHash = d.BeforeFieldHash,
                                        AfterFieldHash = d.AfterFieldHash,
                                        Count = 1
                                    }
                                };
                            instanceChangeTally[result.BeforeInstanceHash].FieldOccurrences[d.BeforeFieldHash] = 1;
                        }
                    }
                } else {
                    instanceChangeTally[result.BeforeInstanceHash].Count++;
                    if (!instanceChangeTally[result.BeforeInstanceHash].ResultDict
                        .ContainsKey(result.AfterInstanceHash)) {
                        instanceChangeTally[result.BeforeInstanceHash].ResultDict[result.AfterInstanceHash] =
                            new List<CompareResult>();
                    }
                    instanceChangeTally[result.BeforeInstanceHash].ResultDict[result.AfterInstanceHash].Add(result);

                    foreach (FieldCompareResult d in result.Fields) {
                        if (instanceChangeTally[result.BeforeInstanceHash].FieldDict.ContainsKey(d.BeforeFieldHash)) {
                            instanceChangeTally[result.BeforeInstanceHash].FieldOccurrences[d.BeforeFieldHash]++;
                            FieldResult f = instanceChangeTally[result.BeforeInstanceHash]
                                .GetField(d.BeforeFieldHash, d.AfterFieldHash);
                            if (f != null) {
                                f.Count++;
                            } else {
                                instanceChangeTally[result.BeforeInstanceHash].FieldDict[d.BeforeFieldHash]
                                    .Add(new FieldResult {
                                        BeforeFieldHash = d.BeforeFieldHash,
                                        AfterFieldHash = d.AfterFieldHash,
                                        Count = 1
                                    });
                            }
                        } else {
                            instanceChangeTally[result.BeforeInstanceHash].FieldDict[d.BeforeFieldHash] =
                                new List<FieldResult> {
                                    new FieldResult {
                                        BeforeFieldHash = d.BeforeFieldHash,
                                        AfterFieldHash = d.AfterFieldHash,
                                        Count = 1
                                    }
                                };
                            instanceChangeTally[result.BeforeInstanceHash].FieldOccurrences[d.BeforeFieldHash] = 1;
                        }
                    }
                }
            }

            if (mode == "list") {
                uint instanceCounter = 0;
                foreach (KeyValuePair<uint, STUInstanceInfo> instance in Instances) {
                    Console.Out.WriteLine($"{instance.Key:X8}: (in {instance.Value.Occurrences}/{both.Count} files)");
                    foreach (STUFieldInfo field in instance.Value.Fields) {
                        Console.Out.WriteLine(
                            $"\t{field.Checksum:X8}: {field.Size} bytes (in {field.Occurrences}/" +
                            $"{instance.Value.Occurrences} instances)");
                    }
                    instanceCounter++;
                    if (instanceCounter != Instances.Count) {
                        Console.Out.WriteLine();
                    }
                }
            } else if (mode == "class") {
                string[] todoInstances;
                if (classInstance == "*") {
                    uint wildcardCount = 0;
                    todoInstances = new string[RealInstances.Count];
                    foreach (KeyValuePair<uint, InstanceData> instance in RealInstances) {
                        todoInstances[wildcardCount] = Hex(instance.Value.Checksum);
                        wildcardCount++;
                    }
                } else {
                    todoInstances = classInstance.Split(':');
                }

                todoInstances = GetClassTodos(todoInstances);
                
                const string stuNamespace = "STULib.Types.posthash";
                const string enumNamespace = stuNamespace+".Enums";
                
                foreach (string t in todoInstances) {
                    uint todoInstance = Convert.ToUInt32(t, 16);
                    if (!RealInstances.ContainsKey(todoInstance)) continue;
                    if (ISTU.InstanceTypes.ContainsKey(todoInstance)) continue;

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("// File auto generated by STUHashTool");
                    sb.AppendLine("using static STULib.Types.Generic.Common;");
                    sb.AppendLine();
                    // sb.AppendLine($"namespace {stuNamespace} {{");
                    
                    ClassBuilder builder = new ClassBuilder(RealInstances[todoInstance]);
                    sb.Append(builder.Build(InstanceNames, EnumNames, FieldNames, stuNamespace));

                    // sb.Append("}");

                    if (outputFolder.Length > 0 && !ISTU.InstanceTypes.ContainsKey(RealInstances[todoInstance].Checksum)) {
                        string name = $"STU_{todoInstance:X8}";
                        if (InstanceNames.ContainsKey(RealInstances[todoInstance].Checksum)) {
                            name = InstanceNames[RealInstances[todoInstance].Checksum];
                        }
                        using (Stream stream =
                            File.OpenWrite($"{outputFolder}{Path.DirectorySeparatorChar}{name}.cs")) {
                            stream.SetLength(0);
                            using (TextWriter writer = new StreamWriter(stream)) {
                                writer.WriteLine(sb);
                            }
                        }
                    }
                    Console.Out.WriteLine(sb);
                }

                foreach (uint todoEnum in TodoEnums) {
                    if (ISTU.EnumTypes.ContainsKey(todoEnum)) continue;
                    string @enum = new EnumBuilder(Enums[todoEnum]).Build(EnumNames, enumNamespace);
                    Console.Out.WriteLine(@enum);
                    if (outputFolder.Length <= 0 || ISTU.EnumTypes.ContainsKey(todoEnum)) continue;
                    if (!Directory.Exists($"{outputFolder}{Path.DirectorySeparatorChar}Enums{Path.DirectorySeparatorChar}")) {
                        Directory.CreateDirectory($"{outputFolder}{Path.DirectorySeparatorChar}Enums{Path.DirectorySeparatorChar}");
                    }
                    string name = $"STUEnum_{todoEnum:X8}";
                    if (EnumNames.ContainsKey(todoEnum)) {
                        name = EnumNames[todoEnum];
                    }
                    using (Stream stream =
                        File.OpenWrite($"{outputFolder}{Path.DirectorySeparatorChar}Enums{Path.DirectorySeparatorChar}{name}.cs")) {
                        stream.SetLength(0);
                        using (TextWriter writer = new StreamWriter(stream)) {
                            writer.WriteLine(@enum);
                        }
                    }
                }
            } else if (mode == "test") {
                ushort fileShort = ushort.Parse(testFileType, NumberStyles.HexNumber);
                    
                Dictionary<ulong, Record> records = new Dictionary<ulong, Record>();
                Dictionary<ushort, List<ulong>> track = new Dictionary<ushort, List<ulong>> {
                    [fileShort] = new List<ulong>()
                };
                
                CASCConfig config = CASCConfig.LoadLocalStorageConfig(cascDir, true, false);
                config.Languages = new HashSet<string>(new[] { "enUS" });
                CASCHandler handler = CASCHandler.OpenStorage(config);
                OwRootHandler root = handler?.Root as OwRootHandler;
                Util.MapCMF(root, handler, records, track, "enUS");
                foreach (ulong file in track[fileShort]) {
                    if (!records.ContainsKey(file)) {
                        Debugger.Log(0, "STUHashTool:test", $"Unable to open file: {file:X} ({GUID.LongKey(file):X12}.{GUID.Type(file):X3})\n");
                        continue;
                    }
                    //if (file != 288230376151714901 && file != 288230376151712128) continue;
                    // if (file != 0x400000000000C55) continue;
                    // if (file != 166633186212711045) continue;  // season 1
                    // if (file != 396316767208603669) continue; // sound 01B
                    // if ($"{GUID.LongKey(file):X12}.{GUID.Type(file):X3}" != "000000000199.068") continue;
                    // if ($"{GUID.LongKey(file):X12}.{GUID.Type(file):X3}" != "000000000BF7.01B") continue;
                    // if ($"{GUID.LongKey(file):X12}.{GUID.Type(file):X3}" != "00000000012E.01B") continue;
                    using (Stream fileStream = Util.OpenFile(records[file], handler)) {
                        // STULib.Types.Map.Map map = new STULib.Types.Map.Map(fileStream, uint.MaxValue);
                        ISTU fileSTU = ISTU.NewInstance(fileStream, uint.MaxValue);
                        Console.WriteLine($"Loaded: {file:X12} {GUID.LongKey(file):X12}.{GUID.Type(file):X3}", Color.LightGray);
                        Utils.DumpSTUFull((Version2) fileSTU, handler, records, 
                            testinstanceWildcard == "*" ? null: testinstanceWildcard);
                    }
                }
            } else if (mode == "newhashes-test") {
                // testing thing, trying to guess fields and whatever
                Dictionary<KeyValuePair<uint, uint>, uint> fieldOccurrences = new Dictionary<KeyValuePair<uint, uint>, uint>();
                Dictionary<uint, uint> totalFieldOccurr = new Dictionary<uint, uint>();
                foreach (KeyValuePair<uint,STUInstanceJSON> oldInstance in OldInstanceJSON) {
                    foreach (KeyValuePair<uint,STUInstanceJSON> newInstance in InstanceJSON) {
                        if (oldInstance.Value.Fields.Length != newInstance.Value.Fields.Length) continue;
                        if (oldInstance.Value.Parent != null && oldInstance.Value.Parent == null) continue;
                        if (oldInstance.Value.Parent == null && oldInstance.Value.Parent != null) continue;

                        bool bad = false;
                        Dictionary<KeyValuePair<uint, uint>, uint> tempFieldOccurr = new Dictionary<KeyValuePair<uint, uint>, uint>();

                        for (int i = 0; i < newInstance.Value.Fields.Length; i++) {
                            STUInstanceJSON.STUFieldJSON oldField = oldInstance.Value.Fields[i];
                            STUInstanceJSON.STUFieldJSON newField = newInstance.Value.Fields[i];
                            if (oldField.SerializationType != newField.SerializationType) bad = true;
                            if (oldField.Size != newField.Size) bad = true;
                            if (oldField.SerializationType == 0 || oldField.SerializationType == 1 ||
                                oldField.SerializationType == 10 || oldField.SerializationType == 11) {
                                if (oldField.Type != newField.Type) bad = true;
                            }
                            
                            if (FieldNames.ContainsKey(oldField.Hash)) {
                                KeyValuePair<uint, uint>
                                    kv = new KeyValuePair<uint, uint>(oldField.Hash, newField.Hash);
                                if (!tempFieldOccurr.ContainsKey(kv)) {
                                    if (fieldOccurrences.ContainsKey(kv)) {
                                        tempFieldOccurr[kv] = fieldOccurrences[kv];
                                    } else {
                                        tempFieldOccurr[kv] = 0;
                                    }
                                }
                                tempFieldOccurr[new KeyValuePair<uint, uint>(oldField.Hash, newField.Hash)]++;
                            }
                        }
                        if (bad) continue;
                        foreach (KeyValuePair<KeyValuePair<uint,uint>,uint> pair in tempFieldOccurr) {
                            
                            fieldOccurrences[pair.Key] = pair.Value;
                        }
                    }
                }
                IOrderedEnumerable<KeyValuePair<KeyValuePair<uint, uint>, uint>> sortedDict = from entry in fieldOccurrences orderby entry.Value descending select entry;
                foreach (KeyValuePair<KeyValuePair<uint, uint>, uint> fieldChange in sortedDict) {
                    if (fieldChange.Value < 10) continue;
                    Console.Out.WriteLine($"{fieldChange.Key.Key:X}:{fieldChange.Key.Value:X}:{FieldNames[fieldChange.Key.Key]} Count: {fieldChange.Value}");
                }
            } else if (mode != "compare-debug") {
                foreach (KeyValuePair<uint, InstanceTally> it in instanceChangeTally) {
                    foreach (KeyValuePair<uint, List<CompareResult>> id in it.Value.ResultDict) {
                        double instanceProbablility = (double) id.Value.Count / it.Value.Count * 100;
                        Console.Out.WriteLine($"{it.Key:X8} => {id.Key:X8} ({instanceProbablility:0.0#}% probability)");
                        foreach (KeyValuePair<uint, List<FieldResult>> field in it.Value.FieldDict) {
                            foreach (FieldResult fieldResult in field.Value) {
                                double fieldProbability =
                                    (double) fieldResult.Count /
                                    it.Value.FieldOccurrences[fieldResult.BeforeFieldHash] * 100;
                                Console.Out.WriteLine(
                                    $"\t{fieldResult.BeforeFieldHash:X8} => {fieldResult.AfterFieldHash:X8} " +
                                    $"({fieldProbability:0.0#}% probability)");
                            }
                        }
                    }
                }
            }
            if (Debugger.IsAttached) {
                List<KeyValuePair<uint, STUInstanceInfo>> instanes = Instances.Where(x => x.Value.IsChained).ToList();
                List<KeyValuePair<uint, InstanceData>> realinstanes = RealInstances.ToList();
                Debugger.Break();
            }
        }

        public static string Hex(uint num) {
            return num.ToString("X").ToUpperInvariant();
        }

        public static List<string> FindNestedTodo(InstanceData instance, string[] todoInstances) {
            List<string> newTodoInstances = todoInstances.ToList();
            if (instance.ParentType != null && !newTodoInstances.Contains(Hex(instance.ParentChecksum))) {
                newTodoInstances.Add(Hex(instance.ParentChecksum));
                FindNestedTodo(instance, newTodoInstances.ToArray());
            }
            foreach (FieldData field in instance.Fields) {
                if (field.IsInline || field.IsInlineArray) {
                    if (!newTodoInstances.Contains(Hex(field.InlineInstanceChecksum)) && !ISTU.InstanceTypes.ContainsKey(field.InlineInstanceChecksum)) {
                        newTodoInstances.Add(Hex(field.InlineInstanceChecksum));
                        newTodoInstances = FindNestedTodo(RealInstances[field.InlineInstanceChecksum],
                            newTodoInstances.ToArray());
                    }
                }
                if (field.IsEmbed || field.IsEmbedArray) {
                    if (!newTodoInstances.Contains(Hex(field.EmbedInstanceChecksum)) && !ISTU.InstanceTypes.ContainsKey(field.InlineInstanceChecksum)) {
                        newTodoInstances.Add(Hex(field.EmbedInstanceChecksum));
                        newTodoInstances = FindNestedTodo(RealInstances[field.EmbedInstanceChecksum],
                            newTodoInstances.ToArray());
                    }
                }

                if (field.IsHashMap) {
                    if (!newTodoInstances.Contains(Hex(field.HashMapChecksum)) && !ISTU.InstanceTypes.ContainsKey(field.HashMapChecksum)) {
                        newTodoInstances.Add(Hex(field.HashMapChecksum));
                        newTodoInstances = FindNestedTodo(RealInstances[field.HashMapChecksum],
                            newTodoInstances.ToArray());
                    }
                }

                if (!field.IsEnum && !field.IsEnumArray) continue;
                if (!TodoEnums.Contains(field.EnumChecksum)) {
                    TodoEnums.Add(field.EnumChecksum);
                }
            }
            return newTodoInstances;
        }

        public static string[] GetClassTodos(string[] todoInstances) {
            List<string> newTodoInstances = todoInstances.ToList();
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (string todo in todoInstances) {
                uint beforeTodoInstance = Convert.ToUInt32(todo, 16);
                if (RealInstances.ContainsKey(beforeTodoInstance)) {
                    newTodoInstances = FindNestedTodo(RealInstances[beforeTodoInstance], 
                        newTodoInstances.ToArray());
                }
            }
            return newTodoInstances.ToArray();
        }

        public static string GetType(FieldData field, bool properTypePaths=false) {
            if (properTypePaths) {  // todo: @zb: please do this better
                switch (field.Type) {
                    default:
                        return null;
                    case "u8":
                        return "byte";
                    case "u16":
                        return "ushort";
                    case "u32":
                        return "uint";
                    case "u64":
                        return "ulong";
                    case "s8":
                        return "sbyte";
                    case "s16":
                        return "short";
                    case "s32":
                        return "int";
                    case "s64":
                        return "long";
                    case "f32":
                        return "float";
                    case "f64":
                        return "double";
                    case "teString":
                        return "string";
                    case "teVec2":
                        return "STULib.Types.Generic.Common.STUVec2";
                    case "teVec3":
                        return "STULib.Types.Generic.Common.STUVec3";
                    case "teVec3A":
                        return "STULib.Types.Generic.Common.STUVec3A";
                    case "teVec4":
                        return "STULib.Types.Generic.Common.STUVec4";
                    case "teEntityID":
                        return "STULib.Types.Generic.Common.STUEntityID";
                    case "teColorRGB":
                        return "STULib.Types.Generic.Common.STUColorRGB";
                    case "teColorRGBA":
                        return "STULib.Types.Generic.Common.STUColorRGBA";
                    case "teQuat":
                        return "STULib.Types.Generic.Common.STUQuaternion";
                    case "ARRAY FILE REFERENCE":
                    case "File Reference":
                        return "STULib.Types.Generic.Common.STUGUID";
                    case "teStructuredDataDateAndTime":
                        return "STULib.Types.Generic.Common.STUDateAndTime";
                    case "teUUID":
                        return "STULib.Types.Generic.Common.STUUUID";
                }
            }
            switch (field.Type) {
                default:
                    return null;
                case "u8":
                    return "byte";
                case "u16":
                    return "ushort";
                case "u32":
                    return "uint";
                case "u64":
                    return "ulong";
                case "s8":
                    return "sbyte";
                case "s16":
                    return "short";
                case "s32":
                    return "int";
                case "s64":
                    return "long";
                case "f32":
                    return "float";
                case "f64":
                    return "double";
                case "teString":
                    return "string";
                case "teVec2":
                    return "STUVec2";
                case "teVec3":
                    return "STUVec3";
                case "teVec3A":
                    return "STUVec3A";
                case "teVec4":
                    return "STUVec4";
                case "teEntityID":
                    return "STUEntityID";
                case "teColorRGB":
                    return "STUColorRGB";
                case "teColorRGBA":
                    return "STUColorRGBA";
                case "teQuat":
                    return "STUQuaternion";
                case "ARRAY FILE REFERENCE":
                case "File Reference":
                    return "STUGUID";
                case "teStructuredDataDateAndTime":
                    return "STUDateAndTime";
                case "teUUID":
                    return "STUUUID";
            }
        }

        public static string GetSizeType(int size) {
            switch (size) {
                default:
                    return null;
                case 8:
                    return "ulong";
                case 4:
                    return "uint";
                case 2:
                    return "ushort";
                case 1:
                    return "byte";
            }
        }

        public static bool ComprareFields(STUFieldInfo[] first, STUFieldInfo[] second) {
            if (first.Length != second.Length) {
                return false;
            }
            uint good = 0;
            foreach (STUFieldInfo f1 in first) {
                bool found = false;
                foreach (STUFieldInfo f2 in second) {
                    if (!f1.Equals(f2)) continue;
                    found = true;
                    good++;
                }
                if (!found) {
                    break;
                }
            }
            return good == first.Length;
        }

        public static string InlineDone(STUFieldInfo[] fields, Dictionary<string, STUFieldInfo[]> doneNests) {
            return (from done in doneNests where ComprareFields(done.Value, fields) select done.Key).FirstOrDefault();
        }

        public static void FindInternalInstances(InstanceData instance, Dictionary<uint, InstanceData> internalInstances, List<uint> alreadyDone=null) {
            if (alreadyDone == null) {
                alreadyDone = new List<uint>();
            }
            if (!alreadyDone.Contains(instance.Checksum)) {
                RealInstances[instance.Checksum] = instance;
                alreadyDone.Add(instance.Checksum);
            }
            if (instance.ParentType != null && !alreadyDone.Contains(instance.ParentChecksum)) {
                RealInstances[instance.ParentChecksum] = internalInstances[instance.ParentChecksum];
                alreadyDone.Add(instance.ParentChecksum);
                FindInternalInstances(internalInstances[instance.ParentChecksum], internalInstances, alreadyDone);
            }
            foreach (FieldData field in instance.Fields) {
                if ((field.IsEmbed || field.IsEmbedArray) && !alreadyDone.Contains(field.EmbedInstanceChecksum)) {
                    RealInstances[field.EmbedInstanceChecksum] = internalInstances[field.EmbedInstanceChecksum];
                    alreadyDone.Add(field.EmbedInstanceChecksum);
                    FindInternalInstances(internalInstances[field.EmbedInstanceChecksum], internalInstances, alreadyDone);
                }
                if ((field.IsInline || field.IsInlineArray) && !alreadyDone.Contains(field.InlineInstanceChecksum)) {
                    RealInstances[field.InlineInstanceChecksum] = internalInstances[field.InlineInstanceChecksum];
                    alreadyDone.Add(field.InlineInstanceChecksum);
                    FindInternalInstances(internalInstances[field.InlineInstanceChecksum], internalInstances, alreadyDone);
                }
                if (field.IsHashMap && !alreadyDone.Contains(field.HashMapChecksum)) {
                    RealInstances[field.HashMapChecksum] = internalInstances[field.HashMapChecksum];
                    alreadyDone.Add(field.HashMapChecksum);
                    FindInternalInstances(internalInstances[field.HashMapChecksum], internalInstances, alreadyDone);
                }
                if (!field.IsEnum && !field.IsEnumArray) continue;
                if (Enums.ContainsKey(field.EnumChecksum)) continue;
                Enums[field.EnumChecksum] = new STUEnumData {
                    Type = GetSizeType(field.Size),
                    Checksum = field.EnumChecksum
                };
            }
        }

        public static void IncrementFieldCount(STUFieldInfo f, FieldGuessData f2) {
            f.Occurrences++;
            bool incr = false;
            if (f2.IsChained) {
                f.IsChained = true;
                f.ChainedInstanceChecksum = f2.ChainedInstanceChecksum;
            }
            if (f2.IsInlineArray) {
                f.InlineArrayOccurrences++;
                f.IsInlineArray = true;
                incr = true;
            }
            if (f2.IsInlineStandard) {
                f.InlineStandardOccurrences++;
                f.IsInlineStandard = true;
                incr = true;
            }
            if (f2.IsArray) {
                f.ArrayOccurrences++;
                f.IsArray = true;
                incr = true;
            }
            if (f2.IsUnknownInline) {
                f.UnknownInlineOccurrences++;
                f.IsUnknownInline = true;
                incr = true;
            }

            if (!incr) {
                f.StandardOccurrences++;
            }
            if (f2.InlineFields == null) return;
            foreach (FieldGuessData nestF in f2.InlineFields) {
                if (f.ContainsInlineField(nestF.Checksum)) {
                    STUFieldInfo gotF = f.GetInlineField(nestF.Checksum);
                    gotF.Occurrences++;
                    IncrementFieldCount(gotF, nestF);
                } else {
                    if (f.InlineFields == null) {
                        f.InlineFields = ConvertFields(f2.InlineFields)?.ToList();
                    } else {
                        f.InlineFields.Add(ConvertField(nestF));
                    }
                }
            }
        }

        public static STUFieldInfo ConvertField(FieldGuessData f, uint occurrences=1) {
            return new STUFieldInfo {
                Size = f.Size,
                Checksum = f.Checksum,
                
                Occurrences = 1,
                ArrayOccurrences = f.IsArray ? occurrences : 0,
                InlineArrayOccurrences = f.IsInlineArray ? occurrences : 0,
                InlineStandardOccurrences = f.IsInlineStandard ? occurrences : 0,
                StandardOccurrences = f.IsArray || f.IsInlineArray || f.IsInlineStandard ? 0 : occurrences,
                
                IsUnknownInline = f.IsUnknownInline,
                UnknownInlineOccurrences =  f.IsUnknownInline ? occurrences : 0,
                IsArray = f.IsArray,
                IsInlineArray = f.IsInlineArray,
                IsInlineStandard = f.IsInlineStandard,
                IsChained = f.IsChained,
                
                ChainedInstanceChecksum = f.ChainedInstanceChecksum,
                InlineFields = ConvertFields(f.InlineFields)?.ToList()
            };
        }

        public static STUFieldInfo[] ConvertFields(FieldGuessData[] fieldsGuess) {
            if (fieldsGuess == null) {
                return null;
            }
            STUFieldInfo[] output = new STUFieldInfo[fieldsGuess.Length];
            uint i = 0;
            foreach (FieldGuessData f in fieldsGuess) {
                output[i] = ConvertField(f);
                i++;
            }
            return output;
        }
    }
}