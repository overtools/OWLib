using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DataTool.Flag;
using DataTool.Helper;
using Newtonsoft.Json;
using TankLib;
using STULib.Types;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.Logger;

namespace DataTool.ToolLogic.List {
    [Tool("list-highlights", Description = "List user highlights", TrackTypes = new ushort[] {}, CustomFlags = typeof(ListFlags))]
    public class ListHighlights: JSONTool, ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        [JsonObject(MemberSerialization.OptOut)]
        public class ReplayJSON {
            public uint BuildNumber;
            public string Gamemode;
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
            public string Gamemode;
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
                Log($"{indent+1}Gamemode: {highlight.Gamemode}");
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
                Log($"{indent+2}Gamemode: {highlight.Replay.Gamemode}");
                Log($"{indent+2}HighlightInfo:");
                PrintHighlightInfoJSON(highlight.Replay.HighlightInfo, indent+3);
                Log($"{indent+1}}}");
                count++;
            }
        }
        
        protected ulong GetCosmeticKey(uint key) => (key & ~0xFFFFFFFF00000000ul) | 0x0250000000000000ul;

        protected HighlightInfoJSON GetHighlightInfo(teHighlight.HighlightInfo infoNew) {
            HighlightInfoJSON outputJson = new HighlightInfoJSON();
            STUHero hero = GetInstance<STUHero>((ulong)infoNew.Hero);

            outputJson.Hero = GetString(hero?.Name);
            outputJson.Player = infoNew.PlayerName;
                
            STUUnlock_HighlightIntro intro = GetInstance<STUUnlock_HighlightIntro>((ulong)infoNew.HighlightIntro);
            outputJson.HighlightIntro = GetString(intro.CosmeticName);
            
            // todo: outputJson.WeaponSkin
            // todo: outputJson.Skin
                
            STUHighlightType highlightType = GetInstance<STUHighlightType>((ulong)infoNew.HighlightType);
            outputJson.HighlightType = GetString(highlightType?.Name) ?? "";
            return outputJson;
        }

        protected string GetMapName(ulong key) {
            STUMap map = GetInstance<STUMap>(key);
            return GetString(map.DisplayName);
        }

        protected HeroInfoJSON GetHeroInfo(teHeroData heroInfo) {
            STUHero hero = GetInstance<STUHero>((ulong)heroInfo.Hero);

            HeroInfoJSON outputHero = new HeroInfoJSON {
                Hero = GetString(hero.Name),
                Sprays = new List<string>(),
                Emotes = new List<string>(),
                VoiceLines = new List<string>()
            };
            foreach (uint sprayId in heroInfo.SprayIds) {
                STUUnlock_Spray spray = GetInstance<STUUnlock_Spray>(GetCosmeticKey(sprayId));
                outputHero.Sprays.Add(GetString(spray.CosmeticName));
            }
            foreach (uint emoteId in heroInfo.EmoteIds) {
                STUUnlock_Emote emote = GetInstance<STUUnlock_Emote>(GetCosmeticKey(emoteId));
                outputHero.Emotes.Add(GetString(emote.CosmeticName));
            }
                
            foreach (uint voiceLineId in heroInfo.VoiceLineIds) {
                STUUnlock_VoiceLine voiceLine = GetInstance<STUUnlock_VoiceLine>(GetCosmeticKey(voiceLineId));
                outputHero.VoiceLines.Add(GetString(voiceLine.CosmeticName));
            }
            STUUnlock_HighlightIntro intro = GetInstance<STUUnlock_HighlightIntro>(GetCosmeticKey(heroInfo.HighlightIntro));
            outputHero.HighlightIntro = GetString(intro.CosmeticName);
                
            // Skin skin = GetInstance<Skin>(GetSkinKey(heroInfo.SkinId));  // todo: this is by skin override
            // outputHero.Skin = GetString(skin?.CosmeticName);
                
            // Weapon weaponSkin = GetInstance<Weapon>(GetCosmeticKey(heroInfo.WeaponSkinId));  // todo: this is by weapon skin override
            // outputHero.WeaponSkin = GetString(weaponSkin?.CosmeticName);

            return outputHero;
        }

        protected string GetGamemode(ulong guid) {
            STUGamemode gamemode = GetInstance<STUGamemode>(guid);
            return GetString(gamemode?.DisplayName);
        }

        protected ReplayJSON GetReplay(teReplay replay) {
            ReplayJSON output = new ReplayJSON {BuildNumber = replay.BuildNumber};

            ulong mapMetadataKey = ((ulong)replay.Map & ~0xFFFFFFFF00000000ul) | 0x0790000000000000ul;
            output.Map = GetMapName(mapMetadataKey);
            output.HighlightInfo = GetHighlightInfo(replay.HighlightInfo);
            output.Gamemode = GetGamemode((ulong)replay.Gamemode);
            
            return output;
        } 
        
        public HighlightJSON GetHighlight(string file) {
            teHighlight highlight = new teHighlight(File.OpenRead(file));
            
            HighlightJSON output = new HighlightJSON {
                PlayerID = highlight.PlayerId,
                Flags = highlight.Flags.ToString(),
                HeroInfo = new List<HeroInfoJSON>(),
                HighlightInfo = new List<HighlightInfoJSON>(),
                UUID = highlight.Info.FirstOrDefault()?.UUID.ToString()
            };
            
            ulong mapMetadataKey = ((ulong)highlight.Map & ~0xFFFFFFFF00000000ul) | 0x0790000000000000ul;
            output.Map = GetMapName(mapMetadataKey);

            foreach (teHeroData heroInfo in highlight.Heroes) {
                output.HeroInfo.Add(GetHeroInfo(heroInfo));
            }

            foreach (teHighlight.HighlightInfo infoNew in highlight.Info) {
                output.HighlightInfo.Add(GetHighlightInfo(infoNew));
            }

            output.Replay = GetReplay(new teReplay(highlight.Replay));
            output.Gamemode = GetGamemode((ulong)highlight.Gamemode);
            
            return output;
        }
    }
}