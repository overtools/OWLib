using STULib;
using STULib.Impl.Version2HashComparer;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace STUHashTool {
    public class InstanceTally {
        public uint count;
        public Dictionary<uint, uint> fieldOccurrences;
        public Dictionary<uint, List<CompareResult>> resultDict;
        public Dictionary<uint, List<FieldResult>> fieldDict;

        public FieldResult getField(uint before, uint after) {
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
    class Program {
        private static ISTU file1STU;
        private static ISTU file2STU;

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

        static void Main(string[] args) {
            // Usage:
            // Single file: "file {before file} {after file}"
            // Iter files in a single directory: "dir {before files directory} {after files directory}"

            // todo: cleanup

            if (args.Length < 3) {
                Console.Out.WriteLine("Usage:");
                Console.Out.WriteLine("Single file: \"file {before file} {after file}\"");
                Console.Out.WriteLine("Iter files in a single directory: \"dir {before files directory} {after files directory}\"");
                return;
            }
            List<string> files1 = new List<string>();
            List<string> files2 = new List<string>();
            string directory1 = "";
            string directory2 = "";
            string mode = args[0];
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
            } else if (mode == "dir-rec") {
                // todo: recurse over every type
                throw new NotImplementedException();
            }

            List<string> both = files2.Intersect(files1).ToList();
            List<CompareResult> results = new List<CompareResult>();

            foreach (string file in both) {
                string file1 = Path.Combine(directory1, file);
                string file2 = Path.Combine(directory2, file);
                using (Stream file1Stream = File.Open(file1, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    using (Stream file2Stream = File.Open(file2, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                        file1STU = ISTU.NewInstance(file1Stream, uint.MaxValue, typeof(Version2Comparer));
                        Version2Comparer file1STU2 = (Version2Comparer)file1STU;

                        file2STU = ISTU.NewInstance(file2Stream, uint.MaxValue, typeof(Version2Comparer));
                        Version2Comparer file2STU2 = (Version2Comparer)file2STU;

                        foreach (STULib.Impl.Version2HashComparer.InstanceData instance1 in file1STU2.instanceDiffData) {
                            foreach (STULib.Impl.Version2HashComparer.InstanceData instance2 in file2STU2.instanceDiffData) {
                                // Console.Out.WriteLine($"Trying {instance1.hash:X}:{instance2.hash:X}");
                                if (instance1.fields.Length != instance2.fields.Length) {
                                    // Console.Out.WriteLine($"{instance1.hash:X} != {instance2.hash:X}, different field count");
                                    continue;
                                }

                                if (instance1.size != instance2.size) {
                                    // Console.Out.WriteLine($"{instance1.hash:X} != {instance2.hash:X}, different size");
                                    continue;
                                }

                                if (file1STU2.instanceDiffData.Length != file2STU2.instanceDiffData.Length) {
                                    // Console.Out.WriteLine($"{instance1.hash:X} != {instance2.hash:X}, can't verify due to different instance count");
                                    continue;
                                }

                                if (instance1.fields.Length != instance2.fields.Length) {
                                    // Console.Out.WriteLine($"{instance1.hash:X} != {instance2.hash:X}, different field count");
                                    continue;
                                }

                                if (file1STU2.instanceDiffData.Length == 1 || file2STU2.instanceDiffData.Length == 1) {
                                    // Console.Out.WriteLine($"{instance1.hash:X} is probably {instance2.hash:X}, only one instance");
                                } else {
                                    // Console.Out.WriteLine($"{instance1.hash:X} might be {instance2.hash:X}");
                                }

                                results.Add(new CompareResult { beforeInstanceHash = instance1.hash, afterInstanceHash = instance2.hash, fields = new List<FieldCompareResult>() });

                                foreach (FieldData field1 in instance1.fields) {
                                    foreach (FieldData field2 in instance2.fields) {
                                        if (field1.size != field2.size) {
                                            // Console.Out.WriteLine($"\t{field1.hash:X} != {field2.hash:X}, difference field size");
                                            continue;
                                        }

                                        if (ArraysEqual(field1.sha1, field2.sha1)) {
                                            // Console.Out.WriteLine($"\t{field1.hash:X} might be {field2.hash:X}, same sha1");
                                            results.Last().fields.Add(new FieldCompareResult { beforeFieldHash = field1.hash, afterFieldHash = field2.hash });
                                        }

                                        if (field1.demangle_sha1 != null || field2.demangle_sha1 != null) {
                                            if (ArraysEqual(field1.demangle_sha1, field2.demangle_sha1)) {
                                                // Console.Out.WriteLine($"\t{field1.hash:X} might be {field2.hash:X}, same demangle sha1");
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
                            FieldResult f = instanceChangeTally[result.beforeInstanceHash].getField(d.beforeFieldHash, d.afterFieldHash);
                            if (f != null) {
                                f.count++;
                            } else {
                                instanceChangeTally[result.beforeInstanceHash].fieldDict[d.beforeFieldHash].Add(new FieldResult { beforeFieldHash = d.beforeFieldHash, afterFieldHash = d.afterFieldHash, count = 1 });
                            }

                        } else {
                            instanceChangeTally[result.beforeInstanceHash].fieldDict[d.beforeFieldHash] = new List<FieldResult>();
                            instanceChangeTally[result.beforeInstanceHash].fieldDict[d.beforeFieldHash].Add(new FieldResult { beforeFieldHash = d.beforeFieldHash, afterFieldHash = d.afterFieldHash, count = 1 });
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
                            FieldResult f = instanceChangeTally[result.beforeInstanceHash].getField(d.beforeFieldHash, d.afterFieldHash);
                            if (f != null) {
                                f.count++;
                            } else {
                                instanceChangeTally[result.beforeInstanceHash].fieldDict[d.beforeFieldHash].Add(new FieldResult { beforeFieldHash = d.beforeFieldHash, afterFieldHash = d.afterFieldHash, count = 1 });
                            }

                        } else {
                            instanceChangeTally[result.beforeInstanceHash].fieldDict[d.beforeFieldHash] = new List<FieldResult>();
                            instanceChangeTally[result.beforeInstanceHash].fieldDict[d.beforeFieldHash].Add(new FieldResult { beforeFieldHash = d.beforeFieldHash, afterFieldHash = d.afterFieldHash, count = 1 });
                            instanceChangeTally[result.beforeInstanceHash].fieldOccurrences[d.beforeFieldHash] = 1;
                        }
                    }
                }
            }
            foreach (KeyValuePair<uint, InstanceTally> it in instanceChangeTally) {
                foreach (KeyValuePair<uint, List<CompareResult>> id in it.Value.resultDict) {
                    double instanceProbablility = id.Value.Count / it.Value.count * 100;
                    Console.Out.WriteLine($"{it.Key:X8} => {id.Key:X8} ({instanceProbablility}% probability)");
                    foreach (KeyValuePair<uint, List<FieldResult>> field in it.Value.fieldDict) {
                        foreach (FieldResult fieldResult in field.Value) {
                            Console.Out.WriteLine($"\t{fieldResult.beforeFieldHash:X8} => {fieldResult.afterFieldHash:X:X8} ({(double)fieldResult.count / it.Value.fieldOccurrences[fieldResult.beforeFieldHash] * 100:0.0#}% probability)");
                        }
                    }
                }
            }
            Debugger.Break();
        }
    }
}