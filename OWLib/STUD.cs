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

    public STUD(Stream input, bool initalizeAll = true, STUDManager manager = null, bool leaveOpen = false, bool suppress = false) {
      if(manager == null) {
        manager = this.manager;
      } else {
        this.manager = manager;
      }

      using(BinaryReader reader = new BinaryReader(input, Encoding.Default, leaveOpen)) {
        header = reader.Read<STUDHeader>();
        if(header.magic != 0x53545544) {
          records = new STUDInstanceRecord[0];
          instances = new ISTUDInstance[0];
          return;
        }
        input.Position = (long)header.instanceTableOffset;
        STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();

        records = new STUDInstanceRecord[ptr.count];
        instances = new ISTUDInstance[ptr.count];

        for(ulong i = 0; i < ptr.count; ++i) {
          records[i] = reader.Read<STUDInstanceRecord>();
        }

        if(initalizeAll) {
          InitializeAll(input, suppress);
        }
      }
    }

    public void InitializeAll(Stream input, bool suppress) {
      for(long i = 0; i < records.LongLength; ++i) {
        instances[i] = Initialize(input, records[i], suppress);
      }
    }

    public void InitializeAll(Stream input, List<ulong> keys, bool suppress) {
      for(long i = 0; i < records.LongLength; ++i) {
        if(keys.Contains(records[i].key)) {
          instances[i] = Initialize(input, records[i], suppress);
        }
      }
    }

    public ISTUDInstance Initialize(Stream input, STUDInstanceRecord instance, bool suppress) {
      input.Position = instance.offset;
      ISTUDInstance ret = null;
      STUD_MANAGER_ERROR err;
      bool outputOffset = STUDManager.Complained.Contains(instance.key);
      if((err = manager.InitializeInstance(instance.key, input, out ret, suppress)) != STUD_MANAGER_ERROR.E_SUCCESS) {
        if(err != STUD_MANAGER_ERROR.E_UNKNOWN_INSTANCE) {
          Console.Error.WriteLine("Error while instancing for STUD type {0:X16}", instance.key);
        } else if(!suppress && !outputOffset) {
          Console.Error.WriteLine("Instance is at offset {0:X16}", instance.offset);
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

    private static HashSet<ulong> complained = new HashSet<ulong>();
    public static HashSet<ulong> Complained => complained;

    private static STUDManager _Instance = NewInstance();
    public static STUDManager Instance => _Instance;

    public IReadOnlyList<Type> Implementations => implementations;
    public IReadOnlyList<ulong> Ids => ids;
    public IReadOnlyList<string> Names => names;

    public STUDManager() {
      implementations = new List<Type>();
      ids = new List<ulong>();
      names = new List<string>();
    }

    public Type GetInstance(ulong id, bool suppress) {
      for(int i = 0; i < implementations.Count; ++i) {
        if(ids[i] == id) {
          return implementations[i];
        }
      }
      if(complained.Add(id) && !suppress) {
        Console.Error.WriteLine("Warning! Unknown Instance ID {0:X16}", id);
      }
      return null;
    }

    public STUD_MANAGER_ERROR InitializeInstance(ulong id, Stream input, out ISTUDInstance instance, bool suppress) {
      return InitializeInstance(GetInstance(id, suppress), input, out instance);
    }

    public STUD_MANAGER_ERROR InitializeInstance(Type inst, Stream input, out ISTUDInstance instance) {
      if(inst == null) {
        instance = null;
        return STUD_MANAGER_ERROR.E_UNKNOWN_INSTANCE;
      }

      if(System.Diagnostics.Debugger.IsAttached) {
        instance = (ISTUDInstance)Activator.CreateInstance(inst);
        instance.Read(input);
        return STUD_MANAGER_ERROR.E_SUCCESS;
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
      if(implementations.Contains(inst)) {
        if(names.Count > implementations.IndexOf(inst)) {
          return names[implementations.IndexOf(inst)];
        }
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
      if(implementations.Contains(instance)) {
        return STUD_MANAGER_ERROR.E_DUPLICATE;
      }
      if(ids.Contains(GetId(instance))) {
        return STUD_MANAGER_ERROR.E_DUPLICATE;
      }
      if(names.Contains(GetName(instance))) {
        return STUD_MANAGER_ERROR.E_DUPLICATE;
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
