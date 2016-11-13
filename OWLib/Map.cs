using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using OWLib.Types.Map;
using OWLib.Types;

namespace OWLib {
  public class Map {
    private MapHeader header;
    private MapCommonHeader[] commonHeaders;
    private IMapFormat[] records;
    private MapManager manager = MapManager.Instance;

    public MapHeader Header => header;
    private MapCommonHeader[] CommonHeaders => commonHeaders;
    public IMapFormat[] Records => records;
    public MapManager Manager => manager;

    public Map(Stream input, bool open = false) {
      using(BinaryReader reader = new BinaryReader(input, Encoding.Default, open)) {
        header = reader.Read<MapHeader>();
        input.Position = header.offset;
        records = new IMapFormat[header.recordCount];
        commonHeaders = new MapCommonHeader[header.recordCount];
        for(uint i = 0; i < header.recordCount; ++i) {
          commonHeaders[i] = reader.Read<MapCommonHeader>();
          long nps = input.Position + commonHeaders[i].size - 24;
          MANAGER_ERROR err;
          if((err = manager.InitializeInstance(commonHeaders[i].type, input, out records[i])) != MANAGER_ERROR.E_SUCCESS) {
            if(System.Diagnostics.Debugger.IsAttached) {
              System.Diagnostics.Debugger.Log(2, "MAP", string.Format("Error reading Map type {0:X}", commonHeaders[i]));
            }
          }
          input.Position = nps;
        }
      }
    }
  }

  public class MapManager {
    private List<Type> implementations;
    private List<ushort> ids;
    private List<string> names;

    public IReadOnlyList<Type> Implementations => implementations;
    public IReadOnlyList<ushort> Ids => ids;
    public IReadOnlyList<string> Names => names;

    private static MapManager _Instance = NewInstance();
    public static MapManager Instance => _Instance;

    public MapManager() {
      implementations = new List<Type>();
      ids = new List<ushort>();
      names = new List<string>();
    }

    public Type GetInstance(ushort id) {
      for(int i = 0; i < implementations.Count; ++i) {
        if(ids[i] == id) {
          return implementations[i];
        }
      }
      return null;
    }

    public MANAGER_ERROR InitializeInstance(ushort id, Stream input, out IMapFormat instance) {
      return InitializeInstance(GetInstance(id), input, out instance);
    }

    public MANAGER_ERROR InitializeInstance(Type inst, Stream input, out IMapFormat instance) {
      if(inst == null) {
        instance = null;
        return MANAGER_ERROR.E_UNKNOWN;
      }

      if(System.Diagnostics.Debugger.IsAttached) {
        instance = (IMapFormat)Activator.CreateInstance(inst);
        instance.Read(input);
        return MANAGER_ERROR.E_SUCCESS;
      }

      try {
        instance = (IMapFormat)Activator.CreateInstance(inst);
        instance.Read(input);
      } catch (Exception ex) {
        Console.Error.WriteLine(ex.Message);
        instance = null;
        return MANAGER_ERROR.E_FAULT;
      }

      return MANAGER_ERROR.E_SUCCESS;
    }

    public string GetName(ushort id) {
      for(int i = 0; i < implementations.Count; ++i) {
        if(ids[i] == id) {
          return names[i];
        }
      }
      return null;
    }

    public string GetName(IMapFormat inst) {
      if(inst == null) {
        return null;
      }
      return inst.Name;
    }

    public string GetName(Type inst) {
      if(inst == null) {
        return null;
      }
      if(implementations.Contains(inst)) {
        if(names.Count > implementations.IndexOf(inst)) {
          return names[implementations.IndexOf(inst)];
        }
      }
      IMapFormat instance = (IMapFormat)Activator.CreateInstance(inst);
      return GetName(instance);
    }

    public ushort GetId(IMapFormat inst) {
      if(inst == null) {
        return 0;
      }
      return inst.Identifier;
    }

    public ushort GetId(Type inst) {
      if(inst == null) {
        return 0;
      }
      IMapFormat instance = (IMapFormat)Activator.CreateInstance(inst);
      return GetId(instance);
    }

    public MANAGER_ERROR AddInstance(IMapFormat instance) {
      if(instance == null) {
        return MANAGER_ERROR.E_FAULT;
      }
      return AddInstance(instance.GetType());
    }

    public MANAGER_ERROR AddInstance(Type instance) {
      if(instance == null) {
        return MANAGER_ERROR.E_FAULT;
      }
      if(implementations.Contains(instance)) {
        return MANAGER_ERROR.E_DUPLICATE;
      }
      if(ids.Contains(GetId(instance))) {
        return MANAGER_ERROR.E_DUPLICATE;
      }
      if(names.Contains(GetName(instance))) {
        return MANAGER_ERROR.E_DUPLICATE;
      }
      implementations.Add(instance);
      ids.Add(GetId(instance));
      names.Add(GetName(instance));
      return MANAGER_ERROR.E_SUCCESS;
    }

    public static MapManager NewInstance() {
      MapManager manager = new MapManager();
      Assembly asm = typeof(IMapFormat).Assembly;
      Type t = typeof(IMapFormat);
      List<Type> types = asm.GetTypes().Where(type => type != t && t.IsAssignableFrom(type)).ToList();
      foreach(Type type in types) {
        if(type.IsInterface) {
          continue;
        }
        manager.AddInstance(type);
      }
      return manager;
    }
  }
}
