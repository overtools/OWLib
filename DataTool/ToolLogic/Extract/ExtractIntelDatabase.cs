using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using DataTool.DataModels.Hero;
using DataTool.Flag;
using DataTool.Helper;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Program;
using Combo = DataTool.FindLogic.Combo;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-intel-database", Aliases = new [] { "extract-intel-db" }, Description = "Extracts assets from the Intel Database", CustomFlags = typeof(ExtractFlags))]
    public class ExtractIntelDatabase : ITool {
        private const string Container = "IntelDatabase";

        public void Parse(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            Combo.ComboInfo texturesCombo = new Combo.ComboInfo();
            var textFilePath = Path.Combine(basePath, Container, "Text");
            IO.CreateDirectorySafe(textFilePath);

            foreach (var key in TrackedFiles[0x14B]) {
                var loreEntry = STUHelper.GetInstance<STU_D23C0F93>(key);
                if (loreEntry == null) break;

                var sb = new StringBuilder();
                string loreEntryFileName = null;

                if (loreEntry is STU_BFEFD7C8 genericLoreEntry) {
                    Combo.Find(texturesCombo, genericLoreEntry.m_1B3F1138); // image

                    loreEntryFileName = IO.GetString(genericLoreEntry.m_93E355A7);
                    var title = IO.GetString(genericLoreEntry.m_0E5B7969);
                    var textContent = IO.GetString(genericLoreEntry.m_2EA7B7ED);
                    if (!string.IsNullOrEmpty(title)) {
                        sb.AppendLine($"Title: {title}");

                        if (textContent != null) {
                            sb.AppendLine();
                            sb.AppendLine(textContent);
                        }

                        if (genericLoreEntry.m_D8438F77 != null) {
                            sb.AppendLine();
                            foreach (var subPage in genericLoreEntry.m_D8438F77) {
                                sb.AppendLine(IO.GetString(subPage.m_453BFD86));
                                sb.AppendLine();
                                sb.AppendLine(IO.GetString(subPage.m_D92261FC));
                                sb.AppendLine();
                            }
                        }
                    }
                }

                if (loreEntry is STU_18CF25E8 cineLoreEntry) {
                    Combo.Find(texturesCombo, cineLoreEntry.m_7E748F9C); // image
                }

                if (loreEntry is STU_A6D9C44D voiceLoreEntry) {
                    Combo.Find(texturesCombo, voiceLoreEntry.m_9FDD57CB); // image
                    SaveVoiceData(flags, basePath, voiceLoreEntry);

                    loreEntryFileName = IO.GetString(voiceLoreEntry.m_BE3CC239);

                    sb.AppendLine($"Title: {IO.GetString(voiceLoreEntry.m_5A93E6EF)}");
                    sb.AppendLine($"Subject: {loreEntryFileName}");
                    sb.AppendLine();
                    sb.AppendLine(IO.GetString(voiceLoreEntry.m_F72B890F));
                }

                if (loreEntry is STU_3C813849 emailLoreEntry) {
                    loreEntryFileName = IO.GetString(emailLoreEntry.m_BE3CC239);

                    sb.AppendLine($"Title: {IO.GetString(emailLoreEntry.m_51A7EAD1)}");
                    sb.AppendLine($"Subject: {loreEntryFileName}");
                    sb.AppendLine();
                    sb.AppendLine(IO.GetString(emailLoreEntry.m_F59A0BC1));
                }

                if (loreEntry is STU_13DB827F chatLogLoreEntry) {
                    foreach (var entry in chatLogLoreEntry.m_F8453BC4) {
                        var heroName = new Hero(entry.m_78468866)?.Name;
                        var message = IO.GetString(entry.m_F59A0BC1);
                        sb.AppendLine($"{heroName}: {message}");
                        sb.AppendLine();
                    }
                }

                if (loreEntryFileName != null && sb.Length > 0) {
                    SaveTextData(key, textFilePath, loreEntryFileName, sb);
                }
            }

            var context = new SaveLogic.Combo.SaveContext(texturesCombo);
            SaveLogic.Combo.SaveLooseTextures(flags, Path.Combine(basePath, Container, "Textures"), context);
        }

        private void SaveTextData(ulong key, string basePath, string fileName, StringBuilder sb) {
            var cleanFileName = IO.GetValidFilename(fileName);

            var filePath = Path.Combine(basePath, $"{teResourceGUID.AsString(key)}-{cleanFileName}.txt");
            File.WriteAllText(filePath, sb.ToString().Trim());
        }

        private void SaveVoiceData(ExtractFlags flags, string basePath, STU_A6D9C44D voiceLoreEntry) {
            Combo.ComboInfo voiceCombo = new Combo.ComboInfo();
            Combo.Find(voiceCombo, voiceLoreEntry.m_voiceDefinition);

            var fileName = IO.GetValidFilename(IO.GetString(voiceLoreEntry.m_BE3CC239));
            var voiceLinesDirectory = Path.Combine(basePath, Container, "Voicelines", fileName);
            var voiceSetLines = voiceCombo.m_voiceSets.GetValueOrDefault(voiceLoreEntry.m_voiceDefinition);

            if (voiceSetLines?.VoiceLineInstances != null) {
                foreach (var (_, voiceLineInstanceInfos) in voiceSetLines.VoiceLineInstances) {
                    // filter out voice lines from the voice set that don't match the stimulus
                    var filteredVoiceLines = voiceLineInstanceInfos.Where(x => x.VoiceStimulus == voiceLoreEntry.m_voiceStimulus).ToArray();
                    if (!filteredVoiceLines.Any()) {
                        continue;
                    }

                    // get first voiceline to try and find conversation
                    var firstVoiceLine = filteredVoiceLines.FirstOrDefault();
                    var conversationGuid = firstVoiceLine?.Conversations?.FirstOrDefault();

                    teResourceGUID?[] voiceLineOrder = null;
                    if (conversationGuid != null) {
                        var conversation = STUHelper.GetInstance<STUVoiceConversation>(conversationGuid.Value);
                        voiceLineOrder = conversation.m_90D76F17?.OrderBy(x => x.m_B4D405A1).Select(x => x.m_E295B99C?.GUID).ToArray();
                    }

                    // order the voicelines by the conversation order
                    var orderedVoiceLines = filteredVoiceLines.OrderBy(x => {
                        if (voiceLineOrder == null) return 0;
                        var index = Array.IndexOf(voiceLineOrder, x.GUIDx06F);
                        return index == -1 ? 0 : index;
                    }).ToArray();

                    var i = 0;
                    foreach (var voiceLineInstanceInfo in orderedVoiceLines) {
                        SaveLogic.Combo.SaveVoiceLineInstance(flags, voiceLinesDirectory, voiceLineInstanceInfo, fileNamePrefix: $"{i++}");
                    }
                }
            }
        }
    }
}