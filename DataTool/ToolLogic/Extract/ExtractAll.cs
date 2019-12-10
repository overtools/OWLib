using System;
using DataTool.Flag;
using TACTLib;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-all", Description = "Extract everything (rip storage space)", CustomFlags = typeof(ExtractFlags))]
    public class ExtractAll : ITool {
        public void Parse(ICLIFlags toolFlags) {
            var flags = (ExtractFlags) toolFlags;
            if (flags.Positionals.Length < 3) {
                Logger.Error(null, "Pass output path.");
                return;
            }
            var newPositionals = new string[4];
            Array.Copy(flags.Positionals, 0, newPositionals, 0, 3);
            newPositionals[3] = "*";
            flags.Positionals = newPositionals;
            flags.FlattenDirectory = true;
            flags.SkipSound = true;
            new ExtractAbilities().Parse(flags);
            new ExtractGeneral().Parse(flags);
            new ExtractNPCs().Parse(flags);
            new ExtractHeroUnlocks().Parse(flags);
            new ExtractMaps().Parse(flags);
            new ExtractMapEnvs().Parse(flags);
            new ExtractLootbox().Parse(flags);
            flags.SkipSound = false;
            new ExtractHeroVoice().Parse(flags);
            new ExtractHeroConversations().Parse(flags);
        }
    }
}
