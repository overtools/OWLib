using STULib;
using STULib.Impl.Version2HashComparer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace STUHashTool {
    public class InstanceTally {
        public uint count;
        public Dictionary<uint, uint> fieldOccurrences;
        public Dictionary<uint, List<CompareResult>> resultDict;
        public Dictionary<uint, List<FieldResult>> fieldDict;

        public FieldResult GetField(uint before, uint after) {
            foreach (FieldResult f in fieldDict[before]) {
                if (f.afterFieldHash == after) {
                    return f;
                }
            }
            return null;
        }
    }

    public class FieldResult {
        public uint beforeFieldHash;
        public uint afterFieldHash;
        public uint count;
    }

    public class CompareResult {
        public uint beforeInstanceHash;
        public uint afterInstanceHash;
        public List<FieldCompareResult> fields;
    }

    public class FieldCompareResult {
        public uint beforeFieldHash;
        public uint afterFieldHash;
    }

    public class STUInstanceInfo {
        public List<STUFieldInfo> fields;
        public uint size;
        public uint hash;
        public uint occurrences;

        public STUFieldInfo GetField(uint hash) {
            foreach (STUFieldInfo f in fields) {
                if (f.hash == hash) {
                    return f;
                }
            }
            return null;
        }
    }

    public class STUFieldInfo {
        public uint hash;
        public uint size;
        public uint occurrences;
        public bool possible_array;
        public uint possible_array_item_size;
        public bool is_nested_standard;
        public bool is_nested_array;
        public int nested_array_occurrences;
        public int nested_standard_occurrences;
        public int possible_array_occurrences;
        public int standard_occurrences;
        public List<STUFieldInfo> nested_fields;

        public bool ContainsNestedField(uint nestedField) {
            if (!is_nested_standard && !is_nested_array) {
                return false;
            }
            foreach (STUFieldInfo f in nested_fields) {
                if (f.hash == nestedField) {
                    return true;
                }
            }
            return false;
        }

        public STUFieldInfo GetNestedField(uint nestedField) {
            if (!is_nested_standard && !is_nested_array) {
                return null;
            }
            foreach (STUFieldInfo f in nested_fields) {
                if (f.hash == nestedField) {
                    return f;
                }
            }
            return null;
        }
    }


    class Program {
        static bool ArraysEqual<T>(T[] a1, T[] a2) {
            if (ReferenceEquals(a1, a2))
                return true;

            if (a1 == null || a2 == null)
                return false;

            if (a1.Length != a2.Length)
                return false;

            EqualityComparer<T> comparer = EqualityComparer<T>.Default;
            for (int i = 0; i < a1.Length; i++) {
                if (!comparer.Equals(a1[i], a2[i])) return false;
            }
            return true;
        }

        static void PrintHelp() {
            Console.Out.WriteLine("Usage:");
            Console.Out.WriteLine("Single file: \"file {before file} {after file}\"");
            Console.Out.WriteLine("Iter files in a single directory: \"dir {before files directory} {after files directory}\"");
            Console.Out.WriteLine("List instances in a directory of files: \"list {files directory}\"");
            Console.Out.WriteLine("Auto generate instance class: \"class {files directory} {instance, \"*\" for all}\"");
        }

        static void Main(string[] args) {
            // Usage:
            // Single file: "file {before file} {after file}"
            // Iter files in a single directory: "dir {before files directory} {after files directory}"
            // List instances in a directory of files: "list {files directory}"
            // Auto generate instance class: "class {files directory} {instance, "*" for all}"

            // todo: cleanup

            if (args.Length > 1) {
                if (args[0] == "list" && args.Length < 2) {
                    PrintHelp();
                    return;
                } else if (args[0] != "list" && args.Length < 3) {
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
            string directory1 = "";
            string directory2 = "";
            string classInstance = "";
            string outputFolder = "";
            if (mode == "file") {
                directory1 = Path.GetDirectoryName(args[1]);
                directory2 = Path.GetDirectoryName(args[2]);
                files1.Add(Path.GetFileName(args[1]));
                files2.Add(Path.GetFileName(args[2]));
            } else if (mode == "dir") {
                directory1 = args[1];
                directory2 = args[2];
                foreach (string f in Directory.GetFiles(args[1], "*", SearchOption.TopDirectoryOnly)) {
                    files1.Add(Path.GetFileName(f));
                }
                foreach (string f in Directory.GetFiles(args[2], "*", SearchOption.TopDirectoryOnly)) {
                    files2.Add(Path.GetFileName(f));
                }
            } else if (mode == "class") {
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

            } else if (mode == "list") {
                directory1 = args[1];
                directory2 = args[1];
                foreach (string f in Directory.GetFiles(args[1], "*", SearchOption.TopDirectoryOnly)) {
                    files1.Add(Path.GetFileName(f));
                    files2.Add(Path.GetFileName(f));
                }
            } else if (mode == "dir-rec") {
                // todo: recurse over every type
                throw new NotImplementedException();
            }

            List<string> both = files2.Intersect(files1).ToList();
            List<CompareResult> results = new List<CompareResult>();
            Dictionary<uint, STUInstanceInfo> instances = new Dictionary<uint, STUInstanceInfo>();

            foreach (string file in both) {
                string file1 = Path.Combine(directory1, file);
                string file2 = Path.Combine(directory2, file);
                using (Stream file1Stream = File.Open(file1, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    using (Stream file2Stream = File.Open(file2, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        Version2Comparer file1STU2;
                        Version2Comparer file2STU2;
                        try {
                            ISTU file1STU = ISTU.NewInstance(file1Stream, uint.MaxValue, typeof(Version2Comparer));
                            file1STU2 = (Version2Comparer) file1STU;

                            ISTU file2STU = ISTU.NewInstance(file2Stream, uint.MaxValue, typeof(Version2Comparer));
                            file2STU2 = (Version2Comparer) file2STU;
                        }
                        catch {
                            continue;
                        }

                        foreach (STULib.Impl.Version2HashComparer.InstanceData instance1 in file1STU2.instanceDiffData) {
                            if (instance1 == null) {
                                continue;
                            }
                            if (!instances.ContainsKey(instance1.hash)) {
                                instances[instance1.hash] = new STUInstanceInfo { size = instance1.size, hash = instance1.hash, occurrences = 1, fields = new List<STUFieldInfo>() };
                                foreach (FieldData f in instance1.fields) {
                                    instances[instance1.hash].fields.Add(ConvertField(f));
                                }
                            } else {
                                instances[instance1.hash].occurrences++;
                                foreach (FieldData f in instance1.fields) {
                                    STUFieldInfo stuF = instances[instance1.hash].GetField(f.hash);
                                    if (stuF != null) {
                                        stuF.occurrences++;
                                        IncrementNestedCount(stuF, f);
                                    } else {
                                        instances[instance1.hash].fields.Add(ConvertField(f));
                                    }
                                }
                            }
                            foreach (STULib.Impl.Version2HashComparer.InstanceData instance2 in file2STU2.instanceDiffData) {
                                if (instance1 == null || instance2 == null) {
                                    continue;
                                }
                                // Console.Out.WriteLine($"Trying {instance1.hash:X}:{instance2.hash:X}");
                                if (instance1.fields.Length != instance2.fields.Length) {
                                    Debugger.Log(0, "STUHashTool", $"[STUHashTool] {file}: {instance1.hash:X8} != {instance2.hash:X8}, different field count\n");
                                    continue;
                                }

                                if (instance1.size != instance2.size) {
                                    Debugger.Log(0, "STUHashTool", $"[STUHashTool] {file}: {instance1.hash:X8} != {instance2.hash:X8}, different size\n");
                                    continue;
                                }

                                //if (file1STU2.instanceDiffData.Length != file2STU2.instanceDiffData.Length) {
                                //    Debugger.Log(0, "STUHashTool", $"[STUHashTool] {file}: {instance1.hash:X} != {instance2.hash:X}, different instance count\n");
                                //    Console.Out.WriteLine($"{instance1.hash:X} != {instance2.hash:X}, can't verify due to different instance count");
                                //    continue;
                                //}

                                if (file1STU2.instanceDiffData.Length == 1 || file2STU2.instanceDiffData.Length == 1) {
                                    Debugger.Log(0, "STUHashTool", $"[STUHashTool] {file}: {instance1.hash:X8} might be {instance2.hash:X8}, only one instance\n");
                                } else {
                                    Debugger.Log(0, "STUHashTool", $"[STUHashTool] {file}: {instance1.hash:X8} might be {instance2.hash:X8}\n");
                                }

                                results.Add(new CompareResult { beforeInstanceHash = instance1.hash, afterInstanceHash = instance2.hash, fields = new List<FieldCompareResult>() });

                                foreach (FieldData field1 in instance1.fields) {
                                    foreach (FieldData field2 in instance2.fields) {
                                        if (field1.size != field2.size) {
                                            continue;
                                        }

                                        if (ArraysEqual(field1.sha1, field2.sha1)) {
                                            Debugger.Log(0, "STUHashTool", $"[STUHashTool] {file}: {instance1.hash:X8}:{field1.hash:X8} == {instance2.hash:X8}:{field2.hash:X8}, same SHA1\n");
                                            results.Last().fields.Add(new FieldCompareResult { beforeFieldHash = field1.hash, afterFieldHash = field2.hash });
                                        }

                                        if (field1.demangle_sha1 != null || field2.demangle_sha1 != null) {
                                            if (ArraysEqual(field1.demangle_sha1, field2.demangle_sha1)) {
                                                Debugger.Log(0, "STUHashTool", $"[STUHashTool] {file}: {instance1.hash:X8}:{field1.hash:X8} == {instance2.hash:X8}:{field2.hash:X8}, same demangled SHA1\n");
                                                results.Last().fields.Add(new FieldCompareResult { beforeFieldHash = field1.hash, afterFieldHash = field2.hash });
                                            }
                                        }
                                    }
                                }
                            }

                        }
                    }
                }
            }
            Dictionary<uint, InstanceTally> instanceChangeTally = new Dictionary<uint, InstanceTally>();
            foreach (CompareResult result in results) {
                if (!instanceChangeTally.ContainsKey(result.beforeInstanceHash)) {
                    instanceChangeTally[result.beforeInstanceHash] = new InstanceTally { count = 1, resultDict = new Dictionary<uint, List<CompareResult>>(), fieldDict = new Dictionary<uint, List<FieldResult>>() };
                    instanceChangeTally[result.beforeInstanceHash].resultDict[result.afterInstanceHash] = new List<CompareResult> { result };
                    instanceChangeTally[result.beforeInstanceHash].fieldOccurrences = new Dictionary<uint, uint>();
                    foreach (FieldCompareResult d in result.fields) {
                        if (instanceChangeTally[result.beforeInstanceHash].fieldDict.ContainsKey(d.beforeFieldHash)) {
                            instanceChangeTally[result.beforeInstanceHash].fieldOccurrences[d.beforeFieldHash]++;
                            FieldResult f = instanceChangeTally[result.beforeInstanceHash].GetField(d.beforeFieldHash, d.afterFieldHash);
                            if (f != null) {
                                f.count++;
                            } else {
                                instanceChangeTally[result.beforeInstanceHash].fieldDict[d.beforeFieldHash].Add(new FieldResult { beforeFieldHash = d.beforeFieldHash, afterFieldHash = d.afterFieldHash, count = 1 });
                            }

                        } else {
                            instanceChangeTally[result.beforeInstanceHash].fieldDict[d.beforeFieldHash] = new List<FieldResult> {
                                new FieldResult { beforeFieldHash = d.beforeFieldHash, afterFieldHash = d.afterFieldHash, count = 1 }
                            };
                            instanceChangeTally[result.beforeInstanceHash].fieldOccurrences[d.beforeFieldHash] = 1;
                        }
                    }
                } else {
                    instanceChangeTally[result.beforeInstanceHash].count++;
                    if (!instanceChangeTally[result.beforeInstanceHash].resultDict.ContainsKey(result.afterInstanceHash)) {
                        instanceChangeTally[result.beforeInstanceHash].resultDict[result.afterInstanceHash] = new List<CompareResult> { };
                    }
                    instanceChangeTally[result.beforeInstanceHash].resultDict[result.afterInstanceHash].Add(result);

                    foreach (FieldCompareResult d in result.fields) {
                        if (instanceChangeTally[result.beforeInstanceHash].fieldDict.ContainsKey(d.beforeFieldHash)) {
                            instanceChangeTally[result.beforeInstanceHash].fieldOccurrences[d.beforeFieldHash]++;
                            FieldResult f = instanceChangeTally[result.beforeInstanceHash].GetField(d.beforeFieldHash, d.afterFieldHash);
                            if (f != null) {
                                f.count++;
                            } else {
                                instanceChangeTally[result.beforeInstanceHash].fieldDict[d.beforeFieldHash].Add(new FieldResult { beforeFieldHash = d.beforeFieldHash, afterFieldHash = d.afterFieldHash, count = 1 });
                            }

                        } else {
                            instanceChangeTally[result.beforeInstanceHash].fieldDict[d.beforeFieldHash] = new List<FieldResult> {
                                new FieldResult { beforeFieldHash = d.beforeFieldHash, afterFieldHash = d.afterFieldHash, count = 1 }
                            };
                            instanceChangeTally[result.beforeInstanceHash].fieldOccurrences[d.beforeFieldHash] = 1;
                        }
                    }
                }
            }

            if (mode == "list") {
                uint instanceCounter = 0;
                foreach (KeyValuePair<uint, STUInstanceInfo> instance in instances) {
                    Console.Out.WriteLine($"{instance.Key:X8}: (in {instance.Value.occurrences}/{both.Count} files)");
                    foreach (STUFieldInfo field in instance.Value.fields) {
                        Console.Out.WriteLine($"\t{field.hash:X8}: {field.size} bytes (in {field.occurrences}/{instance.Value.occurrences} instances)");
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
                        todoInstances[wildcardCount] = Convert.ToString(instance.Value.hash, 16);
                        wildcardCount++;
                    }
                } else {
                    todoInstances = classInstance.Split(':');
                }

                for (int i = 0; i < todoInstances.Length; ++i) {
                    uint todoInstance = Convert.ToUInt32(todoInstances[i], 16);
                    if (!instances.ContainsKey(todoInstance)) {
                        continue;
                    }

                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine("// File auto generated by STUHashTool");
                    sb.AppendLine("using static STULib.Types.Generic.Common;");
                    sb.AppendLine("using System.Numerics;");
                    sb.AppendLine();
                    sb.AppendLine("namespace STULib.Types {");

                    sb.AppendLine($"    [STU(0x{todoInstance:X8})]");
                    sb.Append(CreateInstanceClass($"{todoInstance:X8}", instances[todoInstance].fields.ToArray(), true));

                    sb.Append("}");

                    if (outputFolder.Length > 0) {
                        using (Stream stream = File.OpenWrite($"{outputFolder}{Path.DirectorySeparatorChar}STU_{todoInstance:X8}_{i:X8}.cs")) {
                            using (TextWriter writer = new StreamWriter(stream)) {
                                writer.WriteLine(sb);
                            }
                        }
                    } else {
                        Console.Out.WriteLine(sb);
                    }
                }

            } else {
                foreach (KeyValuePair<uint, InstanceTally> it in instanceChangeTally) {
                    foreach (KeyValuePair<uint, List<CompareResult>> id in it.Value.resultDict) {
                        double instanceProbablility = (double)id.Value.Count / it.Value.count * 100;
                        Console.Out.WriteLine($"{it.Key:X8} => {id.Key:X8} ({instanceProbablility:0.0#}% probability)");
                        foreach (KeyValuePair<uint, List<FieldResult>> field in it.Value.fieldDict) {
                            foreach (FieldResult fieldResult in field.Value) {
                                double fieldProbability = (double)fieldResult.count / it.Value.fieldOccurrences[fieldResult.beforeFieldHash] * 100;
                                Console.Out.WriteLine($"\t{fieldResult.beforeFieldHash:X8} => {fieldResult.afterFieldHash:X8} ({fieldProbability:0.0#}% probability)");
                            }
                        }
                    }
                }
            }
            Debugger.Break();
        }

        public static string GetSizeType(uint size, bool isArray, out string commentString) {
            commentString = "";
            if (isArray) {
                commentString = "  // todo: proper array type";
                return "object";
            }
            switch (size) {
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
            return null;
        }

        public static string CreateInstanceClass(string instanceName, STUFieldInfo[] fields, bool inherit = false, uint indentLevel = 1) {
            StringBuilder sb = new StringBuilder();
            StringBuilder classSb = new StringBuilder();  // things to be written later
            string indentString = String.Concat(Enumerable.Repeat("    ", (int)indentLevel));
            string fieldIndentString = String.Concat(Enumerable.Repeat("    ", (int)indentLevel + 1));
            uint fieldCounter = 1;
            if (inherit) {
                sb.AppendLine($"{indentString}public class STU_{instanceName} : STUInstance {{");
            } else {
                sb.AppendLine($"{indentString}public class STU_{instanceName} {{");
            }

            foreach (STUFieldInfo field in fields) {
                string typeString;
                string typeComment;
                string fieldType = "unknown";

                if (field.nested_array_occurrences == field.occurrences) {  // must pass every time, I think this is a pretty safe assumption
                    fieldType = "nest_a";
                } else if (field.nested_standard_occurrences == field.occurrences) {
                    fieldType = "nest_s";
                } else if (field.possible_array_occurrences == field.occurrences && field.size == 4) {  // should always be 4 for arrays (?)
                    fieldType = "array";
                } else {
                    fieldType = "normal";
                }

                // Console.Out.WriteLine($"{field.hash:X8}: {fieldType}");
                switch (fieldType) {
                    case "unknown":
                        return "";
                    case "nest_s":
                        classSb.AppendLine();
                        classSb.AppendLine(CreateInstanceClass(instanceName + $"_{field.hash:X8}", field.nested_fields.ToArray(), false, indentLevel + 1));
                        sb.AppendLine($"{fieldIndentString}[STUField(0x{field.hash:X8})]");
                        sb.AppendLine($"{fieldIndentString}public STU_{instanceName}_{field.hash:X8} Unknown{fieldCounter};  // todo: check nested");
                        break;
                    case "nest_a":
                        classSb.AppendLine();
                        classSb.AppendLine(CreateInstanceClass(instanceName + $"_{field.hash:X8}", field.nested_fields.ToArray(), false, indentLevel + 1));
                        sb.AppendLine($"{fieldIndentString}[STUField(0x{field.hash:X8})]");
                        sb.AppendLine($"{fieldIndentString}public STU_{instanceName}_{field.hash:X8}[] Unknown{fieldCounter};  // todo: check nested array");
                        break;
                    case "array":
                        typeString = GetSizeType(field.possible_array_item_size, true, out typeComment);
                        // if (typeString != null) {
                        sb.AppendLine($"{fieldIndentString}[STUField(0x{field.hash:X8})]");
                        sb.AppendLine($"{fieldIndentString}public {typeString}[] Unknown{fieldCounter};{typeComment}");
                        //} else {
                        //    sb.AppendLine($"{fieldIndentString}//[STUField(0x{field.hash:X8})]  // unhandled array item size: {field.possible_array_item_size}");
                        //}
                        break;
                    case "normal":
                        typeString = GetSizeType(field.size, false, out typeComment);
                        if (typeString != null) {
                            sb.AppendLine($"{fieldIndentString}[STUField(0x{field.hash:X8})]");
                            sb.AppendLine($"{fieldIndentString}public {typeString} Unknown{fieldCounter};{typeComment}");
                        } else {
                            sb.AppendLine($"{fieldIndentString}//[STUField(0x{field.hash:X8})]  // unhandled field size: {field.size}");
                        }
                        break;
                }

                if (fieldCounter != fields.Length) {
                    sb.AppendLine();
                }
                fieldCounter++;
            }

            sb.Append(classSb.ToString());

            if (inherit) {
                sb.AppendLine($"{indentString}}}");
            } else {
                sb.Append($"{indentString}}}");
            }

            return sb.ToString();
        }

        public static void IncrementNestedCount(STUFieldInfo f, FieldData f2) {
            bool incr = false;
            if (f2.is_nested_array) {
                f.nested_array_occurrences++;
                incr = true;
            }
            if (f2.is_nested_standard) {
                f.nested_standard_occurrences++;
                incr = true;
            }
            if (f2.possible_array) {
                f.possible_array_occurrences++;
                if (f2.possible_array_item_size < f.possible_array_item_size) {
                    f.possible_array_item_size = f2.possible_array_item_size;
                }
                incr = true;
            }

            if (!incr) {
                f.standard_occurrences++;
            }
            if (f2.nested_fields != null) {
                foreach (FieldData nest_f in f2.nested_fields) {
                    if (f.ContainsNestedField(nest_f.hash)) {
                        STUFieldInfo got_f = f.GetNestedField(nest_f.hash);
                        got_f.occurrences++;
                        IncrementNestedCount(got_f, nest_f);
                    } else {
                        if (f.nested_fields == null) {
                            f.nested_fields = ConvertFields(f2.nested_fields)?.ToList();
                        } else {
                            f.nested_fields.Add(ConvertField(nest_f));
                        }
                    }
                }
            }
        }

        public static STUFieldInfo ConvertField(FieldData f) {
            return new STUFieldInfo {
                size = f.size,
                hash = f.hash,
                occurrences = 1,
                possible_array = f.possible_array,
                possible_array_item_size = f.possible_array_item_size,
                is_nested_array = f.is_nested_array,
                is_nested_standard = f.is_nested_standard,
                nested_fields = ConvertFields(f.nested_fields)?.ToList(),
                possible_array_occurrences = f.possible_array ? 1 : 0,
                nested_array_occurrences = f.is_nested_array ? 1 : 0,
                nested_standard_occurrences = f.is_nested_standard ? 1 : 0,
                standard_occurrences = (f.possible_array || f.is_nested_array || f.is_nested_standard) ? 0 : 1
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