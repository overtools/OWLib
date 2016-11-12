using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using OWLib.Types.Chunk;

namespace OWLib.Types {
  public class Chunked {
    public Chunked(Stream input, bool keepOpen = false) {

    }
  }

  public class ChunkManager {
    public ChunkManager Instance = NewInstance();

    public Dictionary<string, Type> chunkMap;

    public void AddChunk(Type type) {

    }

    public IChunk ReadChunk(Stream data, string identifier) {
      return null;
    }

    public static ChunkManager NewInstance() {
      ChunkManager manager = new ChunkManager();
      Assembly asm = typeof(ISTUDInstance).Assembly;
      Type t = typeof(ISTUDInstance);
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
