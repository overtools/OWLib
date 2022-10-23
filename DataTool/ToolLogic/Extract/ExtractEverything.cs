using DataTool.Flag;
using DataTool.ToolLogic.Extract.Debug;
using TankLib.Helpers;
using static DataTool.Program;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-everything", Description = "Extract everything", IsSensitive = true, CustomFlags = typeof(ExtractFlags))]
    public class ExtractEverything : ITool {
        public void Parse(ICLIFlags toolFlags) {
            if (!(toolFlags is ExtractFlags flags)) {
                // wat
                return;
            }

            Logger.Error("ExtractEverything", "Are you sure you want everything? This take a very long time and the output size will be huge");

            var positionals = new System.Collections.Generic.List<string>(flags.Positionals) {"*|*=*"};
            flags.Positionals = positionals.ToArray();
            new ExtractHeroUnlocks().Parse(flags);
            SaveScratchDatabase();
            positionals.RemoveAt(positionals.Count - 1);
            positionals.Add("*");
            flags.Positionals = positionals.ToArray();
            new ExtractAbilities().Parse(flags);
            new ExtractGamemodeImages().Parse(flags);
            new ExtractGeneral().Parse(flags);
            new ExtractMusic().Parse(flags);
            SaveScratchDatabase();
            new ExtractHeroConversations().Parse(flags);
            new ExtractHeroVoiceBetter().Parse(flags);
            new ExtractVoiceSets().Parse(flags);
            new ExtractNPCVoice().Parse(flags);
            new ExtractLootbox().Parse(flags);
            SaveScratchDatabase();
            // new ExtractMapEnvs().Parse(flags);
            // SaveScratchDatabase();
            // new ExtractNPCs().Parse(flags);
            // new ExtractNPCVoice().Parse(flags);
            SaveScratchDatabase();
            new ExtractMaps().Parse(flags);
            SaveScratchDatabase();
        }
    }
}
