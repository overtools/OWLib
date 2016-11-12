using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OWLib.Types.Chunk;

namespace OWLib.Types {
  public class Chunked {
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
      if(manager == null) {
        manager = this.manager;
      } else {
        this.manager = manager;
      }
      chunks = new List<IChunk>();
      entrees = new List<ChunkedEntry>();
      entryOffsets = new List<long>();

      start = input.Position;

      using(BinaryReader reader = new BinaryReader(input, System.Text.Encoding.Default, keepOpen)) {
        header = reader.Read<ChunkedHeader>();
        if(header.magic != 0xF123456F) {
          return;
        }

        long next = input.Position;
        while(next < header.size) {
          ChunkedEntry entry = reader.Read<ChunkedEntry>();
          long offset = input.Position;
          next = offset + entry.size;
          IChunk chunk = manager.NewChunk(entry.GetIdentifier(), header.GetIdentifier());
          if(chunk != null) {
            MemoryStream dataStream = new MemoryStream(entry.size);
            CopyBytes(input, dataStream, entry.size);
            dataStream.Position = 0;
            chunk.Parse(dataStream);
          }
          chunks.Add(chunk);
          entrees.Add(entry);
          entryOffsets.Add(offset);
          input.Position = next;
        }
      }
    }

    public KeyValuePair<int, IChunk> FindNextChunk(Type type, int after = 0) {
      for(int i = after; i < chunks.Count; ++i) {
        if(chunks[i] != null && type.IsInstanceOfType(chunks[i])) {
          return new KeyValuePair<int, IChunk>(i, chunks[i]);
        }
      }
      return new KeyValuePair<int, IChunk>(-1, null);
    }

    public KeyValuePair<int, IChunk> FindNextChunk(string identifier, int after = 0) {
      for(int i = after; i < chunks.Count; ++i) {
        if(chunks[i] != null && chunks[i].Identifier == identifier) {
          return new KeyValuePair<int, IChunk>(i, chunks[i]);
        }
      }
      return new KeyValuePair<int, IChunk>(-1, null);
    }
  }

  public class ChunkManager {
    private static ChunkManager _Instance = NewInstance();
    public static ChunkManager Instance => _Instance;

    private static HashSet<string> unhandledChunkIdentifiers = new HashSet<string>();
    public Dictionary<string, Type> chunkMap;

    public ChunkManager() {
      chunkMap = new Dictionary<string, Type>();
    }

    public MANAGER_ERROR AddChunk(Type chunk) {
      if(chunk == null) {
        return MANAGER_ERROR.E_FAULT;
      }
      if(chunkMap.ContainsValue(chunk)) {
        return MANAGER_ERROR.E_DUPLICATE;
      }
      IChunk instance = (IChunk)Activator.CreateInstance(chunk);
      string identifier = instance.RootIdentifier + instance.Identifier;
      if(identifier == null) {
        Console.Error.WriteLine("Error! {0} has no identifier!", chunk.FullName);
      }
      chunkMap.Add(identifier, chunk);
      return MANAGER_ERROR.E_SUCCESS;
    }

    public IChunk NewChunk(string id, string root) {
      string identifier = root + id;
      if(chunkMap.ContainsKey(identifier)) {
        return (IChunk)Activator.CreateInstance(chunkMap[identifier]);
      } else {
        if(unhandledChunkIdentifiers.Add(identifier)) {
          Console.Error.WriteLine("Error! No handler for chunk type {0}", identifier);
        }
      }
      return null;
    }

    public static ChunkManager NewInstance() {
      ChunkManager manager = new ChunkManager();
      Assembly asm = typeof(IChunk).Assembly;
      Type t = typeof(IChunk);
      List<Type> types = asm.GetTypes().Where(type => type != t && t.IsAssignableFrom(type)).ToList();
      foreach(Type type in types) {
        if(type.IsInterface) {
          continue;
        }
        manager.AddChunk(type);
      }
      return manager;
    }
  }
}
