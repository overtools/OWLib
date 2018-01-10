﻿using System;
using System.Collections.Generic;
using System.IO;
using DataTool.FindLogic;
using DataTool.Flag;
using DataTool.SaveLogic;
using OWLib;
using STULib.Types;
using STULib.Types.Lootboxes;
using STULib.Types.Generic;
using static DataTool.Helper.IO;
using static DataTool.Program;
using static DataTool.Helper.STUHelper;
using Model = DataTool.FindLogic.Model;
using Sound = DataTool.FindLogic.Sound;

namespace DataTool.ToolLogic.Extract {
    [Tool("extract-lootbox", Description = "Extract lootbox models", TrackTypes = new ushort[] {0xCF}, CustomFlags = typeof(ExtractFlags))]
    public class ExtractLootbox : ITool {
        public void IntegrateView(object sender) {
            throw new NotImplementedException();
        }

        public void Parse(ICLIFlags toolFlags) {
            GetLootboxes(toolFlags);
        }

        public const string Container = "Lootboxes";

        public void GetLootboxes(ICLIFlags toolFlags) {
            string basePath;
            if (toolFlags is ExtractFlags flags) {
                basePath = flags.OutputPath;
            } else {
                throw new Exception("no output path");
            }
            
            
            foreach (ulong key in TrackedFiles[0xCF]) {
                STULootbox lootbox = GetInstance<STULootbox>(key);
                if (lootbox == null) continue;
                
                string name = GetValidFilename(lootbox.Event.ToString()) ?? $"Unknown{GUID.Index(key):X}";
                
                HashSet<ModelInfo> models = new HashSet<ModelInfo>();
                models = Model.FindModels(models, lootbox.Entity);
                models = Model.FindModels(models, lootbox.Entity2);
                models = Model.FindModels(models, lootbox.Effect1);
                models = Model.FindModels(models, lootbox.Effect2);
                models = Model.FindModels(models, lootbox.Effect3);
                models = Model.FindModels(models, lootbox.ModelLook);
                models = Model.FindModels(models, lootbox.Look2);
                
                Dictionary<ulong, List<SoundInfo>> music = new Dictionary<ulong, List<SoundInfo>>();

                foreach (Common.STUGUID stuguid in new [] {lootbox.Effect1, lootbox.Effect2, lootbox.Effect3, lootbox.Entity2, lootbox.Entity}) {
                    music = Sound.FindSounds(music, stuguid, null, true);
                }
            
                SaveLogic.Sound.Save(toolFlags, Path.Combine(basePath, $"{Container}", name, "Music"), music);

                foreach (ModelInfo model in models) {
                    SaveLogic.Model.Save(flags, Path.Combine(basePath, $"{Container}\\{name}\\Models"), model, $"Lootbox {lootbox.Event}_{GUID.Index(model.GUID):X}");
                    Entity.Save(flags, Path.Combine(basePath, $"{Container}\\{name}\\Entities"), model.Entities.Values);
                }
            }
        }
    }
}