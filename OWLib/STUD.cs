using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using OWLib.Types;
using System.Reflection;

namespace OWLib {
  public class STUD {    
    private STUDHeader header;
    private STUDInstanceRecord[] records;
    private ISTUDInstance[] instances;

    private STUDManager manager = STUDManager.Instance;

    public STUDHeader Header => header;
    public STUDInstanceRecord[] Records => records;
    public ISTUDInstance[] Instances => instances;
    public STUDManager Manager => manager;

    public STUD(Stream input, bool initalizeAll = true, STUDManager manager = null) {
      if(manager == null) {
        manager = this.manager;
      } else {
        this.manager = manager;
      }

      using(BinaryReader reader = new BinaryReader(input, Encoding.Default, true)) {
        header = reader.Read<STUDHeader>();
        input.Position = (long)header.instanceTableOffset;
        STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();

        records = new STUDInstanceRecord[ptr.count];
        instances = new ISTUDInstance[ptr.count];

        for(ulong i = 0; i < ptr.count; ++i) {
          records[i] = reader.Read<STUDInstanceRecord>();
        }

        if(initalizeAll) {
          InitializeAll(input);
        }
      }
    }

    public void InitializeAll(Stream input) {
      for(long i = 0; i < records.LongLength; ++i) {
        instances[i] = Initialize(input, records[i]);
      }
    }

    public void InitializeAll(Stream input, List<ulong> keys) {
      for(long i = 0; i < records.LongLength; ++i) {
        if(keys.Contains(records[i].key)) {
          instances[i] = Initialize(input, records[i]);
        }
      }
    }

    public ISTUDInstance Initialize(Stream input, STUDInstanceRecord instance) {
      input.Position = instance.offset;
      ISTUDInstance ret = null;
      STUD_MANAGER_ERROR err;
      if((err = manager.InitializeInstance(instance.key, input, out ret)) != STUD_MANAGER_ERROR.E_SUCCESS) {
        if(err != STUD_MANAGER_ERROR.E_UNKNOWN_INSTANCE) {
          Console.Error.WriteLine("Error while instancing for STUD type {0:X16}", err);
        }
        return null;
      }
      return ret;
    }
  }

  public class STUDManager {
    private List<Type> implementations;
    private List<ulong> ids;
    private List<string> names;
    private HashSet<ulong> complained;

    private static STUDManager _Instance = NewInstance();
    public static STUDManager Instance => _Instance;

    public IReadOnlyList<Type> Implementations => implementations;
    public IReadOnlyList<ulong> Ids => ids;
    public IReadOnlyList<string> Names => names;

    public STUDManager() {
      implementations = new List<Type>();
      ids = new List<ulong>();
      names = new List<string>();
      complained = new HashSet<ulong>();
    }

    public Type GetInstance(ulong id) {
      for(int i = 0; i < implementations.Count; ++i) {
        if(ids[i] == id) {
          return implementations[i];
        }
      }
      if(complained.Add(id)) {
        Console.Error.WriteLine("Warning! Unknown Instance ID {0:X16}", id);
      }
      return null;
    }

    public STUD_MANAGER_ERROR InitializeInstance(ulong id, Stream input, out ISTUDInstance instance) {
      return InitializeInstance(GetInstance(id), input, out instance);
    }

    public STUD_MANAGER_ERROR InitializeInstance(Type inst, Stream input, out ISTUDInstance instance) {
      if(inst == null) {
        instance = null;
        return STUD_MANAGER_ERROR.E_UNKNOWN_INSTANCE;
      }

      try {
        instance = (ISTUDInstance)Activator.CreateInstance(inst);
        instance.Read(input);
      } catch (Exception ex) {
        Console.Error.WriteLine(ex.Message);
        instance = null;
        return STUD_MANAGER_ERROR.E_FAULT;
      }

      return STUD_MANAGER_ERROR.E_SUCCESS;
    }

    public string GetName(ulong id) {
      for(int i = 0; i < implementations.Count; ++i) {
        if(ids[i] == id) {
          return names[i];
        }
      }
      return null;
    }

    public string GetName(ISTUDInstance inst) {
      if(inst == null) {
        return null;
      }
      return inst.Name;
    }

    public string GetName(Type inst) {
      if(inst == null) {
        return null;
      }
      ISTUDInstance instance = (ISTUDInstance)Activator.CreateInstance(inst);
      return GetName(instance);
    }

    public ulong GetId(ISTUDInstance inst) {
      if(inst == null) {
        return 0;
      }
      return inst.Key;
    }

    public ulong GetId(Type inst) {
      if(inst == null) {
        return 0;
      }
      ISTUDInstance instance = (ISTUDInstance)Activator.CreateInstance(inst);
      return GetId(instance);
    }

    public STUD_MANAGER_ERROR AddInstance(ISTUDInstance instance) {
      if(instance == null) {
        return STUD_MANAGER_ERROR.E_FAULT;
      }
      return AddInstance(instance.GetType());
    }

    public STUD_MANAGER_ERROR AddInstance(Type instance) {
      if(instance == null) {
        return STUD_MANAGER_ERROR.E_FAULT;
      }
      implementations.Add(instance);
      ids.Add(GetId(instance));
      names.Add(GetName(instance));
      return STUD_MANAGER_ERROR.E_SUCCESS;
    }

    public static STUDManager NewInstance() {
      STUDManager manager = new STUDManager();
      Assembly asm = typeof(ISTUDInstance).Assembly;
      Type t = typeof(ISTUDInstance);
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
