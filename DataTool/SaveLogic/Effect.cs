using System;
using System.Collections.Generic;
using System.IO;
using DataTool.Helper;
using OWLib;
using OWLib.Types;
using OWLib.Types.Map;
using OWLib.Writer;
using TankLib;
using TankLib.ExportFormats;
using Animation = OWLib.Animation;

namespace DataTool.SaveLogic {
    public class Effect {
        public class OWEffectWriter : IDataWriter {
            public WriterSupport SupportLevel => WriterSupport.MODEL; // i guess?
            public char[] Identifier => new[] {'o', 'w', 'e', 'f', 'f', 'e', 'c', 't'};
            public string Format => ".oweffect";
            public string Name => "OWM Effect Format";
            
            public const ushort VersionMajor = 1;
            public const ushort VersionMinor = 2;

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
            
            public void Write(Stream output, FindLogic.Combo.EffectInfoCombo effectInfo, FindLogic.Combo.ComboInfo info, Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> svceLines) {
                using (BinaryWriter writer = new BinaryWriter(output)) {
                    Write(writer, effectInfo, info, svceLines);
                }
            }
            
            // combo
            public void Write(BinaryWriter writer, FindLogic.Combo.EffectInfoCombo effectInfo, FindLogic.Combo.ComboInfo info, Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> svceLines) {
                writer.Write(new string(Identifier));
                writer.Write(VersionMajor);
                writer.Write(VersionMinor);

                EffectParser.EffectInfo effect = effectInfo.Effect;
                
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
                    FindLogic.Combo.ModelInfoNew modelInfo = info.Models[dmceInfo.Model];
                    writer.Write($"Models\\{modelInfo.GetName()}\\{modelInfo.GetNameIndex()}.owmdl");
                    if (dmceInfo.Animation == 0) {
                        writer.Write("null");
                    } else {
                        FindLogic.Combo.AnimationInfoNew animationInfo = info.Animations[dmceInfo.Animation];
                        writer.Write($"Models\\{modelInfo.GetName()}\\{Model.AnimationEffectDir}\\{animationInfo.GetNameIndex()}\\{animationInfo.GetNameIndex()}.owanim");
                    }
                }

                foreach (EffectParser.CECEInfo ceceInfo in effect.CECEs) {
                    WriteTime(writer, ceceInfo.PlaybackInfo);
                    writer.Write((byte)ceceInfo.Action);
                    writer.Write(ceceInfo.Animation);
                    writer.Write(ceceInfo.EntityVariable);
                    writer.Write(teResourceGUID.Index(ceceInfo.EntityVariable));
                    if (ceceInfo.Animation != 0) {
                        FindLogic.Combo.AnimationInfoNew animationInfo = info.Animations[ceceInfo.Animation];
                        writer.Write($"{Model.AnimationEffectDir}\\{animationInfo.GetNameIndex()}\\{animationInfo.GetNameIndex()}.owanim");
                    } else {
                        writer.Write("null");
                    }
                }
                
                foreach (EffectParser.NECEInfo neceInfo in effect.NECEs) {
                    WriteTime(writer, neceInfo.PlaybackInfo);
                    writer.Write(neceInfo.GUID);
                    writer.Write(teResourceGUID.Index(neceInfo.Variable));
                    FindLogic.Combo.EntityInfoNew entityInfo = info.Entities[neceInfo.GUID];
                    
                    writer.Write($"Entities\\{entityInfo.GetName()}\\{entityInfo.GetName()}.owentity");
                }
                
                foreach (EffectParser.RPCEInfo rpceInfo in effect.RPCEs) {
                    WriteTime(writer, rpceInfo.PlaybackInfo);
                    writer.Write(rpceInfo.Model);
                    // todo: make the materials work
                    writer.Write(rpceInfo.Material);
                    FindLogic.Combo.ModelInfoNew modelInfo = info.Models[rpceInfo.Model];
                    //writer.Write(rpceInfo.TextureDefiniton);
                    
                    writer.Write($"Models\\{modelInfo.GetName()}\\{modelInfo.GetName()}.owmdl");
                }

                foreach (EffectParser.SVCEInfo svceInfo in effect.SVCEs) {
                    WriteTime(writer, svceInfo.PlaybackInfo);
                    writer.Write(teResourceGUID.Index(svceInfo.VoiceStimulus));
                    if (svceLines.ContainsKey(svceInfo.VoiceStimulus)) {
                        HashSet<FindLogic.Combo.VoiceLineInstanceInfo> lines = svceLines[svceInfo.VoiceStimulus];
                        writer.Write(lines.Count);

                        foreach (FindLogic.Combo.VoiceLineInstanceInfo voiceLineInstance in lines) {
                            writer.Write(voiceLineInstance.SoundFiles.Count);
                            foreach (ulong soundFile in voiceLineInstance.SoundFiles) {
                                FindLogic.Combo.SoundFileInfo soundFileInfo =
                                    info.VoiceSoundFiles[soundFile];
                                writer.Write($"Sounds\\{soundFileInfo.GetNameIndex()}.ogg");
                            }
                        }
                    } else {
                        writer.Write(0);
                    }
                }

                // foreach (EffectParser.FECEInfo feceInfo in effect.FECEs) {
                //     WriteTime(writer, feceInfo.PlaybackInfo);
                //     writer.Write(feceInfo.GUID);
                // }
                // 
                // foreach (EffectParser.OSCEInfo osceInfo in effect.OSCEs) {
                //     // this needs preprocessing, get the sounds
                //     WriteTime(writer, osceInfo.PlaybackInfo);
                //     writer.Write(osceInfo.Sound);
                // }
            }

            public bool Write(Chunked model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, params object[] data) {
                throw new NotImplementedException();
            }

            public bool Write(Map10 physics, Stream output, params object[] data) {
                throw new NotImplementedException();
            }

            public bool Write(Animation anim, Stream output, params object[] data) {
                throw new NotImplementedException();
            }

            public Dictionary<ulong, List<string>>[] Write(Stream output, OWLib.Map map, OWLib.Map detail1, OWLib.Map detail2, OWLib.Map props, OWLib.Map lights, string name,
                IDataWriter modelFormat) {
                throw new NotImplementedException();
            }
        }
        
        public class OWAnimWriter : IDataWriter {
            public WriterSupport SupportLevel => WriterSupport.ANIM;
            public char[] Identifier => new[] { 'o', 'w', 'a', 'n', 'i', 'm'};
            public string Format => ".owanim";

            public const ushort VersionMajor = 1;
            public const ushort VersionMinor = 0;
            
            public string Name => "OWM Animation Format";

            public enum OWAnimType {
                Unknown = -1,
                Data = 0,
                Reference = 1,
                Reset = 2
            }

            
            // plan and stuff:
            //     dmce submodels
            //     how are weapons attached? TICK
            //     stretch goal:
            //         how do camera
            
            public void WriteReference(Stream output, FindLogic.Combo.ComboInfo info, FindLogic.Combo.AnimationInfoNew animation, ulong model) {
                using (BinaryWriter writer = new BinaryWriter(output)) {
                    writer.Write(new string(Identifier));
                    writer.Write(VersionMajor);
                    writer.Write(VersionMinor);
                    writer.Write(teResourceGUID.Index(animation.GUID));
                    writer.Write(animation.FPS);
                    writer.Write((int)OWAnimType.Reference);

                    FindLogic.Combo.ModelInfoNew modelInfo = info.Models[model];
                    
                    writer.Write($"Models\\{modelInfo.GetName()}\\{Model.AnimationEffectDir}\\{animation.GetNameIndex()}\\{animation.GetNameIndex()}{Format}"); // so I can change it in DataTool and not go mad
                }
            }
            
            public void Write(Stream output, FindLogic.Combo.ComboInfo info, FindLogic.Combo.AnimationInfoNew animation,
                FindLogic.Combo.EffectInfoCombo animationEffect, ulong model, 
                Dictionary<ulong, HashSet<FindLogic.Combo.VoiceLineInstanceInfo>> svceLines) {
                using (BinaryWriter writer = new BinaryWriter(output)) {
                    writer.Write(new string(Identifier));
                    writer.Write(VersionMajor);
                    writer.Write(VersionMinor);
                    writer.Write(teResourceGUID.Index(animation.GUID));
                    writer.Write(animation.FPS);
                    writer.Write((int)OWAnimType.Data);
                    
                    FindLogic.Combo.ModelInfoNew modelInfo = info.Models[model];
                    
                    writer.Write($"Models\\{modelInfo.GetName()}\\Animations\\{animation.Priority}\\{animation.GetNameIndex()}.seanim");
                    writer.Write($"Models\\{modelInfo.GetName()}\\{modelInfo.GetNameIndex()}.owmdl");
                    // wrap oweffect
                    OWEffectWriter effectWriter = new OWEffectWriter();
                    effectWriter.Write(writer, animationEffect, info, svceLines);
                }
            }

            public bool Write(Chunked model, Stream output, List<byte> LODs, Dictionary<ulong, List<ImageLayer>> layers, params object[] data) {
                throw new NotImplementedException();
            }

            public bool Write(Map10 physics, Stream output, params object[] data) {
                throw new NotImplementedException();
            }

            public bool Write(Animation anim, Stream output, params object[] data) {
                throw new NotImplementedException();
            }

            public Dictionary<ulong, List<string>>[] Write(Stream output, OWLib.Map map, OWLib.Map detail1, OWLib.Map detail2, OWLib.Map props, OWLib.Map lights, string name, IDataWriter modelFormat) {
                throw new NotImplementedException();
            }
        }
    }
}