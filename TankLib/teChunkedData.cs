using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
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

        /// <summary>Copy bytes from one <see cref="Stream"/> to another</summary>
        /// <param name="input">The stream to read from</param>
        /// <param name="output">The stream to write to</param>
        /// <param name="size">Number of bytes to read and write</param>
        private static void CopyBytes(Stream input, Stream output, int size) {
            byte[] buffer = new byte[size];
            input.Read(buffer, 0, size);
            output.Write(buffer, 0, size);
            buffer = null;
        }
        
        /// <summary>Load chunk data from a <see cref="Stream"/></summary>
        /// <param name="input">The source <see cref="Stream"/>></param>
        public teChunkedData(Stream input) {
            if (input == null) {
                return;
            }
            
            List<IChunk> chunks = new List<IChunk>();

            long start = input.Position;

            using (BinaryReader reader = new BinaryReader(input)) {
                Header = reader.Read<teChunkDataHeader>();
                if (Header.Magic != Magic) {
                    Header = default(teChunkDataHeader);
                    return;
                }

                long next = start + input.Position;
                while (next < Header.Size) {
                    teChunkDataEntry entry = reader.Read<teChunkDataEntry>();
                    next = start + input.Position + entry.Size;
                    
                    IChunk chunk = Manager.CreateChunkInstance(entry.StringIdentifier, Header.StringIdentifier);
                    if (chunk != null) {
                        MemoryStream dataStream = new MemoryStream(entry.Size);
                        CopyBytes(input, dataStream, entry.Size);
                        dataStream.Position = 0;
                        
                        chunk.Parse(dataStream);
                        
                        dataStream.Dispose();
                    }
                    
                    chunks.Add(chunk);
                    input.Position = next;
                }
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

        public IEnumerable<IChunk> GetChunks<T>() where T : IChunk {
            return Chunks.Where(x => x is T);
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
            if (ChunkTypes.ContainsKey(id)) {
                return (IChunk) Activator.CreateInstance(ChunkTypes[id]);
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