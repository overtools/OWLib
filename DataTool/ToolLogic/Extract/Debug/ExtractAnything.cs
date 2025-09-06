using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using TankLib;
using TankLib.Helpers;

namespace DataTool.ToolLogic.Extract.Debug;

[Tool("extract-anything", Description = "Extract any specific asset (terms & conditions apply)", CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
public class ExtractAnything : QueryParser, ITool, IQueryParser {
    public List<QueryType> QueryTypes { get; } = []; // synthetic only
    public Dictionary<string, string> QueryNameOverrides => null;
    public string DynamicChoicesKey => ""; // not supported
        
    public void Parse(ICLIFlags toolFlags) {
        var query = ParseQuery(toolFlags, QueryTypes, QueryNameOverrides);
        if (query == null) {
            Logger.Warn("No query specified.");
            return;
        }
            
        Combo.ComboInfo findInfo = new Combo.ComboInfo();
        foreach (var queryKey in query.Keys) {
            if (!TryParseQuery(queryKey, out var queryGUID)) {
                Logger.Error(null, "Unable to parse query: \"{0}\"", queryKey);
                continue;
            }

            Combo.Find(findInfo, queryGUID);
        }
            
        var flags = (ExtractFlags) toolFlags;
        flags.EnsureOutputDirectory();
        var outputPath = Path.Combine(flags.OutputPath, "Anything");
            
        // todo: can create duplicates
        // e.g input is voice set, will save all sounds loose + whole voice set
        var saveContext = new SaveLogic.Combo.SaveContext(findInfo);
        SaveLogic.Combo.Save(toolFlags, outputPath, saveContext);
        SaveLogic.Combo.SaveLooseTextures(toolFlags, outputPath, saveContext);
        SaveLogic.Combo.SaveAllVoiceSets(toolFlags, outputPath, saveContext);
        SaveLogic.Combo.SaveAllStrings(toolFlags, outputPath, saveContext);
        SaveLogic.Combo.SaveAllSoundFiles(toolFlags, outputPath, saveContext);
        SaveLogic.Combo.SaveAllVoiceSoundFiles(toolFlags, outputPath, saveContext);
        SaveLogic.Combo.SaveAllMaterials(toolFlags, outputPath, saveContext);
        SaveLogic.Combo.SaveAllModelLooks(toolFlags, outputPath, saveContext);
        SaveLogic.Combo.SaveAllAnimations(toolFlags, outputPath, saveContext);
    }

    private static bool TryParseQuery(string queryKey, out ulong queryGUID) {
        queryGUID = 0;
            
        if (queryKey.StartsWith("0x")) {
            return ulong.TryParse(queryKey.AsSpan(2), NumberStyles.HexNumber, null, out queryGUID);
        }

        if (queryKey.Contains('.')) {
            var split = queryKey.Split('.');

            if (!ulong.TryParse(split[0], NumberStyles.HexNumber, null, out var idPart)) {
                return false;
            }
            if (!ushort.TryParse(split[1], NumberStyles.HexNumber, null, out var typePart)) {
                return false;
            }

            queryGUID = new teResourceGUID(idPart).WithType(typePart);
            return true;
        }
            
        return false;
    }
}