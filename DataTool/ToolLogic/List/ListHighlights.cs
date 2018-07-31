using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.Flag;
using DataTool.Helper;
using Newtonsoft.Json;
using TankLib.Replay;
using TankLib.STU.Types;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.Logger;
using STUHero = TankLib.STU.Types.STUHero;
using STUUnlock_Emote = TankLib.STU.Types.STUUnlock_Emote;
using STUUnlock_VoiceLine = TankLib.STU.Types.STUUnlock_VoiceLine;

namespace DataTool.ToolLogic.List {
    [Tool("list-highlights", Description = "List user highlights", TrackTypes = new ushort[] {}, CustomFlags = typeof(ListFlags))]
    public class ListHighlights: JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        [JsonObject(MemberSerialization.OptOut)]
        public class ReplayJSON {
            public uint BuildNumber;
            public string GameMode;
            public HighlightInfoJSON HighlightInfo;
            public string Map;
        }
        

        [JsonObject(MemberSerialization.OptOut)]
        public class HeroInfoJSON {
            public string Hero;
            public List<string> Sprays;
            public List<string> Emotes;
            public List<string> VoiceLines;
            public string HighlightIntro;
            public string Skin;
        }
        
        [JsonObject(MemberSerialization.OptOut)]
        public class HighlightInfoJSON {
            public string Player;
            public string Hero;
            public string HighlightIntro;
            public string Skin;
            public string WeaponSkin;
            public string HighlightType;
        }
        
        [JsonObject(MemberSerialization.OptOut)]
        public class HighlightJSON {
            public string UUID;
            public long PlayerID;
            public string Flags;
            public string Map;
            public string GameMode;
            public List<HeroInfoJSON> HeroInfo;
            public List<HighlightInfoJSON> HighlightInfo;
            public ReplayJSON Replay;
        }

        public void PrintHighlightInfoJSON(HighlightInfoJSON info, IndentHelper indent) {
            Log($"{indent}Player: {info.Player}");
            Log($"{indent}Hero: {info.Hero}");
            Log($"{indent}Skin: {info.Skin}");
            Log($"{indent}Weapon: {info.WeaponSkin}");
            Log($"{indent}HighlightType: {info.HighlightType}");
        }

        public void Parse(ICLIFlags toolFlags) {
            List<HighlightJSON> highlights = new List<HighlightJSON>();
            
            DirectoryInfo overwatchAppdataFolder = new DirectoryInfo(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Blizzard Entertainment/Overwatch"));
            foreach (DirectoryInfo userFolder in overwatchAppdataFolder.GetDirectories()) {
                DirectoryInfo highlightsFolder = new DirectoryInfo(Path.Combine(userFolder.FullName,
                    $"{(Program.IsPTR ? "PTR\\" : "")}Highlights"));
                if (!highlightsFolder.Exists) continue;
                highlights.AddRange(highlightsFolder.GetFiles().Select(item => GetHighlight(item.FullName)));
            }
            
            if (toolFlags is ListFlags flags) {
                if (flags.JSON) {
                    ParseJSON(highlights, flags);
                    return;
                }
            }
            
            // todo: timestamp
            IndentHelper indent = new IndentHelper();
            uint count = 0;
            foreach (HighlightJSON highlight in highlights) {
                if (count != 0) {
                    Log($"{Environment.NewLine}");
                }
                Log($"{indent}{highlight.UUID}:");
                Log($"{indent+1}Map: {highlight.Map}");
                Log($"{indent+1}Gamemode: {highlight.GameMode}");
                Log($"{indent+1}HighlightInfo:");
                for (int i = 0; i < highlight.HighlightInfo.Count; i++) {
                    Log($"{indent+2}[{i}] {{");
                    PrintHighlightInfoJSON(highlight.HighlightInfo[i], indent+3);
                    Log($"{indent+2}}}");
                }
                Log($"{indent+1}Heroes:");
                for (int i = 0; i < highlight.HeroInfo.Count; i++) {
                    Log($"{indent+2}[{i}] {{");
                    Log($"{indent+3}Hero: {highlight.HeroInfo[i].Hero}");
                    Log($"{indent+3}Skin: {highlight.HeroInfo[i].Skin}");
                    Log($"{indent+3}HiglightIntro: {highlight.HeroInfo[i].HighlightIntro}");
                    
                    Log($"{indent+3}Sprays:");
                    foreach (string spray in highlight.HeroInfo[i].Sprays) {
                        Log($"{indent+4}{spray}");
                    }
                    
                    Log($"{indent+3}Emotes:");
                    foreach (string emote in highlight.HeroInfo[i].Emotes) {
                        Log($"{indent+4}{emote}");
                    }
                    
                    Log($"{indent+3}VoiceLines:");
                    foreach (string voiceLine in highlight.HeroInfo[i].VoiceLines) {
                        Log($"{indent+4}{voiceLine}");
                    }
                    
                    Log($"{indent+2}}}");
                }
                Log($"{indent+1}Replay: {{");
                Log($"{indent+2}Map: {highlight.Replay.Map}");
                Log($"{indent+2}Gamemode: {highlight.Replay.GameMode}");
                Log($"{indent+2}HighlightInfo:");
                PrintHighlightInfoJSON(highlight.Replay.HighlightInfo, indent+3);
                Log($"{indent+1}}}");
                count++;
            }
        }
        
        protected ulong GetCosmeticKey(uint key) => (key & ~0xFFFFFFFF00000000ul) | 0x0250000000000000ul;

        protected HighlightInfoJSON GetHighlightInfo(tePlayerHighlight.HighlightInfo infoNew) {
            HighlightInfoJSON outputJson = new HighlightInfoJSON();
            STUHero hero = GetInstance<STUHero>(infoNew.Hero);

            outputJson.Hero = GetString(hero?.m_0EDCE350);
            outputJson.Player = infoNew.PlayerName;
                
            STUUnlock_POTGAnimation intro = GetInstance<STUUnlock_POTGAnimation>(infoNew.HighlightIntro);
            outputJson.HighlightIntro = GetString(intro.m_name);
            
            // todo: outputJson.WeaponSkin
            // todo: outputJson.Skin
                
            STU_C25281C3 highlightType = GetInstance<STU_C25281C3>(infoNew.HighlightType);
            outputJson.HighlightType = GetString(highlightType?.m_description) ?? "";
            return outputJson;
        }

        protected static string GetMapName(ulong key) {
            STUMapHeader map = GetInstance<STUMapHeader>(key);
            return GetString(map.m_displayName);
        }

        protected HeroInfoJSON GetHeroInfo(HeroData heroInfo) {
            STUHero hero = GetInstance<STUHero>(heroInfo.Hero);

            HeroInfoJSON outputHero = new HeroInfoJSON {
                Hero = GetString(hero.m_0EDCE350),
                Sprays = new List<string>(),
                Emotes = new List<string>(),
                VoiceLines = new List<string>()
            };
            foreach (uint sprayId in heroInfo.SprayIds) {
                STUUnlock_SprayPaint spray = GetInstance<STUUnlock_SprayPaint>(GetCosmeticKey(sprayId));
                outputHero.Sprays.Add(GetString(spray.m_name));
            }
            foreach (uint emoteId in heroInfo.EmoteIds) {
                STUUnlock_Emote emote = GetInstance<STUUnlock_Emote>(GetCosmeticKey(emoteId));
                outputHero.Emotes.Add(GetString(emote.m_name));
            }
                
            foreach (uint voiceLineId in heroInfo.VoiceLineIds) {
                STUUnlock_VoiceLine voiceLine = GetInstance<STUUnlock_VoiceLine>(GetCosmeticKey(voiceLineId));
                outputHero.VoiceLines.Add(GetString(voiceLine.m_name));
            }
            STUUnlock_POTGAnimation intro = GetInstance<STUUnlock_POTGAnimation>(GetCosmeticKey(heroInfo.POTGAnimation));
            outputHero.HighlightIntro = GetString(intro.m_name);
                
            // Skin skin = GetInstance<Skin>(GetSkinKey(heroInfo.SkinId));  // todo: this is by skin override
            // outputHero.Skin = GetString(skin?.CosmeticName);
                
            // Weapon weaponSkin = GetInstance<Weapon>(GetCosmeticKey(heroInfo.WeaponSkinId));  // todo: this is by weapon skin override
            // outputHero.WeaponSkin = GetString(weaponSkin?.CosmeticName);

            return outputHero;
        }

        protected string GetGamemode(ulong guid) {
            STUGameMode gamemode = GetInstance<STUGameMode>(guid);
            return GetString(gamemode?.m_displayName);
        }

        protected ReplayJSON GetReplay(tePlayerReplay playerReplay) {
            ReplayJSON output = new ReplayJSON {BuildNumber = playerReplay.BuildNumber};

            ulong mapMetadataKey = (playerReplay.Map & ~0xFFFFFFFF00000000ul) | 0x0790000000000000ul;
            output.Map = GetMapName(mapMetadataKey);
            output.HighlightInfo = GetHighlightInfo(playerReplay.HighlightInfo);
            output.GameMode = GetGamemode(playerReplay.GameMode);
            
            return output;
        } 
        
        public HighlightJSON GetHighlight(string file) {
            tePlayerHighlight playerHighlight = new tePlayerHighlight(File.OpenRead(file));
            
            HighlightJSON output = new HighlightJSON {
                PlayerID = playerHighlight.PlayerId,
                Flags = playerHighlight.Flags.ToString(),
                HeroInfo = new List<HeroInfoJSON>(),
                HighlightInfo = new List<HighlightInfoJSON>(),
                UUID = playerHighlight.Info.FirstOrDefault()?.UUID.ToString()
            };
            
            ulong mapHeaderGuid = (playerHighlight.Map & ~0xFFFFFFFF00000000ul) | 0x0790000000000000ul;
            output.Map = GetMapName(mapHeaderGuid);

            foreach (HeroData heroInfo in playerHighlight.Heroes) {
                output.HeroInfo.Add(GetHeroInfo(heroInfo));
            }

            foreach (tePlayerHighlight.HighlightInfo infoNew in playerHighlight.Info) {
                output.HighlightInfo.Add(GetHighlightInfo(infoNew));
            }

            output.Replay = GetReplay(new tePlayerReplay(playerHighlight.Replay));
            output.GameMode = GetGamemode(playerHighlight.GameMode);
            
            return output;
        }
    }
}