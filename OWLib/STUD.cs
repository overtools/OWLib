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

    public long end = -1;
    public long start = -1;

    public STUD(Stream input, bool initalizeAll = true, STUDManager manager = null, bool leaveOpen = false, bool suppress = false) {
      if(manager == null) {
        manager = this.manager;
      } else {
        this.manager = manager;
      }

      if(input == null) {
        return;
      }

      using(BinaryReader reader = new BinaryReader(input, Encoding.Default, leaveOpen)) {
        start = input.Position;
        header = reader.Read<STUDHeader>();
        if(header.magic != 0x53545544) {
          records = new STUDInstanceRecord[0];
          instances = new ISTUDInstance[0];
          return;
        }
        input.Position = start + (long)header.instanceTableOffset;
        STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();

        records = new STUDInstanceRecord[ptr.count];
        instances = new ISTUDInstance[ptr.count];

        for(ulong i = 0; i < ptr.count; ++i) {
          records[i] = reader.Read<STUDInstanceRecord>();
        }

        end = input.Position;

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

    public ISTUDInstance Initialize(Stream input, STUDInstanceRecord instance, bool suppress) {
      input.Position = start + instance.offset;
      uint id = 0;
      using(BinaryReader reader = new BinaryReader(input, Encoding.Default, true)) {
        id = reader.ReadUInt32();
      }
      input.Position -= 4;
      ISTUDInstance ret = null;
      MANAGER_ERROR err;
      bool outputOffset = STUDManager.Complained.Contains(id);
      if((err = manager.InitializeInstance(id, input, out ret, suppress)) != MANAGER_ERROR.E_SUCCESS) {
        if(err != MANAGER_ERROR.E_UNKNOWN) {
          if(System.Diagnostics.Debugger.IsAttached) {
            System.Diagnostics.Debugger.Log(2, "STUD", string.Format("[STUD] Error while instancing for STUD type {0:X8}\n", id));
          }
        } else if(!outputOffset) {
          if(System.Diagnostics.Debugger.IsAttached) {
            System.Diagnostics.Debugger.Log(2, "STUD", string.Format("[STUD] Instance is at offset {0:X16}\n", start + instance.offset));
          }
        }
        return null;
      }
      return ret;
    }
  }

  public class STUDManager {
    private List<Type> implementations;
    private List<uint> ids;
    private List<ulong> instanceIds;
    private List<string> names;

    private static HashSet<uint> complained = new HashSet<uint>();
    public static HashSet<uint> Complained => complained;

    private static STUDManager _Instance = NewInstance();
    public static STUDManager Instance => _Instance;

    public IReadOnlyList<Type> Implementations => implementations;
    public IReadOnlyList<ulong> InstanceIds => instanceIds;
    public IReadOnlyList<uint> Ids => ids;
    public IReadOnlyList<string> Names => names;

    public STUDManager() {
      implementations = new List<Type>();
      ids = new List<uint>();
      instanceIds = new List<ulong>();
      names = new List<string>();
    }
    
    public Type GetInstance(uint id, bool suppress) {
      for(int i = 0; i < implementations.Count; ++i) {
        if(ids[i] == id) {
          return implementations[i];
        }
      }
      if(complained.Add(id)) {
        if(System.Diagnostics.Debugger.IsAttached) {
          System.Diagnostics.Debugger.Log(2, "STUD", string.Format("[STUD] Warning! Unknown Instance ID {0:X8}\n", id));
        }
      }
      return null;
    }

    public Type GetInstance(ulong id, bool suppress) {
      for(int i = 0; i < implementations.Count; ++i) {
        if(instanceIds[i] == id) {
          return implementations[i];
        }
      }
      return null;
    }
    
    public MANAGER_ERROR InitializeInstance(uint id, Stream input, out ISTUDInstance instance, bool suppress) {
      return InitializeInstance(GetInstance(id, suppress), input, out instance);
    }

    public MANAGER_ERROR InitializeInstance(ulong id, Stream input, out ISTUDInstance instance, bool suppress) {
      return InitializeInstance(GetInstance(id, suppress), input, out instance);
    }

    public MANAGER_ERROR InitializeInstance(Type inst, Stream input, out ISTUDInstance instance) {
      if(inst == null) {
        instance = null;
        return MANAGER_ERROR.E_UNKNOWN;
      }

      if(System.Diagnostics.Debugger.IsAttached) {
        instance = (ISTUDInstance)Activator.CreateInstance(inst);
        instance.Read(input);
        return MANAGER_ERROR.E_SUCCESS;
      }

      try {
        instance = (ISTUDInstance)Activator.CreateInstance(inst);
        instance.Read(input);
      } catch (Exception ex) {
        Console.Error.WriteLine(ex.Message);
        instance = null;
        return MANAGER_ERROR.E_FAULT;
      }

      return MANAGER_ERROR.E_SUCCESS;
    }

    public string GetName(uint id) {
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

    public uint GetId(ISTUDInstance inst) {
      if(inst == null) {
        return 0;
      }
      return inst.Id;
    }

    public ulong GetKey(ISTUDInstance inst) {
      if(inst == null) {
        return 0;
      }
      return inst.Key;
    }

    public uint GetId(Type inst) {
      if(inst == null) {
        return 0;
      }
      ISTUDInstance instance = (ISTUDInstance)Activator.CreateInstance(inst);
      return GetId(instance);
    }

    public ulong GetKey(Type inst) {
      if(inst == null) {
        return 0;
      }
      ISTUDInstance instance = (ISTUDInstance)Activator.CreateInstance(inst);
      return GetKey(instance);
    }

    public MANAGER_ERROR AddInstance(ISTUDInstance instance) {
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
      ulong key = GetKey(instance);
      uint id = GetId(instance);
      string name = GetName(instance);
      if(id == 0) {
        if(System.Diagnostics.Debugger.IsAttached) {
          System.Diagnostics.Debugger.Log(2, "STUD", string.Format("Error! {0:X16} still has no ID!\n", key));
        }
      }
      if(instanceIds.Contains(key) && ids.Contains(id)) {
        return MANAGER_ERROR.E_DUPLICATE;
      }
      if(names.Contains(name)) {
        return MANAGER_ERROR.E_DUPLICATE;
      }
      implementations.Add(instance);
      ids.Add(id);
      instanceIds.Add(key);
      names.Add(name);
      return MANAGER_ERROR.E_SUCCESS;
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
