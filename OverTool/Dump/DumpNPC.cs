using System;
using System.Collections.Generic;
using CASCExplorer;
using OWLib;
using OWLib.Types;
using OWLib.Types.STUD;
using System.IO;

namespace OverTool {
  class DumpNPC : IOvertool {
    public string Help => "output [names]";
    public uint MinimumArgs => 1;
    public char Opt => 'N';
    public string Title => "Extract NPC";
    public ushort[] Track => new ushort[1] { 0x75 };

    public void Parse(Dictionary<ushort, List<ulong>> track, Dictionary<ulong, Record> map, CASCHandler handler, string[] args) {
      List<ulong> masters = track[0x75];
      List<ulong> blank = new List<ulong>();
      Dictionary<ulong, ulong> blankdict = new Dictionary<ulong, ulong>();
      List<char> blankchar = new List<char>();
      List<string> extract = new List<string>();
      for(int i = 1; i < args.Length; ++i) {
        extract.Add(args[i].ToLowerInvariant());
      }
      foreach(ulong masterKey in masters) {
        if(!map.ContainsKey(masterKey)) {
          continue;
        }
        STUD masterStud = new STUD(Util.OpenFile(map[masterKey], handler));
        if(masterStud.Instances == null) {
          continue;
        }
        HeroMaster master = (HeroMaster)masterStud.Instances[0];
        if(master == null) {
          continue;
        }
        string heroName = Util.GetString(master.Header.name.key, map, handler);
        if(heroName == null) {
          continue;
        }
        if(extract.Count > 0 && !extract.Contains(heroName.ToLowerInvariant())) {
          continue;
        }
        if(master.Header.itemMaster.key != 0) { // AI
          continue;
        }
        Console.Out.WriteLine("{0} {1:X}", heroName, APM.keyToIndexID(masterKey));
        string path = string.Format("{0}{1}{2}{1}{3:X}{1}", args[0], Path.DirectorySeparatorChar, Util.Strip(Util.SanitizePath(heroName)), APM.keyToIndexID(masterKey));

        HashSet<ulong> models = new HashSet<ulong>();
        Dictionary<ulong, ulong> animList = new Dictionary<ulong, ulong>();
        HashSet<ulong> parsed = new HashSet<ulong>();
        Dictionary<ulong, List<ImageLayer>> layers = new Dictionary<ulong, List<ImageLayer>>();

        ExtractLogic.Skin.FindModels(master.Header.binding, blank, models, animList, layers, blankdict, parsed, map, handler);

        ExtractLogic.Skin.Save(master, path, heroName, $"{APM.keyToIndexID(masterKey):X}", blankdict, parsed, models, layers, animList, blankchar, track, map, handler);
      }
    }
  }
}