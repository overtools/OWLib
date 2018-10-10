using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DataTool.Helper;
using TankLib;
using TankLib.Chunks;
using TankLib.STU;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.FindLogic {
    public static class Combo {
        private static readonly HashSet<ushort> UnhandledTypes = new HashSet<ushort>();

        public class ComboInfo {
            // keep everything at top level, stops us from doing the same things again.
            // everything here is unsorted, but we can use GUIDs as references.
            public Dictionary<ulong, EntityInfoNew> Entities;
            public Dictionary<ulong, HashSet<ulong>> EntitiesByIdentifier;
            public Dictionary<ulong, ModelInfoNew> Models;
            public Dictionary<ulong, MaterialInfo> Materials;
            public Dictionary<ulong, MaterialDataInfo> MaterialDatas;
            public Dictionary<ulong, ModelLookInfo> ModelLooks;
            public Dictionary<ulong, AnimationInfoNew> Animations;
            public Dictionary<ulong, TextureInfoNew> Textures;
            public Dictionary<ulong, EffectInfoCombo> Effects;
            public Dictionary<ulong, EffectInfoCombo> AnimationEffects;
            public Dictionary<ulong, SoundInfoNew> Sounds;
            public Dictionary<ulong, WWiseBankInfo> SoundBanks;
            public Dictionary<ulong, SoundFileInfo> VoiceSoundFiles;
            public Dictionary<ulong, SoundFileInfo> SoundFiles;
            public Dictionary<ulong, VoiceSetInfo> VoiceSets;
            public Dictionary<ulong, MapInfoNew> Maps;
            public Dictionary<ulong, StringInfo> Strings;

            public ComboConfig Config = new ComboConfig();
            public ComboSaveConfig SaveConfig = new ComboSaveConfig();

            public ComboInfo() {
                Entities = new Dictionary<ulong, EntityInfoNew>();
                EntitiesByIdentifier = new Dictionary<ulong, HashSet<ulong>>();
                Models = new Dictionary<ulong, ModelInfoNew>();
                Materials = new Dictionary<ulong, MaterialInfo>();
                MaterialDatas = new Dictionary<ulong, MaterialDataInfo>();
                ModelLooks = new Dictionary<ulong, ModelLookInfo>();
                Animations = new Dictionary<ulong, AnimationInfoNew>();
                Textures = new Dictionary<ulong, TextureInfoNew>();
                Effects = new Dictionary<ulong, EffectInfoCombo>();
                AnimationEffects = new Dictionary<ulong, EffectInfoCombo>();
                Sounds = new Dictionary<ulong, SoundInfoNew>();
                SoundBanks = new Dictionary<ulong, WWiseBankInfo>();
                VoiceSoundFiles = new Dictionary<ulong, SoundFileInfo>();
                SoundFiles = new Dictionary<ulong, SoundFileInfo>();
                VoiceSets = new Dictionary<ulong, VoiceSetInfo>();
                Maps = new Dictionary<ulong, MapInfoNew>();
                Strings = new Dictionary<ulong, StringInfo>();
            }

            public void SetEntityName(ulong entity, string name, Dictionary<ulong, ulong> replacements=null) {
                if (replacements != null) entity = GetReplacement(entity, replacements);
                if (Entities.ContainsKey(entity)) {
                    Entities[entity].Name = name.TrimEnd(' ');
                }
            }

            public void SetTextureName(ulong texture, string name, Dictionary<ulong, ulong> replacements=null) {
                if (replacements != null) texture = GetReplacement(texture, replacements);
                if (Textures.ContainsKey(texture)) {
                    Textures[texture].Name = name.TrimEnd(' ');
                }
            }
            
            public void SetEffectName(ulong effect, string name, Dictionary<ulong, ulong> replacements=null) {
                if (replacements != null) effect = GetReplacement(effect, replacements);
                if (Effects.ContainsKey(effect)) {
                    Effects[effect].Name = name.TrimEnd(' ');
                }
                if (AnimationEffects.ContainsKey(effect)) {
                    AnimationEffects[effect].Name = name.TrimEnd(' ');
                }
            }

            public void SetModelLookName(ulong look, string name, Dictionary<ulong, ulong> replacements=null) {
                if (replacements != null) look = GetReplacement(look, replacements);
                if (ModelLooks.ContainsKey(look)) {
                    ModelLooks[look].Name = name.TrimEnd(' ');
                }
            }
            
            public void SetEffectVoiceSet(ulong effect, ulong voiceSet) {
                if (AnimationEffects.ContainsKey(effect)) SetEffectVoiceSet(AnimationEffects[effect], voiceSet);
                if (Effects.ContainsKey(effect)) SetEffectVoiceSet(Effects[effect], voiceSet);
            }
            public void SetEffectVoiceSet(EffectInfoCombo effect, ulong voiceSet) {
                effect.Effect.VoiceSet = voiceSet;
            }
        }

        public class ComboConfig {
            public bool DoExistingEntities = false;
            public bool FullLog = false;
        }

        public class ComboSaveConfig {
            public bool SaveAnimationEffects = true;
        }
        
        public class ComboType {
            public ulong GUID;

            public ComboType(ulong guid) {
                GUID = guid;
            }
            
            public virtual string GetName() {
                return GetFileName(GUID);
            }
        
            public virtual string GetNameIndex() {
                return $"{GUID & 0xFFFFFFFFFFFF:X12}";
            }
        }
        
        public class ComboNameable : ComboType {
            public string Name;

            public ComboNameable(ulong guid) : base(guid) {
                if (GUID == 0) return;
                uint type = teResourceGUID.Type(GUID);
                uint index = teResourceGUID.Index(GUID);
                if (!GUIDTable.ContainsKey(type)) return;
                if (GUIDTable[type].ContainsKey(index)) {
                    Name = GUIDTable[type][index];
                }
            }

            public override string GetName() {
                return GetValidFilename(Name) ?? GetFileName(GUID);
            }

            public override string GetNameIndex() {
                return GetValidFilename(Name) ?? $"{GUID & 0xFFFFFFFFFFFF:X12}";
            }
        }

        public class StringInfo : ComboType {
            public StringInfo(ulong guid) : base(guid) {}

            public string Value;
        }

        public class MapInfoNew : ComboType {
            public MapInfoNew(ulong guid) : base(guid) { }

            public ulong SkyboxModel;  // todo
        }

        public class VoiceSetInfo : ComboType {
            public VoiceSetInfo(ulong guid) : base(guid) { }
            
            public Dictionary<ulong, HashSet<VoiceLineInstanceInfo>> VoiceLineInstances;
            // key = 078 voice stimulus
        }

        public class VoiceLineInstanceInfo {
            public ulong GUIDx06F;
            public ulong GUIDx09B;
            public ulong GUIDx03C;
            public ulong VoiceLineSet;
            public ulong ExternalSound;
            public ulong VoiceStimulus;
            public ulong VoiceConversation;
            public ulong Subtitle;
            public HashSet<ulong> SoundFiles;
        }
        
        public class SoundFileInfo : ComboType {
            public SoundFileInfo(ulong guid) : base(guid) { }
        }

        public class SoundInfoNew : ComboType {
            public Dictionary<uint, ulong> SoundFiles;
            public Dictionary<uint, ulong> SoundStreams;
            public ulong SoundBank;
            public SoundInfoNew(ulong guid) : base(guid) { }
        }

        public class WWiseBankEvent {
            public ConvertLogic.Sound.BankObjectEventAction.EventActionType Type;
            public uint StartDelay;  // milliseconds
            public uint SoundID;
        }

        public class WWiseBankInfo : ComboType {
            public List<WWiseBankEvent> Events;
            public WWiseBankInfo(ulong guid) : base(guid) { }
        }
        
        public class EffectInfoCombo : ComboNameable {
            // wrap
            public EffectParser.EffectInfo Effect;

            public EffectInfoCombo(ulong guid) : base(guid) { }
        }

        public class EntityInfoNew : ComboNameable {
            public ulong Model;
            public ulong RootEffect; // todo: STUEffectComponent defined instead of model is like a model in behaviour?
            public ulong VoiceSet;
            public HashSet<ulong> Animations;
            public HashSet<ulong> Effects;
            public HashSet<ulong> AnimationEffects;

            public List<ChildEntityReferenceNew> Children;

            public EntityInfoNew(ulong guid) : base(guid) {
                Animations = new HashSet<ulong>();
                Effects = new HashSet<ulong>();
                AnimationEffects = new HashSet<ulong>();
            }
        }

        public class ChildEntityReferenceNew {
            public ulong Hardpoint;
            public ulong Variable;
            public ulong GUID;

            public ChildEntityReferenceNew(STUChildEntityDefinition childEntityDefinition, Dictionary<ulong, ulong> replacements) {
                GUID = GetReplacement((ulong)childEntityDefinition.m_child, replacements);
                Hardpoint = childEntityDefinition.m_hardPoint;
                Variable = childEntityDefinition.m_49F782CE;
            }
        }

        public class ModelLookInfo : ComboNameable {
            public HashSet<ulong> Materials;  // id, guid
            public ModelLookInfo(ulong guid) : base(guid) { }
        }

        public class MaterialInfo : ComboType {
            public ulong MaterialData;
            public ulong ShaderSource;
            
            // shader info;
            // main shader = 44, used to be A5
            // golden = 50
            
            public HashSet<ulong> MaterialIDs;
            
            public MaterialInfo(ulong guid) : base(guid) { }
        }

        public class MaterialDataInfo : ComboType {
            public Dictionary<ulong, uint> Textures;

            public MaterialDataInfo(ulong guid) : base(guid) { }
        }

        public class TextureInfoNew : ComboNameable {
            public bool Loose;
            public TextureInfoNew(ulong guid) : base(guid) { }
        }

        public class ModelInfoNew : ComboType {
            public ulong Skeleton;
            public HashSet<ulong> Animations;
            public HashSet<ulong> ModelLooks;
            public HashSet<IEnumerable<ulong>> ModelLookSets;
            public HashSet<ulong> LooseMaterials;

            public ModelInfoNew(ulong guid) : base(guid) {
                Animations = new HashSet<ulong>();
                ModelLooks = new HashSet<ulong>();
                ModelLookSets = new HashSet<IEnumerable<ulong>>();
                LooseMaterials = new HashSet<ulong>();
            }
        }

        public class AnimationInfoNew : ComboNameable {
            public float FPS;
            public uint Priority;
            public ulong Effect;

            public AnimationInfoNew(ulong guid) : base(guid) { }
        }

        public class ComboContext {
            // Models + Effects + Entities
            public ulong Model;
            public ulong ModelLook;
            public ulong Effect;
            public ulong Entity;

            // Animation Effects
            public ulong Animation;
            
            // Model Looks
            public ulong Material;
            public ulong MaterialID;
            public ulong MaterialData;
            
            // Child entities
            public ulong ChildEntityIdentifier;

            public ComboContext Clone() {
                return new ComboContext {
                    Model = Model, ModelLook = ModelLook, Entity = Entity, Effect = Effect, 
                    Animation = Animation, Material = Material, MaterialID = MaterialID, MaterialData = MaterialData
                };
            }
        }
        
        public static ulong GetReplacement(ulong guid, Dictionary<ulong, ulong> replacements) {
            if (replacements == null) return guid;
            if (replacements.ContainsKey(guid)) return replacements[guid];
            return guid;
        }
        
        public static bool RemoveDuplicateVoiceSetEntries(ComboInfo @base, ref ComboInfo target, ulong voiceSet, ulong targetVoiceSet) {
            if (!@base.VoiceSets.ContainsKey(voiceSet) || !target.VoiceSets.ContainsKey(targetVoiceSet)) {
                return false;
            }

            HashSet<ulong> keys = new HashSet<ulong>();
            foreach (KeyValuePair<ulong, HashSet<VoiceLineInstanceInfo>> pair in @base.VoiceSets[voiceSet].VoiceLineInstances) {
                foreach (VoiceLineInstanceInfo voice in pair.Value) {
                    foreach (ulong guid in voice.SoundFiles) {
                        keys.Add(guid);
                    }
                }
            }

            bool hasData = false;

            // we have to call toarray here to "freeze" the GC stack and allow us to modify the "original" without C# bitching.
            foreach (KeyValuePair<ulong, HashSet<VoiceLineInstanceInfo>> pair in target.VoiceSets[targetVoiceSet].VoiceLineInstances.ToArray()) {
                HashSet<VoiceLineInstanceInfo> newSet = new HashSet<VoiceLineInstanceInfo>();
                foreach (VoiceLineInstanceInfo voice in pair.Value) {
                    foreach (ulong guid in voice.SoundFiles.ToArray()) {  // and here
                        if (!keys.Add(guid)) {
                            voice.SoundFiles.Remove(guid);
                        }
                    }
                    if (voice.SoundFiles.Count > 0) {
                        newSet.Add(voice);
                        hasData = true;
                    }
                }
                target.VoiceSets[targetVoiceSet].VoiceLineInstances[pair.Key] = newSet;
            }

            return hasData;
        }
        
        public static ComboInfo Find(ComboInfo info, ulong guid, Dictionary<ulong, ulong> replacements=null , ComboContext context=null) {
            if (info == null) info = new ComboInfo();
            if (context == null) context = new ComboContext();
            // it's time to redesign our FindLogic architecture
            // changes:
            //     All findlogics in one.
            //     ComboContext tells the function what to do.
            
            // this allows for:
            //     Entities that do not have a model
            //     Model effects
            
            if (guid == 0) return info;
            guid = GetReplacement(guid, replacements);

            if (info.Config.FullLog) {
                Console.Out.WriteLine($"[DataTool.FindLogic.Combo]: Searching in {GetFileName(guid)}");
            }
            
            // Debugger break area:
            // if (GetFileName(guid) == "000000000F6D.00C") Debugger.Break();  // TIME VORTEX MANIPULATOR / TARDIS
            // in 216172782113785973 / 000000000875.00D
            // in 216172782113784100 / 000000000124.00D
            // if (GetFileName(guid) == "00000000302E.00C") Debugger.Break();  // albino TARDIS (NO COLOUR)
            // in 216172782113785973 / 000000000875.00D
            // in 216172782113784100 / 000000000124.00D
            
            // 000000000124.00D - Playable ent, hardpoint = x11
            // 000000000875.00D - Main ent, hardpoint = null
            
            // if (GetFileName(guid) == "0000000050F2.00C") Debugger.Break();  // renhardt OWL
            // if (GetFileName(guid) == "000000005100.00C") Debugger.Break();  // zen OWL
            
            // if (GetFileName(guid) == "000000000AA9.008") Debugger.Break();
            
            // 508906757892874256 / 000000002010.08F = ANCR_badass_POTG effect
            // 288230376151718579 / 000000001AB3.003 = shield entity
            
            //if (GetFileName(guid) == "0000000014EF.003") Debugger.Break();  // ilios windmill
            //if (GetFileName(guid) == "0000000014F4.003") Debugger.Break();  // ilios bigboat
            //if (GetFileName(guid) == "000000001AF7.003") Debugger.Break();  // black forest (winter) windmill
            //if (GetFileName(guid) == "000000001A2E.003") Debugger.Break();  // black forest (winter) spawndoor
            //if (GetFileName(guid) == "000000001B4E.003") Debugger.Break();  // black forest (winter) middle cog
            //if (GetFileName(guid) == "000000001BDB.003") Debugger.Break();  // black forest (winter) capture point
            
            ushort guidType = teResourceGUID.Type(guid);
            if (guidType == 0 || guidType == 1) return info;
            switch (guidType) {
                case 0x2:
                    if (info.Maps.ContainsKey(guid)) break;

                    MapInfoNew mapInfo = new MapInfoNew(guid);
                    info.Maps[guid] = mapInfo;

                    /*// <read the actual 002>
                    // todo
                    // </read the actual 002>

                    using (Stream mapBStream = OpenFile(GetMapDataKey(guid, 0xB))) {
                        Map mapBData = new Map(mapBStream, BuildVersion, true);

                        //int stuCount = mapBData.Records.Sum(record => ((MapEntity) record).STUBindings.Length);
                        //
                        //Debug.Assert(stuCount == mapBData.STUs.Count);

                        foreach (ISTU mapBstu in mapBData.STUs) {
                            Dictionary<ulong, ulong> thisReplacements = new Dictionary<ulong, ulong>();
                            //foreach (Common.STUInstance stuInstance in mapBstu.Instances) {
                            //    if (stuInstance.GetType() == typeof(STU_83DEC8C7)) {
                            //        
                            //    }
                            //}
                            // STUStatescriptComponentInstanceData componentInstanceData = stu.Instances.OfType<STUStatescriptComponentInstanceData>().FirstOrDefault();
                            // if (componentInstanceData == null) continue;
                            // foreach (STUStatescriptGraphWithOverrides graph in componentInstanceData.m_6D10093E) {
                            //     ComboContext graphContext = new ComboContext(); // wipe?
                            //     foreach (STUStatescriptSchemaEntry schemaEntry in graph.m_1EB5A024) {
                            //         if (schemaEntry.Value == null) continue;
                            //         if (schemaEntry.Value is STU_9EA8D0BA schemaX9Ea) {
                            //             Find(info, schemaX9Ea.m_4C167404, thisReplacements, graphContext);
                            //         } else if (schemaEntry.Value is STUStatescriptDataStoreComponent2 schemaEntity2) {
                            //             Find(info, schemaEntity2.Entity, thisReplacements);  // wipe context again
                            //             graphContext.Entity = GetReplacement(schemaEntity2.Entity, thisReplacements);
                            //             graphContext.Model = info.Entities[graphContext.Entity].Model;
                            //             graphContext.Effect = info.Entities[graphContext.Entity].Effect;
                            //         } else if (schemaEntry.Value is STU_12B8954B schemaX12B) {
                            //             Find(info, schemaX12B.Animation, thisReplacements, graphContext);
                            //         }
                            //     }
                            // }
                        }
                    }*/

                    break;
                case 0x3:
                    if (info.Config == null || info.Config.DoExistingEntities == false) {
                        if (info.Entities.ContainsKey(guid)) break;
                    }

                    STUEntityDefinition entityDefinition = GetInstance<STUEntityDefinition>(guid);
                    if (entityDefinition == null) break;

                    EntityInfoNew entityInfo;
                    info.Entities.TryGetValue(guid, out entityInfo);
                    if (entityInfo == null) {
                        entityInfo = new EntityInfoNew(guid);
                        info.Entities[guid] = entityInfo;
                    }
                    
                    ComboContext entityContext = context.Clone();
                    entityContext.Entity = guid;

                    if (context.ChildEntityIdentifier != 0) {
                        if (!info.EntitiesByIdentifier.ContainsKey(context.ChildEntityIdentifier)) {
                            info.EntitiesByIdentifier[context.ChildEntityIdentifier] = new HashSet<ulong>();
                        }

                        info.EntitiesByIdentifier[context.ChildEntityIdentifier].Add(guid);
                    }

                    if (entityDefinition.m_childEntityData != null) {
                        entityInfo.Children = new List<ChildEntityReferenceNew>();
                        foreach (STUChildEntityDefinition childEntityDefinition in entityDefinition.m_childEntityData) {
                            if (childEntityDefinition == null) continue;
                            ComboContext childContext = new ComboContext {
                                ChildEntityIdentifier = childEntityDefinition.m_49F782CE
                            };
                            Find(info, (ulong)childEntityDefinition.m_child, replacements, childContext);
                            if (info.Entities.ContainsKey(GetReplacement((ulong)childEntityDefinition.m_child, replacements))) {
                                // sometimes the entity can't be loaded
                                entityInfo.Children.Add(new ChildEntityReferenceNew(childEntityDefinition, replacements));
                            }
                        }
                    }

                    if (entityDefinition.m_componentMap != null) {
                        STUEntityComponent[] components = entityDefinition.m_componentMap.Values
                            .OrderBy(x => x?.GetType() != typeof(STUModelComponent) &&
                                          x?.GetType() != typeof(STUEffectComponent)).ToArray();
                        // STUModelComponent first because we need model for context
                        // STUEffectComponent second(ish) because we need effect for context
                        foreach (STUEntityComponent component in components) {
                            if (component == null) continue;
                            if (component is STUModelComponent modelComponent) {
                                entityContext.Model = GetReplacement(modelComponent.m_model, replacements);

                                Find(info, modelComponent.m_model, replacements, entityContext);
                                Find(info, modelComponent.m_look, replacements, entityContext);
                                Find(info, modelComponent.m_animBlendTreeSet, replacements, entityContext);
                                Find(info, modelComponent.m_36F54327, replacements, entityContext);
                            } else if (component is STUEffectComponent effectComponent) {
                                entityContext.Effect = GetReplacement(effectComponent.m_effect, replacements);
                                Find(info, effectComponent.m_effect, replacements, entityContext);
                            } else if (component is STUStatescriptComponent statescriptComponent) {
                                if (statescriptComponent.m_B634821A != null) {
                                    foreach (STUStatescriptGraphWithOverrides graphWithOverrides in statescriptComponent.m_B634821A) {
                                        Find(info, graphWithOverrides, replacements, entityContext);
                                    }
                                }
                            } else if (component is STUWeaponComponent weaponComponent) {
                                Find(info, weaponComponent.m_managerScript, replacements, entityContext);
                                if (weaponComponent.m_weapons != null) {
                                    foreach (STUWeaponDefinition weaponDefinition in weaponComponent.m_weapons) {
                                        Find(info, weaponDefinition.m_script, replacements, entityContext);
                                        Find(info, weaponDefinition.m_graph, replacements, entityContext);
                                    }
                                }
                            } else if (component is STUVoiceSetComponent voiceSetComponent) {
                                entityInfo.VoiceSet = GetReplacement(voiceSetComponent.m_voiceDefinition, replacements);
                                Find(info, voiceSetComponent.m_voiceDefinition, replacements, entityContext);
                            } else if (component is STUFirstPersonComponent firstPersonComponent) {
                                Find(info, firstPersonComponent.m_entity, replacements, entityContext);
                            } else if (component is STUHealthComponent healthComponent) {
                                Find(info, healthComponent.m_63FBB2D3, replacements, entityContext);
                            } else if (component is STULocalIdleAnimComponent localIdleAnimComponent) {
                                Find(info, localIdleAnimComponent.m_idleAnimation, replacements, entityContext);
                            } else if (component is STUMirroredIdleAnimComponent mirroredIdleAnimComponent) {
                                Find(info, mirroredIdleAnimComponent.m_idleAnimation, replacements, entityContext);
                            } else if (component is STU_05DE82F2 unkComponent1) {
                                Find(info, unkComponent1.m_4A83FA61, replacements, entityContext);
                            } else if (component is STU_3CFA8C4A unkComponent2) {
                                if (unkComponent2.m_entries == null) continue;
                                foreach (STU_FB16F341 unkComponent2Entry in unkComponent2.m_entries) {
                                    Find(info, unkComponent2Entry.m_animation, replacements, entityContext);
                                }
                            }
                        }
                    }

                    // assign voice master to effects
                    if (entityInfo.VoiceSet != 0) {
                        foreach (ulong entityAnimation in entityInfo.Animations) {
                            AnimationInfoNew entityAnimationInfo = info.Animations[entityAnimation];
                            if (entityAnimationInfo.Effect == 0) continue;

                            info.SetEffectVoiceSet(entityAnimationInfo.Effect, entityInfo.VoiceSet);
                        }
                    }

                    entityInfo.Model = entityContext.Model;
                    entityInfo.RootEffect = entityContext.Effect;

                    break;
                case 0x4:
                case 0xF1:
                    if (info.Textures.ContainsKey(guid)) break;
                    TextureInfoNew textureInfo = new TextureInfoNew(guid);
                    info.Textures[guid] = textureInfo;

                    if (context.Material == 0) {
                        textureInfo.Loose = true;
                    }

                    break;
                case 0x6:
                    if (guid == 720575940379302199) {
                        
                    }                   
                    
                    if (info.Animations.ContainsKey(guid)) {
                        if (context.Model != 0) {
                            info.Models[context.Model].Animations.Add(guid);
                        }

                        if (context.Entity != 0) {
                            info.Entities[context.Entity].Animations.Add(guid);
                        }

                        break;
                    }

                    AnimationInfoNew animationInfo = new AnimationInfoNew(guid);

                    ComboContext animationContext = context.Clone();
                    animationContext.Animation = guid;

                    using (Stream animationStream = OpenFile(guid)) {
                        if (animationStream == null) break;
                        using (BinaryReader animationReader = new BinaryReader(animationStream)) {
                            animationStream.Position = 0;
                            uint priority = animationReader.ReadUInt32();
                            animationStream.Position = 8;
                            float fps = animationReader.ReadSingle();
                            animationStream.Position = 0x18L;
                            ulong effectKey = animationReader.ReadUInt64();
                            animationInfo.FPS = fps;
                            animationInfo.Priority = priority;
                            animationInfo.Effect = GetReplacement(effectKey, replacements);
                            Find(info, effectKey, replacements, animationContext);
                        }
                    }

                    if (context.Model != 0) {
                        info.Models[context.Model].Animations.Add(guid);
                    }

                    if (context.Entity != 0) {
                        info.Entities[context.Entity].Animations.Add(guid);
                    }

                    info.Animations[guid] = animationInfo;
                    break;
                case 0x8:
                    if (info.Materials.ContainsKey(guid) &&
                        (info.Materials[guid].MaterialIDs.Contains(context.MaterialID) || context.MaterialID == 0)) break;
                    // ^ break if material exists and has id, or id is 0
                    teMaterial material = new teMaterial(OpenFile(guid));

                    MaterialInfo materialInfo;
                    if (!info.Materials.ContainsKey(guid)) {
                        materialInfo = new MaterialInfo(guid) {
                            MaterialData = GetReplacement(material.Header.MaterialData, replacements),
                            MaterialIDs = new HashSet<ulong>()
                        };
                        info.Materials[guid] = materialInfo;
                    } else {
                        materialInfo = info.Materials[guid];
                    }

                    materialInfo.MaterialIDs.Add(context.MaterialID);
                    materialInfo.ShaderSource = GetReplacement(material.Header.ShaderSource, replacements);

                    if (context.ModelLook == 0 && context.Model != 0) {
                        info.Models[context.Model].LooseMaterials.Add(guid);
                    }

                    ComboContext materialContext = context.Clone();
                    materialContext.Material = guid;
                    Find(info, material.Header.MaterialData, replacements, materialContext);
                    break;
                case 0xC:
                    if (info.Models.ContainsKey(guid)) break;
                    ModelInfoNew modelInfo = new ModelInfoNew(guid);
                    info.Models[guid] = modelInfo;
                    break;
                case 0xD:
                case 0x8F: // sorry for breaking order
                case 0x8E:
                    if (info.Effects.ContainsKey(guid)) break;
                    if (info.AnimationEffects.ContainsKey(guid)) break;

                    EffectParser.EffectInfo effectInfo = new EffectParser.EffectInfo {
                        GUID = guid
                    };
                    effectInfo.SetupEffect();


                    if (guidType == 0xD || guidType == 0x8E) {
                        info.Effects[guid] = new EffectInfoCombo(guid) {Effect = effectInfo};
                        if (context.Entity != 0) {
                            info.Entities[context.Entity].Effects.Add(guid);
                        }
                    } else if (guidType == 0x8F) {
                        info.AnimationEffects[guid] = new EffectInfoCombo(guid) {Effect = effectInfo};
                        if (context.Entity != 0) {
                            info.Entities[context.Entity].AnimationEffects.Add(guid);
                        }
                    }

                    using (Stream effectStream = OpenFile(guid)) {
                        teChunkedData chunkedData = new teChunkedData(effectStream);
                        EffectParser parser = new EffectParser(chunkedData, guid);

                        ulong lastParticleModel = 0;

                        foreach (KeyValuePair<EffectParser.ChunkPlaybackInfo, IChunk> chunk in parser.GetChunks()) {
                            parser.Process(effectInfo, chunk, replacements);

                            if (chunk.Value is teEffectComponentModel model) {
                                ComboContext dmceContext = new ComboContext {
                                    Model = GetReplacement(model.Header.Model, replacements)
                                };
                                Find(info, model.Header.Model, replacements, dmceContext);
                                Find(info, model.Header.ModelLook, replacements, dmceContext);
                                Find(info, model.Header.Animation, replacements, dmceContext);
                            } else if (chunk.Value is teEffectComponentEffect effect) {
                                Find(info, effect.Header.Effect, replacements); // clean context
                            } else if (chunk.Value is teEffectComponentEntity entity) {
                                ComboContext neceContext =
                                    new ComboContext {ChildEntityIdentifier = entity.Header.Identifier};
                                Find(info, entity.Header.Entity, replacements, neceContext);
                            } else if (chunk.Value is teEffectComponentEntityControl entityControl) {
                                if (entityControl.Header.Animation == 0) continue;
                                Find(info, entityControl.Header.Animation, replacements);
                                if (!info.EntitiesByIdentifier.ContainsKey(entityControl.Header.Identifier)) continue;
                                foreach (ulong ceceEntity in info.EntitiesByIdentifier[entityControl.Header.Identifier]) {
                                    EntityInfoNew ceceEntityInfo = info.Entities[ceceEntity];
                                    ceceEntityInfo.Animations.Add(GetReplacement(entityControl.Header.Animation, replacements));
                                    if (ceceEntityInfo.Model != 0) {
                                        info.Models[ceceEntityInfo.Model].Animations.Add(GetReplacement(entityControl.Header.Animation, replacements));
                                    }
                                }
                            } else if (chunk.Value is teEffectComponentSound soundComponent) {
                                Find(info, soundComponent.Header.Sound, replacements);
                            } else if (chunk.Value is teEffectChunkShaderSetup shaders) {
                                ComboContext ssceContext = new ComboContext {Model = lastParticleModel};
                                Find(info, shaders.Header.Material, replacements, ssceContext);
                                Find(info, shaders.Header.MaterialData, replacements, ssceContext);
                            }
                            
                            if (chunk.Value is teEffectComponentParticle particle) {
                                Find(info, particle.Header.Model, replacements);
                                lastParticleModel = GetReplacement(particle.Header.Model, replacements);
                            } else {
                                lastParticleModel = 0;
                            }
                        }
                    }

                    break;

                case 0x1A:
                    if (info.ModelLooks.ContainsKey(guid)) {
                        if (context.Model != 0) {
                            info.Models[context.Model].ModelLooks.Add(guid);
                        }

                        break;
                    }

                    STUModelLook modelLook = GetInstance<STUModelLook>(guid);
                    if (modelLook == null) break;

                    ModelLookInfo modelLookInfo = new ModelLookInfo(guid);
                    info.ModelLooks[guid] = modelLookInfo;

                    ComboContext modelLookContext = context.Clone();
                    modelLookContext.ModelLook = guid;
                    
                    if (modelLook.m_materials != null) {
                        modelLookInfo.Materials = new HashSet<ulong>();
                        foreach (STUModelMaterial modelLookMaterial in modelLook.m_materials) {
                            FindModelMaterial(info, modelLookMaterial, modelLookInfo, modelLookContext, replacements);
                        }
                    }

                    //if (modelLook.m_materialEffects != null) {
                    //    if (modelLookInfo.Materials == null) modelLookInfo.Materials = new HashSet<ulong>();
                    //    foreach (STUMaterialEffect materialEffect in modelLook.m_materialEffects) {
                    //        foreach (STUModelMaterial materialEffectMaterial in materialEffect.m_materials) {
                    //            FindModelMaterial(info, materialEffectMaterial, modelLookInfo, modelLookContext, replacements);
                    //        }
                    //    }
                    //}
                    
                    if (context.Model != 0) {
                        info.Models[context.Model].ModelLooks.Add(guid);
                    }

                    break;
                case 0x1B:
                    STUConfigVar[] configVars = GetInstances<STUConfigVar>(guid);
                    if (configVars == null) break;

                    foreach (STUConfigVar configVar in configVars) {
                        Find(info, configVar, replacements, context);
                    }
                    //STUStatescriptGraph graph = GetInstanceNew<STUStatescriptGraph>(guid);
                    //if (graph == null) break;
                    //foreach (STUStatescriptBase node in graph.m_nodes) {
                    //    if (node is STUStatescriptStateCosmeticEntity cosmeticEntity) {
                    //        Find(info, cosmeticEntity.m_entityDef, replacements, context);
                    //    } else if (node is STUStatescriptStateWeaponVolley weaponVolley) {
                    //        if (weaponVolley.m_projectileEntity != null) {
                    //            Find(info, weaponVolley.m_projectileEntity.m_entityDef, replacements, context);
                    //        }
                    //    } else if (node is STUStatescriptStatePet statePet) {
                    //        Find(info, statePet.m_entityDefinition, replacements, context);
                    //    } else if (node is STUStatescriptActionEffect actionEffect) {
                    //        Find(info, actionEffect.m_effect, replacements, context);
                    //    } else if (node is STUStatescriptActionCreateEntity actionEntity) {
                    //        Find(info, actionEntity.m_entityDef, replacements, context);
                    //    } else if (node is STUStatescriptActionPlayScript actionPlayScript) {
                    //        Find(info, actionPlayScript.m_script, replacements, context);
                    //    } 
                    //}
                    break;
                case 0x20:
                    STUAnimBlendTree blendTree = GetInstance<STUAnimBlendTree>(guid);
                    foreach (STUAnimNode_Base animNode in blendTree.m_animNodes) {
                        if (animNode is STUAnimNode_Animation animNodeAnimation) {
                            Find(info, animNodeAnimation?.m_animation?.m_value, replacements, context);
                        } else if (animNode is STUAnimNode_AnimationPose2d animNodePose2D) {
                            Find(info, animNodePose2D?.m_animation?.m_value, replacements, context);
                        }
                    }
                    break;
                case 0x21:
                    STUAnimBlendTreeSet blendTreeSet = GetInstance<STUAnimBlendTreeSet>(guid);
                    if (blendTreeSet == null) break;
                    
                    foreach (ulong externalRef in blendTreeSet.m_externalRefs) {
                        Find(info, externalRef, replacements, context);
                    }

                    foreach (STUAnimBlendTreeSet_BlendTreeItem blendTreeItem in blendTreeSet.m_blendTreeItems) {
                        Find(info, blendTreeItem.m_C0214513, replacements, context);

                        if (blendTreeItem.m_gameData is STU_7D00A73D animGameDataUnk1) {
                            if (animGameDataUnk1.m_animDatas != null) {
                                foreach (STUAnimGameData_AnimationData gameDataAnimationData in animGameDataUnk1.m_animDatas) {
                                    Find(info, gameDataAnimationData.m_9FCB2C8A, replacements, context);
                                }
                            }
                        }
                        
                        if (blendTreeItem?.m_onFinished?.m_slotAnims != null) {
                            foreach (STUAnimBlendTree_SlotAnimation blendTreeSlotAnimation in blendTreeItem.m_onFinished.m_slotAnims) {
                                Find(info, blendTreeSlotAnimation?.m_animation, replacements, context);
                            }
                        }
                    }
                    break;
                case 0x2C:
                    if (info.Sounds.ContainsKey(guid)) break;

                    STUSound sound = GetInstance<STUSound>(guid);
                    if (sound == null) break;
                    
                    SoundInfoNew soundInfo = new SoundInfoNew(guid);
                    info.Sounds[guid] = soundInfo;

                    if (sound.m_C32C2195 != null) {
                        if (sound.m_C32C2195.m_soundWEMFiles != null) {
                            soundInfo.SoundFiles = new Dictionary<uint, ulong>();

                            int i = 0;
                            foreach (teStructuredDataAssetRef<STU_FBCC5EB2> soundWemFile in sound.m_C32C2195.m_soundWEMFiles) {
                                Find(info, soundWemFile, replacements, context);
                                
                                soundInfo.SoundFiles[sound.m_C32C2195.m_wwiseWEMFileIDs[i]] = GetReplacement(soundWemFile, replacements);
                                i++;
                            }
                        }
                        if (sound.m_C32C2195.m_soundWEMStreams != null) {
                            soundInfo.SoundStreams = new Dictionary<uint, ulong>();
                            
                            int i = 0;
                            foreach (teStructuredDataAssetRef<STU_FBCC5EB2> soundWemStream in sound.m_C32C2195.m_soundWEMStreams) {
                                Find(info, soundWemStream, replacements, context);
                                
                                soundInfo.SoundStreams[sound.m_C32C2195.m_wwiseWEMStreamIDs[i]] = GetReplacement(soundWemStream, replacements);
                                i++;
                            }
                        }

                        if (sound.m_C32C2195.m_09D4067B != null) {
                            foreach (teStructuredDataAssetRef<STU_C77C3128> soundUnk1 in sound.m_C32C2195.m_09D4067B) {
                                Find(info, soundUnk1, replacements, context);
                            }
                        }
                        if (sound.m_C32C2195.m_4587972B != null) {
                            foreach (teStructuredDataAssetRef<STU_221B83D5> soundUnk2 in sound.m_C32C2195.m_4587972B) {
                                Find(info, soundUnk2, replacements, context);
                            }
                        }
                        Find(info, sound.m_C32C2195.m_soundBank);
                    }
                    break;
                case 0x3F:
                    if (info.SoundFiles.ContainsKey(guid)) break;
                    SoundFileInfo soundFileInfo = new SoundFileInfo(guid);
                    info.SoundFiles[guid] = soundFileInfo;
                    break;
                case 0x43:
                    // todo: no point parsing this right now, not used
#if ALL_FEATURES_WOULD_CEASE_TO_EXIST
                    if (info.SoundBanks.ContainsKey(guid)) break;
                    
                    WWiseBankInfo bankInfo = new WWiseBankInfo(guid);
                    info.SoundBanks[guid] = bankInfo;
                    
                    bankInfo.Events = new List<WWiseBankEvent>();
                    using (Stream bankStream = OpenFile(guid)) {
                        ConvertLogic.Sound.WwiseBank bank = new ConvertLogic.Sound.WwiseBank(bankStream);
                        foreach (ConvertLogic.Sound.BankObjectEvent bankEvent in bank.ObjectsOfType<ConvertLogic.Sound.BankObjectEvent>()) {
                            foreach (uint eventAction in bankEvent.Actions) {
                                ConvertLogic.Sound.BankObjectEventAction action = bank.Objects[eventAction] as ConvertLogic.Sound.BankObjectEventAction;
                                if (action == null) continue;
                                WWiseBankEvent @event = new WWiseBankEvent {Type = action.Type};
                                bankInfo.Events.Add(@event);
                                if (action.ReferenceObjectID == 0) continue;
                                if (action.Scope != ConvertLogic.Sound.BankObjectEventAction.EventActionScope.GameObjectReference) continue;
                                foreach (KeyValuePair<ConvertLogic.Sound.BankObjectEventAction.EventActionParameterType,object> actionParameter in action.Parameters) {
                                    if (actionParameter.Key == ConvertLogic.Sound.BankObjectEventAction.EventActionParameterType.Play) {
                                        @event.StartDelay = (uint) actionParameter.Value;
                                    }
                                }
                                if (!bank.Objects.ContainsKey(action.ReferenceObjectID)) continue;
                                ConvertLogic.Sound.IBankObject referencedObject = bank.Objects[action.ReferenceObjectID];
                                if (referencedObject is ConvertLogic.Sound.BankObjectSoundSFX sfxObject) {
                                    @event.SoundID = sfxObject.SoundID;
                                }
                            }
                        }
                    }
#endif
                    break;
                case 0x5F:
                    if (info.VoiceSets.ContainsKey(guid)) break;

                    STUVoiceSet voiceSet = GetInstance<STUVoiceSet>(guid);

                    //string firstName = IO.GetString(voiceSet.m_269FC4E9);
                    //string lastName = IO.GetString(voiceSet.m_C0835C08);

                    if (voiceSet == null) break;
                    
                    VoiceSetInfo voiceSetInfo = new VoiceSetInfo(guid);
                    info.VoiceSets[guid] = voiceSetInfo;

                    if (voiceSet.m_voiceLineInstances == null) break;
                    voiceSetInfo.VoiceLineInstances = new Dictionary<ulong, HashSet<VoiceLineInstanceInfo>>();
                    for (int i = 0; i < voiceSet.m_voiceLineInstances.Length; i++) {
                        STUVoiceLineInstance voiceLineInstance = voiceSet.m_voiceLineInstances[i];
                        if (voiceLineInstance == null) continue;

                        VoiceLineInstanceInfo voiceLineInstanceInfo =
                            new VoiceLineInstanceInfo {
                                GUIDx06F = voiceSet.m_voiceLineGuids[i],
                                GUIDx09B = voiceSet.m_D1ABBE04[i],
                                GUIDx03C = voiceLineInstance.m_effectHardpoint,
                                Subtitle = voiceLineInstance.m_43C90056
                            };
                        if (voiceLineInstance.m_voiceLineRuntime != null) {
                            voiceLineInstanceInfo.VoiceLineSet = voiceLineInstance.m_voiceLineRuntime.m_set;
                            voiceLineInstanceInfo.VoiceStimulus = voiceLineInstance.m_voiceLineRuntime.m_stimulus;
                            voiceLineInstanceInfo.ExternalSound = voiceLineInstance.m_voiceLineRuntime.m_externalSound;
                            voiceLineInstanceInfo.VoiceConversation = voiceLineInstance.m_voiceLineRuntime.m_voiceConversation;
                            Find(info, voiceLineInstanceInfo.ExternalSound, replacements, context);
                        } else {
                            Console.Out.WriteLine("[DataTool.FindLogic.Combo]: ERROR: voice data container was null (please contact the developers)");
                            if (Debugger.IsAttached) {
                                Debugger.Break();
                            }
                            break;
                        }
                        
                        voiceLineInstanceInfo.SoundFiles = new HashSet<ulong>();

                        if (voiceLineInstance.m_AF226247 != null) {
                            foreach (var soundFile in new[] {
                                voiceLineInstance.m_AF226247.m_1485B834, voiceLineInstance.m_AF226247.m_798027DE,
                                voiceLineInstance.m_AF226247.m_A84AA2B5, voiceLineInstance.m_AF226247.m_D872E45C
                            }) {
                                if (soundFile == null) continue;
                                voiceLineInstanceInfo.SoundFiles.Add(soundFile.m_3C099E86);
                                Find(info, soundFile.m_3C099E86, replacements, context);
                            }
                        }

                        if (!voiceSetInfo.VoiceLineInstances.ContainsKey(voiceLineInstanceInfo.VoiceStimulus)) {
                            voiceSetInfo.VoiceLineInstances[voiceLineInstanceInfo.VoiceStimulus] = new HashSet<VoiceLineInstanceInfo>();
                        }
                        voiceSetInfo.VoiceLineInstances[voiceLineInstanceInfo.VoiceStimulus].Add(voiceLineInstanceInfo);
                    }
                    
                    break;
                case 0x7C:
                    if (info.Strings.ContainsKey(guid)) break;
                    
                    StringInfo stringInfo = new StringInfo(guid) {
                        Value = GetString(guid)
                    };

                    info.Strings[guid] = stringInfo;
                    break;
                case 0xA5:
                    // hmm, if existing?
                    STUUnlock cosmetic = GetInstance<STUUnlock>(guid);

                    if (cosmetic is STUUnlock_Emote unlockEmote) {
                        Find(info, unlockEmote.m_emoteBlendTreeSet, replacements, context);
                    } else if (cosmetic is STUUnlock_Pose unlockPose) {
                        Find(info, unlockPose.m_pose, replacements, context);
                    } else if (cosmetic is STUUnlock_VoiceLine unlockVoiceLine) {
                        Find(info, unlockVoiceLine.m_F57B051E, replacements, context);
                        Find(info, unlockVoiceLine.m_1B25AB90?.m_effect, replacements, context);
                        Find(info, unlockVoiceLine.m_1B25AB90?.m_effectLook, replacements, context);
                    } else if (cosmetic is STUUnlock_SprayPaint unlockSpray) {
                        Find(info, unlockSpray.m_1B25AB90?.m_effect, replacements, context);
                        Find(info, unlockSpray.m_1B25AB90?.m_effectLook, replacements, context);
                        
                        Find(info, unlockSpray.m_ABFBD552?.m_effect, replacements, context);
                        Find(info, unlockSpray.m_ABFBD552?.m_effectLook, replacements, context);
                    } else if (cosmetic is STUUnlock_AvatarPortrait unlockIcon) {
                        Find(info, unlockIcon.m_1B25AB90?.m_effect, replacements, context);
                        Find(info, unlockIcon.m_1B25AB90?.m_effectLook, replacements, context);
                    } else if (cosmetic is STUUnlock_POTGAnimation unlockHighlightIntro) {
                        Find(info, unlockHighlightIntro.m_animation, replacements, context);
                    }

                    break;
                case 0xA6:
                    // why not
                    if (replacements == null) break;
                    STUSkinTheme skinOverride = GetInstance<STUSkinTheme>(guid);
                    if (skinOverride?.m_runtimeOverrides == null) break;
                    foreach (KeyValuePair<ulong, STUSkinRuntimeOverride> replacement in skinOverride.m_runtimeOverrides) {
                        if (replacements.ContainsKey(replacement.Key)) continue;
                        replacements[replacement.Key] = replacement.Value.m_3D884507;
                    }
                    // replacements one object that gets modified
                    break;
                case 0xA8:
                    // hmm, if existing?
                    STUEffectLook effectLook = GetInstance<STUEffectLook>(guid);
                    foreach (teStructuredDataAssetRef<ulong> effectLookMaterialData in effectLook.m_materialData) {
                        Find(info, effectLookMaterialData, replacements, context);
                    }
                    break;
                case 0xB2:
                    if (info.VoiceSoundFiles.ContainsKey(guid)) break;
                    SoundFileInfo voiceSoundFileInfo = new SoundFileInfo(guid);
                    info.VoiceSoundFiles[guid] = voiceSoundFileInfo;
                    break;
                case 0xB3:
                    if (info.MaterialDatas.ContainsKey(guid)) break;
                    ComboContext materialDataContext = context.Clone();
                    materialDataContext.MaterialData = guid;
                    
                    MaterialDataInfo materialDataInfo = new MaterialDataInfo(guid);

                    info.MaterialDatas[guid] = materialDataInfo;
                    
                    teMaterialData materialData = new teMaterialData(OpenFile(guid));
                    if (materialData.Textures != null) {
                        materialDataInfo.Textures = new Dictionary<ulong, uint>();
                        foreach (teMaterialData.Texture matDataTex in materialData.Textures) {
                            Find(info, matDataTex.TextureGUID, replacements, materialDataContext);
                            materialDataInfo.Textures[matDataTex.TextureGUID] = matDataTex.NameHash;
                        }
                    }
                    
                    break;
                case 0xBF:
                    STULineupPose lineupPose = GetInstance<STULineupPose>(guid);
                    if (lineupPose == null) break;
                    
                    Find(info, lineupPose.m_E599EB7C, replacements, context);

                    Find(info, lineupPose.m_0189332F?.m_11E0A658, replacements, context);
                    Find(info, lineupPose.m_BEF008DE?.m_11E0A658, replacements, context);
                    Find(info, lineupPose.m_DE70F501?.m_11E0A658, replacements, context);
                    break;
                default:
                    if (UnhandledTypes.Add(guidType)) {
                        Debugger.Log(0, "DataTool", $"[DataTool.FindLogic.Combo]: Unhandled type: {guidType:X3}\r\n");
                    }
                    break;
            }

            return info;
        }

        private static void Find(ComboInfo info, STUStatescriptGraphWithOverrides graphWithOverrides, Dictionary<ulong, ulong> replacements, ComboContext context) {
            if (graphWithOverrides == null) return;
            Find(info, graphWithOverrides.m_graph, replacements, context);
            if (graphWithOverrides.m_1EB5A024 != null) {
                foreach (STUStatescriptSchemaEntry schemaEntry in graphWithOverrides.m_1EB5A024) {
                    Find(info, schemaEntry.m_value, replacements, context);
                }
            }
        }

        private static void Find(ComboInfo info, STUConfigVar configVar, Dictionary<ulong, ulong> replacements, ComboContext context) {
            if (configVar == null) return;
            if (configVar is STU_8556841E configVarEntity) {
                Find(info, configVarEntity.m_entityDef, replacements, context);
            } else if (configVar is STUConfigVarEffect configVarEffect) {
                Find(info, configVarEffect.m_effect, replacements, context);
            } else if (configVar is STU_105E1BCC configVarScript) {
                // prolly STUConfigVarStatescriptGraph but there are two types that I can't tell from eachother
                Find(info, configVarScript.m_graph, replacements, context);
            } else if (configVar is STU_433DFB35 configVarScript2) {
                Find(info, configVarScript2.m_graph, replacements, context);
            } else if (configVar is STUConfigVarAnimation configVarAnim) {
                Find(info, configVarAnim.m_animation, replacements, context);
            } else if (configVar is STUConfigVarModelLook configVarModelLook) {
                Find(info, (ulong)configVarModelLook.m_modelLook, replacements, context);
            } else if (configVar is STUConfigVarExpression configVarExpression) {
                if (configVarExpression.m_configVars != null) {
                    foreach (STUConfigVar subVar in configVarExpression.m_configVars) {
                        Find(info, subVar, replacements, context);
                    }
                }
            } else if (configVar is STUConfigVarTexture configVarTexture) {
                Find(info, configVarTexture.m_texture, replacements, context);
            }
        }

        private static void FindModelMaterial(ComboInfo info, STUModelMaterial modelMaterial, ModelLookInfo modelLookInfo, ComboContext modelLookContext, Dictionary<ulong, ulong> replacements) {
            if (modelMaterial == null || modelMaterial.m_material == 0) return;
            modelLookInfo.Materials.Add(GetReplacement((ulong)modelMaterial.m_material, replacements));
            ComboContext modelMaterialContext = modelLookContext.Clone();
            modelMaterialContext.MaterialID = modelMaterial.m_DC05EA3B;
            Find(info, (ulong)modelMaterial.m_material, replacements, modelMaterialContext);
        }
    }
}