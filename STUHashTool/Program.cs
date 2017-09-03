using STULib;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using CASCExplorer;
using OverTool;
using STULib.Impl;
using STULib.Impl.Version2HashComparer;

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
        public uint Size;
        public uint Hash;
        public uint Count;

        public STUFieldInfo GetField(uint hash) {
            return Fields.FirstOrDefault(f => f.Hash == hash);
        }
    }

    public class STUFieldInfo {
        public uint Hash;
        public uint Size;
        public uint Occurrences;
        public bool PossibleArray;
        public uint PossibleArrayItemSize;
        public bool IsNestedStandard;
        public bool IsNestedArray;
        
        public int NestedArrayOccurrences;
        public int NestedStandardOccurrences;
        public int PossibleArrayOccurrences;
        public int StandardOccurrences;
        public List<STUFieldInfo> NestedFields;

        public bool ContainsNestedField(uint nestedField) {
            if (!IsNestedStandard && !IsNestedArray) return false;
            return NestedFields.Any(f => f.Hash == nestedField);
        }

        public STUFieldInfo GetNestedField(uint nestedField) {
            if (!IsNestedStandard && !IsNestedArray) return null;
            return NestedFields.FirstOrDefault(f => f.Hash == nestedField);
        }

        public bool Equals(STUFieldInfo obj) {
            bool firstPass = obj.Hash == Hash && obj.PossibleArrayItemSize == PossibleArrayItemSize &&
                             obj.IsNestedArray == IsNestedArray
                             && obj.IsNestedStandard == IsNestedStandard && obj.PossibleArray == PossibleArray;
            if (!firstPass) return false;
            if (!IsNestedStandard && !IsNestedArray) return true;
            if (obj.NestedFields.Count != NestedFields.Count) return false;
            foreach (STUFieldInfo objF in obj.NestedFields)
                if (ContainsNestedField(objF.Hash)) {
                    STUFieldInfo gotF = GetNestedField(objF.Hash);
                    if (!objF.Equals(gotF)) return false;
                } else {
                    return false;
                }
            return true;
        }
    }

    internal class Program {
        // ReSharper disable once SuggestBaseTypeForParameter
        private static bool ArraysEqual<T>(T[] a1, T[] a2) {
            if (ReferenceEquals(a1, a2))
                return true;

            if (a1 == null || a2 == null)
                return false;

            if (a1.Length != a2.Length)
                return false;

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            return !a1.Where((t, i) => !comparer.Equals(t, a2[i])).Any();
        }

        private static void PrintHelp() {
            Console.Out.WriteLine("Usage:");
            Console.Out.WriteLine("Single file: \"file {before file} {after file}\"");
            Console.Out.WriteLine(
                "Iter files in a single directory: \"dir {before files directory} {after files directory}\"");
            Console.Out.WriteLine("List instances in a directory of files: \"list {files directory}\"");
            Console.Out.WriteLine(
                "Auto generate instance class: \"class {files directory} {instance, \"*\" for all}\"");
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
                if (args[0] != "list" && args.Length < 3) {
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
                    classInstance = args[2];
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
            Dictionary<uint, STUInstanceInfo> instances = new Dictionary<uint, STUInstanceInfo>();

            foreach (string file in both) {
                if (directory1 == null || directory2 == null) {
                    break;
                }
                string file1 = Path.Combine(directory1, file);
                string file2 = Path.Combine(directory2, file);
                if (mode != "class") {
                    Console.Out.WriteLine(file1);
                }
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
                        }

                        foreach (STULib.Impl.Version2HashComparer.InstanceData instance1 in file1STU2.InstanceDiffData
                        ) {
                            if (instance1 == null) {
                                continue;
                            }
                            if (!instances.ContainsKey(instance1.Hash)) {
                                instances[instance1.Hash] = new STUInstanceInfo {
                                    Size = instance1.Size,
                                    Hash = instance1.Hash,
                                    Count = 1,
                                    Fields = new List<STUFieldInfo>()
                                };
                                foreach (FieldData f in instance1.Fields) {
                                    instances[instance1.Hash].Fields.Add(ConvertField(f));
                                }
                            } else {
                                instances[instance1.Hash].Count++;
                                foreach (FieldData f in instance1.Fields) {
                                    STUFieldInfo stuF = instances[instance1.Hash].GetField(f.Hash);
                                    if (stuF != null) {
                                        stuF.Occurrences++;
                                        IncrementNestedCount(stuF, f);
                                    } else {
                                        instances[instance1.Hash].Fields.Add(ConvertField(f));
                                    }
                                }
                            }
                            if (mode == "class") {
                                continue;
                            }
                            foreach (STULib.Impl.Version2HashComparer.InstanceData instance2 in file2STU2
                                .InstanceDiffData) {
                                if (instance1 == null || instance2 == null) {
                                    continue;
                                }
                                // Console.Out.WriteLine($"Trying {instance1.hash:X}:{instance2.hash:X}");
                                if (instance1.Fields.Length != instance2.Fields.Length) {
                                    Debugger.Log(0, "STUHashTool",
                                        $"[STUHashTool] {file}: {instance1.Hash:X8} != {instance2.Hash:X8}, " +
                                        "different field count\n");
                                    continue;
                                }

                                if (instance1.Size != instance2.Size) {
                                    Debugger.Log(0, "STUHashTool",
                                        $"[STUHashTool] {file}: {instance1.Hash:X8} != {instance2.Hash:X8}, " +
                                        "different size\n");
                                    continue;
                                }

                                //if (file1STU2.instanceDiffData.Length != file2STU2.instanceDiffData.Length) {
                                //    Debugger.Log(0, "STUHashTool", $"[STUHashTool] {file}: {instance1.hash:X} != {instance2.hash:X}, different instance count\n");
                                //    Console.Out.WriteLine($"{instance1.hash:X} != {instance2.hash:X}, can't verify due to different instance count");
                                //    continue;
                                //}

                                if (file1STU2.InstanceDiffData.Length == 1 || file2STU2.InstanceDiffData.Length == 1) {
                                    Debugger.Log(0, "STUHashTool",
                                        $"[STUHashTool] {file}: {instance1.Hash:X8} might be {instance2.Hash:X8}, " +
                                        "only one instance\n");
                                } else {
                                    Debugger.Log(0, "STUHashTool",
                                        $"[STUHashTool] {file}: {instance1.Hash:X8} might be {instance2.Hash:X8}\n");
                                }

                                results.Add(new CompareResult {
                                    BeforeInstanceHash = instance1.Hash,
                                    AfterInstanceHash = instance2.Hash,
                                    Fields = new List<FieldCompareResult>()
                                });

                                foreach (FieldData field1 in instance1.Fields) {
                                    foreach (FieldData field2 in instance2.Fields) {
                                        if (field1.Size != field2.Size) {
                                            continue;
                                        }

                                        if (ArraysEqual(field1.Sha1, field2.Sha1)) {
                                            Debugger.Log(0, "STUHashTool",
                                                $"[STUHashTool] {file}: {instance1.Hash:X8}:{field1.Hash:X8} == " +
                                                $"{instance2.Hash:X8}:{field2.Hash:X8}, same SHA1\n");
                                            results.Last().Fields.Add(new FieldCompareResult {
                                                BeforeFieldHash = field1.Hash,
                                                AfterFieldHash = field2.Hash
                                            });
                                        }

                                        if (field1.DemangleSha1 == null && field2.DemangleSha1 == null) continue;
                                        if (!ArraysEqual(field1.DemangleSha1, field2.DemangleSha1)) continue;
                                        Debugger.Log(0, "STUHashTool",
                                            $"[STUHashTool] {file}: {instance1.Hash:X8}:{field1.Hash:X8} == " +
                                            $"{instance2.Hash:X8}:{field2.Hash:X8}, same demangled SHA1\n");
                                        results.Last().Fields.Add(new FieldCompareResult {
                                            BeforeFieldHash = field1.Hash,
                                            AfterFieldHash = field2.Hash
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
                foreach (KeyValuePair<uint, STUInstanceInfo> instance in instances) {
                    Console.Out.WriteLine($"{instance.Key:X8}: (in {instance.Value.Count}/{both.Count} files)");
                    foreach (STUFieldInfo field in instance.Value.Fields) {
                        Console.Out.WriteLine(
                            $"\t{field.Hash:X8}: {field.Size} bytes (in {field.Occurrences}/" +
                            $"{instance.Value.Count} instances)");
                    }
                    instanceCounter++;
                    if (instanceCounter != instances.Count) {
                        Console.Out.WriteLine();
                    }
                }
            } else if (mode == "class") {
                string[] todoInstances;
                if (classInstance == "*") {
                    uint wildcardCount = 0;
                    todoInstances = new string[instances.Count];
                    foreach (KeyValuePair<uint, STUInstanceInfo> instance in instances) {
                        todoInstances[wildcardCount] = Convert.ToString(instance.Value.Hash, 16);
                        wildcardCount++;
                    }
                } else {
                    todoInstances = classInstance.Split(':');
                }

                foreach (string t in todoInstances) {
                    uint todoInstance = Convert.ToUInt32(t, 16);
                    if (!instances.ContainsKey(todoInstance)) {
                        continue;
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("// File auto generated by STUHashTool");
                    sb.AppendLine("using static STULib.Types.Generic.Common;");
                    sb.AppendLine();
                    sb.AppendLine("namespace STULib.Types {");

                    sb.AppendLine($"    [STU(0x{todoInstance:X8})]");
                    sb.Append(CreateInstanceClass($"{todoInstance:X8}", instances[todoInstance].Fields.ToArray(),
                        true));

                    sb.Append("}");

                    if (outputFolder.Length > 0) {
                        using (Stream stream =
                            File.OpenWrite($"{outputFolder}{Path.DirectorySeparatorChar}STU_{todoInstance:X8}.cs")) {
                            using (TextWriter writer = new StreamWriter(stream)) {
                                writer.WriteLine(sb);
                            }
                        }
                    } else {
                        Console.Out.WriteLine(sb);
                    }
                }
            } else if (mode == "test") {
                ushort fileShort = ushort.Parse(testFileType, System.Globalization.NumberStyles.HexNumber);
                    
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
            } else {
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
                Debugger.Break();
            }
        }

        public static string GetSizeType(uint size, bool isArray, out string commentString) {
            commentString = "";
            if (isArray) {
                commentString = "  // todo: proper array type";
                return "object";
            }
            switch (size) {
                default:
                    return null;
                case 16:
                    return "STUVec4";
                case 12:
                    return "STUVec3";
                case 8:
                    commentString = "  //todo: check if STUGUID";
                    return "ulong";
                case 4:
                    return "uint";
                case 2:
                    return "ushort";
                case 1:
                    commentString = "  //todo: check if char";
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

        public static string NestedDone(STUFieldInfo[] fields, Dictionary<string, STUFieldInfo[]> doneNests) {
            return (from done in doneNests where ComprareFields(done.Value, fields) select done.Key).FirstOrDefault();
        }

        public static string CreateInstanceClass(string instanceName, STUFieldInfo[] fields, bool inherit = false, 
            uint indentLevel = 1) {
            StringBuilder sb = new StringBuilder();
            StringBuilder classSb = new StringBuilder(); // things to be written later
            string indentString = string.Concat(Enumerable.Repeat("    ", (int) indentLevel));
            string fieldIndentString = string.Concat(Enumerable.Repeat("    ", (int) indentLevel + 1));
            uint fieldCounter = 1;
            uint nestedCounter = 1;
            Dictionary<string, STUFieldInfo[]> doneNests = new Dictionary<string, STUFieldInfo[]>();

            sb.AppendLine(inherit
                ? $"{indentString}public class STU_{instanceName} : STUInstance {{"
                : $"{indentString}public class STU_{instanceName} {{");

            foreach (STUFieldInfo field in fields) {
                string typeString;
                string typeComment;
                string nestName;
                string fieldType;

                if (field.NestedArrayOccurrences == field.Occurrences) {
                    // must pass every time, I think this is a pretty safe assumption
                    fieldType = "nest_a";
                } else if (field.NestedStandardOccurrences == field.Occurrences) {
                    fieldType = "nest_s";
                } else if (field.PossibleArrayOccurrences == field.Occurrences && field.Size == 4) {
                    // should always be 4 for arrays (?)
                    fieldType = "array";
                } else {
                    fieldType = "normal";
                }

                // Console.Out.WriteLine($"{field.hash:X8}: {fieldType}");
                switch (fieldType) {
                    default:
                        continue;
                    case "nest_s":
                        nestName = NestedDone(field.NestedFields.ToArray(), doneNests);
                        if (nestName == null) {
                            nestName = $"{instanceName}_UnknownNested{nestedCounter}";
                            doneNests[nestName] = field.NestedFields.ToArray();
                            classSb.AppendLine();
                            classSb.AppendLine(CreateInstanceClass(nestName, field.NestedFields.ToArray(), false, 
                                indentLevel + 1));
                            nestedCounter++;
                        }
                        sb.AppendLine($"{fieldIndentString}[STUField(0x{field.Hash:X8})]");
                        sb.AppendLine(
                            $"{fieldIndentString}public STU_{nestName} Unknown{fieldCounter};  // todo: check nested");
                        break;
                    case "nest_a":
                        nestName = NestedDone(field.NestedFields.ToArray(), doneNests);
                        if (nestName == null) {
                            nestName = $"{instanceName}_UnknownNested{nestedCounter}";
                            doneNests[nestName] = field.NestedFields.ToArray();
                            classSb.AppendLine();
                            classSb.AppendLine(CreateInstanceClass(nestName, field.NestedFields.ToArray(), false, 
                                indentLevel + 1));
                            nestedCounter++;
                        }
                        sb.AppendLine($"{fieldIndentString}[STUField(0x{field.Hash:X8})]");
                        sb.AppendLine(
                            $"{fieldIndentString}public STU_{nestName}[] Unknown{fieldCounter};  // todo: check " +
                            "nested array");
                        break;
                    case "array":
                        typeString = GetSizeType(field.PossibleArrayItemSize, true, out typeComment);
                        sb.AppendLine($"{fieldIndentString}[STUField(0x{field.Hash:X8})]");
                        sb.AppendLine($"{fieldIndentString}public {typeString}[] Unknown{fieldCounter};{typeComment}");
                        break;
                    case "normal":
                        typeString = GetSizeType(field.Size, false, out typeComment);
                        if (typeString != null) {
                            sb.AppendLine($"{fieldIndentString}[STUField(0x{field.Hash:X8})]");
                            sb.AppendLine(
                                $"{fieldIndentString}public {typeString} Unknown{fieldCounter};{typeComment}");
                        } else {
                            sb.AppendLine(
                                $"{fieldIndentString}//[STUField(0x{field.Hash:X8})]  // unhandled field size: " +
                                $"{field.Size}");
                        }
                        break;
                }

                if (fieldCounter != fields.Length) {
                    sb.AppendLine();
                }
                fieldCounter++;
            }

            sb.Append(classSb);

            if (inherit) {
                sb.AppendLine($"{indentString}}}");
            } else {
                sb.Append($"{indentString}}}");
            }

            return sb.ToString();
        }

        public static void IncrementNestedCount(STUFieldInfo f, FieldData f2) {
            bool incr = false;
            if (f2.IsNestedArray) {
                f.NestedArrayOccurrences++;
                f.IsNestedArray = true;
                incr = true;
            }
            if (f2.IsNestedStandard) {
                f.NestedStandardOccurrences++;
                f.IsNestedStandard = true;
                incr = true;
            }
            if (f2.PossibleArray) {
                f.PossibleArrayOccurrences++;
                f.PossibleArray = true;
                if (f2.PossibleArrayItemSize < f.PossibleArrayItemSize) {
                    f.PossibleArrayItemSize = f2.PossibleArrayItemSize;
                }
                incr = true;
            }

            if (!incr) {
                f.StandardOccurrences++;
            }
            if (f2.NestedFields == null) return;
            foreach (FieldData nestF in f2.NestedFields) {
                if (f.ContainsNestedField(nestF.Hash)) {
                    STUFieldInfo gotF = f.GetNestedField(nestF.Hash);
                    gotF.Occurrences++;
                    IncrementNestedCount(gotF, nestF);
                } else {
                    if (f.NestedFields == null) {
                        f.NestedFields = ConvertFields(f2.NestedFields)?.ToList();
                    } else {
                        f.NestedFields.Add(ConvertField(nestF));
                    }
                }
            }
        }

        public static STUFieldInfo ConvertField(FieldData f) {
            return new STUFieldInfo {
                Size = f.Size,
                Hash = f.Hash,
                Occurrences = 1,
                PossibleArray = f.PossibleArray,
                PossibleArrayItemSize = f.PossibleArrayItemSize,
                IsNestedArray = f.IsNestedArray,
                IsNestedStandard = f.IsNestedStandard,
                NestedFields = ConvertFields(f.NestedFields)?.ToList(),
                PossibleArrayOccurrences = f.PossibleArray ? 1 : 0,
                NestedArrayOccurrences = f.IsNestedArray ? 1 : 0,
                NestedStandardOccurrences = f.IsNestedStandard ? 1 : 0,
                StandardOccurrences = f.PossibleArray || f.IsNestedArray || f.IsNestedStandard ? 0 : 1
            };
        }

        public static STUFieldInfo[] ConvertFields(FieldData[] fields) {
            if (fields == null) {
                return null;
            }
            STUFieldInfo[] output = new STUFieldInfo[fields.Length];
            uint i = 0;
            foreach (FieldData f in fields) {
                output[i] = ConvertField(f);
                i++;
            }
            return output;
        }
    }
}