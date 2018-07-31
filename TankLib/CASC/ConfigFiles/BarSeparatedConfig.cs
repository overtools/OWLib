using System;
using System.Collections.Generic;
using System.IO;

namespace TankLib.CASC.ConfigFiles {
    /// <summary>Bar separated config file, see .build.info</summary>
    public class BarSeparatedConfig {
        public readonly List<Dictionary<string, string>> Data = new List<Dictionary<string, string>>();
        public Dictionary<string, string> this[int index] => Data[index];

        /// <summary>Read from <param name="stream"></param></summary>
        /// <param name="stream">The stream to read from</param>
        public static BarSeparatedConfig Read(Stream stream) {
            using (StreamReader sr = new StreamReader(stream)) {
                return Read(sr);
            }
        }

        /// <summary>Read from <param name="reader"></param></summary>
        /// <param name="reader">The reader to read from</param>
        public static BarSeparatedConfig Read(TextReader reader) {
            BarSeparatedConfig result = new BarSeparatedConfig();

            int lineNum = 0;
            string[] fields = null;
            string line;

            while ((line = reader.ReadLine()) != null) {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) // skip empty lines and comments
                    continue;

                string[] tokens = line.Split('|');

                if (lineNum == 0) { // keys
                    fields = new string[tokens.Length];

                    for (int i = 0; i < tokens.Length; ++i) {
                        fields[i] = tokens[i].Split('!')[0].Replace(" ", "");
                    }
                } else { // values
                    result.Data.Add(new Dictionary<string, string>());

                    for (int i = 0; i < tokens.Length; ++i) {
                        if (fields == null) throw new NullReferenceException(nameof(fields));
                        result.Data[lineNum - 1].Add(fields[i], tokens[i]);
                    }
                }

                lineNum++;
            }

            return result;
        }
    }
}