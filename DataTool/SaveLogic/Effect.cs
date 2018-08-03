using System.Collections.Generic;
using System.IO;
using DataTool.Helper;
using TankLib;
using TankLib.ExportFormats;

namespace DataTool.SaveLogic {
    public static class Effect {
        public class OverwatchEffect : IExportFormat {
            public virtual string Extension => "oweffect";
            
            public static void WriteTime(BinaryWriter writer, EffectParser.ChunkPlaybackInfo playbackInfo) {
                writer.Write(playbackInfo.TimeInfo == null);
                if (playbackInfo.TimeInfo != null) {
                    writer.Write(playbackInfo.TimeInfo.StartTime);
                    writer.Write(playbackInfo.TimeInfo.EndTime);
                } else {
                    writer.Write(0f);
                    writer.Write(0f);
                }
                if (playbackInfo.Hardpoint != 0) {
                    writer.Write(OverwatchModel.IdToString("hardpoint", teResourceGUID.Index(playbackInfo.Hardpoint)));
                } else {
                    writer.Write("null");
                }
            }
            
            public const ushort EffectVersionMajor = 1;
            public const ushort EffectVersionMinor = 2;

            protected readonly FindLogic.Combo.ComboInfo Info;
            protected readonly FindLogic.Combo.EffectInfoCombo EffectInfo;
            protected readonly Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> VoiceStimuli;

            public OverwatchEffect(FindLogic.Combo.ComboInfo info, FindLogic.Combo.EffectInfoCombo effectInfo,
                Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> voiceStimuli) {
                Info = info;
                EffectInfo = effectInfo;
                VoiceStimuli = voiceStimuli;
            }

            public virtual void Write(Stream stream) {
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    WriteEffect(writer);
                }
            }

            protected void WriteEffect(BinaryWriter writer) {
                writer.Write("oweffect");
                writer.Write(EffectVersionMajor);
                writer.Write(EffectVersionMinor);

                EffectParser.EffectInfo effect = EffectInfo.Effect;
                
                writer.Write(teResourceGUID.Index(effect.GUID));
                writer.Write(effect.EffectLength);
                
                writer.Write(effect.DMCEs.Count);
                writer.Write(effect.CECEs.Count);
                writer.Write(effect.NECEs.Count);
                writer.Write(effect.RPCEs.Count);
                writer.Write(effect.FECEs.Count);
                writer.Write(effect.OSCEs.Count);
                writer.Write(effect.SVCEs.Count);

                foreach (EffectParser.DMCEInfo dmceInfo in effect.DMCEs) {
                    WriteTime(writer, dmceInfo.PlaybackInfo);
                    writer.Write(dmceInfo.Animation);
                    writer.Write(dmceInfo.Material);
                    writer.Write(dmceInfo.Model);
                    FindLogic.Combo.ModelInfoNew modelInfo = Info.Models[dmceInfo.Model];
                    writer.Write($"Models\\{modelInfo.GetName()}\\{modelInfo.GetNameIndex()}.owmdl");
                    if (dmceInfo.Animation == 0) {
                        writer.Write("null");
                    } else {
                        FindLogic.Combo.AnimationInfoNew animationInfo = Info.Animations[dmceInfo.Animation];
                        writer.Write($"Models\\{modelInfo.GetName()}\\{OverwatchAnimationEffect.AnimationEffectDir}\\{animationInfo.GetNameIndex()}\\{animationInfo.GetNameIndex()}.owanim");
                    }
                }

                foreach (EffectParser.CECEInfo ceceInfo in effect.CECEs) {
                    WriteTime(writer, ceceInfo.PlaybackInfo);
                    writer.Write((byte)ceceInfo.Action);
                    writer.Write(ceceInfo.Animation);
                    writer.Write(ceceInfo.Identifier);
                    writer.Write(teResourceGUID.Index(ceceInfo.Identifier));
                    if (ceceInfo.Animation != 0) {
                        FindLogic.Combo.AnimationInfoNew animationInfo = Info.Animations[ceceInfo.Animation];
                        writer.Write($"{OverwatchAnimationEffect.AnimationEffectDir}\\{animationInfo.GetNameIndex()}\\{animationInfo.GetNameIndex()}.owanim");
                    } else {
                        writer.Write("null");
                    }
                }
                
                foreach (EffectParser.NECEInfo neceInfo in effect.NECEs) {
                    WriteTime(writer, neceInfo.PlaybackInfo);
                    writer.Write(neceInfo.GUID);
                    writer.Write(teResourceGUID.Index(neceInfo.Identifier));
                    FindLogic.Combo.EntityInfoNew entityInfo = Info.Entities[neceInfo.GUID];
                    
                    writer.Write($"Entities\\{entityInfo.GetName()}\\{entityInfo.GetName()}.owentity");
                }
                
                foreach (EffectParser.RPCEInfo rpceInfo in effect.RPCEs) {
                    WriteTime(writer, rpceInfo.PlaybackInfo);
                    writer.Write(rpceInfo.Model);
                    // todo: make the materials work
                    writer.Write(rpceInfo.Material);
                    FindLogic.Combo.ModelInfoNew modelInfo = Info.Models[rpceInfo.Model];
                    //writer.Write(rpceInfo.TextureDefiniton);
                    
                    writer.Write($"Models\\{modelInfo.GetName()}\\{modelInfo.GetName()}.owmdl");
                }

                foreach (EffectParser.SVCEInfo svceInfo in effect.SVCEs) {
                    WriteTime(writer, svceInfo.PlaybackInfo);
                    writer.Write(teResourceGUID.Index(svceInfo.VoiceStimulus));
                    if (VoiceStimuli.ContainsKey(svceInfo.VoiceStimulus)) {
                        HashSet<FindLogic.Combo.VoiceLineInstanceInfo> lines = VoiceStimuli[svceInfo.VoiceStimulus];
                        writer.Write(lines.Count);

                        foreach (FindLogic.Combo.VoiceLineInstanceInfo voiceLineInstance in lines) {
                            writer.Write(voiceLineInstance.SoundFiles.Count);
                            foreach (ulong soundFile in voiceLineInstance.SoundFiles) {
                                FindLogic.Combo.SoundFileInfo soundFileInfo =
                                    Info.VoiceSoundFiles[soundFile];
                                writer.Write($"Sounds\\{soundFileInfo.GetNameIndex()}.ogg");
                            }
                        }
                    } else {
                        writer.Write(0);
                    }
                }
            }
        }

        public class OverwatchAnimationEffect : OverwatchEffect {
            public override string Extension => "owanim";
            
            public const string AnimationEffectDir = "AnimationEffects";

            protected readonly FindLogic.Combo.AnimationInfoNew Animation;
            protected readonly ulong Model;
            
            public const ushort AnimVersionMajor = 1;
            public const ushort AnimVersionMinor = 0;
            
            public OverwatchAnimationEffect(FindLogic.Combo.ComboInfo info,
                 FindLogic.Combo.EffectInfoCombo animationEffect,
                Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> voiceStimuli,
                FindLogic.Combo.AnimationInfoNew animation,
                ulong model) : base(info, animationEffect, voiceStimuli) {

                Animation = animation;
                Model = model;
            }
            
            public enum OWAnimType {
                Unknown = -1,
                Data = 0,
                Reference = 1,
                Reset = 2
            }

            public override void Write(Stream stream) {
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    writer.Write(Extension);
                    writer.Write(AnimVersionMajor);
                    writer.Write(AnimVersionMinor);
                    writer.Write(teResourceGUID.Index(Animation.GUID));
                    writer.Write(Animation.FPS);
                    writer.Write((int)OWAnimType.Data);
                    
                    FindLogic.Combo.ModelInfoNew modelInfo = Info.Models[Model];
                    
                    writer.Write($"Models\\{modelInfo.GetName()}\\Animations\\{Animation.Priority}\\{Animation.GetNameIndex()}.seanim");
                    writer.Write($"Models\\{modelInfo.GetName()}\\{modelInfo.GetNameIndex()}.owmdl");
                    
                    // wrap oweffect
                    WriteEffect(writer);
                }
            }
        }

        public class OverwatchAnimationEffectReference : IExportFormat {
            public string Extension => "owanim";

            protected readonly FindLogic.Combo.ComboInfo Info;
            protected readonly FindLogic.Combo.AnimationInfoNew Animation;
            protected readonly ulong Model;

            public OverwatchAnimationEffectReference(FindLogic.Combo.ComboInfo info, FindLogic.Combo.AnimationInfoNew animation, ulong model) {
                Info = info;
                Animation = animation;
                Model = model;
            }

            public void Write(Stream stream) {
                using (BinaryWriter writer = new BinaryWriter(stream)) {
                    writer.Write(Extension); // identifier
                    writer.Write(OverwatchAnimationEffect.AnimVersionMajor);
                    writer.Write(OverwatchAnimationEffect.AnimVersionMinor);
                    writer.Write(teResourceGUID.Index(Animation.GUID));
                    writer.Write(Animation.FPS);
                    writer.Write((int)OverwatchAnimationEffect.OWAnimType.Reference);

                    FindLogic.Combo.ModelInfoNew modelInfo = Info.Models[Model];
                    
                    writer.Write($"Models\\{modelInfo.GetName()}\\{OverwatchAnimationEffect.AnimationEffectDir}\\{Animation.GetNameIndex()}\\{Animation.GetNameIndex()}.{Extension}"); // so I can change it in DataTool and not go mad
                }
            }
        }
    }
}