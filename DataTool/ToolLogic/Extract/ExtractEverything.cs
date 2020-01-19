using DataTool.Flag;
using DataTool.ToolLogic.Dbg;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-everything", Description = "Extract everything", CustomFlags = typeof(ExtractMapEnvFlags))]
    public class ExtractEverything : ITool {
        public void Parse(ICLIFlags toolFlags) {
            if (!(toolFlags is ExtractMapEnvFlags flags)) {
                // wat
                return;
            }

            var positionals = new System.Collections.Generic.List<string>(flags.Positionals) {"*|*=*"};
            flags.Positionals = positionals.ToArray();
            new ExtractHeroUnlocks().Parse(flags);
            positionals.RemoveAt(positionals.Count - 1);
            positionals.Add("*");
            flags.Positionals = positionals.ToArray();
            new ExtractAbilities().Parse(flags);
            new ExtractGeneral().Parse(flags);
            new ExtractHeroConversations().Parse(flags);
            new ExtractHeroVoiceBetter().Parse(flags);
            new ExtractLootbox().Parse(flags);
            new ExtractMapEnvs().Parse(flags);
            new ExtractNPCs().Parse(flags);
            new ExtractMaps().Parse(flags);
        }
    }
}
