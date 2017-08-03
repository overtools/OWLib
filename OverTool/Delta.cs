using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using CASCExplorer;

namespace OverTool {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct DeltaRecord {
        public int Size;
        public MD5Hash Hash;
    }

    public class DeltaFile : IDisposable {
        private Stream baseFile;

        private int VERSION = 1;
        public int Version => VERSION;

        private Dictionary<ulong, DeltaRecord> files = new Dictionary<ulong, DeltaRecord>();
        private string name = string.Empty;

        public Dictionary<ulong, DeltaRecord> Files => files;
        public string Name => name;

        private const uint DELTAHEADER_V1 = 0x444C5441;
        private const uint DELTAHEADER_V2 = 0x444C5442;

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
                if (magic == DELTAHEADER_V1) {
                    ParseV1(reader);
                } else if (magic == DELTAHEADER_V2) {
                    ParseV2(reader);
                }
            }
        }

        private void ParseV1(BinaryReader reader) {
            VERSION = 1;
            name = reader.ReadString();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; ++i) {
                ulong key = reader.ReadUInt64();
                files[key] = new DeltaRecord { Size = reader.ReadInt32() };
            }
        }

        private void ParseV2(BinaryReader reader) {
            VERSION = 2;
            name = reader.ReadString();
            int count = reader.ReadInt32();
            for (int i = 0; i < count; ++i) {
                ulong key = reader.ReadUInt64();
                files[key] = reader.Read<DeltaRecord>();
            }
        }


        public void Save() {
            using (BinaryWriter writer = new BinaryWriter(baseFile, System.Text.Encoding.ASCII, true)) {
                baseFile.Position = 0;
                writer.Write(DELTAHEADER_V2);
                writer.Write(name);
                writer.Write(files.Count);
                foreach (KeyValuePair<ulong, DeltaRecord> pair in files) {
                    writer.Write(pair.Key);
                    writer.Write(pair.Value.Size);
                    unsafe
                    {
                        DeltaRecord record = pair.Value;
                        byte* offset = record.Hash.Value;
                        writer.Write(*(ulong*)offset);
                        offset += 8;
                        writer.Write(*(ulong*)offset);
                    }
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
        public string FullOpt => "delta";
        public string Help => "command file...";
        public uint MinimumArgs => 2;
        public ushort[] Track => new ushort[0];
        public bool Display => true;

        public void Save(string path, Record record, CASCHandler handler, string mode, bool quiet) {
            string output = Path.Combine(path, mode, $"{OWLib.GUID.Type(record.record.Key):X3}", $"{OWLib.GUID.LongKey(record.record.Key):X12}.{OWLib.GUID.Type(record.record.Key):X3}");

            using (Stream acp = Util.OpenFile(record, handler)) {
                if (acp == null) {
                    return;
                }
                if (!Directory.Exists(Path.GetDirectoryName(output))) {
                    Directory.CreateDirectory(Path.GetDirectoryName(output));
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
                            foreach (KeyValuePair<ulong, DeltaRecord> pair in @new.Files) {
                                if (old.Files.ContainsKey(pair.Key)) {
                                    if (old.Files[pair.Key].Size == pair.Value.Size) {
                                        if (old.Version != 2 || @new.Version != 2 || old.Files[pair.Key].Hash.EqualsTo(pair.Value.Hash)) {
                                            continue;
                                        }
                                    }
                                    Console.Out.WriteLine("{0:X12}.{1:X3} changed ({2} delta bytes)", OWLib.GUID.LongKey(pair.Key), OWLib.GUID.Type(pair.Key), old.Files[pair.Key].Size - pair.Value.Size);
                                } else {
                                    Console.Out.WriteLine("{0:X12}.{1:X3} added ({2} bytes)", OWLib.GUID.LongKey(pair.Key), OWLib.GUID.Type(pair.Key), pair.Value.Size);
                                }
                            }
                            foreach (KeyValuePair<ulong, DeltaRecord> pair in old.Files) {
                                if (!@new.Files.ContainsKey(pair.Key)) {
                                    Console.Out.WriteLine("{0:X12}.{1:X3} removed", OWLib.GUID.LongKey(pair.Key), OWLib.GUID.Type(pair.Key));
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
                            Console.Out.WriteLine("Comparing {0} with current ({1})", old.Name, handler.Config.BuildName);
                            foreach (KeyValuePair<ulong, Record> pair in map) {
                                if (handler.Encoding.GetEntry(pair.Value.record.ContentKey, out EncodingEntry enc)) {
                                    if (old.Files.ContainsKey(pair.Key)) {
                                        if (old.Files[pair.Key].Size == pair.Value.record.Size) {
                                            if (old.Version != 2 || old.Files[pair.Key].Hash.EqualsTo(enc.Key)) {
                                                continue;
                                            }
                                        }
                                        Console.Out.WriteLine("{0:X12}.{1:X3} changed ({2} delta bytes)", OWLib.GUID.LongKey(pair.Key), OWLib.GUID.Type(pair.Key), old.Files[pair.Key].Size - pair.Value.record.Size);
                                    } else {
                                        Console.Out.WriteLine("{0:X12}.{1:X3} added ({2} bytes)", OWLib.GUID.LongKey(pair.Key), OWLib.GUID.Type(pair.Key), pair.Value.record.Size);
                                    }
                                }
                            }
                            foreach (KeyValuePair<ulong, DeltaRecord> pair in old.Files) {
                                if (!map.ContainsKey(pair.Key)) {
                                    Console.Out.WriteLine("{0:X12}.{1:X3} removed", OWLib.GUID.LongKey(pair.Key), OWLib.GUID.Type(pair.Key));
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
                            if (handler.Encoding.GetEntry(pair.Value.record.ContentKey, out EncodingEntry enc)) {
                                delta.Files[pair.Key] = new DeltaRecord { Size = pair.Value.record.Size, Hash = enc.Key };
                            }
                        }
                        delta.Save();
                        delta.Dispose();
                        break;
                    }
                case "dump": {
                        DeltaFile old = new DeltaFile(flags.Positionals[4]);
                        List<ushort> types = new List<ushort>();
                        if (flags.Positionals.Length > 5) {
                            types.AddRange(flags.Positionals.Skip(5).Select((it) => ushort.Parse(it, System.Globalization.NumberStyles.HexNumber)));
                        }
                        if (string.IsNullOrEmpty(old.Name) || old.Files.Count == 0) {
                            Console.Error.WriteLine("Invalid old file");
                        } else {
                            foreach (KeyValuePair<ulong, Record> pair in map) {
                                if (types.Count > 0 && !types.Contains(OWLib.GUID.Type(pair.Key))) {
                                    continue;
                                }
                                if (handler.Encoding.GetEntry(pair.Value.record.ContentKey, out EncodingEntry enc)) {
                                    if (old.Files.ContainsKey(pair.Key)) {
                                        if (old.Files[pair.Key].Size == pair.Value.record.Size) {
                                            if (old.Version != 2 || old.Files[pair.Key].Hash.EqualsTo(enc.Key)) {
                                                continue;
                                            }
                                        }
                                        Save(flags.Positionals[3], pair.Value, handler, "changed", quiet);
                                    } else {
                                        Save(flags.Positionals[3], pair.Value, handler, "new", quiet);
                                    }
                                }
                            }
                        }
                        old.Dispose();
                        break;
                    }
                default: {
                        Console.Out.WriteLine("Valid modes: list, create, delta, dump");
                        Console.Out.WriteLine("create [destination owdelta]");
                        Console.Out.WriteLine("list [old owdelta] [new owdelta]");
                        Console.Out.WriteLine("delta [old owdelta]");
                        Console.Out.WriteLine("dump [detination] [old owdelta] [types]");
                        break;
                    }
            }
        }
    }
}
