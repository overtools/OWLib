using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace TankLibHelper {
    public class StructuredDataInfo {
        public Dictionary<uint, string> KnownInstances;
        public Dictionary<uint, string> KnownEnums;
        public Dictionary<uint, string> KnownFields;
        public Dictionary<uint, string> KnownEnumNames;
        public List<uint> BrokenInstances;
        public Dictionary<uint, STUInstanceJSON> Instances;
        public Dictionary<uint, STUEnumJSON> Enums;

        //private readonly string _directory;
        
        public StructuredDataInfo(string directory) {
            //_directory = directory;
            BrokenInstances = new List<uint>();
            KnownEnums = new Dictionary<uint, string>();
            KnownFields = new Dictionary<uint, string>();
            KnownInstances = new Dictionary<uint, string>();
            KnownEnumNames = new Dictionary<uint, string>();
            Instances = new Dictionary<uint, STUInstanceJSON>();
            Enums = new Dictionary<uint, STUEnumJSON>();
            
            Load(directory);
        }

        private void Load(string directory) {
            LoadBrokenInstances(Path.Combine(directory, "IgnoredBrokenSTUs.txt"));
            LoadInstances(Path.Combine(directory, "RegisteredSTUTypes.json"));
            LoadEnums(Path.Combine(directory, "RegisteredEnums.json"));
            LoadNames(directory);
        }
        private void LoadNames(string directory) {
            LoadHashCSV(Path.Combine(directory, "KnownTypes.csv"), KnownInstances);
            LoadHashCSV(Path.Combine(directory, "KnownFields.csv"), KnownFields);
            LoadHashCSV(Path.Combine(directory, "KnownEnums.csv"), KnownEnums);
            LoadHashCSV(Path.Combine(directory, "KnownEnumNames.csv"), KnownEnumNames);
        }

        public void LoadExtra(string directory) {
            LoadNames(directory);
        }
        
        public string GetInstanceName(uint hash) {
            if (KnownInstances.ContainsKey(hash)) {
                return KnownInstances[hash];
            }
            return $"STU_{hash:X8}";
        }
        
        public string GetFieldName(uint hash) {
            if (KnownFields.ContainsKey(hash)) {
                return KnownFields[hash];
            }
            return $"m_{hash:X8}";
        }

        public string GetEnumName(uint hash) {
            if (KnownEnums.ContainsKey(hash)) {
                return KnownEnums[hash];
            }
            return $"Enum_{hash:X8}";
        }

        public string GetEnumValueName(uint hash) {
            if (KnownEnumNames.ContainsKey(hash)) {
                return KnownEnumNames[hash];
            }
            return $"x{hash:X8}";
        }

        private void LoadInstances(string filename) {
            JObject stuTypesJson = JObject.Parse(File.ReadAllText(filename));
            foreach (KeyValuePair<string, JToken> pair in stuTypesJson) {
                uint checksum = uint.Parse(pair.Key.Split('_')[1], NumberStyles.HexNumber);
                STUInstanceJSON instance = new STUInstanceJSON {
                    Fields = null,
                    Hash = checksum,
                    Parent = (string)pair.Value["parent"] == null ? 0 : uint.Parse(((string)pair.Value["parent"]).Split('_')[1], NumberStyles.HexNumber)
                };
                Instances[checksum] = instance;

                JToken fields = pair.Value["fields"];
                if (fields == null) continue;
                instance.Fields = new STUFieldJSON[fields.Count()];
                
                uint i = 0;
                foreach (JToken field in fields) {
                    instance.Fields[i] = new STUFieldJSON {
                        Hash = uint.Parse((string)field["name"], NumberStyles.HexNumber),
                        SerializationType = (int)field["serializationType"],
                        Size = field.Value<int>("size"),
                        Type = field.Value<string>("type")
                    };
                    i++;
                }       
            }
        }

        private void LoadEnums(string filename) {
            JArray stuEnumJson = JArray.Parse(File.ReadAllText(filename));
            foreach (JToken token in stuEnumJson) {
                uint checksum = uint.Parse((string)token["hash"], NumberStyles.HexNumber);
                STUEnumJSON instance = new STUEnumJSON {
                    Hash = checksum
                };
                Enums[checksum] = instance;

                JToken values = token["values"];
                if (values == null) continue;
                instance.Values = new STUEnumValueJSON[values.Count()];
                
                uint i = 0;
                foreach (JToken value in values) {
                    var valueText = (string) value["value"];
                    instance.Values[i] = new STUEnumValueJSON {
                        Hash = uint.Parse((string)value["hash"], NumberStyles.HexNumber),
                        Value = valueText.StartsWith("-") ? (ulong)long.Parse(valueText) : ulong.Parse(valueText)
                    };
                    i++;
                }       
            }
        }
        
        private void LoadHashCSV(string filepath, Dictionary<uint, string> dict) {
            if (string.IsNullOrEmpty(filepath)) {
                return;
            }
            string[] rows = File.ReadAllLines(filepath);
            if (rows.Length < 2) { // If it doesn't have at least 1 row after the header
                return;
            }
            foreach (string row in rows.Skip(1)) {
                string[] split = row.Split(',');
                if (split.Length != 2) continue;

                string val = split[1].Trim();
                if (val != "N/A") {
                    uint hash = uint.Parse(split[0], NumberStyles.HexNumber);
                    if (dict.ContainsKey(hash)) {
                        Debugger.Log(0, "StructuredDataInfo", $"Known hash already exists ({Path.GetFileName(filepath)}). This={val}, preexisting={dict[hash]}\r\n");
                        continue;
                        //throw new Exception($"Known hash already exists ({Path.GetFileName(filepath)}). This={val}, preexisting={dict[hash]}");
                    }
                        
                    dict.Add(uint.Parse(split[0], NumberStyles.HexNumber), val);
                }
            }
        }

        private void LoadBrokenInstances(string filename) {
            IEnumerable<uint> brokenInsts = File.Exists(filename)
                ? File.ReadAllLines(filename).Where(x => !string.IsNullOrEmpty(x)).Select(x => uint.Parse(x.Split(' ')[0].Split('_')[1], NumberStyles.HexNumber))
                : null;
            if (brokenInsts != null) {
                BrokenInstances.AddRange(brokenInsts);
            }
        }

        public static string GetDefaultDirectory() {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        }
    }
    
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class STUInstanceJSON {
        public uint Hash;
        public uint Parent;
        public STUFieldJSON[] Fields;
        internal string DebuggerDisplay => $"{Hash:X8}{(Parent == 0 ? "" : $" (Parent: {Parent:X8})")}";
    }
    
    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class STUEnumJSON {
        public uint Hash;
        public STUEnumValueJSON[] Values;
        internal string DebuggerDisplay => $"{Hash:X8}";
    }

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class STUEnumValueJSON {
        public uint Hash;
        public ulong Value;
        internal string DebuggerDisplay => $"{Hash:X8}: {Value}";
    }

    [DebuggerDisplay("{" + nameof(DebuggerDisplay) + "}")]
    public class STUFieldJSON {
        public uint Hash;
        public string Type;
        public int SerializationType;
        public int Size = -1;
        internal string DebuggerDisplay => $"{Hash:X8} (Type: {Type})";
    }
}