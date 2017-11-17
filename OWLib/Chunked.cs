using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OWLib.Types;
using OWLib.Types.Chunk;

namespace OWLib {
    public class MemoryChunk : IChunk {
        public string Identifier { get; set; }
        public string RootIdentifier { get; set; }

        public MemoryStream Data { get; private set; }

        public void Parse(Stream input) {
            if (!Util.DEBUG) return;
            Data = new MemoryStream();
            input.CopyTo(Data);
            Data.Position = 0;
        }
    }

    public class Chunked : IDisposable {
        public const uint ChunkMagic = 0xF123456F;

        public List<IChunk> Chunks { get; }
        public ChunkedHeader Header { get; }

        private long start;
        private List<ChunkedEntry> entrees;
        private List<long> entryOffsets;
        public readonly ChunkManager Manager = ChunkManager.Instance;

        private static void CopyBytes(Stream i, Stream o, int sz) {
            byte[] buffer = new byte[sz];
            i.Read(buffer, 0, sz);
            o.Write(buffer, 0, sz);
            buffer = null;
        }

        public Chunked(Stream input, bool keepOpen = false, ChunkManager manager = null) {
            if (manager == null) {
                manager = Manager;
            } else {
                Manager = manager;
            }
            Chunks = new List<IChunk>();
            entrees = new List<ChunkedEntry>();
            entryOffsets = new List<long>();
            if (input == null) {
                return;
            }

            start = input.Position;

            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, keepOpen)) {
                Header = reader.Read<ChunkedHeader>();
                if (Header.magic != ChunkMagic) {
                    return;
                }

                long next = input.Position;
                while (next < Header.size) {
                    ChunkedEntry entry = reader.Read<ChunkedEntry>();
                    long offset = input.Position;
                    next = offset + entry.size;
                    IChunk chunk = manager.NewChunk(entry.StringIdentifier, Header.StringIdentifier);
                    if (chunk != null) {
                        MemoryStream dataStream = new MemoryStream(entry.size);
                        CopyBytes(input, dataStream, entry.size);
                        dataStream.Position = 0;
                        chunk.Parse(dataStream);
                        try {
                            dataStream.Dispose();
                        } catch { }
                    }
                    Chunks.Add(chunk);
                    entrees.Add(entry);
                    entryOffsets.Add(offset);
                    input.Position = next;
                }
            }
        }

        public KeyValuePair<int, IChunk> FindNextChunk(Type type, int after = 0) {
            for (int i = after; i < Chunks.Count; ++i) {
                if (Chunks[i] != null && type.IsInstanceOfType(Chunks[i])) {
                    return new KeyValuePair<int, IChunk>(i, Chunks[i]);
                }
            }
            return new KeyValuePair<int, IChunk>(-1, null);
        }

        public KeyValuePair<int, IChunk> FindNextChunk(string identifier, int after = 0) {
            for (int i = after; i < Chunks.Count; ++i) {
                if (Chunks[i] != null && Chunks[i].Identifier == identifier) {
                    return new KeyValuePair<int, IChunk>(i, Chunks[i]);
                }
            }
            return new KeyValuePair<int, IChunk>(-1, null);
        }

        public KeyValuePair<int, T>[] GetAllOfType<T>(int after = 0) where T : IChunk {
            List<KeyValuePair<int, T>> ret = new List<KeyValuePair<int, T>>();
            Type type = typeof(T);
            for (int i = after; i < Chunks.Count; ++i) {
                if (Chunks[i] != null && type.IsInstanceOfType(Chunks[i])) {
                    ret.Add(new KeyValuePair<int, T>(i, (T)Chunks[i]));
                }
            }
            return ret.ToArray<KeyValuePair<int, T>>();
        }

        public T[] GetAllOfTypeFlat<T>(int after = 0) where T : IChunk {
            KeyValuePair<int, T>[] v = GetAllOfType<T>(after);
            T[] ret = new T[v.Length];
            for (int i = 0; i < v.Length; ++i) {
                ret[i] = v[i].Value;
            }
            return ret;
        }

        public void Dispose() {
            Chunks.Clear();
            entrees.Clear();
            entryOffsets.Clear();
            GC.SuppressFinalize(this);
        }

        public bool HasChunk<T>() {
            Type type = typeof(T);
            return Chunks.Any(chunk => chunk != null && type.IsInstanceOfType(chunk));
        }
    }

    public class ChunkManager {
        public static ChunkManager Instance { get; } = NewInstance();

        public HashSet<string> UnhandledChunkIdentifiers = new HashSet<string>();
        public Dictionary<string, Type> ChunkMap;

        private readonly Type _memoryType = typeof(MemoryChunk);

        public ChunkManager() {
            ChunkMap = new Dictionary<string, Type>();
        }

        public MANAGER_ERROR AddChunk(Type chunk) {
            if (chunk == null) {
                return MANAGER_ERROR.E_FAULT;
            }
            if (ChunkMap.ContainsValue(chunk)) {
                return MANAGER_ERROR.E_DUPLICATE;
            }
            IChunk instance = (IChunk)Activator.CreateInstance(chunk);
            if (instance.RootIdentifier == null || instance.Identifier == null) {
                return MANAGER_ERROR.E_SUCCESS;
            }
            string identifier = instance.RootIdentifier + instance.Identifier;
            if (identifier == null) {
                if (System.Diagnostics.Debugger.IsAttached) {
                    System.Diagnostics.Debugger.Log(2, "CHUNK", $"Error! {chunk.FullName} has no identifier!\n");
                }
            }
            ChunkMap.Add(identifier, chunk);
            return MANAGER_ERROR.E_SUCCESS;
        }
        
        public static string ReverseString(string text){
            if (text == null) return null;
            char[] array = text.ToCharArray();
            Array.Reverse(array);
            return new string(array);
        }

        public IChunk NewChunk(string id, string root) {
            string identifier = root + id;
            if (ChunkMap.ContainsKey(identifier)) {
                return (IChunk)Activator.CreateInstance(ChunkMap[identifier]);
            }
            if (UnhandledChunkIdentifiers.Add(identifier)) {
                if (System.Diagnostics.Debugger.IsAttached) {
                    System.Diagnostics.Debugger.Log(2, "CHUNK", $"Error! No handler for chunk type {identifier} ({ReverseString(root)}:{ReverseString(id)})\n");
                }
            }
            if (!System.Diagnostics.Debugger.IsAttached && !Util.DEBUG) return null;
            MemoryChunk memory = (MemoryChunk)Activator.CreateInstance(_memoryType);
            memory.Identifier = id;
            memory.RootIdentifier = root;
            return memory;
        }

        public static ChunkManager NewInstance() {
            ChunkManager manager = new ChunkManager();
            Assembly asm = typeof(IChunk).Assembly;
            Type t = typeof(IChunk);
            List<Type> types = asm.GetTypes().Where(type => type != t && t.IsAssignableFrom(type)).ToList();
            foreach (Type type in types) {
                if (type.IsInterface) {
                    continue;
                }
                manager.AddChunk(type);
            }
            return manager;
        }
    }
}
