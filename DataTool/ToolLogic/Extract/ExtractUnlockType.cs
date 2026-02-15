using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.ToolLogic.List;
using DataTool.ToolLogic.Util;

namespace DataTool.ToolLogic.Extract;

public abstract class ExtractUnlockType : QueryParser, ITool, IQueryParser {
    public List<QueryType> QueryTypes { get; } = []; // synthetic only
    public string DynamicChoicesKey => UtilDynamicChoices.GetUnlockKey(GetUnlockType());
        
    public abstract UnlockType GetUnlockType();
    public abstract string GetFolderName();
        
    public void Parse(ICLIFlags toolFlags) {
        var flags = (ExtractFlags) toolFlags;
        flags.EnsureOutputDirectory();
            
        var unlockType = GetUnlockType();
        var unlockTypeName = CosmeticType.UnlockTypeToName(unlockType);
        var fakeQueryType = new CosmeticType(unlockType, "doesntmatter");
        var fullArg = new ParsedArg(fakeQueryType);
            
        // turn the query into the format that ExtractUnlocks expects
        // (it's used to dealing with heroes)
        var parsedQuery = ParseQuery(flags, QueryTypes);
        if (parsedQuery != null) {
            foreach (var passedName in parsedQuery.Keys) {
                fullArg.Values.Add(passedName);
            }
        } else {
            // no specific name query, allow everything
            fullArg.Values.Add("*");
        }

        var fullConfig = new IgnoreCaseDict<ParsedArg> {
            { unlockTypeName, fullArg }
        };

        var allUnlocksRaw = ListAllUnlocks.GetData();
        var allUnlocks = allUnlocksRaw.Values.Select(Unlock (x) => x).ToArray();
            
        // todo: we end up with double path because of SaveUnlock logic...
        // /Charms/WeaponCharm/{event}/{name}
        // but i think its nice to keep the folder name consistent with the mode name
        string path = Path.Combine(flags.OutputPath, GetFolderName());
        ExtractHeroUnlocks.SaveUnlocks(toolFlags, allUnlocks, path, null, fullConfig, null, null, null);
        
        foreach (var allowed in fullArg.Values.Allowed) {
            if (parsedQuery == null) continue;
            if (!parsedQuery.TryGetValue(allowed.Value, out var originalQueryPart)) {
                continue;
            }

            originalQueryPart.Matched = allowed.Matched;
        }
        LogUnknownQueries(parsedQuery);
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