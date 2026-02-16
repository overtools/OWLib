using System;
using System.IO;
using System.Linq;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.ToolLogic.Extract;

namespace DataTool.ToolLogic.Dump {
    [Tool("dump-pw2-name-guids", 
          Description = "Dump ow2 skin name guids",
          CustomFlags = typeof(ExtractFlags),
          IsSensitive = true)]
    public class DumpHeroOW2SkinNameGUIDs : ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (ExtractFlags)toolFlags;
            if (flags.OutputPath == null)
                throw new Exception("no output path");

            var outputPath = Path.Combine(flags.OutputPath, "LocalizedNamesMapping");
            IO.CreateDirectorySafe(outputPath);
            
            using var output = new StreamWriter(Path.Combine(outputPath, "OW2Skins.csv"));
            
            foreach (var hero in Helpers.GetHeroes().Values) {
                if (!hero.IsHero) continue;

                // (this is intended to be run on 2026 builds, so the name is "Overwatch" now)
                var defaultSkin = new ProgressionUnlocks(hero.STU).LevelUnlocks!.First().Unlocks.Single(x => x.GetName() == "Overwatch");
                
                output.WriteLine($"0x{defaultSkin.STU.m_name.GUID.GUID:X16} ; {hero.Name}");
            }
        }
    }
}
