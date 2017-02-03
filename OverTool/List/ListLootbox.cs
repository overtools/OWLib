using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using CASCExplorer;
using OWLib;
using OWLib.Types.STUD;

namespace OverTool.List {
  class ListLootbox : IOvertool {
    public string Help => "No additional requirements";
    public uint MinimumArgs => 0;
    public char Opt => 'l';
    public string Title => "List Lootbox";
    public ushort[] Track => new ushort[1] { 0xCF };

    public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, string[] args) {
      Console.Out.WriteLine();
      foreach(ulong master in track[0xCF]) {
        if(!map.ContainsKey(master)) {
          continue;
        }
        STUD lootbox = new STUD(Util.OpenFile(map[master], handler));
        Lootbox box = lootbox.Instances[0] as Lootbox;
        if(box == null) {
          continue;
        }
        Console.Out.WriteLine(box.EventNameNormal);
        Console.Out.WriteLine("\t{0}", Util.GetString(box.Master.title, map, handler));
        foreach(Lootbox.Bundle bundle in box.Bundles) {
          Console.Out.WriteLine("\t\t{0}", Util.GetString(bundle.title, map, handler));
        }
      }
    }
  }
}
