using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.ToolLogic.List;

namespace DataTool.ToolLogic.Extract {
    public abstract class ExtractUnlockType : ITool {
        public abstract UnlockType GetUnlockType();
        public abstract string GetFolderName();
        
        public void Parse(ICLIFlags toolFlags) {
            var flags = (ExtractFlags) toolFlags;
            flags.EnsureOutputDirectory();
            
            var fakeArg = new ParsedArg(new CosmeticType(GetUnlockType(), "doesntmatter")) {
                Allowed = ["*"] // everything please
            };
            var fakeConfig = new Dictionary<string, ParsedArg> {
                {CosmeticType.UnlockTypeToName(GetUnlockType()), fakeArg}
            };

            var allUnlocksRaw = ListAllUnlocks.GetData();
            var allUnlocks = allUnlocksRaw.Values.Select(Unlock (x) => x).ToArray();
            
            // todo: we end up with double path because of SaveUnlock logic...
            // /Charms/WeaponCharm/{event}/{name}
            // but i think its nice to keep the folder name consistent with the mode name
            string path = Path.Combine(flags.OutputPath, GetFolderName());
            ExtractHeroUnlocks.SaveUnlocks(toolFlags, allUnlocks, path, null, fakeConfig, null, null, null);
        }
    }
    
    [Tool("extract-charms", Aliases = ["extract-weapon-charms"], Description = "Extract all Weapon Charms", CustomFlags = typeof(ExtractFlags))]
    public class ExtractCharms : ExtractUnlockType {
        public override UnlockType GetUnlockType() => UnlockType.WeaponCharm;
        public override string GetFolderName() => "Charms";
    }
    
    [Tool("extract-sprays", Description = "Extract all Sprays", CustomFlags = typeof(ExtractFlags))]
    public class ExtractSprays : ExtractUnlockType {
        public override UnlockType GetUnlockType() => UnlockType.Spray;
        public override string GetFolderName() => "Sprays";
    }
    
    [Tool("extract-name-cards", Aliases = ["extract-namecards"], Description = "Extract all Name Cards", CustomFlags = typeof(ExtractFlags))]
    public class ExtractNameCards : ExtractUnlockType {
        public override UnlockType GetUnlockType() => UnlockType.NameCard;
        public override string GetFolderName() => "NameCards";
    }
    
    [Tool("extract-souvenirs", Aliases = ["extract-souvenirs"], Description = "Extract all Souvenirs", CustomFlags = typeof(ExtractFlags))]
    public class ExtractSouvenirs : ExtractUnlockType {
        public override UnlockType GetUnlockType() => UnlockType.Souvenir;
        public override string GetFolderName() => "Souvenirs";
    }
    
    [Tool("extract-player-icons", Aliases = ["extract-icons", "extract-playericons"], Description = "Extract all Player Icons", CustomFlags = typeof(ExtractFlags))]
    public class ExtractPlayerIcons : ExtractUnlockType {
        public override UnlockType GetUnlockType() => UnlockType.Icon;
        public override string GetFolderName() => "PlayerIcons";
    }
}
