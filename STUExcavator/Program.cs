using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CASCLib;
using DataTool;
using DataTool.Helper;
using Microsoft.CSharp;
using Newtonsoft.Json;
using STUHashTool;
using STULib;
using STULib.Impl.Version2HashComparer;
using STULib.Types.Generic;
using Version1 = STULib.Impl.Version1;
using Version2 = STULib.Impl.Version2;
using static DataTool.Helper.Logger;

namespace STUExcavator {
    public enum SerializationType {
        Unknown = 0,
        Raw = 1,
        // ReSharper disable once InconsistentNaming
        STUv1 = 2,
        // ReSharper disable once InconsistentNaming
        STUv2 = 3
    }
    [JsonObject(MemberSerialization.OptOut)]
    public class AssetTypeSummary {
        public string Type;
        public SerializationType SerializationType;
        public HashSet<string> STUInstanceTypes;
        public HashSet<string> GUIDTypes;
        public bool Incomplete;
        [JsonIgnore]
        public List<Asset> Assets;
    }

    public class Asset {
        public string GUID;
        public HashSet<string> GUIDs;
        public SerializationType SerializationType;
        public HashSet<string> STUInstances;
    }
    
    public class Program {
        public static Dictionary<ulong, MD5Hash> Files;
        public static Dictionary<ushort, HashSet<ulong>> TrackedFiles;
        public static CASCConfig Config;
        public static CASCHandler CASC;
        // dawn of a new project
        // STUExcavator:
        // go through all stu files and find GUIDs
        
        // future possibilities:
        // STUv1 excavation:
        //     find padding, then try for guid

        public static void Main(string[] args) {
            string overwatchDir = args[0];
            string outputDir = args[1];
            const string language = "enUS";

            // casc setup
            Config = CASCConfig.LoadLocalStorageConfig(overwatchDir, false, false);
            Config.Languages = new HashSet<string> {language};
            CASC = CASCHandler.OpenStorage(Config);
            DataTool.Program.Files = new Dictionary<ulong, MD5Hash>();
            DataTool.Program.TrackedFiles = new Dictionary<ushort, HashSet<ulong>>();
            DataTool.Program.CASC = CASC;
            DataTool.Program.Root = CASC.Root as OwRootHandler;
            DataTool.Program.Flags = new ToolFlags {OverwatchDirectory = outputDir, Language = language};
            IO.MapCMF(true);
            Files = DataTool.Program.Files;
            TrackedFiles = DataTool.Program.TrackedFiles;
            
            // prepare Version2Comparer
            Version2Comparer.InstanceJSON = STUHashTool.Program.LoadInstanceJson("RegisteredSTUTypes.json");
            
            // wipe ISTU
            ISTU.Clear();
            
            // actual tool
            Dictionary<string, AssetTypeSummary> types = new Dictionary<string, AssetTypeSummary>();
            
            foreach (KeyValuePair<ushort,HashSet<ulong>> keyValuePair in TrackedFiles.OrderBy(x => x.Key)) {
                // wipe ISTU
                ISTU.Clear();
                
                string type = keyValuePair.Key.ToString("X3");
                Log($"Processing type: {type}");
                types[type] = Excavate(keyValuePair.Key, keyValuePair.Value);
                
                IO.CreateDirectoryFromFile(Path.Combine(outputDir, type, "master.json"));
                using (Stream masterFile =
                    File.OpenWrite(Path.Combine(outputDir, type, "master.json"))) {
                    string masterJson = JsonConvert.SerializeObject(types[type], Formatting.Indented);
                    using (TextWriter writer = new StreamWriter(masterFile)) {
                        writer.WriteLine(masterJson);
                    }
                }
                
                if (types[type].Assets == null) continue;
                foreach (Asset asset in types[type].Assets) {
                    string assetFile = Path.Combine(outputDir, type, "assets", $"{asset.GUID}.json");
                    IO.CreateDirectoryFromFile(assetFile);
                    using (Stream assetStream = File.OpenWrite(assetFile)) {
                        string assetJson = JsonConvert.SerializeObject(asset, Formatting.Indented);
                        using (TextWriter writer = new StreamWriter(assetStream)) {
                            writer.WriteLine(assetJson);
                        }
                    }
                }
            }
        }

        public static Asset Excavate(ushort type, ulong guid) {
            Asset asset = new Asset {
                GUID = IO.GetFileName(guid),
                SerializationType = SerializationType.Unknown
            };

            using (Stream file = IO.OpenFile(guid)) {
                using (BinaryReader reader = new BinaryReader(file)) {
                    if (Version1.IsValidVersion(reader)) {
                        reader.BaseStream.Position = 0;
                        asset.SerializationType = SerializationType.STUv1;  // todo:
                    } else {
                        if (Version2.IsValidVersion(reader)) {   // why is there no magic, blizz pls
                            reader.BaseStream.Position = 0;
                            Version2 stuVersion2 = null;
                            try {
                                stuVersion2 = new Version2(file, uint.MaxValue);
                                asset.SerializationType = SerializationType.STUv2;
                                asset.GUIDs = new HashSet<string>();
                                asset.STUInstances = new HashSet<string>();
                            } catch (Exception) {
                                asset.SerializationType = SerializationType.Unknown;
                            }
                            if (stuVersion2 != null) {
                                asset.GUIDs = GetGUIDs(stuVersion2);
                                // broken: todo
                                foreach (Common.STUInstance stuInstance in stuVersion2.Instances.Concat(stuVersion2.HiddenInstances)) {
                                    STUAttribute attr = stuInstance?.GetType().GetCustomAttribute<STUAttribute>();
                                    if (attr == null) continue;
                                    asset.STUInstances.Add(attr.Checksum.ToString("X8"));
                                }
                            }
                        }
                    } 
                }
            }
            return asset;
        }

        public static AssetTypeSummary Excavate(ushort type, HashSet<ulong> files) {
            AssetTypeSummary summary = new AssetTypeSummary {Type = $"{type:X3}", SerializationType = SerializationType.Unknown};
            
            if (files.Count == 0) {
                summary.Incomplete = true;
                return summary;
            }
            // look at first file:
            Asset firstAsset = Excavate(type, files.First());
            if (firstAsset.SerializationType == SerializationType.Unknown) {
                summary.Incomplete = true;
                return summary;
            }
            summary.SerializationType = firstAsset.SerializationType;

            switch (summary.SerializationType) {
                case SerializationType.STUv2:
                    // compile the classes
                    summary.GUIDTypes = new HashSet<string>();
                    summary.STUInstanceTypes = new HashSet<string>();
                    using (Stream firstStream = IO.OpenFile(files.First())) {
                        bool beforeChildren = Version2Comparer.GetAllChildren;
                        Version2Comparer.GetAllChildren = true;
                        Version2Comparer version2Comparer =
                            ISTU.NewInstance(firstStream, uint.MaxValue, typeof(Version2Comparer)) as Version2Comparer;
                        Version2Comparer.GetAllChildren = beforeChildren;

                        StringBuilder sb = new StringBuilder();
                        // sb.AppendLine("using static STULib.Types.Generic.Common;");  // compiler no likey
                        // sb.AppendLine();
                        HashSet<uint> doneEnums = new HashSet<uint>();
                        HashSet<uint> doneInstances = new HashSet<uint>();
                        
                        if (version2Comparer == null) throw new InvalidDataException();

                        foreach (InstanceData instanceData in version2Comparer.InternalInstances.Values.Concat(
                            version2Comparer.InstanceData)) {
                            if (instanceData == null) continue;
                            if (doneInstances.Contains(instanceData.Checksum)) continue;
                            doneInstances.Add(instanceData.Checksum);
                            ClassBuilder builder = new ClassBuilder(instanceData);
                            string @class = builder.Build(new Dictionary<uint, string>(),
                                new Dictionary<uint, string>(), new Dictionary<uint, string>(),
                                $"STUExcavator.Types.x{type:X3}", false, true);
                            sb.AppendLine(@class);

                            foreach (FieldData field in instanceData.Fields) {
                                if (!field.IsEnum && !field.IsEnumArray) continue;
                                if (doneEnums.Contains(field.EnumChecksum)) continue;
                                doneEnums.Add(field.EnumChecksum);
                                EnumBuilder enumBuilder = new EnumBuilder(new STUEnumData {
                                    Type = STUHashTool.Program.GetSizeType(field.Size),
                                    Checksum = field.EnumChecksum
                                });
                                sb.AppendLine(enumBuilder.Build(new Dictionary<uint, string>(),
                                    $"STUExcavator.Types.x{type:X3}.Enums", true));
                            }
                        }

                        CSharpCodeProvider provider = new CSharpCodeProvider();
                        CompilerParameters parameters = new CompilerParameters();
                        parameters.ReferencedAssemblies.Add("STULib.dll");
                        parameters.ReferencedAssemblies.Add("OWLib.dll");
                        parameters.GenerateInMemory = true;
                        CompilerResults results = provider.CompileAssemblyFromSource(parameters, sb.ToString());

                        if (results.Errors.HasErrors) {
                            StringBuilder sb2 = new StringBuilder();

                            foreach (CompilerError error in results.Errors) {
                                sb2.AppendLine($"Error ({error.ErrorNumber}): {error.ErrorText}");
                            }

                            throw new InvalidOperationException(sb2.ToString());
                        }

                        firstStream.Position = 0;
                        Assembly assembly = results.CompiledAssembly;
                        foreach (KeyValuePair<uint, InstanceData> inst in version2Comparer.InternalInstances) {
                            summary.STUInstanceTypes.Add(inst.Value.Checksum.ToString("X8")); // todo: bad?
                            Type compiledInst =
                                assembly.GetType($"STUExcavator.Types.x{type:X3}.STU_{inst.Value.Checksum:X8}");
                            ISTU.InstanceTypes[inst.Value.Checksum] = compiledInst;
                        }
                    }
                    break;
            }
            List<Asset> assets = new List<Asset>();
            
            foreach (ulong guid in files) {
                Asset asset = Excavate(type, guid);
                assets.Add(asset);
                if (asset.GUIDs != null) {
                    foreach (string assetGUID in asset.GUIDs) {
                        summary.GUIDTypes.Add(assetGUID.Split('.')[1]);
                    }
                }
                // broken: todo
                // if (asset.STUInstances != null) {
                //     foreach (string instance in asset.STUInstances) {
                //         summary.STUInstanceTypes.Add(instance);
                //     }
                // }
            }

            summary.Assets = assets;

            return summary;
        }

        public static HashSet<string> GetGUIDs(Version2 stu) {
            HashSet<string> guids = new HashSet<string>();

            foreach (Common.STUInstance instance in stu.Instances.Concat(stu.HiddenInstances)) {
                // this means all instances, we don't need to recurse
                if (instance == null) continue;
                FieldInfo[] fields = GetFields(instance.GetType(), true);
                foreach (FieldInfo field in fields) {
                    object fieldValue = field.GetValue(instance);
                    if (fieldValue == null) continue;
                    if (field.FieldType == typeof(Common.STUGUID[])) {
                        foreach (Common.STUGUID guid in (Common.STUGUID[]) fieldValue) guids.Add(IO.GetFileName(guid));
                    }
                    if (field.FieldType == typeof(Common.STUGUID)) {
                        guids.Add(IO.GetFileName(fieldValue as Common.STUGUID));
                    }
                }
            }
            return guids;
        }

        internal static FieldInfo[] GetFields(Type type, bool doParent=false) {
            FieldInfo[] parent = new FieldInfo[0];
            if (type.BaseType != null && type.BaseType.Namespace != null &&
                !type.BaseType.Namespace.StartsWith("System.") && doParent) parent = GetFields(type.BaseType);
            return parent.Concat(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                                BindingFlags.DeclaredOnly)).ToArray();
        }
    }
}