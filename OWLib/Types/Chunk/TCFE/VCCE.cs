using System.IO;
using System.Runtime.InteropServices;

namespace OWLib.Types.Chunk {
    public class VCCE : IChunk {
        public string Identifier => "VCCE"; // ECEC - Effect Chunk ??
        public string RootIdentifier => "TCFE"; // EFCT - Effect

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Structure {
            public long TableOffset;
            public long Unknown;
            public short TableCount;
            
            // ... more
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct Entry {
            public float A;
            public float B;
            public float C;
            public float D;
        }
        
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct SecondaryEntry {
            public float A;
            public int B;
        }

        public Structure Data { get; private set; }
        public Entry[] Entries { get; private set; }
        public SecondaryEntry[] SecondaryEntries { get; private set; }

        public void Parse(Stream input) {
            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, true)) {
                Data = reader.Read<Structure>();

                if (Data.TableOffset == 0 || Data.TableCount == -1) return;
                reader.BaseStream.Position = Data.TableOffset;

                Entries = new Entry[Data.TableCount];
                SecondaryEntries = new SecondaryEntry[Data.TableCount];
                for (int i = 0; i < Data.TableCount; i++) {
                    Entries[i] = reader.Read<Entry>();
                }
                for (int i = 0; i < Data.TableCount; i++) {
                    SecondaryEntries[i] = reader.Read<SecondaryEntry>();
                }
            }
        }
    }
}
