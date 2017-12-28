using System;
using System.Diagnostics;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using STULib.Types;
using static DataTool.Program;
using static DataTool.Helper.IO;
using static DataTool.Helper.STUHelper;

namespace DataTool.ToolLogic.Extract.Debug {
    [Tool("extract-debug-combo", Description = "Extract hero using FindLogic.Combo (debug)", TrackTypes = new ushort[] {0x75}, CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
    public class ExtractDebugComboDemo : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            ExtractHeroes(toolFlags);
        }

        public void ExtractHeroes(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }

            const string container = "DebugCombo";
            
            foreach (ulong key in TrackedFiles[0x75]) {
                // if (GUID.Index(key) != 0xDEADBEEF) continue;

                STUHero hero = GetInstance<STUHero>(key);
                string heroName = GetString(hero.Name);
                
                if (heroName != "Tracer") continue;

                Stopwatch stopwatch = Stopwatch.StartNew();
                Combo.ComboInfo info = new Combo.ComboInfo();
                Combo.Find(info, hero.EntityHeroSelect);
                Combo.Find(info, hero.EntityHighlightIntro);
                Combo.Find(info, hero.EntityMain);
                Combo.Find(info, hero.EntityPlayable);
                Combo.Find(info, hero.EntityThirdPerson);
                info.SetEntityName(hero.EntityHeroSelect, $"{heroName}-HeroSelect");
                info.SetEntityName(hero.EntityHighlightIntro, $"{heroName}-HighlightIntro");
                info.SetEntityName(hero.EntityMain, $"{heroName}-Main");
                info.SetEntityName(hero.EntityPlayable, $"{heroName}-Playable");
                info.SetEntityName(hero.EntityThirdPerson, $"{heroName}-Thirdperson");
                stopwatch.Stop();
                long newTime = stopwatch.ElapsedMilliseconds;
                
                SaveLogic.Combo.Save(flags, Path.Combine(basePath, heroName), info);
                
                // stopwatch.Reset();
                // stopwatch.Start();
                // 
                // HashSet<ModelInfo> models = new HashSet<ModelInfo>();
                // Model.FindModels(models, hero.EntityHeroSelect);
                // Model.FindModels(models, hero.EntityHighlightIntro);
                // Model.FindModels(models, hero.EntityMain);
                // Model.FindModels(models, hero.EntityPlayable);
                // Model.FindModels(models, hero.EntityThirdPerson);
                // stopwatch.Stop();
                // long oldTime = stopwatch.ElapsedMilliseconds;
            }
        }
    }
}