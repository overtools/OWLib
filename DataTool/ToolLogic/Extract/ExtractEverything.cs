using DataTool.Flag;
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

            #region Heroes

            var positionals = new System.Collections.Generic.List<string>(flags.Positionals) { "*|*=*" };
            flags.Positionals = positionals.ToArray();
            new ExtractHeroUnlocks().Parse(flags);
            SaveScratchDatabase();

            #endregion

            #region Generic

            positionals.RemoveAt(positionals.Count - 1);
            positionals.Add("*");
            flags.Positionals = positionals.ToArray();

            new ExtractAbilities().Parse(flags);
            new ExtractGamemodeImages().Parse(flags);
            new ExtractGeneral().Parse(flags);
            new ExtractLootbox().Parse(flags);
            new ExtractNPCs().Parse(flags);
            SaveScratchDatabase();

            #endregion

            #region Sound

            var soundSkipped = flags.SkipSound;
            flags.SkipSound = false;
            new ExtractMusic().Parse(flags);
            new ExtractHeroConversations().Parse(flags);
            new ExtractHeroVoice().Parse(flags);
            new ExtractVoiceSets().Parse(flags);
            new ExtractNPCVoice().Parse(flags);
            SaveScratchDatabase();
            flags.SkipSound = soundSkipped;

            #endregion

            #region Maps

            // new ExtractMapEnvs().Parse(flags);
            new ExtractMaps().Parse(flags);
            SaveScratchDatabase();

            #endregion
        }
    }
}
