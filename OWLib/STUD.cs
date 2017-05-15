using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using OWLib.Types;
using System.Reflection;

namespace OWLib {
    public class STUD : IDisposable {
        public const string STUD_DEBUG_STR = "{Name, nq} - {Id}";

        private STUDHeader header;
        private STUDInstanceRecord[] records;
        private ISTUDInstance[] instances;

        private STUDManager manager;

        public STUDHeader Header => header;
        public STUDInstanceRecord[] Records => records;
        public ISTUDInstance[] Instances => instances;
        public STUDManager Manager => manager;

        public long end = -1;
        public long start = -1;

        private bool complete = false;
        public bool Complete => complete;
        private bool suppress = false;
        public bool Suppress => suppress;

        private Stream studstream = null;
        public Stream STUDStream => studstream;

        public STUD(Stream input, bool initalizeAll = true, STUDManager manager = null, bool leaveOpen = false, bool suppress = false) {
            if (manager == null) {
                this.manager = STUDManager.Instance;
                manager = this.manager;
            } else {
                this.manager = manager;
            }

            if (input == null) {
                return;
            }

            this.suppress = suppress;
            studstream = input;

            using (BinaryReader reader = new BinaryReader(input, Encoding.Default, leaveOpen)) {
                start = input.Position;
                header = reader.Read<STUDHeader>();
                if (header.magic != 0x53545544) {
                    records = new STUDInstanceRecord[0];
                    instances = new ISTUDInstance[0];
                    return;
                }
                input.Position = start + (long)header.instanceTableOffset;
                STUDArrayInfo ptr = reader.Read<STUDArrayInfo>();

                records = new STUDInstanceRecord[ptr.count];
                instances = new ISTUDInstance[ptr.count];

                for (ulong i = 0; i < ptr.count; ++i) {
                    records[i] = reader.Read<STUDInstanceRecord>();
                }

                end = input.Position;

                if (initalizeAll) {
                    InitializeAll(input, suppress);
                }
            }
        }

        public void InitializeAll(Stream input, bool suppress) {
            for (long i = 0; i < records.LongLength; ++i) {
                if (instances[i] == null) {
                    instances[i] = Initialize(input, records[i], suppress);
                }
            }
            complete = true;
        }

        public void SetInstance(long index, ISTUDInstance instance) {
            instances[index] = instance;
        }

        public STUDReference GetInstanceAtOffset(ulong offset) {
            for (long i = 0; i < records.LongLength; ++i) {
                if (records[i].offset == offset) {
                    return new STUDReference(this, i);
                }
            }
            return null;
        }

        public ISTUDInstance Initialize(Stream input, STUDInstanceRecord instance, bool suppress) {
            input.Position = start + instance.offset;
            uint id = 0;
            using (BinaryReader reader = new BinaryReader(input, Encoding.Default, true)) {
                id = reader.ReadUInt32();
            }
            input.Position -= 4;
            MANAGER_ERROR err;
            bool outputOffset = STUDManager.Complained.Contains(id);
            if ((err = manager.InitializeInstance(id, input, out ISTUDInstance ret, suppress, this)) != MANAGER_ERROR.E_SUCCESS) {
                if (err != MANAGER_ERROR.E_UNKNOWN) {
                    if (System.Diagnostics.Debugger.IsAttached) {
                        System.Diagnostics.Debugger.Log(2, "STUD", string.Format("[STUD] Error while instancing for STUD type {0:X8}\n", id));
                    }
                } else if (!outputOffset) {
                    if (System.Diagnostics.Debugger.IsAttached) {
                        System.Diagnostics.Debugger.Log(2, "STUD", string.Format("[STUD] Instance is at offset {0:X16}\n", start + instance.offset));
                    }
                }
                return new STUDummy(id, instance.offset);
            }
            return ret;
        }

        public void Dispose() {
            records = null;
            instances = null;
            GC.SuppressFinalize(this);
        }
    }

    [System.Diagnostics.DebuggerDisplay(STUD.STUD_DEBUG_STR)]
    public class STUDummy : ISTUDInstance {
        private string name = "Dummy";
        public string Name => name;

        private uint id = 0;
        public uint Id => id;

        public void Read(Stream input, STUD stud) {
            return;
        }

        public STUDummy(uint id, uint offset) {
            this.id = id;
            name = $"Dummy({offset})";
        }
    }

    public class STUDReference {
        private long index;
        private STUD stud;

        public ISTUDInstance Value {
            get {
                if (stud.Instances == null) {
                    return null;
                }
                if (stud.Instances.LongLength >= index) {
                    return null;
                }
                if (stud.STUDStream != null && stud.STUDStream.CanRead && stud.Complete == false && stud.Instances[index] == null) {
                    STUDInstanceRecord record = stud.Records[index];
                    stud.SetInstance(index, stud.Initialize(stud.STUDStream, record, stud.Suppress));
                }
                return stud.Instances[index];
            }
        }

        public STUDReference(STUD stud, long index) {
            this.index = index;
            this.stud = stud;
        }
    }

    public class STUDManager {
        private List<Type> implementations;
        private List<uint> ids;
        private List<string> names;

        private static HashSet<uint> complained = new HashSet<uint>();
        public static HashSet<uint> Complained => complained;

        private static STUDManager _Instance = null;
        public static STUDManager Instance {
            get {
                if (_Instance == null) {
                    _Instance = NewInstance();
                }
                return _Instance;
            }
        }

        public IReadOnlyList<Type> Implementations => implementations;
        public IReadOnlyList<uint> Ids => ids;
        public IReadOnlyList<string> Names => names;

        public STUDManager() {
            implementations = new List<Type>();
            ids = new List<uint>();
            names = new List<string>();
        }

        public Type GetInstance(uint id, bool suppress) {
            for (int i = 0; i < implementations.Count; ++i) {
                if (ids[i] == id) {
                    return implementations[i];
                }
            }
            if (complained.Add(id)) {
                if (System.Diagnostics.Debugger.IsAttached) {
                    System.Diagnostics.Debugger.Log(2, "STUD", string.Format("[STUD] Warning! Unknown Instance ID {0:X8}\n", id));
                }
            }
            return null;
        }

        public MANAGER_ERROR InitializeInstance(uint id, Stream input, out ISTUDInstance instance, bool suppress, STUD stud) {
            return InitializeInstance(GetInstance(id, suppress), input, out instance, stud);
        }

        public MANAGER_ERROR InitializeInstance(Type inst, Stream input, out ISTUDInstance instance, STUD stud) {
            if (inst == null) {
                instance = null;
                return MANAGER_ERROR.E_UNKNOWN;
            }

            if (System.Diagnostics.Debugger.IsAttached) {
                instance = (ISTUDInstance)Activator.CreateInstance(inst);
                instance.Read(input, stud);
                return MANAGER_ERROR.E_SUCCESS;
            }

            try {
                instance = (ISTUDInstance)Activator.CreateInstance(inst);
                instance.Read(input, stud);
            } catch (Exception ex) {
                Console.Error.WriteLine("Error with {0}", inst.FullName);
                Console.Error.WriteLine(ex.Message);
                instance = null;
                return MANAGER_ERROR.E_FAULT;
            }

            return MANAGER_ERROR.E_SUCCESS;
        }

        public string GetName(uint id) {
            for (int i = 0; i < implementations.Count; ++i) {
                if (ids[i] == id) {
                    return names[i];
                }
            }
            return null;
        }

        public string GetName(ISTUDInstance inst) {
            if (inst == null) {
                return null;
            }
            return inst.Name;
        }

        public string GetName(Type inst) {
            if (inst == null) {
                return null;
            }
            if (implementations.Contains(inst)) {
                if (names.Count > implementations.IndexOf(inst)) {
                    return names[implementations.IndexOf(inst)];
                }
            }
            ISTUDInstance instance = (ISTUDInstance)Activator.CreateInstance(inst);
            return GetName(instance);
        }

        public uint GetId(ISTUDInstance inst) {
            if (inst == null) {
                return 0;
            }
            return inst.Id;
        }

        public uint GetId(Type inst) {
            if (inst == null) {
                return 0;
            }
            ISTUDInstance instance = (ISTUDInstance)Activator.CreateInstance(inst);
            return GetId(instance);
        }

        public MANAGER_ERROR AddInstance(ISTUDInstance instance) {
            if (instance == null) {
                return MANAGER_ERROR.E_FAULT;
            }
            return AddInstance(instance.GetType());
        }

        public MANAGER_ERROR AddInstance(Type instance) {
            if (instance == null) {
                return MANAGER_ERROR.E_FAULT;
            }
            if (implementations.Contains(instance)) {
                return MANAGER_ERROR.E_DUPLICATE;
            }
            uint id = GetId(instance);
            string name = GetName(instance);
            if (ids.Contains(id)) {
                return MANAGER_ERROR.E_DUPLICATE;
            }
            if (names.Contains(name)) {
                return MANAGER_ERROR.E_DUPLICATE;
            }
            implementations.Add(instance);
            ids.Add(id);
            names.Add(name);
            return MANAGER_ERROR.E_SUCCESS;
        }

        public static STUDManager NewInstance() {
            STUDManager manager = new STUDManager();
            Assembly asm = typeof(ISTUDInstance).Assembly;
            Type t = typeof(ISTUDInstance);
            List<Type> types = asm.GetTypes().Where(type => type != t && t.IsAssignableFrom(type)).ToList();
            foreach (Type type in types) {
                if (type.IsInterface) {
                    continue;
                }
                if (type.IsEquivalentTo(typeof(STUDummy))) {
                    continue;
                }
                manager.AddInstance(type);
            }
            return manager;
        }
    }
}
