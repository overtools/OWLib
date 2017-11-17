using System;
using System.IO;
using OWLib;
using OWLib.Types;
using OWLib.Types.Chunk;
using static STULib.Types.Generic.Version2;
using STULib;

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
            if (stud.Instances != null)
                foreach (ISTUDInstance instance in stud.Instances) {
                    STUDummy dummy = instance as STUDummy;
                    if (dummy == null) {
                        if (System.Diagnostics.Debugger.IsAttached) {
                            System.Diagnostics.Debugger.Break();
                        }
                    } else {
                        string traditional_name = manager.GetName(dummy.Id) ?? $"{dummy.Id:X8}";
                        string filename =
                            $"{i:X8}_{dummy.Offset:X8}_{OverTool.Util.SanitizePath(traditional_name)}.stu";
                        using (Stream outputStream = File.Open(Path.Combine(@out, filename), FileMode.Create,
                            FileAccess.Write, FileShare.Read)) {
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
            if (chunked.Chunks != null)
                foreach (IChunk instance in chunked.Chunks) {
                    MemoryChunk dummy = instance as MemoryChunk;
                    if (dummy == null) {
                        if (System.Diagnostics.Debugger.IsAttached) {
                            System.Diagnostics.Debugger.Break();
                        }
                    } else {
                        string filename = $"{i:X8}_{dummy.RootIdentifier}_{dummy.Identifier}.chunk";
                        using (Stream outputStream = File.Open(Path.Combine(@out, filename), FileMode.Create,
                            FileAccess.Write, FileShare.Read)) {
                            dummy.Data.Position = 0;
                            dummy.Data.CopyTo(outputStream);
                        }
                    }
                    i++;
                }
            Console.Out.WriteLine("Dumped {0} chunks", i);
        }

        public static void ListSTU(Stream file) {
            using (BinaryReader reader = new BinaryReader(file)) {
                STUHeader header = reader.Read<STUHeader>();

                Util.DumpStruct(header, "");
                Console.Out.WriteLine("{0} instances", header.InstanceCount);
                file.Position = header.InstanceListOffset;
                long totalSize = 0;
                for (uint i = 0; i < header.InstanceCount; ++i) {
                    STUInstanceRecord record = reader.Read<STUInstanceRecord>();
                    Console.Out.WriteLine("\t{0:X8} - {1:X8} - {2} - {3} bytes", record.InstanceChecksum, record.AssignFieldChecksum, record.AssignInstanceIndex, record.InstanceSize - 4);
                    long position = file.Position;
                    file.Position = header.Offset + totalSize;
                    int listIndex = reader.ReadInt32();
                    if (listIndex > -1 && (uint)listIndex >= header.InstanceFieldListCount) {
                        throw new Exception();
                    }
                    Console.Out.WriteLine("\t\tfield list index {0:X}", listIndex);
                    file.Position = position;
                    totalSize += record.InstanceSize;
                }
                Console.Out.WriteLine("Total: {0} bytes", totalSize);
                if (header.EntryInstanceCount > 0) {
                    Console.Out.WriteLine("{0} reference entries", header.EntryInstanceCount);
                    file.Position = header.EntryInstanceListOffset;
                    for (int i = (int)header.EntryInstanceCount; i > 0; --i) {
                        STUInstanceField entry = reader.Read<STUInstanceField>();
                        Console.Out.WriteLine("\t\t{0:X8} - {1} bytes", entry.FieldChecksum, entry.FieldSize);
                    }
                }
                Console.Out.WriteLine("{0} variable lists", header.InstanceFieldListCount);
                file.Position = header.InstanceFieldListOffset;
                for (uint i = 0; i < header.InstanceFieldListCount; ++i) {
                    STUInstanceFieldList info = reader.Read<STUInstanceFieldList>();
                    Console.Out.WriteLine("\t{0} variables", info.FieldCount);
                    long tmp = file.Position;
                    file.Position = info.ListOffset;
                    totalSize = 0;
                    for (uint j = 0; j < info.FieldCount; ++j) {
                        STUInstanceField entry = reader.Read<STUInstanceField>();
                        Console.Out.WriteLine("\t\t{0:X8} - {1} bytes", entry.FieldChecksum, entry.FieldSize);
                        totalSize += entry.FieldSize;
                    }
                    Console.Out.WriteLine("\t\tTotal: {0} bytes", totalSize);
                    file.Position = tmp;
                }

                if (System.Diagnostics.Debugger.IsAttached) {
                    file.Position = 0;
                    ISTU stu = ISTU.NewInstance(file, uint.MaxValue);
                    System.Diagnostics.Debugger.Break();
                }
            }
        }

        public static void Main(string[] args) {
            if (args.Length < 1) {
                Console.Out.WriteLine("Usage: STUDebug file [output_dir]");
                return;
            }

            string argFile = args[0];

            Util.DEBUG = true;

            var dirPart = Path.GetDirectoryName(argFile);
            if (dirPart.Length == 0)
                dirPart = ".";
            var filePart = Path.GetFileName(argFile);

            foreach (string file in Directory.GetFiles(dirPart, filePart)) {
                Console.Out.WriteLine(file);
                using (Stream fileStream = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.Read)) {
                    using (BinaryReader magicReader = new BinaryReader(fileStream, System.Text.Encoding.Default, true)) {
                        uint magic = magicReader.ReadUInt32();
                        fileStream.Position = 0;

                        if (magic == STUD.STUD_MAGIC) {
                            if (args.Length < 2) {
                                Console.Out.WriteLine("Usage: STUDebug file [output_dir]");
                                return;
                            }

                            Console.Out.WriteLine("STU File Detected");
                            DumpSTU(fileStream, args[1]);
                        } else if (magic == Chunked.ChunkMagic) {
                            if (args.Length < 2) {
                                Console.Out.WriteLine("Usage: STUDebug file [output_dir]");
                                return;
                            }

                            Console.Out.WriteLine("Chunk File Detected");
                            DumpChunks(fileStream, args[1]);
                        } else {
                            ListSTU(fileStream);
                        }
                    }
                }
            }
        }
    }
}

