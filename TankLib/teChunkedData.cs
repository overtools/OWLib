using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using TankLib.CASC;
using TankLib.Chunks;

namespace TankLib {
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct teChunkDataHeader {
        public uint Magic;
        public uint ID;
        public int Size;
        public int Unknown;

        public string StringIdentifier => Encoding.ASCII.GetString(BitConverter.GetBytes(ID)).ReverseXor();

        public new string ToString() {
            return base.ToString() + $" ({StringIdentifier})";
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct teChunkDataEntry {
        public uint ID;
        public int Unknown1;
        public int Size;
        public ushort Unknown2;
        public ushort Unknown3;

        public string StringIdentifier => Encoding.ASCII.GetString(BitConverter.GetBytes(ID)).ReverseXor();

        public new string ToString() {
            return base.ToString() + $" ({StringIdentifier})";
        }
    }
    
    /// <summary>"Chunked" file parser</summary>
    public class teChunkedData {
        public const uint Magic = 0xF123456F;

        public IChunk[] Chunks;
        public teChunkDataHeader Header;
        
        public static teChunkManager Manager = new teChunkManager();

        /// <summary>Load chunk data from a <see cref="Stream"/></summary>
        /// <param name="input">The source <see cref="Stream"/>></param>
        /// <param name="keepOpen">Keep the stream open after reading</param>
        public teChunkedData(Stream input, bool keepOpen=false) {
            if (input == null) {
                return;
            }

            using (BinaryReader reader = new BinaryReader(input, Encoding.UTF8, keepOpen)) {
                Read(reader);
            }
        }
        
        /// <summary>Load chunk data from a <see cref="BinaryReader"/></summary>
        /// <param name="reader">The source <see cref="BinaryReader"/>></param>
        public teChunkedData(BinaryReader reader) {
            Read(reader);
        }

        private unsafe void Read(BinaryReader reader) {
            long start = reader.BaseStream.Position;

            List<IChunk> chunks = new List<IChunk>();

            Header = reader.Read<teChunkDataHeader>();
            if (Header.Magic != Magic) {
                return;
            }

            long next = reader.BaseStream.Position - start;  // rel stream pos
            
            while (next < Header.Size) {
                teChunkDataEntry entry = reader.Read<teChunkDataEntry>();
                next += entry.Size + sizeof(teChunkDataEntry);
                    
                IChunk chunk = Manager.CreateChunkInstance(entry.StringIdentifier, Header.StringIdentifier);
                if (chunk != null) {
                    MemoryStream dataStream = new MemoryStream(entry.Size);
                    reader.BaseStream.CopyBytes(dataStream, entry.Size);
                    dataStream.Position = 0;
                        
                    chunk.Parse(dataStream);
                        
                    dataStream.Dispose();
                }
                    
                chunks.Add(chunk);
                reader.BaseStream.Position = next + start;
            }
            Chunks = chunks.ToArray();
        }

        public IEnumerable<T> EnumerateChunks<T>() where T : IChunk {
            foreach (IChunk chunk in Chunks) {
                if (chunk is T cast) {
                    yield return cast;
                }
            }
        }

        public bool HasChunk<T>() where T : IChunk {
            return Chunks.Any(x => x is T);
        }

        public T GetChunk<T>() where T : IChunk {
            return (T)Chunks.FirstOrDefault(x => x is T);
        }

        public IEnumerable<T> GetChunks<T>() where T : IChunk {
            return Chunks.OfType<T>();
        }
    }

    /// <summary>Chunk type manager</summary>
    public class teChunkManager {
        /// <summary>Chunk type lookup</summary>
        public Dictionary<string, Type> ChunkTypes;
        
        /// <summary>Chunks that have no implementation</summary>
        public HashSet<string> UnhandledChunks;
        
        public teChunkManager() {
            ChunkTypes = new Dictionary<string, Type>();
            UnhandledChunks = new HashSet<string>();
            
            AddAssemblyChunks(typeof(IChunk).Assembly);
        }
        
        /// <summary>Add all chunk types in an <see cref="Assembly"/></summary>
        /// <param name="assembly">The <see cref="Assembly"/> to load from</param>
        public void AddAssemblyChunks(Assembly assembly) {
            Type t = typeof(IChunk);
            List<Type> types = assembly.GetTypes().Where(type => type != t && t.IsAssignableFrom(type)).ToList();
            foreach (Type type in types) {
                if (type.IsInterface) {
                    continue;
                }
                AddChunk(type);
            }
        }

        /// <summary>Add a chunk type</summary>
        /// <param name="type">The type to add</param>
        public void AddChunk(Type type) {
            IChunk instance = (IChunk)Activator.CreateInstance(type);
            if (instance.ID == null) {
                Debugger.Log(0, "teChunkManager", $"{type.FullName} has no identifier!\r\n");
                return;
            }
            ChunkTypes.Add(instance.ID, type);
        }

        /// <summary>Create a chunk instance from a chunk ID</summary>
        /// <param name="id">ID of instance to create</param>
        /// <param name="rootID">ID of the header chunk</param>
        public IChunk CreateChunkInstance(string id, string rootID) {
            if (ChunkTypes.TryGetValue(id, out Type chunkType)) {
                return (IChunk) Activator.CreateInstance(chunkType);
            }
            
            if (UnhandledChunks.Add(id)) {
                if (Debugger.IsAttached) {
                    Debugger.Log(0, "teChunkManager", $"No handler for {rootID}:{id}\r\n");
                }
            }
                
            return null;
        }
    }
}