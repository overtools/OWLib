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
        public uint[] BrokenInstances;
        public Dictionary<uint, STUInstanceJSON> Instances;

        private readonly string _directory;
        
        public StructuredDataInfo(string directory) {
            _directory = directory;
            
            LoadBrokenInstances(Path.Combine(_directory, "IgnoredBrokenSTUs.txt"));
            LoadInstances(Path.Combine(_directory, "RegisteredSTUTypes.json"));
            
            LoadHashCSV(Path.Combine(_directory, "KnownTypes.csv"), out KnownInstances);
            LoadHashCSV(Path.Combine(_directory, "KnownFields.csv"), out KnownFields);
            LoadHashCSV(Path.Combine(_directory, "KnownEnums.csv"), out KnownEnums);
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

        private void LoadInstances(string filename) {
            Instances = new Dictionary<uint, STUInstanceJSON>();
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
        
        private void LoadHashCSV(string filepath, out Dictionary<uint, string> dict) {
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

        private void LoadBrokenInstances(string filename) {
            BrokenInstances = File.Exists(filename)
                ? File.ReadAllLines(filename).Where(x => !string.IsNullOrEmpty(x)).Select(x => uint.Parse(x.Split(' ')[0].Split('_')[1], NumberStyles.HexNumber)).ToArray()
                : null;
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
    public class STUFieldJSON {
        public uint Hash;
        public string Type;
        public int SerializationType;
        public int Size = -1;
        internal string DebuggerDisplay => $"{Hash:X8} (Type: {Type})";
    }
}