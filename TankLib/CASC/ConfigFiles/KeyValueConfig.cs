using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TankLib.CASC.ConfigFiles {
    /// <summary>Key value config file</summary>
    public class KeyValueConfig {
        public readonly Dictionary<string, List<string>> KeyValue = new Dictionary<string, List<string>>();

        public List<string> this[string key] {
            get {
                KeyValue.TryGetValue(key, out List<string> ret);
                return ret;
            }
        }

        /// <summary>Read from <param name="stream"></param></summary>
        /// <param name="stream">The stream to read from</param>
        public static KeyValueConfig Read(Stream stream) {
            StreamReader sr = new StreamReader(stream);
            return Read(sr);
        }

        /// <summary>Read from <param name="reader"></param></summary>
        /// <param name="reader">The reader to read from</param>
        public static KeyValueConfig Read(TextReader reader) {
            KeyValueConfig result = new KeyValueConfig();
            string line;

            while ((line = reader.ReadLine()) != null) {
                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#")) // skip empty lines and comments
                    continue;

                string[] tokens = line.Split(new[] {'='}, StringSplitOptions.RemoveEmptyEntries);

                if (tokens.Length != 2)
                    throw new Exception("KeyValueConfig: tokens.Length != 2");

                string[] values = tokens[1].Trim().Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
                List<string> valuesList = values.ToList();
                result.KeyValue.Add(tokens[0].Trim(), valuesList);
            }

            return result;
        }
    }
}