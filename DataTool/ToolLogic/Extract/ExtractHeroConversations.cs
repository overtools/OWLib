﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DataTool.DataModels;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.Helper;
using DataTool.SaveLogic.Unlock;
using DataTool.ToolLogic.Util;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;
using static DataTool.Helper.Logger;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-hero-convo", Description = "Extract hero voice conversations", CustomFlags = typeof(ExtractFlags))]
    public class ExtractHeroConversations : QueryParser, ITool, IQueryParser {
        public string DynamicChoicesKey => UtilDynamicChoices.VALID_HERO_NAMES;

        public List<QueryType> QueryTypes => new List<QueryType>();

        public Dictionary<string, string> QueryNameOverrides => ExtractHeroUnlocks.HeroMapping;

        public void Parse(ICLIFlags toolFlags) {
            ExtractHeroConvos(toolFlags);
        }

        protected override void QueryHelp(List<QueryType> types) {
            IndentHelper indent = new IndentHelper();

            Log("Please specify what you want to extract:");
            Log($"{indent + 1}Command format: \"{{hero name}}|{{type}}=({{tag name}}={{tag}}),{{item name}}\"");
            Log($"{indent + 1}Each query should be surrounded by \", and individual queries should be separated by spaces");

            Log($"{indent + 1}All hero names are in your selected locale");

            Log("\r\nExample commands: ");
            Log($"{indent + 1}\"Lúcio\"");
            Log($"{indent + 1}\"Torbjörn\"");
            Log($"{indent + 1}\"Moira\"");
            Log($"{indent + 1}\"Wrecking Ball\"");
        }

        private const string Container = "HeroConvo";

        public void ExtractHeroConvos(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            if (flags.Positionals.Length < 4) {
                QueryHelp(QueryTypes);
                return;
            }

            string path = Path.Combine(basePath, Container);

            Dictionary<string, Dictionary<string, ParsedArg>> parsedTypes =
                ParseQuery(flags, QueryTypes, QueryNameOverrides);
            if (parsedTypes == null) return;

            Dictionary<ulong, VoiceSet> allVoiceSets = new Dictionary<ulong, VoiceSet>();
            foreach (var voiceSetGUID in Program.TrackedFiles[0x5F]) {
                STUVoiceSet set = GetInstance<STUVoiceSet>(voiceSetGUID);

                if (set?.m_voiceLineInstances == null) continue;
                allVoiceSets[voiceSetGUID] = new VoiceSet(set);
            }

            // Dictionary<uint, string> mapNames = new Dictionary<uint, string>();
            // foreach (ulong mapGuid in Program.TrackedFiles[0x9F]) {
            //     STUMapHeader mapHeader = GetInstance<STUMapHeader>(mapGuid);
            //     if (mapHeader == null) continue;
            //
            //     mapNames[teResourceGUID.Index(mapGuid)] = GetValidFilename(GetString(mapHeader.m_1C706502) ?? GetString(mapHeader.m_displayName));
            // }

            Combo.ComboInfo comboInfo = new Combo.ComboInfo();
            var comboSaveContext = new SaveLogic.Combo.SaveContext(comboInfo);

            foreach (ulong heroGuid in Program.TrackedFiles[0x75]) {
                STUHero hero = GetInstance<STUHero>(heroGuid);
                if (hero == null) continue;
                STUVoiceSetComponent voiceSetComponent = GetInstance<STUVoiceSetComponent>(hero.m_gameplayEntity);

                if (voiceSetComponent?.m_voiceDefinition == null || !allVoiceSets.TryGetValue(voiceSetComponent.m_voiceDefinition, out var set)) {
                    Debugger.Log(0, "DataTool.SaveLogic.Unlock.VoiceLine",
                                 "[DataTool.SaveLogic.Unlock.VoiceLine]: VoiceSet not found\r\n");
                    continue;
                }

                string heroNameActual =
                    (GetString(hero.m_0EDCE350) ?? $"Unknown{teResourceGUID.Index(heroGuid)}").TrimEnd(' ');

                Dictionary<string, ParsedArg> config = GetQuery(parsedTypes, heroNameActual.ToLowerInvariant(), "*", teResourceGUID.Index(heroGuid).ToString("X"));

                if (config.Count == 0) continue;
                Log($"Processing data for {heroNameActual}");
                heroNameActual = GetValidFilename(heroNameActual);

                foreach (VoiceLineInstance lineInstance in set.VoiceLines.Values) {
                    // if (lineInstance.STU.m_voiceLineRuntime.m_4FF98D41 != null) {
                    //     var cond = lineInstance.STU.m_voiceLineRuntime.m_4FF98D41;
                    //
                    //     HandleCondition(flags, comboInfo, lineInstance, path, heroNameActual, cond, mapNames);
                    // }

                    if (lineInstance.Conversations == null || lineInstance.Conversations.Length == 0) continue;

                    foreach (var conversationGuid in lineInstance.Conversations) {
                        STUVoiceConversation conversation = GetInstance<STUVoiceConversation>(conversationGuid);

                        if (conversation == null) continue; // wtf, blizz pls

                        string convoDir = Path.Combine(path, heroNameActual, GetFileName(conversationGuid));
                        foreach (STUVoiceConversationLine line in conversation.m_90D76F17) {
                            string linePath = Path.Combine(convoDir, line.m_B4D405A1.ToString());
                            foreach (VoiceSet voiceSet in allVoiceSets.Values) {
                                if (voiceSet.VoiceLines.ContainsKey(line.m_E295B99C)) {
                                    VoiceLine.SaveVoiceLine(flags, voiceSet.VoiceLines[line.m_E295B99C], linePath, comboSaveContext);
                                }
                            }
                        }
                    }
                }
            }

            comboSaveContext.Wait();
        }

        /*public void HandleCondition(ICLIFlags flags, Combo.ComboInfo comboInfo, VoiceLineInstance lineInstance,
            string path, string heroNameActual, STU_C1A2DB26 cond, Dictionary<uint, string> mapNames) {
            if (cond is STU_32A19631 cond2) {
                var subCond = cond2.m_4FF98D41;

                HandleCondition(flags, comboInfo, lineInstance, path, heroNameActual, subCond, mapNames);
            } else {

            }
        }

        public void HandleCondition(ICLIFlags flags, Combo.ComboInfo comboInfo, VoiceLineInstance lineInstance,
            string path, string heroNameActual, STU_2F33B1B7 cond, Dictionary<uint, string> mapNames) {

            if (cond is STU_7C69EA0F arrayCond) {
                foreach (STU_C1A2DB26 arrayElem in arrayCond.m_4FF98D41) {
                    HandleCondition(flags, comboInfo, lineInstance, path, heroNameActual, arrayElem, mapNames);
                }
            }

            if (cond is STU_E9DB72FF mapCond) {
                VoiceLine.SaveVoiceLine(flags, lineInstance, Path.Combine(path, heroNameActual, "Maps", mapNames[teResourceGUID.Index(mapCond.m_map)]), comboInfo);
            }

            if (cond is STU_C37857A5 celebrationCond) {
                VoiceLine.SaveVoiceLine(flags, lineInstance, Path.Combine(path, heroNameActual, "Celebrations", GetFileName(celebrationCond.m_celebrationType)), comboInfo);
            }
        }*/
    }
}
