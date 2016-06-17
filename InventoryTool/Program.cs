using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using CASCExplorer;
using System.Reflection;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;

namespace InventoryTool {
  class Program {
    public struct InventoryToolDescriptor {
      public CASCExplorer.PackageIndexRecord record;
      public CASCExplorer.PackageIndex index;
      public CASCExplorer.APMPackage package;
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
        Console.Out.WriteLine("Usage: InventoryTool.exe \"root directory\" [query]");
        Console.Out.WriteLine("Query is optional, it specifies if certain things should be exported. Parameters that end with elipses can be repeated");
        Console.Out.WriteLine("Possible Queries:");
        Console.Out.WriteLine("\"destination folder\" type \"list\"...");
        Console.Out.WriteLine("You can combine types with the + operator, valid types:");
        Console.Out.WriteLine("\"destination folder\" skin \"hero name:skin name\"...");
        Console.Out.WriteLine("\"destination folder\" spray \"hero name\"...");
        Console.Out.WriteLine("\"destination folder\" icon \"hero name\"...");
        Console.Out.WriteLine("");
        Console.Out.WriteLine("Examples:");
        Console.Out.WriteLine("InventoryTool.exe overwatch");
        Console.Out.WriteLine("InventoryTool.exe overwatch icons icon hanzo");
        Console.Out.WriteLine("InventoryTool.exe overwatch sprays spray tracer");
        Console.Out.WriteLine("InventoryTool.exe overwatch tracer skin+spray tracer tracer:classic");
        return;
      }
      string root = args[0];
      bool complex = args.Length > 3;
      if(complex) {
        string[] types = { "skin", "icon", "spray" };
        string[] qtypes = args[2].ToLowerInvariant().Split('+');
        foreach(string qtype in qtypes) {
          if(!types.Contains(qtype)) {
            Console.Out.WriteLine("Invalid type {0}", qtype);
            return;
          }
        }
      }

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
      foreach(APMFile apm in ow.APMFiles) {
        if(!apm.Name.ToLowerInvariant().Contains("rcn")) {
          continue;
        }
        for(int i = 0; i < apm.Packages.Length; ++i) {
          CASCExplorer.APMPackage package = apm.Packages[i];
          CASCExplorer.PackageIndexRecord[] records = apm.Records[i];
          CASCExplorer.PackageIndex index = apm.Indexes[i];
          List<ulong> tmp = new List<ulong>(records.Length);
          for(int j = 0; j < records.Length; ++j) {
            CASCExplorer.PackageIndexRecord record = records[j];
            if(map.ContainsKey(record.Key)) {
              continue;
            }
            ulong type = APM.keyToTypeID(record.Key);

            map.Add(record.Key, new InventoryToolDescriptor { package = package, record = record, index = index });

            if(type == 0x75 && !o75.Contains(record.Key)) {
              tmp.Add(record.Key);
            }
          }
          if(tmp.Count > 0) {
            o75 = o75.Concat(tmp).ToArray();
          }
        }
      }

      if(complex) {
        DoComplex(o75, map, handler, args[1], args[2].ToLowerInvariant().Split('+'), args.Skip(3).ToArray());
        return;
      }

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
              x33F56AC1 im = (x33F56AC1) inventoryMasterSTUD.Blob;
              if(im == null) {
                continue;
              }
              
              foreach(STUDDataHeader kp in im.Achievables) {
                if(!map.ContainsKey(kp.key)) {
                  continue;
                }
                FindInventoryName(map[kp.key], handler, manager, map);
              }

              foreach(STUDDataHeader[] g in im.Defaults) {
                foreach(STUDDataHeader kp in g) {
                  if(!map.ContainsKey(kp.key)) {
                    continue;
                  }
                  FindInventoryName(map[kp.key], handler, manager, map);
                }
              }

              foreach(STUDDataHeader[] g in im.Items) {
                foreach(STUDDataHeader kp in g) {
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

    // warning this code is awful.
    private static void DoComplex(ulong[] o75, Dictionary<ulong, InventoryToolDescriptor> map, CASCHandler handler, string destination, string[] types, string[] query) {
      Dictionary<string, x33F56AC1> inventoryMap = new Dictionary<string, x33F56AC1>();
      Dictionary<string, string> nameinv = new Dictionary<string, string>();
      STUDManager manager = STUDManager.Create();

      // first pass
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
            // TODO: Follow chain to model files.
            masterStream.Position = 0x170;
            ulong inventoryMasterKey = masterReader.ReadUInt64();
            if(!map.ContainsKey(inventoryMasterKey)) {
              continue;
            }
            string name = GetName(map[nameKey], handler);
            if(name == null) {
              continue;
            }

            using(Stream inventoryMasterStream = OpenFile(map[inventoryMasterKey], handler)) {
              if(inventoryMasterStream == null) {
                continue;
              }
              STUD inventoryMasterSTUD = new STUD(manager, inventoryMasterStream);
              x33F56AC1 im = (x33F56AC1)inventoryMasterSTUD.Blob;
              if(im == null) {
                continue;
              }
              inventoryMap.Add(name, im);
              nameinv[name.ToLowerInvariant()] = name;
            }
          }
        }
      }
      
      List<string> heroes = new List<string>();
      List<string> subhero = new List<string>();
      foreach(string selector in query) {
        string[] p = selector.ToLowerInvariant().Split(':');
        heroes.Add(p[0]);
        if(p.Length == 1) {
          subhero.Add(null);
        } else {
          subhero.Add(p[1]);
        }
      }

      Dictionary<string, Dictionary<string, List<KeyValuePair<string, STUDBlob>>>> inventoryValues = new Dictionary<string, Dictionary<string, List<KeyValuePair<string, STUDBlob>>>>();

      // second pass
      foreach(KeyValuePair<string, string> sp in nameinv) {
        if(heroes.Contains(sp.Key)) {
          Console.Out.WriteLine("Loading data for {0}...", sp.Value);
          Dictionary<string, List<KeyValuePair<string, STUDBlob>>> inventory = new Dictionary<string, List<KeyValuePair<string, STUDBlob>>>();
          inventory.Add("Icon", new List<KeyValuePair<string, STUDBlob>>());
          inventory.Add("Skin", new List<KeyValuePair<string, STUDBlob>>());
          inventory.Add("Spray", new List<KeyValuePair<string, STUDBlob>>());
          x33F56AC1 im = inventoryMap[sp.Value];
              
          foreach(STUDDataHeader kp in im.Achievables) {
            if(!map.ContainsKey(kp.key)) {
              continue;
            }
            KeyValuePair<string, STUDInventoryItemGeneric> vp = GetInventory(map[kp.key], handler, manager, map);
            if(inventory.ContainsKey(vp.Key)) {
              string name = GetName(map[vp.Value.InventoryHeader.stringKey], handler);
              inventory[vp.Key].Add(new KeyValuePair<string, STUDBlob>(name, vp.Value));
            }
          }

          foreach(STUDDataHeader[] g in im.Defaults) {
            foreach(STUDDataHeader kp in g) {
              if(!map.ContainsKey(kp.key)) {
                continue;
              }
              KeyValuePair<string, STUDInventoryItemGeneric> vp = GetInventory(map[kp.key], handler, manager, map);
              if(inventory.ContainsKey(vp.Key)) {
                string name = GetName(map[vp.Value.InventoryHeader.stringKey], handler);
                inventory[vp.Key].Add(new KeyValuePair<string, STUDBlob>(name, vp.Value));
              }
            }
          }

          foreach(STUDDataHeader[] g in im.Items) {
            foreach(STUDDataHeader kp in g) {
              if(!map.ContainsKey(kp.key)) {
                continue;
              }
              KeyValuePair<string, STUDInventoryItemGeneric> vp = GetInventory(map[kp.key], handler, manager, map);
              if(inventory.ContainsKey(vp.Key)) {
                string name = GetName(map[vp.Value.InventoryHeader.stringKey], handler);
                inventory[vp.Key].Add(new KeyValuePair<string, STUDBlob>(name, vp.Value));
              }
            }
          }

          inventoryValues.Add(sp.Key, inventory);
        }
      }

      // third pass
      bool subdir = types.Length > 1;
      bool subdir2 = heroes.Count > 1;

      for(int i = 0; i < heroes.Count; ++i) {
        string hero = heroes[i];
        string[] subs = null;
        if(subhero[i] != null) {
          subs = subhero[i].Split('+');
        }
        if(!inventoryValues.ContainsKey(hero)) {
          continue;
        }
        // record type, <record name, record>...
        Dictionary<string, List<KeyValuePair<string, STUDBlob>>> inventory = inventoryValues[hero];
        if(subs != null) {
          // extract skins
          List<KeyValuePair<string, STUDBlob>> records = inventory["Skin"];
          foreach(KeyValuePair<string, STUDBlob> recordkp in records) {
            if(subs.Contains(recordkp.Key.ToLowerInvariant())) {
              x8B9DEB02 inventorySkin = (x8B9DEB02)recordkp.Value;
              string path = string.Format("{0}{1}", destination, Path.DirectorySeparatorChar);
              if(subdir2) {
                path += string.Format("{0}{1}", nameinv[hero], Path.DirectorySeparatorChar); // destination/hero/Skin/name/
              }
              if(subdir) {
                path += string.Format("Skin{0}", Path.DirectorySeparatorChar);
              }
              if(subs.Length > 1) {
                path += string.Format("{0}{1}", recordkp.Key, Path.DirectorySeparatorChar);
              }
              char[] invalids = Path.GetInvalidPathChars();
              path = string.Join("_", path.Split(invalids, StringSplitOptions.RemoveEmptyEntries));
              path = string.Join(Path.DirectorySeparatorChar.ToString(), path.Split(Path.DirectorySeparatorChar));
              Extract(inventorySkin, map, handler, path, manager);
            }
          }
        }
        foreach(string type in types) {
          if(type == "skin") {
            continue;
          }
          string ntype = "";
          if(type == "spray") {
            ntype = "Spray";
          } else if(type == "icon") {
            ntype = "Icon";
          } else {
            continue;
          }
          string path = string.Format("{0}{1}", destination, Path.DirectorySeparatorChar);

          if(subdir2) {
            path += string.Format("{0}{1}", nameinv[hero], Path.DirectorySeparatorChar);
          }
          if(subdir) {
            path += string.Format("{0}{1}", ntype, Path.DirectorySeparatorChar);
          }
          List<KeyValuePair<string, STUDBlob>> records = inventory[ntype];
          foreach(KeyValuePair<string, STUDBlob> recordkp in records) {
            string ext = "dds";
            char[] invalids = Path.GetInvalidPathChars();
            char[] invalids2 = Path.GetInvalidFileNameChars();
            path = string.Join("_", path.Split(invalids, StringSplitOptions.RemoveEmptyEntries));
            string tmpPath = path + Path.DirectorySeparatorChar + string.Format("{0}.{1}", string.Join("_", recordkp.Key.Split(invalids2, StringSplitOptions.RemoveEmptyEntries)), ext);
            tmpPath = string.Join(Path.DirectorySeparatorChar.ToString(), tmpPath.Split(Path.DirectorySeparatorChar));

            if(ntype == "Icon") {
              Extract((x8CDAA871)recordkp.Value, map, handler, tmpPath, manager);
            } else if(ntype == "Spray") {
              Extract((x15720E8A)recordkp.Value, map, handler, tmpPath, manager);
            } else {
              continue;
            }
          }
        }
      }
    }

    private static void Extract(x15720E8A inventorySpray, Dictionary<ulong, InventoryToolDescriptor> map, CASCHandler handler, string path, STUDManager manager) {
      if(!map.ContainsKey(inventorySpray.Header.f0A8Key)) {
        return;
      }

      using(Stream studDecalStream = OpenFile(map[inventorySpray.Header.f0A8Key], handler)) {
        STUD studDecal = new STUD(manager, studDecalStream);
        FF82DF73 decal = (FF82DF73)studDecal.Blob;
        if(decal == null) {
          return;
        }

        ulong key = decal.Mips[0].imageDefinition.key;
        if(!map.ContainsKey(key)) {
          return;
        }
        using(Stream imageDef = OpenFile(map[key], handler)) {
          ImageDefinition id = new ImageDefinition(imageDef);
          ulong imageKey = id.Layers[0].key;
          if(!map.ContainsKey(imageKey)) {
            return;
          }
          ulong imageDataKey = (imageKey & 0xFFFFFFFFUL) | (0x100000000UL) | (0x0320000000000000UL);
          bool dbl = map.ContainsKey(imageDataKey);
          if(!Directory.Exists(Path.GetDirectoryName(path))) {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
          }
          string a = imageKey.ToString("X");
          string b = imageDataKey.ToString("X");
          using(Stream f004Stream = OpenFile(map[imageKey], handler)) {
            if(dbl) {
              using(Stream f04DStream = OpenFile(map[imageDataKey], handler)) {
                Texture texture = new Texture(f004Stream, f04DStream);
                if(!texture.Loaded) {
                  return;
                }
                using(Stream outputStream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write)) {
                  texture.Save(outputStream);
                }
              }
            } else {
              TextureLinear texture = new TextureLinear(f004Stream);
              if(!texture.Loaded) {
                return;
              }
              using(Stream outputStream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write)) {
                texture.Save(outputStream);
              }
            }
            Console.Out.WriteLine("Saved file {0}", path);
          }
        }
      }
    }

    private static void Extract(x8CDAA871 inventoryIcon, Dictionary<ulong, InventoryToolDescriptor> map, CASCHandler handler, string path, STUDManager manager) {
      if(!map.ContainsKey(inventoryIcon.Header.f0A8Key)) {
        return;
      }

      using(Stream studDecalStream = OpenFile(map[inventoryIcon.Header.f0A8Key], handler)) {
        STUD studDecal = new STUD(manager, studDecalStream);
        FF82DF73 decal = (FF82DF73)studDecal.Blob;
        if(decal == null) {
          return;
        }

        ulong key = decal.Mips[0].imageDefinition.key;
        if(!map.ContainsKey(key)) {
          return;
        }
        using(Stream imageDef = OpenFile(map[key], handler)) {
          ImageDefinition id = new ImageDefinition(imageDef);
          ulong imageKey = id.Layers[0].key;
          if(!map.ContainsKey(imageKey)) {
            return;
          }
          ulong imageDataKey = (imageKey & 0xFFFFFFFFUL) | (0x100000000UL) | (0x0320000000000000UL);
          bool dbl = map.ContainsKey(imageDataKey);
          if(!Directory.Exists(Path.GetDirectoryName(path))) {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
          }
          string a = imageKey.ToString("X");
          string b = imageDataKey.ToString("X");
          using(Stream f004Stream = OpenFile(map[imageKey], handler)) {
            if(dbl) {
              using(Stream f04DStream = OpenFile(map[imageDataKey], handler)) {
                Texture texture = new Texture(f004Stream, f04DStream);
                if(!texture.Loaded) {
                  return;
                }
                using(Stream outputStream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write)) {
                  texture.Save(outputStream);
                }
              }
            } else {
              TextureLinear texture = new TextureLinear(f004Stream);
              if(!texture.Loaded) {
                return;
              }
              using(Stream outputStream = File.Open(path, FileMode.OpenOrCreate, FileAccess.Write)) {
                texture.Save(outputStream);
              }
            }
            Console.Out.WriteLine("Saved file {0}", path);
          }
        }
      }
    }

    private static void Extract(x8B9DEB02 inventorySkin, Dictionary<ulong, InventoryToolDescriptor> map, CASCHandler handler, string path, STUDManager manager) {
      throw new NotImplementedException();
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

    private static KeyValuePair<string, STUDInventoryItemGeneric> GetInventory(InventoryToolDescriptor inventoryToolDescriptor, CASCHandler handler, STUDManager manager, Dictionary<ulong, InventoryToolDescriptor> map) {
      using(Stream studStream = OpenFile(inventoryToolDescriptor, handler)) {
        if(studStream == null) {
          return new KeyValuePair<string, STUDInventoryItemGeneric>(null, null);
        }

        STUD stud = new STUD(manager, studStream);
        STUDInventoryItemGeneric iig = (STUDInventoryItemGeneric)stud.Blob;
        if(iig == null) {
          return new KeyValuePair<string, STUDInventoryItemGeneric>(null, null);
        }
        if(!map.ContainsKey(iig.InventoryHeader.stringKey)) {
          return new KeyValuePair<string, STUDInventoryItemGeneric>(null, null);
        }
        return new KeyValuePair<string, STUDInventoryItemGeneric>(stud.Name, iig);
      }
    }

    private static string FindInventoryName(InventoryToolDescriptor inventoryToolDescriptor, CASCHandler handler, STUDManager manager, Dictionary<ulong, InventoryToolDescriptor> map) {
      KeyValuePair<string, STUDInventoryItemGeneric> kp = GetInventory(inventoryToolDescriptor, handler, manager, map);
      STUDInventoryItemGeneric iig = kp.Value;
      if(iig == null) {
        return null;
      }
      string name = GetName(map[iig.InventoryHeader.stringKey], handler);
      if(name == null) {
        Console.Out.WriteLine("\tType: {0}", kp.Key);
        if(iig.InventoryHeader.rarity < RarityMap.Length) {
          Console.Out.WriteLine("\tRarity: {0}", RarityMap[(int)iig.InventoryHeader.rarity]);
        } else {
          Console.Out.WriteLine("\tRarity: Unknown{0}", iig.InventoryHeader.rarity);
        }
        Console.Out.WriteLine("\t\tCannot find name...");
      } else {
        if(iig.InventoryHeader.rarity < RarityMap.Length) {
          Console.Out.WriteLine("\t{0} ({2} {1})", name, kp.Key, RarityMap[(int)iig.InventoryHeader.rarity]);
        } else {
          Console.Out.WriteLine("\t{0} (Unknown{2} {1})", name, kp.Key, iig.InventoryHeader.rarity);
        }
      }
      return name;
    }
  }
}
