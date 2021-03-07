using DataTool.ToolLogic.Util;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-npcs", Name = "NPCs", Description = "Extract npcs", CustomFlags = typeof(ExtractFlags))]
    // ReSharper disable once InconsistentNaming
    public class ExtractNPCs : ExtractHeroUnlocks {
        protected override string RootDir => "NPCs";
        protected override bool NPCs => true;
        public override string DynamicChoicesKey => UtilDynamicChoices.VALID_NPC_NAMES;
    }
}
