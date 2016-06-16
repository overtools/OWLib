using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using CASCExplorer;
using System.Reflection;
using OWLib;

namespace InventoryTool {
  class Program {
    public struct InventoryToolDescriptor {
      public PackageIndexRecord record;
      public PackageIndex index;
      public APMPackage package;
    }

    private static void CopyBytes(Stream i, Stream o, int sz) {
      byte[] buffer = new byte[sz];
      i.Read(buffer, 0, sz);
      o.Write(buffer, 0, sz);
      buffer = null;
    }

    public static Stream OpenFile(InventoryToolDescriptor desc, CASCHandler handler) {
      bool bundle = false;
      CASCExplorer.MD5Hash contentKey;
      if(((ContentFlags)desc.record.Flags & ContentFlags.Bundle) == ContentFlags.Bundle) {
        bundle = true;
        contentKey = desc.index.bundleContentKey;
      } else {
        contentKey = desc.record.ContentKey;
      }

      EncodingEntry enc;
      if(handler.Encoding.GetEntry(contentKey, out enc)) {
        MemoryStream ms = new MemoryStream(desc.record.Size);
        using(Stream stream = handler.OpenFile(enc.Key)) {
          if(bundle) {
            stream.Position = desc.record.Offset;
          } else {
            stream.Position = 0;
          }
          ms.Position = 0;
          CopyBytes(stream, ms, desc.record.Size);
          ms.Position = 0;
          return ms;
        }
      }
      return null;
    }

    static void Main(string[] args) {
      if(args.Length < 1) {
        Console.Out.WriteLine("Usage: InventoryTool.exe \"root directory\"");
        Console.Out.WriteLine("");
        Console.Out.WriteLine("Examples:");
        Console.Out.WriteLine("InventoryTool.exe overwatch");
        return;
      }
      string root = args[0];

      Console.Out.WriteLine("{0} v{1}", Assembly.GetExecutingAssembly().GetName().Name, Assembly.GetExecutingAssembly().GetName().Version.ToString());
      
      Console.Out.WriteLine("Loading");
      STUDManager manager = STUDManager.Create();
      CASCConfig config = CASCConfig.LoadLocalStorageConfig(root);
      CASCHandler handler = CASCHandler.OpenStorage(config);
      OwRootHandler ow = handler.Root as OwRootHandler;
      if(ow == null) {
        Console.Error.WriteLine("Not a valid Overwatch installation");
        return;
      }

      Dictionary<ulong, InventoryToolDescriptor> map = new Dictionary<ulong, InventoryToolDescriptor>();
      ulong[] o75 = new ulong[] { };
      Console.Out.WriteLine("Finding");
      foreach(APMFile apm in ow.APMFiles) {
        for(int i = 0; i < apm.Packages.Length; ++i) {
          APMPackage package = apm.Packages[i];
          PackageIndexRecord[] records = apm.Records[i];
          PackageIndex index = apm.Indexes[i];
          Dictionary<ulong, InventoryToolDescriptor> tmp = new Dictionary<ulong, InventoryToolDescriptor>(records.Length);
          List<ulong> tmp2 = new List<ulong>(records.Length);
          for(int j = 0; j < records.Length; ++j) {
            PackageIndexRecord record = records[j];
            if(map.ContainsKey(record.Key)) {
              continue;
            }
            ulong type = APM.keyToTypeID(record.Key);

            if(type == 0x7C || type == 0x75 || type == 0x58 || type == 0xA5) {
              tmp.Add(record.Key, new InventoryToolDescriptor { package = package, record = record, index = index });
            }

            if(type == 0x75) {
              tmp2.Add(record.Key);
            }
          }
          if(tmp.Count > 0) {
            map = map.Concat(tmp).ToDictionary(k => k.Key, v => v.Value);
            o75 = o75.Concat(tmp2).ToArray();
          }
        }
      }
      Console.Out.WriteLine("Iterating");
      foreach(ulong master in o75) {
        InventoryToolDescriptor desc = map[master];
        using(Stream masterStream = OpenFile(desc, handler)) {
          if(masterStream == null) {
            continue;
          }

          using(BinaryReader masterReader = new BinaryReader(masterStream)) {
            masterStream.Position = 0x50;
            ulong nameKey = masterReader.ReadUInt64();
            if(!map.ContainsKey(nameKey)) {
              continue;
            }
            masterStream.Position = 0x170;
            ulong inventoryMasterKey = masterReader.ReadUInt64();
            if(!map.ContainsKey(inventoryMasterKey)) {
              continue;
            }

            if(!PrintName(map[nameKey], handler)) {
              continue;
            }

            using(Stream inventoryMasterStream = OpenFile(map[inventoryMasterKey], handler)) {
              if(inventoryMasterStream == null) {
                continue;
              }
              STUD inventoryMasterSTUD = new STUD(manager, inventoryMasterStream);
              OWLib.Types.STUD.x33F56AC1 im = (OWLib.Types.STUD.x33F56AC1) inventoryMasterSTUD.Blob;
              if(im == null) {
                continue;
              }
              
              foreach(OWLib.Types.STUDDataHeader kp in im.Achievables) {
                if(!map.ContainsKey(kp.key)) {
                  continue;
                }
                FindInventoryName(map[kp.key], handler, manager, map);
              }

              foreach(OWLib.Types.STUDDataHeader[] g in im.Defaults) {
                foreach(OWLib.Types.STUDDataHeader kp in g) {
                  if(!map.ContainsKey(kp.key)) {
                    continue;
                  }
                  FindInventoryName(map[kp.key], handler, manager, map);
                }
              }

              foreach(OWLib.Types.STUDDataHeader[] g in im.Items) {
                foreach(OWLib.Types.STUDDataHeader kp in g) {
                  if(!map.ContainsKey(kp.key)) {
                    continue;
                  }
                  FindInventoryName(map[kp.key], handler, manager, map);
                }
              }
              Console.Out.WriteLine("");
            }
          }
        }
      }
    }

    private static string GetName(InventoryToolDescriptor desc, CASCHandler handler) {
      using(Stream nameStream = OpenFile(desc, handler)) {
        if(nameStream == null) {
          return null;
        }
        OWString name = new OWString(nameStream);
        return name.Value;
      }
    }

    private static bool PrintName(InventoryToolDescriptor desc, CASCHandler handler, string padding = "") {
      string name = GetName(desc, handler);
      if(name == null) {
        return false;
      } 
      Console.Out.WriteLine("{0}{1}", padding, name);
      return true;
    }

    public readonly static string[] RarityMap = new string[4] {
      "Common", "Rare", "Epic", "Legendary"
    };

    private static string FindInventoryName(InventoryToolDescriptor inventoryToolDescriptor, CASCHandler handler, STUDManager manager, Dictionary<ulong, InventoryToolDescriptor> map) {
      using(Stream studStream = OpenFile(inventoryToolDescriptor, handler)) {
        if(studStream == null) {
          return null;
        }

        STUD stud = new STUD(manager, studStream);
        OWLib.Types.STUD.STUDInventoryItemGeneric iig = (OWLib.Types.STUD.STUDInventoryItemGeneric) stud.Blob;
        if(!map.ContainsKey(iig.InventoryHeader.stringKey)) {
          return null;
        }
        string name = GetName(map[iig.InventoryHeader.stringKey], handler);
        if(name == null) {
          Console.Out.WriteLine("\tType: {0}", stud.Name);
          if(iig.InventoryHeader.rarity < RarityMap.Length) {
            Console.Out.WriteLine("\tRarity: {0}", RarityMap[(int)iig.InventoryHeader.rarity]);
          } else {
            Console.Out.WriteLine("\tRarity: Unknown{0}", iig.InventoryHeader.rarity);
          }
          Console.Out.WriteLine("\t\tCannot find name...");
        } else {
          if(iig.InventoryHeader.rarity < RarityMap.Length) {
            Console.Out.WriteLine("\t{0} ({2} {1})", name, stud.Name, RarityMap[(int)iig.InventoryHeader.rarity]);
          } else {
            Console.Out.WriteLine("\t{0} (Unknown{2} {1})", name, stud.Name, iig.InventoryHeader.rarity);
          }
        }
        return name;
      }
    }
  }
}
