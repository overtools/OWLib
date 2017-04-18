using System;
using System.IO;
using System.Text;

namespace BNKTool {
    class Program {
        public static void CopyBytes(Stream i, Stream o, int sz) {
            byte[] buffer = new byte[sz];
            i.Read(buffer, 0, sz);
            o.Write(buffer, 0, sz);
            buffer = null;
        }

        static void Main(string[] args) {
            if (args.Length < 1) {
                Console.Out.WriteLine("Usage: BNKTool file.bnk");
                return;
            }
            string BNKFile = args[0];
            using (Stream input = File.Open(BNKFile, FileMode.Open, FileAccess.Read)) {
                using (BinaryReader reader = new BinaryReader(input)) {
                    long didxOffset = -1;
                    long dataOffset = -1;

                    while (input.Position < input.Length) {
                        string ident = Encoding.ASCII.GetString(reader.ReadBytes(4));
                        long offset = input.Position;
                        if (ident == "DIDX") { // Data Index
                            didxOffset = offset;
                        }
                        if (ident == "DATA") { // Data
                            dataOffset = offset + 4;
                        }
                        long length = reader.ReadUInt32();
                        input.Position += length;
                        Console.Out.WriteLine("Parsing section {0}", ident);
                    }

                    if (didxOffset == -1 || dataOffset == -1) {
                        Console.Out.WriteLine("No embedded WEM data");
                        return;
                    }

                    string output = $"{Path.GetDirectoryName(BNKFile)}{Path.DirectorySeparatorChar}{Path.GetFileNameWithoutExtension(BNKFile)}";
                    if (!Directory.Exists(output)) {
                        Directory.CreateDirectory(output);
                    }

                    input.Position = didxOffset;
                    uint didxLength = reader.ReadUInt32();
                    for (int i = 0; i < didxLength / 12; ++i) {
                        uint id = reader.ReadUInt32();
                        uint offset = reader.ReadUInt32();
                        int length = reader.ReadInt32();
                        long tmp = input.Position;

                        input.Position = dataOffset + offset;
                        using (Stream outputs = File.Open($"{output}{Path.DirectorySeparatorChar}{id:X8}.wem", FileMode.OpenOrCreate, FileAccess.Write)) {
                            CopyBytes(input, outputs, length);
                            Console.Out.WriteLine("Wrote WEM {0:X8}", id);
                        }
                        input.Position = tmp;
                    }
                }
            }
        }
    }
}
