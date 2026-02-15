using System;
using System.IO;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.ToolLogic.Extract;
using DataTool.ToolLogic.List;
using TankLib;
using TankLib.STU.Types;

namespace DataTool.ToolLogic.Dump {
    [Tool("dump-localized-name-mapping", 
          Description = "Dump hero localized name mapping",
          CustomFlags = typeof(ExtractFlags),
          IsSensitive = true,
          UtilNoArchiveNeeded = true)]
    public class DumpHeroLocalizedNameMapping : ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (ExtractFlags)toolFlags;
            if (flags.OutputPath == null)
                throw new Exception("no output path");

            var outputPath = Path.Combine(flags.OutputPath, "LocalizedNamesMapping");
            IO.CreateDirectorySafe(outputPath);

            TankLib.TACT.LoadHelper.PreLoad();
            using var output = new StreamWriter(Path.Combine(outputPath, "Heroes.csv"));
            
            foreach (var locale in Program.ValidLanguages) {
                DumpStringsLocale.InitStorage(locale);
                
                output.WriteLine($";{locale},,");

                // heroes util has a cache, so we can't use that
                foreach (teResourceGUID key in Program.TrackedFiles[0x75]) {
                    var hero = HeroVM.Load(key);
                    if (hero == null) continue;
                    
                    if (!hero.IsHero) continue;
                    if (hero.Name == null) continue;

                    var name = hero.Name.ToLowerInvariant();
                    name = name.Replace("<en>", "");
                    name = name.Replace("</en>", "");
                    output.WriteLine($"{teResourceGUID.Index(hero.GUID):X},{teResourceGUID.Type(hero.GUID):X},{name}");
                }
                
                output.Flush(); // takes ages to process so might as well flush
            }
        }
    }
}
