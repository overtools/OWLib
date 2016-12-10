using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;
using OWLib.Types.STUD.Binding;
using OWLib.Types.STUD.GameParam;

namespace OverTool.ExtractLogic {
  class VoiceLine {
    public static void CopyBytes(Stream i, Stream o, int sz) {
      byte[] buffer = new byte[sz];
      i.Read(buffer, 0, sz);
      o.Write(buffer, 0, sz);
      buffer = null;
    }

    public static List<ulong> FlattenSounds(List<ulong> pairs, Dictionary<ulong, Record> map, CASCHandler handler, Dictionary<ulong, ulong> replace = null) {
      List<ulong> ret = new List<ulong>();
      if(replace == null) {
        replace = new Dictionary<ulong, ulong>();
      }
      HashSet<ulong> done = new HashSet<ulong>();

      foreach(ulong _skey in pairs) {
        ulong skey = _skey;
        if(replace.ContainsKey(skey)) {
          skey = replace[skey];
        }
        ulong id = APM.keyToIndexID(skey);
        ulong typ = APM.keyToTypeID(skey);
        if(!map.ContainsKey(skey)) {
          continue;
        }
        if(!done.Add(skey)) {
          continue;
        }
        using(Stream studStream = Util.OpenFile(map[skey], handler)) {
          if(studStream == null) {
            continue;
          }
          STUD stud = new STUD(studStream, true, STUDManager.Instance, false, true);
          foreach(ISTUDInstance instance in stud.Instances) {
            if(instance == null) {
              continue;
            }

            if(instance.Name == stud.Manager.GetName(typeof(SoundBindingReference))) {
              SoundBindingReference reference = (SoundBindingReference)instance;
              ulong tgt = reference.Reference.sound.key;
              if(replace.ContainsKey(tgt)) {
                tgt = replace[tgt];
              }
              ret.Add(tgt);
            }
          }
        }
      }

      return ret;
    }

    public static void FindSoundsEx(ulong key, HashSet<ulong> done, List<ulong> ret, Dictionary<ulong, Record> map, CASCHandler handler, Dictionary<ulong, ulong> replace) {
      if(replace.ContainsKey(key)) {
        key = replace[key];
      }
      if(!map.ContainsKey(key)) {
        return;
      }
      if(!done.Add(key)) {
        return;
      }

      using(Stream studStream = Util.OpenFile(map[key], handler)) {
        if(studStream == null) {
          return;
        }
        STUD stud = new STUD(studStream, true, STUDManager.Instance, false, true);
        FindSoundsSTUD(stud, done, ret, map, handler, replace);
      }
    }

    public static void FindSoundsSTUD(STUD stud, HashSet<ulong> done, List<ulong> ret, Dictionary<ulong, Record> map, CASCHandler handler, Dictionary<ulong, ulong> replace) {
      foreach(ISTUDInstance instance in stud.Instances) {
        if(instance == null) {
          continue;
        }

        if(instance.Name == stud.Manager.GetName(typeof(GenericRecordReference))) {
          GenericRecordReference inst = (GenericRecordReference)instance;
          FindSoundsEx(inst.Reference.key.key, done, ret, map, handler, replace);
        } else if(instance.Name == stud.Manager.GetName(typeof(SoundMasterReference))) {
          SoundMasterReference smr = (SoundMasterReference)instance;
          if(!ret.Contains(smr.Data.sound.key)) {
            ret.Add(smr.Data.sound.key);
          }
          FindSoundsEx(smr.Data.owner.key, done, ret, map, handler, replace);
        } else if(instance.Name == stud.Manager.GetName(typeof(ParameterRecord))) {
          ParameterRecord parameter = (ParameterRecord)instance;
          foreach(ParameterRecord.ParameterEntry entry in parameter.Parameters) {
            FindSoundsEx(entry.parameter.key, done, ret, map, handler, replace);
          }
        } else if(instance.Name == stud.Manager.GetName(typeof(BindingRecord))) {
          BindingRecord record = (BindingRecord)instance;
          FindSoundsEx(record.Param.binding.key, done, ret, map, handler, replace);
        }
      }
    }

    public static List<ulong> FindSounds(HeroMaster master, Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, Dictionary<ulong, ulong> replace = null) {
      List<ulong> ret = new List<ulong>();

      HashSet<ulong> done = new HashSet<ulong>();

      if(replace == null) {
        replace = new Dictionary<ulong, ulong>();
      }

      FindSoundsEx(master.Header.binding.key, done, ret, map, handler, replace);
      FindSoundsEx(master.Header.child1.key, done, ret, map, handler, replace);
      FindSoundsEx(master.Header.child2.key, done, ret, map, handler, replace);
      FindSoundsEx(master.Header.child3.key, done, ret, map, handler, replace);
      FindSoundsEx(master.Header.child4.key, done, ret, map, handler, replace);

      return ret;
    }
  }
}
