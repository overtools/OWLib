using STULib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using CASCLib;
using Newtonsoft.Json.Linq;
using OverTool;
using STULib.Impl;
using STULib.Impl.Version2HashComparer;
using InstanceData = STULib.Impl.Version2HashComparer.InstanceData;

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

        public STUFieldInfo Copy(uint occurrences=1) {
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

    internal class Program {
        // ReSharper disable once SuggestBaseTypeForParameter
        
        public static Dictionary<uint, STUInstanceInfo> Instances = new Dictionary<uint, STUInstanceInfo>();
        public static Dictionary<uint, InstanceData> RealInstances = new Dictionary<uint, InstanceData>();
        public static Dictionary<uint, STUInstanceJSON> InstanceJSON = new Dictionary<uint, STUInstanceJSON>();

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
                    JObject stuTypesJson = JObject.Parse(File.ReadAllText("RegisteredSTUTypes.json"));
                    foreach (KeyValuePair<string,JToken> pair in stuTypesJson) {
                        uint checksum = uint.Parse(pair.Key.Split('_')[1], NumberStyles.HexNumber);
                        InstanceJSON[checksum] = new STUInstanceJSON {
                            Fields = null,
                            Hash = checksum,
                            Parent = (string)pair.Value["parent"],
                            Name = pair.Key
                        };
                        if (pair.Value["fields"] == null) continue;
                        InstanceJSON[checksum].Fields = new STUInstanceJSON.STUFieldJSON[pair.Value["fields"].Count()];
                        uint fieldCounter = 0;
                        foreach (JToken field in pair.Value["fields"]) {
                            InstanceJSON[checksum].Fields[fieldCounter] = new STUInstanceJSON.STUFieldJSON {
                                Hash = uint.Parse((string)field["name"], NumberStyles.HexNumber),
                                Name = (string)field["name"],
                                SerializationType = (int)field["serializationType"],
                                Size = field.Value<int>("size"),
                                Type = field.Value<string>("type")
                            };
                            fieldCounter++;
                        }
                        
                    }
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
                case "gendata":
                    //directory1 = null;
                    //directory2 = null;
                    throw new NotImplementedException();
                    //break;
                case "dir-rec":
                    // todo: recurse over every type
                    throw new NotImplementedException();
            }

            List<string> both = files2.Intersect(files1).ToList();
            List<CompareResult> results = new List<CompareResult>();

            foreach (string file in both) {
                if (directory1 == null || directory2 == null) {
                    break;
                }
                string file1 = Path.Combine(directory1, file);
                string file2 = Path.Combine(directory2, file);
                Debugger.Log(0, "STUHashTool", $"[STUHashTool]: Loading file: {file1}\n");
                using (Stream file1Stream = File.Open(file1, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    using (Stream file2Stream = File.Open(file2, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        ISTU file1STU = ISTU.NewInstance(file1Stream, uint.MaxValue, typeof(Version2Comparer));
                        Version2Comparer file1STU2 = (Version2Comparer) file1STU;

                        Version2Comparer file2STU2;
                        if (mode == "class" || mode == "test") {
                            file2STU2 = file1STU2;
                        } else {
                            ISTU file2STU = ISTU.NewInstance(file2Stream, uint.MaxValue, typeof(Version2Comparer));
                            file2STU2 = (Version2Comparer) file2STU;
                            // if (mode == "compare-debug") {
                            //     continue;
                            // }
                        }

                        foreach (InstanceData instanceData in file1STU2.InstanceData) {
                            if (instanceData == null) continue;
                            if (!RealInstances.ContainsKey(instanceData.Checksum)) {
                                RealInstances[instanceData.Checksum] = instanceData;
                            }
                            FindInternalInstances(instanceData, file1STU2.InternalInstances);
                        }

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
                
                foreach (string t in todoInstances) {
                    uint todoInstance = Convert.ToUInt32(t, 16);
                    if (!RealInstances.ContainsKey(todoInstance)) {
                        continue;
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("// File auto generated by STUHashTool");
                    sb.AppendLine("using static STULib.Types.Generic.Common;");
                    sb.AppendLine();
                    sb.AppendLine("namespace STULib.Types {");

                    sb.AppendLine($"    [STU(0x{todoInstance:X8})]");
                    sb.Append(CreateInstanceClass(RealInstances[todoInstance]));

                    sb.Append("}");

                    if (outputFolder.Length > 0 && !ISTU.InstanceTypes.ContainsKey(RealInstances[todoInstance].Checksum)) {
                        using (Stream stream =
                            File.OpenWrite($"{outputFolder}{Path.DirectorySeparatorChar}STU_{todoInstance:X8}.cs")) {
                            using (TextWriter writer = new StreamWriter(stream)) {
                                writer.WriteLine(sb);
                            }
                        }
                    }
                    //} else {
                    Console.Out.WriteLine(sb);
                    //}
                }

                foreach (uint todoEnum in TodoEnums) {
                    string @enum = new EnumBuilder(Enums[todoEnum]).Build(EnumNames);
                    Console.Out.WriteLine(@enum);
                    if (outputFolder.Length <= 0 || ISTU.EnumTypes.ContainsKey(todoEnum)) continue;
                    using (Stream stream =
                        File.OpenWrite($"{outputFolder}{Path.DirectorySeparatorChar}Enums{Path.DirectorySeparatorChar}STUEnum_{todoEnum:X8}.cs")) {
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
                    using (Stream fileStream = Util.OpenFile(records[file], handler)) {
                        ISTU fileSTU = ISTU.NewInstance(fileStream, uint.MaxValue);
                        Utils.DumpSTUFull((Version2) fileSTU, handler, records, 
                            testinstanceWildcard == "*" ? null: testinstanceWildcard);
                    }
                }
            } else if (mode != "compare-debug"){
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
                        FindNestedTodo(RealInstances[field.InlineInstanceChecksum],
                            newTodoInstances.ToArray());
                    }
                }
                if (field.IsEmbed || field.IsEmbed) {
                    if (!newTodoInstances.Contains(Hex(field.EmbedInstanceChecksum)) && !ISTU.InstanceTypes.ContainsKey(field.InlineInstanceChecksum)) {
                        newTodoInstances.Add(Hex(field.EmbedInstanceChecksum));
                        FindNestedTodo(RealInstances[field.EmbedInstanceChecksum],
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

        public static string GetType(FieldData field) {
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
                case "teVec3a":
                    return "STUVec3A";
                case "teVec4":
                    return "STUVec4";
                case "teEntityID":
                    return "STUEntityID";
                case "teColorRGB":
                    return "STUColorRGB";
                case "teColorRGBA":
                    return "STUColorRGBA";
                case "ARRAY FILE REFERENCE":
                case "File Reference":
                    return "STUGUID";
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

        public static string CreateInstanceClass(InstanceData instance, uint indentLevel = 1) {
            StringBuilder sb = new StringBuilder();
            StringBuilder classSb = new StringBuilder(); // things to be written later
            string indentString = string.Concat(Enumerable.Repeat("    ", (int) indentLevel));
            string fieldIndentString = string.Concat(Enumerable.Repeat("    ", (int) indentLevel + 1));
            string instanceName = $"STU_{instance.Checksum:X8}";
            string parentName = $"STU_{instance.ParentChecksum:X8}";
            if (InstanceNames.ContainsKey(instance.Checksum)) {
                instanceName = InstanceNames[instance.Checksum];
            }
            if (instance.ParentChecksum != 0 && InstanceNames.ContainsKey(instance.ParentChecksum)) {
                parentName = InstanceNames[instance.ParentChecksum];
            }
            uint fieldCounter = 1;

            sb.AppendLine(instance.ParentType == null
                ? $"{indentString}public class {instanceName} : STUInstance {{"
                : $"{indentString}public class {instanceName} : {parentName} {{");

            foreach (FieldData field in instance.Fields) {
                string type = GetType(field);
                string fieldName = $"m_{field.Checksum:X8}";
                string fieldDefinition = $"[STUField(0x{field.Checksum:X8})]";
                if (FieldNames.ContainsKey(field.Checksum)) {
                    fieldName = FieldNames[field.Checksum];
                    fieldDefinition = $"[STUField(0x{field.Checksum:X8}, \"{fieldName}\")]";
                }
                if (field.SerializationType == 12 || field.SerializationType == 13) {
                    type = "STUGUID";
                }
                if (field.IsInline || field.IsEmbed || field.IsEmbedArray || field.IsInlineArray) {  //  
                    string instanceType = ISTU.InstanceTypes.ContainsKey(field.TypeInstanceChecksum) ? 
                        ISTU.InstanceTypes[field.TypeInstanceChecksum].FullName : $"STU_{field.TypeInstanceChecksum:X8}";
                    if (InstanceNames.ContainsKey(field.TypeInstanceChecksum)) {
                        instanceType = InstanceNames[field.TypeInstanceChecksum];
                    }
                    sb.AppendLine($"{fieldIndentString}{fieldDefinition}");
                    sb.AppendLine($"{fieldIndentString}public {instanceType}{(field.IsEmbedArray||field.IsInlineArray ? "[]" : "")} {fieldName};");
                } else if (type == null && !field.IsEnum && !field.IsEnumArray) {
                    Debugger.Log(0, "STUHashTool", $"[STUHashTool:class] Unhandled type: \"{field.Type}\" (st: {field.SerializationType})\n");
                    sb.AppendLine($"{fieldIndentString}//{fieldDefinition}");
                    sb.AppendLine($"{fieldIndentString}//public object {fieldName};  // todo: unhandled type: {field.Type} (st: {field.SerializationType})");
                } else if (field.IsPrimitive || field.IsGUID || field.IsGUIDOther) {
                    sb.AppendLine($"{fieldIndentString}{fieldDefinition}");
                    sb.AppendLine($"{fieldIndentString}public {type} {fieldName};");
                } else if (field.IsPrimitiveArray || field.IsGUIDArray || field.IsGUIDOtherArray) {
                    sb.AppendLine($"{fieldIndentString}{fieldDefinition}");
                    sb.AppendLine($"{fieldIndentString}public {type}[] {fieldName};");
                } else if (field.IsEnum || field.IsEnumArray) {
                    string enumName = $"Enums.STUEnum_{field.EnumChecksum:X8}";
                    if (EnumNames.ContainsKey(field.EnumChecksum)) {
                        enumName = $"Enums.{EnumNames[field.EnumChecksum]}";
                    }
                    if (ISTU.EnumTypes.ContainsKey(field.EnumChecksum)) {
                        enumName = ISTU.EnumTypes[field.EnumChecksum].FullName;
                    }
                    sb.AppendLine($"{fieldIndentString}{fieldDefinition}");
                    sb.AppendLine($"{fieldIndentString}public {enumName}{(field.IsEnumArray ? "[]" : "")} {fieldName};");
                } else {
                    Debugger.Log(0, "STUHashTool",
                        $"[STUHashTool:class]: Unhandled Serialization type {field.SerializationType} of field {instance.Checksum:X8}:{field.Checksum:X8}\n");
                }

                if (fieldCounter != instance.Fields.Length) {
                    sb.AppendLine();
                }
                fieldCounter++;
            }

            sb.Append(classSb);

            //if (inherit) {
            //    sb.AppendLine($"{indentString}}}");
            //} else {
            sb.AppendLine($"{indentString}}}");
            //}

            return sb.ToString();
        }

        public static void FindInternalInstances(InstanceData instance, Dictionary<uint, InstanceData> internalInstances) {
            if (instance.ParentType != null) {
                RealInstances[instance.ParentChecksum] = internalInstances[instance.ParentChecksum];
                FindInternalInstances(internalInstances[instance.ParentChecksum], internalInstances);
            }
            foreach (FieldData field in instance.Fields) {
                if (field.IsEmbed || field.IsEmbedArray) {
                    RealInstances[field.EmbedInstanceChecksum] = internalInstances[field.EmbedInstanceChecksum];
                    FindInternalInstances(internalInstances[field.EmbedInstanceChecksum], internalInstances);
                }
                if (field.IsInline || field.IsInlineArray) {
                    RealInstances[field.InlineInstanceChecksum] = internalInstances[field.InlineInstanceChecksum];
                    FindInternalInstances(internalInstances[field.InlineInstanceChecksum], internalInstances);
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