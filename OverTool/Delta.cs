using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using OWLib;

namespace OverTool {
    public class DeltaFile : IDisposable {
        private Stream baseFile;

        private Dictionary<ulong, int> files = new Dictionary<ulong, int>();
        private string name = string.Empty;

        public Dictionary<ulong, int> Files => files;
        public string Name => name;

        private const uint DELTAHEADER_V1 = 0x444C5441;

        public DeltaFile(string path) {
            if (!Directory.Exists(Path.GetDirectoryName(path))) {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            bool exists = File.Exists(path);
            baseFile = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            if (exists) {
                Parse();
            }
        }

        public void SetName(string name) => this.name = name;

        public void Parse() {
            using (BinaryReader reader = new BinaryReader(baseFile, System.Text.Encoding.ASCII, true)) {
                uint magic = reader.ReadUInt32();
                if (magic != DELTAHEADER_V1) {
                    return;
                }
                name = reader.ReadString();
                int count = reader.ReadInt32();
                for (int i = 0; i < count; ++i) {
                    ulong key = reader.ReadUInt64();
                    int size = reader.ReadInt32();
                    files[key] = size;
                }
            }
        }

        public void Save() {
            using (BinaryWriter writer = new BinaryWriter(baseFile, System.Text.Encoding.ASCII, true)) {
                baseFile.Position = 0;
                writer.Write(DELTAHEADER_V1);
                writer.Write(name);
                writer.Write(files.Count);
                foreach(KeyValuePair<ulong, int> pair in files) {
                    writer.Write(pair.Key);
                    writer.Write(pair.Value);
                }
                writer.Flush();
            }
        }

        public void Dispose() {
            if (baseFile != null && (baseFile.CanRead || baseFile.CanWrite)) {
                baseFile.Dispose();
                baseFile = null;
            }
        }
    }

    public class Delta : IOvertool {
        public string Title => "Delta";
        public char Opt => 'z';
        public string Help => "command file...";
        public uint MinimumArgs => 2;
        public ushort[] Track => new ushort[0];
        public bool Display => true;

        public void Save(string path, Record record, CASCHandler handler, string mode, bool quiet) {
            string output = Path.Combine(path, mode, $"{GUID.Type(record.record.Key):X3}", $"{GUID.LongKey(record.record.Key):X12}.{GUID.Type(record.record.Key):X3}");
            if (!Directory.Exists(Path.GetDirectoryName(output))) {
                Directory.CreateDirectory(Path.GetDirectoryName(output));
            }

            using (Stream acp = Util.OpenFile(record, handler)) {
                if (acp == null) {
                    return;
                }
                using (Stream file = File.Open(output, FileMode.Create)) {
                    Util.CopyBytes(acp, file, (int)acp.Length);
                    if (!quiet) {
                        Console.Out.WriteLine("Wrote file {0}", output);
                    }
                }
            }
        }

        public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, bool quiet, OverToolFlags flags) {
            switch (flags.Positionals[2].ToLowerInvariant()) {
                case "list": {
                        DeltaFile old = new DeltaFile(flags.Positionals[3]);
                        DeltaFile @new = new DeltaFile(flags.Positionals[4]);
                        if (string.IsNullOrEmpty(old.Name) || old.Files.Count == 0) {
                            Console.Error.WriteLine("Invalid old file");
                        } else if (string.IsNullOrEmpty(@new.Name) || @new.Files.Count == 0) {
                            Console.Error.WriteLine("Invalid new file");
                        } else {
                            Console.Out.WriteLine("Comparing {0} with {1}", old.Name, @new.Name);
                            foreach (KeyValuePair<ulong, int> pair in @new.Files) {
                                if (old.Files.ContainsKey(pair.Key)) {
                                    if (old.Files[pair.Key] == pair.Value) {
                                        continue;
                                    }
                                    Console.Out.WriteLine("{0:X12}.{1:X3} changed ({2} bytes)", GUID.LongKey(pair.Key), GUID.Type(pair.Key), old.Files[pair.Key] - pair.Value);
                                } else {
                                    Console.Out.WriteLine("{0:X12}.{1:X3} added ({2} bytes)", GUID.LongKey(pair.Key), GUID.Type(pair.Key), pair.Value);
                                }
                            }
                            foreach (KeyValuePair<ulong, int> pair in old.Files) {
                                if (!@new.Files.ContainsKey(pair.Key)) {
                                    Console.Out.WriteLine("{0:X12}.{1:X3} removed", GUID.LongKey(pair.Key), GUID.Type(pair.Key));
                                }
                            }
                        }
                        old.Dispose();
                        @new.Dispose();
                        break;
                    }
                case "delta": {
                        DeltaFile old = new DeltaFile(flags.Positionals[3]);
                        if (string.IsNullOrEmpty(old.Name) || old.Files.Count == 0) {
                            Console.Error.WriteLine("Invalid old file");
                        } else {
                            Console.Out.WriteLine("Comparing {0} with current ({1})", old.Name, handler.Config.BuildName    );
                            foreach (KeyValuePair<ulong, Record> pair in map) {
                                if (old.Files.ContainsKey(pair.Key)) {
                                    if (old.Files[pair.Key] == pair.Value.record.Size) {
                                        continue;
                                    }
                                    Console.Out.WriteLine("{0:X12}.{1:X3} changed ({2} bytes)", GUID.LongKey(pair.Key), GUID.Type(pair.Key), old.Files[pair.Key] - pair.Value.record.Size);
                                } else {
                                    Console.Out.WriteLine("{0:X12}.{1:X3} added ({2} bytes)", GUID.LongKey(pair.Key), GUID.Type(pair.Key), pair.Value.record.Size);
                                }
                            }
                            foreach (KeyValuePair<ulong, int> pair in old.Files) {
                                if (!map.ContainsKey(pair.Key)) {
                                    Console.Out.WriteLine("{0:X12}.{1:X3} removed", GUID.LongKey(pair.Key), GUID.Type(pair.Key));
                                }
                            }
                        }
                        old.Dispose();
                        break;
                    }
                case "create": {
                        DeltaFile delta = new DeltaFile(flags.Positionals[3]);
                        delta.SetName(handler.Config.BuildName);
                        foreach (KeyValuePair<ulong, Record> pair in map) {
                            delta.Files[pair.Key] = pair.Value.record.Size;
                        }
                        delta.Save();
                        delta.Dispose();
                        break;
                    }
                case "dump": {
                        DeltaFile old = new DeltaFile(flags.Positionals[4]);
                        if (string.IsNullOrEmpty(old.Name) || old.Files.Count == 0) {
                            Console.Error.WriteLine("Invalid old file");
                        } else {
                            foreach (KeyValuePair<ulong, Record> pair in map) {
                                if (old.Files.ContainsKey(pair.Key)) {
                                    if (old.Files[pair.Key] == pair.Value.record.Size) {
                                        continue;
                                    }
                                    Save(flags.Positionals[3], pair.Value, handler, "changed", quiet);
                                } else {
                                    Save(flags.Positionals[3], pair.Value, handler, "new", quiet);
                                }
                            }
                        }
                        old.Dispose();
                        break;
                    }
                default: {
                        Console.Out.WriteLine("Valid modes: list, create, delta, dump");
                        break;
                    }
            }
        }
    }
}
