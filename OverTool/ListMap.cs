using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CASCExplorer;
using OWLib;
using OWLib.Types.STUD;

namespace OverTool {
  class ListMap {
    public static void TryString(string n, string str) {
      if(str == null) {
        return;
      } else {
        Console.Out.WriteLine("\t{0}: {1}", n, str);
      }
    }

    public static void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, string[] args) {
      List<ulong> masters = track[0x9F];
      foreach(ulong masterKey in masters) {
        Console.Out.WriteLine("");
        if(!map.ContainsKey(masterKey)) {
          continue;
        }
        STUD masterStud = new STUD(Util.OpenFile(map[masterKey], handler), true, STUDManager.Instance, false, true);
        if(masterStud.Instances == null) {
          continue;
        }
        MapMaster master = (MapMaster)masterStud.Instances[0];
        if(master == null) {
          continue;
        }
        
        string name = Util.GetString(master.Header.name.key, map, handler);
        Console.Out.WriteLine(name);
        Console.Out.WriteLine("\tID: {0:X8}", APM.keyToIndex(masterKey));
        
        string subline = Util.GetString(master.Header.subline.key, map, handler);
        if(subline == null) {
          subline = "";
        }
        if(master.Header.subline.key != master.Header.subline2.key && master.Header.subline2.key != 0) {
          subline += " " + Util.GetString(master.Header.subline2.key, map, handler);
        }
        subline = subline.Trim();
        if(subline.Length > 0) {
          Console.Out.WriteLine("\tSubline: {0}", subline);
        }

        TryString("State", Util.GetString(master.Header.stateA.key, map, handler));
        TryString("State", Util.GetString(master.Header.stateB.key, map, handler));

        string description = Util.GetString(master.Header.description1.key, map, handler);
        if(description == null) {
          description = "";
        }
        if(master.Header.description1.key != master.Header.description2.key && master.Header.description2.key != 0) {
          description += " " + Util.GetString(master.Header.description2.key, map, handler);
        }
        description = description.Trim();
        if(description.Length > 0) {
          Console.Out.WriteLine("\tDescription: {0}", description);
        }
      }
    }
  }
}
