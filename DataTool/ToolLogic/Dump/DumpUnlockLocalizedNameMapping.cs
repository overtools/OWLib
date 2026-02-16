using System;
using System.IO;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.ToolLogic.Extract;
using TankLib;
using TankLib.STU.Types;

namespace DataTool.ToolLogic.Dump {
    [Tool("dump-unlock-localized-name-mapping", 
          Description = "Dump localized name mapping for ow2 -> ow rename",
          CustomFlags = typeof(ExtractFlags),
          IsSensitive = true,
          UtilNoArchiveNeeded = true)]
    public class DumpUnlockLocalizedNameMapping : ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (ExtractFlags)toolFlags;
            if (flags.OutputPath == null)
                throw new Exception("no output path");

            var outputPath = Path.Combine(flags.OutputPath, "LocalizedNamesMapping");
            IO.CreateDirectorySafe(outputPath);

            TankLib.TACT.LoadHelper.PreLoad();
            using var output = new StreamWriter(Path.Combine(outputPath, "DefaultSkins.csv"));
            
            foreach (var locale in Program.ValidLanguages) {
                DumpStringsLocale.InitStorage(locale);
                    
                // (tracer)
                // todo: not good enough. a lot of ow2 skins share localized name asset, but not all
                // see DumpHeroOW2SkinNameGUIDs
                var ow1Skin = STUHelper.GetInstance<STUUnlock>(0x0250000000000409)!;
                var ow2Skin = STUHelper.GetInstance<STUUnlock>(0x02500000000028D2)!;

                var ow1Name = IO.GetString(ow1Skin.m_name);
                var ow2Name = IO.GetString(ow2Skin.m_name);

                var ow1Line = $"{teResourceGUID.Index(ow1Skin.m_name):X},7C,{ow1Name}";
                var ow2Line = $"{teResourceGUID.Index(ow2Skin.m_name):X},7C,{ow2Name}";
                    
                Console.Out.WriteLine(ow1Line);
                Console.Out.WriteLine(ow2Line);
                    
                output.WriteLine(ow1Line);
                output.WriteLine(ow2Line);
                output.Flush(); // takes ages to process so might as well flush
            }
        }
    }
}
