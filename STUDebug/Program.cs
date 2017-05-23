using System;
using System.IO;
using OWLib;
using OWLib.Types;
using OWLib.Types.Chunk;

namespace STUDebug {
    public class Program {
        public static void DumpSTU(Stream file, string @out) {
            if (!Directory.Exists(@out)) {
                Directory.CreateDirectory(@out);
            }

            STUDManager manager = STUDManager.Instance;
            file.Position = 0;

            STUD stud = new STUD(file, true, new STUDManager(), false, true);
            if (stud.Instances == null) {
                Console.Out.WriteLine("Unknown error while parsing STU");
            }

            uint i = 0;
            foreach (ISTUDInstance instance in stud.Instances) {
                STUDummy dummy = instance as STUDummy;
                if (dummy == null) {
                    if (System.Diagnostics.Debugger.IsAttached) {
                        System.Diagnostics.Debugger.Break();
                    }
                } else {
                    string traditional_name = manager.GetName(dummy.Id);
                    if (traditional_name == null) {
                        traditional_name = $"{dummy.Id:X8}";
                    }
                    string filename = $"{i:X8}_{dummy.Offset:X8}_{OverTool.Util.SanitizePath(traditional_name)}.stu";
                    using (Stream outputStream = File.Open(Path.Combine(@out, filename), FileMode.Create, FileAccess.Write, FileShare.Read)) {
                        dummy.Data.Position = 0;
                        dummy.Data.CopyTo(outputStream);
                    }
                }
                ++i;
            }
            Console.Out.WriteLine("Dumped {0} STU instances", i);
        }

        public static void DumpChunks(Stream file, string @out) {
            if (!Directory.Exists(@out)) {
                Directory.CreateDirectory(@out);
            }
            
            file.Position = 0;

            Chunked chunked = new Chunked(file, false, new ChunkManager());
            if (chunked.Chunks == null) {
                Console.Out.WriteLine("Unknown error while parsing chunk file");
            }

            uint i = 0;
            foreach (IChunk instance in chunked.Chunks) {
                MemoryChunk dummy = instance as MemoryChunk;
                if (dummy == null) {
                    if (System.Diagnostics.Debugger.IsAttached) {
                        System.Diagnostics.Debugger.Break();
                    }
                } else {
                    string filename = $"{i:X8}_{dummy.RootIdentifier}_{dummy.Identifier}.chunk";
                    using (Stream outputStream = File.Open(Path.Combine(@out, filename), FileMode.Create, FileAccess.Write, FileShare.Read)) {
                        dummy.Data.Position = 0;
                        dummy.Data.CopyTo(outputStream);
                    }
                }
                i++;
            }
            Console.Out.WriteLine("Dumped {0} chunks", i);
        }

        public static void Main(string[] args) {
            if (args.Length < 2) {
                Console.Out.WriteLine("Usage: STUDebug file output_dir");
                return;
            }

            string file = args[0];
            string @out = args[1];

            Util.DEBUG = true;

            if (!File.Exists(file)) {
                Console.Out.WriteLine("File {0} does not exist!", file);
                return;
            }

            using (Stream fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                using (BinaryReader magicReader = new BinaryReader(fileStream, System.Text.Encoding.Default, true)) {
                    uint magic = magicReader.ReadUInt32();

                    if (magic == STUD.STUD_MAGIC) {
                        Console.Out.WriteLine("STU File Detected");
                        DumpSTU(fileStream, @out);
                    } else if (magic == Chunked.CHUNK_MAGIC) {
                        Console.Out.WriteLine("Chunk File Detected");
                        DumpChunks(fileStream, @out);
                    }
                }
            }
        }
    }
}
