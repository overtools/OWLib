using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using TankLib.STU;

namespace TankLibHelper.Modes {
    public class GenerateKnownEnumNames : IMode {
        public ModeResult Run(string[] args) {
            var asm = typeof(teStructuredData).Assembly;
            var knownEnums = asm.GetTypes().Where(x => x.IsEnum && x.GetCustomAttribute<STUEnumAttribute>() != null).ToDictionary(x => x.GetCustomAttribute<STUEnumAttribute>().Hash, GetEnumDict);

            var dataDirectory = args.Length >= 2 ? args[1] : StructuredDataInfo.GetDefaultDirectory();
            var _info = new StructuredDataInfo(dataDirectory);
            string[] extraData = args.Skip(2).ToArray();
            foreach (string extra in extraData) {
                _info.LoadExtra(extra);
            }

            using (Stream file = File.OpenWrite(Path.Combine(dataDirectory, "KnownEnumNames.gen.csv"))) 
            using (TextWriter writer = new StreamWriter(file)) {
                writer.WriteLine("Hash, Name");
                foreach (var entry in _info.Enums) {
                    if (!knownEnums.ContainsKey(entry.Key) || knownEnums.Count <= 0) continue;
                    var reverseDictionary = ToDictionarySafe(entry.Value.m_values, x => x.m_value, y => y.Hash2);
                    foreach (var pair in reverseDictionary) {
                        if (knownEnums[entry.Key].ContainsKey(pair.Key)) {
                            writer.WriteLine($"{pair.Value:X8}, {knownEnums[entry.Key][pair.Key]}");
                        }
                    }
                }
            }

            return ModeResult.Success;
        }

        private Dictionary<A, B> ToDictionarySafe<T, A, B>(IEnumerable<T> values, Func<T, A> keySelector, Func<T, B> valueSelector) {
            var d = new Dictionary<A, B>();
            foreach (var entry in values) {
                var key = keySelector(entry);
                var value = valueSelector(entry);
                if (d.ContainsKey(key)) {
                    continue;
                }

                d[key] = value;
            }
            return d;
        }

        private Dictionary<long, string> GetEnumDict(Type enumType) {
            var names = enumType.GetEnumNames();
            var values = enumType.GetEnumValues();
            var d = new Dictionary<long, string>();
            for (int i = 0; i < names.Length; ++i) {
                d[Convert.ToInt64(values.GetValue(i))] = names[i];
            }

            return d;
        }

        public string Mode => "generate-enum-names";
    }
}
