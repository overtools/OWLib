using System;
using System.IO;
using OWLib.Types;
using OWLib.Types.STUD;

namespace OWLib {
  public class STUD {
    private STUDManager manager;

    private STUDHeader header;
    private STUDTableInstanceRecord[] instanceTable;
    private STUDBlob blob;

    public STUDHeader Header => header;
    public STUDTableInstanceRecord[] InstanceTable => instanceTable;
    public STUDBlob Blob => blob;

    public string Name
    {
      get
      {
        return manager.GetName(header.type);
      }
    }

    public STUD(STUDManager manager, Stream stream) {
      stream.Seek(0, SeekOrigin.Begin);
      this.manager = manager;
      using(BinaryReader reader = new BinaryReader(stream)) {
        header = reader.Read<STUDHeader>();
        blob = manager.NewInstance(header.type, stream);
        if(blob == null) {
          throw new Exception(string.Format("Unknown STUD type {0:X8}", header.type));
        }
        STUDPointer ptr = reader.Read<STUDPointer>();
        stream.Seek((long)ptr.offset, SeekOrigin.Begin);
        instanceTable = new STUDTableInstanceRecord[ptr.count];
        for(ulong i = 0; i < ptr.count; ++i) {
          instanceTable[i] = reader.Read<STUDTableInstanceRecord>();
        }
      }
    }

    public static void DumpInstance(TextWriter writer, STUDTableInstanceRecord instance) {
      writer.WriteLine("\tID: {0}", instance.id);
      writer.WriteLine("\tFlags: {0}", instance.flags);
      writer.WriteLine("\tKey: {0}", instance.key);
    }
    
    public void Dump(TextWriter writer) {
      writer.WriteLine("STUD Identifier: {0} / {0:X8}", header.type);
      writer.WriteLine("{0} instance records...", InstanceTable.Length);
      for(int i = 0; i < InstanceTable.Length; ++i) {
        DumpInstance(writer, InstanceTable[i]);
        writer.WriteLine("");
      }
      manager.Dump(header.type, blob, Console.Out);
    }
  }

  public class STUDManager {

    private Type[] handlers;
    private uint[] handlerIds;
    private string[] handlerNames;

    public STUDManager() {
      handlers = new Type[0];
      handlerIds = new uint[0];
      handlerNames = new string[0];
    }

    public static STUDManager Create() {
      STUDManager stud = new STUDManager();
      stud.AddHandler<A301496F>();
      stud.AddHandler<x3ECCEB5D>();
      stud.AddHandler<x15720E8A>();
      stud.AddHandler<x0BCAF9C9>();
      stud.AddHandler<x8CDAA871>();
      stud.AddHandler<x8B9DEB02>();
      stud.AddHandler<x61632B43>();
      stud.AddHandler<x4EE84DC0>();
      stud.AddHandler<x01609B4D>();
      stud.AddHandler<E533D614>();
      stud.AddHandler<x018667E2>();
      stud.AddHandler<x090B30AB>();
      stud.AddHandler<x33F56AC1>();
      stud.AddHandler<FF82DF73>();
      return stud;
    }

    public void AddHandler(Type T) {
      int i = handlers.Length;

      {
        Type[] tmpT = new Type[handlers.Length + 1];
        handlers.CopyTo(tmpT, 0);
        handlers = tmpT;
        uint[] tmpI = new uint[handlerIds.Length + 1];
        handlerIds.CopyTo(tmpI, 0);
        handlerIds = tmpI;
        string[] tmpN = new string[handlerNames.Length + 1];
        handlerNames.CopyTo(tmpN, 0);
        handlerNames = tmpN;
      }

      handlers[i] = T;
      handlerIds[i] = (uint)handlers[i].GetField("id", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).GetValue(null);
      handlerNames[i] = (string)handlers[i].GetField("name", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public).GetValue(null);
    }

    public void AddHandler<T>() where T : STUDBlob {
      AddHandler(typeof(T));
    }

    public STUDBlob NewInstance(uint id, Stream data) {
      for(int i = 0; i < handlerIds.Length; ++i) {
        if(handlerIds[i] == id) {
          STUDBlob inst = (STUDBlob)(handlers[i].GetConstructor(new Type[] { }).Invoke(new object[] { }));
          handlers[i].GetMethod("Read", new Type[] { typeof(Stream) }).Invoke(inst, new object[] { data });
          return inst;
        }
      }
      return null;
    }

    public string GetName(uint id) {
      for(int i = 0; i < handlerIds.Length; ++i) {
        if(handlerIds[i] == id) {
          return handlerNames[i];
        }
      }
      return STUDBlob.name;
    }

    public void Dump(uint id, STUDBlob inst, TextWriter writer) {
      for(int i = 0; i < handlerIds.Length; ++i) {
        if(handlerIds[i] == id) {
          handlers[i].GetMethod("Dump", new Type[] { typeof(TextWriter) }).Invoke(inst, new object[] { writer });
          break;
        }
      }
    }
  }
}
