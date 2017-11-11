using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OWLib.Types;
using OWLib.Types.Chunk;

namespace OWLib {
    public class MemoryChunk : IChunk {
        private string identifier = null;
        public string Identifier {
            get {
                return identifier;
            } set {
                identifier = value;
            }
        }

        private string rootIdentifier = null;
        public string RootIdentifier {
            get {
                return rootIdentifier;
            } set {
                rootIdentifier = value;
            }
        }
        
        private MemoryStream data;
        public MemoryStream Data => data;

        public void Parse(Stream input) {
            if (Util.DEBUG) {
                data = new MemoryStream();
                input.CopyTo(data);
                data.Position = 0;
            }
        }
    }

    public class Chunked : IDisposable {
        public const uint CHUNK_MAGIC = 0xF123456F;

        private List<IChunk> chunks;
        public IReadOnlyList<IChunk> Chunks => chunks;

        private ChunkedHeader header;
        public ChunkedHeader Header => header;

        private long start;
        private List<ChunkedEntry> entrees;
        private List<long> entryOffsets;
        private ChunkManager manager = ChunkManager.Instance;

        private static void CopyBytes(Stream i, Stream o, int sz) {
            byte[] buffer = new byte[sz];
            i.Read(buffer, 0, sz);
            o.Write(buffer, 0, sz);
            buffer = null;
        }

        public Chunked(Stream input, bool keepOpen = false, ChunkManager manager = null) {
            if (manager == null) {
                manager = this.manager;
            } else {
                this.manager = manager;
            }
            chunks = new List<IChunk>();
            entrees = new List<ChunkedEntry>();
            entryOffsets = new List<long>();
            if (input == null) {
                return;
            }

            start = input.Position;

            using (BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, keepOpen)) {
                header = reader.Read<ChunkedHeader>();
                if (header.magic != CHUNK_MAGIC) {
                    return;
                }

                long next = input.Position;
                while (next < header.size) {
                    ChunkedEntry entry = reader.Read<ChunkedEntry>();
                    long offset = input.Position;
                    next = offset + entry.size;
                    IChunk chunk = manager.NewChunk(entry.StringIdentifier, header.StringIdentifier);
                    if (chunk != null) {
                        MemoryStream dataStream = new MemoryStream(entry.size);
                        CopyBytes(input, dataStream, entry.size);
                        dataStream.Position = 0;
                        chunk.Parse(dataStream);
                        try {
                            dataStream.Dispose();
                        } catch { }
                    }
                    chunks.Add(chunk);
                    entrees.Add(entry);
                    entryOffsets.Add(offset);
                    input.Position = next;
                }
            }
        }

        public KeyValuePair<int, IChunk> FindNextChunk(Type type, int after = 0) {
            for (int i = after; i < chunks.Count; ++i) {
                if (chunks[i] != null && type.IsInstanceOfType(chunks[i])) {
                    return new KeyValuePair<int, IChunk>(i, chunks[i]);
                }
            }
            return new KeyValuePair<int, IChunk>(-1, null);
        }

        public KeyValuePair<int, IChunk> FindNextChunk(string identifier, int after = 0) {
            for (int i = after; i < chunks.Count; ++i) {
                if (chunks[i] != null && chunks[i].Identifier == identifier) {
                    return new KeyValuePair<int, IChunk>(i, chunks[i]);
                }
            }
            return new KeyValuePair<int, IChunk>(-1, null);
        }

        public KeyValuePair<int, T>[] GetAllOfType<T>(int after = 0) where T : IChunk {
            List<KeyValuePair<int, T>> ret = new List<KeyValuePair<int, T>>();
            Type type = typeof(T);
            for (int i = after; i < chunks.Count; ++i) {
                if (chunks[i] != null && type.IsInstanceOfType(chunks[i])) {
                    ret.Add(new KeyValuePair<int, T>(i, (T)chunks[i]));
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
            chunks.Clear();
            entrees.Clear();
            entryOffsets.Clear();
            GC.SuppressFinalize(this);
        }

        public bool HasChunk<T>() {
            Type type = typeof(T);
            for (int i = 0; i < chunks.Count; ++i) {
                if (chunks[i] != null && type.IsInstanceOfType(chunks[i])) {
                    return true;
                }
            }
            return false;
        }
    }

    public class ChunkManager {
        private static ChunkManager _Instance = NewInstance();
        public static ChunkManager Instance => _Instance;

        private static HashSet<string> unhandledChunkIdentifiers = new HashSet<string>();
        public Dictionary<string, Type> chunkMap;

        private Type MEMORY_TYPE = typeof(MemoryChunk);

        public ChunkManager() {
            chunkMap = new Dictionary<string, Type>();
        }

        public MANAGER_ERROR AddChunk(Type chunk) {
            if (chunk == null) {
                return MANAGER_ERROR.E_FAULT;
            }
            if (chunkMap.ContainsValue(chunk)) {
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
            chunkMap.Add(identifier, chunk);
            return MANAGER_ERROR.E_SUCCESS;
        }
        
        public string ReverseString(string text){
            if (text == null) return null;
            char[] array = text.ToCharArray();
            Array.Reverse(array);
            return new string(array);
        }

        public IChunk NewChunk(string id, string root) {
            string identifier = root + id;
            if (chunkMap.ContainsKey(identifier)) {
                return (IChunk)Activator.CreateInstance(chunkMap[identifier]);
            } else {
                if (unhandledChunkIdentifiers.Add(identifier)) {
                    if (System.Diagnostics.Debugger.IsAttached) {
                        System.Diagnostics.Debugger.Log(2, "CHUNK", $"Error! No handler for chunk type {identifier} ({ReverseString(root)}:{ReverseString(id)})\n");
                    }
                }
                if (System.Diagnostics.Debugger.IsAttached || Util.DEBUG) {
                    MemoryChunk memory = (MemoryChunk)Activator.CreateInstance(MEMORY_TYPE);
                    memory.Identifier = id;
                    memory.RootIdentifier = root;
                    return memory;
                }
            }
            return null;
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
