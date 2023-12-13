using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using Utf8Json;

namespace TankLibHelper {
    public class StructuredDataInfo {
        public Dictionary<uint, string> KnownInstances;
        public Dictionary<uint, string> KnownEnums;
        public Dictionary<uint, string> KnownFields;
        public Dictionary<uint, string> KnownEnumNames;
        public Dictionary<uint, InstanceNew> Instances;
        public Dictionary<uint, EnumNew> Enums;
        
        public StructuredDataInfo(string directory) {
            KnownEnums = new Dictionary<uint, string>();
            KnownFields = new Dictionary<uint, string>();
            KnownInstances = new Dictionary<uint, string>();
            KnownEnumNames = new Dictionary<uint, string>();
            Instances = new Dictionary<uint, InstanceNew>();
            Enums = new Dictionary<uint, EnumNew>();
            
            Load(directory);
        }

        private void Load(string directory) {
            LoadInstances(Path.Combine(directory, "STUDump.json"));
            LoadEnums(Path.Combine(directory, "EnumDump.json"));
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
            List<InstanceNew> list;
            using (var stream = File.OpenRead(filename)) {
                list = JsonSerializer.Deserialize<List<InstanceNew>>(stream);
            }

            foreach (InstanceNew instanceNew in list) {
                Instances[instanceNew.Hash2] = instanceNew;
            }
        }

        private void LoadEnums(string filename) {
            List<EnumNew> list;
            using (var stream = File.OpenRead(filename)) {
                list = JsonSerializer.Deserialize<List<EnumNew>>(stream);
            }
            foreach (EnumNew enumNew in list) {
                Enums[enumNew.Hash2] = enumNew;
            }
        }
        
        private void LoadHashCSV(string filepath, Dictionary<uint, string> dict) {
            if (string.IsNullOrEmpty(filepath)) {
                return;
            }
            if (!File.Exists(filepath)) return;
            string[] rows = File.ReadAllLines(filepath);
            if (rows.Length < 2) { // If it doesn't have at least 1 row after the header
                return;
            }
            foreach (string row in rows.Skip(1)) {
                if (row.StartsWith("#")) continue; // ignore comments

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

        public static string GetDefaultDirectory() {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        }
    }
    
    public class InstanceNew {
        [DataMember(Name="Name")] public string m_name;
        [DataMember(Name="Hash")] public string m_hash;
        [DataMember(Name="ParentName")] public string m_parentName;
        [DataMember(Name="ParentHash")] public string m_parentHash;
        [DataMember(Name="Fields")] public List<FieldNew> m_fields;
        [DataMember(Name="InstanceSize")] public uint m_size;
        [DataMember(Name="InstanceAlignment")] public uint m_alignment;
        
        public uint Hash2 => uint.Parse(m_hash, NumberStyles.HexNumber);
        public uint ParentHash2 => m_parentHash == null ? 0 : uint.Parse(m_parentHash, NumberStyles.HexNumber);
    }

    public class FieldNew {
        [DataMember(Name="Name")] public string m_name;
        [DataMember(Name="Hash")] public string m_hash;
        [DataMember(Name="TypeName")] public string m_typeName;
        [DataMember(Name="TypeHash")] public string m_typeHash;
        [DataMember(Name="SerializationType")] public uint m_serializationType;
        [DataMember(Name="Size")] public uint m_size;
        [DataMember(Name="DefaultValue")] public FieldDefaultValue m_defaultValue;
        [DataMember(Name="Offset")] public uint m_offset;

        public uint Hash2 => uint.Parse(m_hash, NumberStyles.HexNumber);
        public uint TypeHash2 => uint.Parse(m_typeHash, NumberStyles.HexNumber);
    }

    public class FieldDefaultValue {
        [DataMember(Name="Value")] public dynamic m_value;
        [DataMember(Name="Hex")] public string m_hexValue;

        [DataMember(Name="X")] public float m_x;
        [DataMember(Name="Y")] public float m_y;
        [DataMember(Name="Z")] public float m_z;
        [DataMember(Name="W")] public float m_w;
        
        [DataMember(Name="R")] public float m_r;
        [DataMember(Name="G")] public float m_g;
        [DataMember(Name="B")] public float m_b;
        [DataMember(Name="A")] public float m_a;
    }

    public class EnumNew {
        [DataMember(Name="Name")] public string m_name;
        [DataMember(Name="Hash")] public string m_hash;
        [DataMember(Name="Values")] public List<EnumValueNew> m_values;
        
        public uint Hash2 => uint.Parse(m_hash, NumberStyles.HexNumber);
    }

    public class EnumValueNew {
        [DataMember(Name="Name")] public string m_name;
        [DataMember(Name="Hash")] public string m_hash;
        [DataMember(Name="Value")] public long m_value;
        [DataMember(Name="Hex")] public string m_hexValue;

        public uint Hash2 => uint.Parse(m_hash, NumberStyles.HexNumber);

        public long GetSafeValue(FieldNew field) {
            return TruncateValue(m_value, field);
        }
        
        public static long TruncateValue(long val, FieldNew field) {
            var safeValue = val;
            switch (field.m_size) {
                case 1:
                    safeValue = (byte)(safeValue & byte.MaxValue);
                    break;
                case 2:
                    safeValue = (short)(safeValue & ushort.MaxValue);
                    break;
                case 4:
                    safeValue = (int)(safeValue & uint.MaxValue);
                    break;
            }
            return safeValue;
        }
    }
}