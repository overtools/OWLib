using System;
using System.Collections.Generic;
using System.IO;
using CASCExplorer;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;
using OWLib.Types.STUD.Binding;
using OWLib.Types.STUD.GameParam;
using OWLib.Types.STUD.InventoryItem;

namespace OverTool.ExtractLogic {
  class VoiceLine {
    public struct SoundOwnerPair {
      public ulong owner;
      public ulong sound;
    }

    public static void CopyBytes(Stream i, Stream o, int sz) {
      byte[] buffer = new byte[sz];
      i.Read(buffer, 0, sz);
      o.Write(buffer, 0, sz);
      buffer = null;
    }

    public static void Extract(HeroMaster master, STUD itemStud, string output, string heroName, string itemName, Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler) {
      Dictionary<ulong, List<SoundOwnerPair>> soundData = FindSounds(master, track, map, handler);

      VoiceLineItem item = (VoiceLineItem)itemStud.Instances[0];

      string path = string.Format("{0}{1}{2}{1}{3}{1}{4}", output, Path.DirectorySeparatorChar, Util.Strip(Util.SanitizePath(heroName)), Util.SanitizePath(item.Name), Util.SanitizePath(itemName));

      uint suffix = 0;
      HashSet<ulong> done = new HashSet<ulong>();
      // TODO: Resolve 00D to the sound 00D?
      if(soundData.ContainsKey(item.Data.f00D.key)) {
        List<ulong> sounds = FlattenSounds(soundData[item.Data.f00D.key], map, handler);
        foreach(ulong soundKey in sounds) {
          if(!map.ContainsKey(soundKey)) {
            continue;
          }
          if(!done.Add(soundKey)) {
            continue;
          }
          string outputPath = path;
          if(suffix > 0) {
            outputPath += string.Format("_{0}", suffix);
          }
          outputPath += ".wem";
          suffix += 1;
          if(!Directory.Exists(Path.GetDirectoryName(outputPath))) {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
          }
          using(Stream soundStream = Util.OpenFile(map[soundKey], handler)) {
            using(Stream outputStream = File.Open(outputPath, FileMode.Create)) {
              CopyBytes(soundStream, outputStream, (int)soundStream.Length);
            }
          }
        }
      }
      if(soundData.ContainsKey(item.Data.f00D_2.key)) {
        List<ulong> sounds = FlattenSounds(soundData[item.Data.f00D_2.key], map, handler);
        foreach(ulong soundKey in sounds) {
          if(!map.ContainsKey(soundKey)) {
            continue;
          }
          if(!done.Add(soundKey)) {
            continue;
          }
          string outputPath = path;
          if(suffix > 0) {
            outputPath += string.Format("_{0}", suffix);
          }
          outputPath += ".wem";
          suffix += 1;
          if(!Directory.Exists(Path.GetDirectoryName(outputPath))) {
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
          }
          using(Stream soundStream = Util.OpenFile(map[soundKey], handler)) {
            using(Stream outputStream = File.Open(outputPath, FileMode.Create)) {
              CopyBytes(soundStream, outputStream, (int)soundStream.Length);
            }
          }
        }
      }
    }

    public static List<ulong> FlattenSounds(List<SoundOwnerPair> pairs, Dictionary<ulong, Record> map, CASCHandler handler) {
      List<ulong> ret = new List<ulong>();
      HashSet<ulong> done = new HashSet<ulong>();

      foreach(SoundOwnerPair pair in pairs) {
        if(!map.ContainsKey(pair.sound)) {
          continue;
        }
        if(!done.Add(pair.sound)) {
          continue;
        }
        using(Stream studStream = Util.OpenFile(map[pair.sound], handler)) {
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
              ret.Add(reference.Reference.sound.key);
            }
          }
        }
      }

      return ret;
    }

    public static void FindSoundsEx(ulong key, HashSet<ulong> done, Dictionary<ulong, List<SoundOwnerPair>> ret, Dictionary<ulong, Record> map, CASCHandler handler) {
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
        foreach(ISTUDInstance instance in stud.Instances) {
          if(instance == null) {
            continue;
          }

          if(instance.Name == stud.Manager.GetName(typeof(GenericRecordReference))) {
            GenericRecordReference inst = (GenericRecordReference)instance;
            FindSoundsEx(inst.Reference.key.key, done, ret, map, handler);
          } else if(instance.Name == stud.Manager.GetName(typeof(SoundMasterReference))) {
            SoundMasterReference smr = (SoundMasterReference)instance;
            SoundOwnerPair pair = new SoundOwnerPair { owner = smr.Data.owner.key, sound = smr.Data.sound.key };
            if(!ret.ContainsKey(smr.Data.definition.key)) {
              ret[smr.Data.definition.key] = new List<SoundOwnerPair>();
            }
            ret[smr.Data.definition.key].Add(pair);
          } else if(instance.Name == stud.Manager.GetName(typeof(ParameterRecord))) {
            ParameterRecord parameter = (ParameterRecord)instance;
            foreach(ParameterRecord.ParameterEntry entry in parameter.Parameters) {
              FindSoundsEx(entry.parameter.key, done, ret, map, handler);
            }
          } else if(instance.Name == stud.Manager.GetName(typeof(BindingRecord))) {
            BindingRecord record = (BindingRecord)instance;
            FindSoundsEx(record.Param.binding.key, done, ret, map, handler);
          }
        }
      }
    }

    public static Dictionary<ulong, List<SoundOwnerPair>> FindSounds(HeroMaster master, Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler) {
      Dictionary<ulong, List<SoundOwnerPair>> ret = new Dictionary<ulong, List<SoundOwnerPair>>();

      HashSet<ulong> done = new HashSet<ulong>();

      FindSoundsEx(master.Header.binding.key, done, ret, map, handler);
      FindSoundsEx(master.Header.child1.key, done, ret, map, handler);
      FindSoundsEx(master.Header.child2.key, done, ret, map, handler);
      FindSoundsEx(master.Header.child3.key, done, ret, map, handler);
      FindSoundsEx(master.Header.child4.key, done, ret, map, handler);

      return ret;
    }
  }
}
