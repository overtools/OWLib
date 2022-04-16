using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DataTool.Helper;
using Newtonsoft.Json;
using TankLib;
using TankLib.Chunks;
using TankLib.STU;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.FindLogic {
    public static class Combo {
        private static readonly HashSet<ushort> s_unhandledTypes = new HashSet<ushort>();

        public class ComboInfo {
            // keep everything at top level, stops us from doing the same things again.
            // everything here is unsorted, but we can use GUIDs as references.
            public Dictionary<ulong, EntityAsset> m_entities;
            public Dictionary<ulong, HashSet<ulong>> m_entitiesByIdentifier;
            public Dictionary<ulong, ModelAsset> m_models;
            public Dictionary<ulong, MaterialAsset> m_materials;
            public Dictionary<ulong, MaterialDataAsset> m_materialData;
            public Dictionary<ulong, ModelLookAsset> m_modelLooks;
            public Dictionary<ulong, AnimationAsset> m_animations;
            public Dictionary<ulong, TextureAsset> m_textures;
            public Dictionary<ulong, EffectInfoCombo> m_effects;
            public Dictionary<ulong, EffectInfoCombo> m_animationEffects;
            public Dictionary<ulong, SoundInfoNew> m_sounds;
            public Dictionary<ulong, WWiseBankInfo> m_soundBanks;
            public Dictionary<ulong, SoundFileAsset> m_voiceSoundFiles;
            public Dictionary<ulong, SoundFileAsset> m_soundFiles;
            public Dictionary<ulong, VoiceSetAsset> m_voiceSets;
            public Dictionary<ulong, DisplayTextAsset> m_displayText;
            public Dictionary<ulong, SubtitleAsset> m_subtitles;
            public HashSet<ulong> m_doneScripts;

            public bool m_processExistingEntities = false;
            public bool m_fullLog = false;

            public ComboInfo() {
                m_entities = new Dictionary<ulong, EntityAsset>();
                m_entitiesByIdentifier = new Dictionary<ulong, HashSet<ulong>>();
                m_models = new Dictionary<ulong, ModelAsset>();
                m_materials = new Dictionary<ulong, MaterialAsset>();
                m_materialData = new Dictionary<ulong, MaterialDataAsset>();
                m_modelLooks = new Dictionary<ulong, ModelLookAsset>();
                m_animations = new Dictionary<ulong, AnimationAsset>();
                m_textures = new Dictionary<ulong, TextureAsset>();
                m_effects = new Dictionary<ulong, EffectInfoCombo>();
                m_animationEffects = new Dictionary<ulong, EffectInfoCombo>();
                m_sounds = new Dictionary<ulong, SoundInfoNew>();
                m_soundBanks = new Dictionary<ulong, WWiseBankInfo>();
                m_voiceSoundFiles = new Dictionary<ulong, SoundFileAsset>();
                m_soundFiles = new Dictionary<ulong, SoundFileAsset>();
                m_voiceSets = new Dictionary<ulong, VoiceSetAsset>();
                m_displayText = new Dictionary<ulong, DisplayTextAsset>();
                m_subtitles = new Dictionary<ulong, SubtitleAsset>();
                m_doneScripts = new HashSet<ulong>();
            }

            public void RemoveByKey(ulong key) {
                m_entities.Remove(key);
                m_entitiesByIdentifier.Remove(key);
                m_models.Remove(key);
                m_materials.Remove(key);
                m_materialData.Remove(key);
                m_modelLooks.Remove(key);
                m_animations.Remove(key);
                m_textures.Remove(key);
                m_effects.Remove(key);
                m_animationEffects.Remove(key);
                m_sounds.Remove(key);
                m_soundBanks.Remove(key);
                m_voiceSoundFiles.Remove(key);
                m_soundFiles.Remove(key);
                m_voiceSets.Remove(key);
                m_displayText.Remove(key);
                m_subtitles.Remove(key);
            }

            public void RemoveByKey(ulong[] keys) {
                foreach (var @ulong in keys) {
                    RemoveByKey(@ulong);
                }
            }

            private static void SetAssetName<T>(ulong guid, string name, Dictionary<ulong, T> map, Dictionary<ulong, ulong> replacements = null) where T : ComboAsset {
                if (replacements != null) guid = GetReplacement(guid, replacements);
                if (!map.TryGetValue(guid, out var asset)) return;
                asset.m_name = name.TrimEnd(' ');
            }

            public void SetEntityName(ulong entity, string name, Dictionary<ulong, ulong> replacements = null) => SetAssetName(entity, name, m_entities, replacements);

            public void SetTextureName(ulong texture, string name, Dictionary<ulong, ulong> replacements = null) => SetAssetName(texture, name, m_textures, replacements);

            public void SetTextureProcessIcon(ulong texture) {
                if (!m_textures.TryGetValue(texture, out var asset)) return;
                asset.m_processIcon = true;
            }

            public void SetTextureSplit(ulong texture) {
                if (!m_textures.TryGetValue(texture, out var asset)) return;
                asset.m_split = true;
            }

            /// <summary>
            /// Overrides the file type the texture is saved as
            /// </summary>
            /// <param name="texture"></param>
            /// <param name="fileType">tif, png, dds</param>
            public void SetTextureFileType(ulong texture, string fileType) {
                if (!m_textures.TryGetValue(texture, out var asset)) return;
                asset.m_fileType = fileType;
            }

            public void SetEffectName(ulong effect, string name, Dictionary<ulong, ulong> replacements = null) {
                SetAssetName(effect, name, m_effects, replacements);
                SetAssetName(effect, name, m_animationEffects, replacements);
            }

            public void SetModelLookName(ulong look, string name, Dictionary<ulong, ulong> replacements = null) => SetAssetName(look, name, m_modelLooks, replacements);

            public void SetEffectVoiceSet(ulong effect, ulong voiceSet) {
                if (m_animationEffects.ContainsKey(effect)) SetEffectVoiceSet(m_animationEffects[effect], voiceSet);
                if (m_effects.ContainsKey(effect)) SetEffectVoiceSet(m_effects[effect], voiceSet);
            }

            public static void SetEffectVoiceSet(EffectInfoCombo effect, ulong voiceSet) {
                effect.Effect.VoiceSet = voiceSet;
            }
        }

        public class ComboAsset {
            public ulong m_GUID;
            public string m_name;

            protected ComboAsset(ulong guid) {
                m_name = null;
                m_GUID = guid;
                m_name = GetNullableGUIDName(guid);
            }

            public string GetName() {
                return GetValidFilename(m_name, false) ?? GetFileName(m_GUID);
            }

            public string GetNameIndex() {
                return GetValidFilename(m_name, false) ?? $"{m_GUID & 0xFFFFFFFFFFFF:X12}";
            }
        }

        public class DisplayTextAsset : ComboAsset {
            public string m_text;

            public DisplayTextAsset(ulong guid, string text) : base(guid) {
                m_text = text;
            }
        }

        public class VoiceSetAsset : ComboAsset {
            public VoiceSetAsset(ulong guid) : base(guid) { }

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
            public ulong[] Conversations;
            public ulong Subtitle;
            public ulong SubtitleRuntime;
            public HashSet<ulong> SoundFiles;
        }

        public class SoundFileAsset : ComboAsset {
            public SoundFileAsset(ulong guid) : base(guid) { }
        }

        public class SoundInfoNew : ComboAsset {
            public Dictionary<uint, ulong> SoundFiles;
            public Dictionary<uint, ulong> SoundStreams;
            public ulong SoundBank;
            public SoundInfoNew(ulong guid) : base(guid) { }
        }

        public class WWiseBankEvent {
            public ConvertLogic.Sound.BankObjectEventAction.EventActionType Type;
            public uint StartDelay; // milliseconds
            public uint SoundID;
        }

        public class WWiseBankInfo : ComboAsset {
            public List<WWiseBankEvent> Events;
            public WWiseBankInfo(ulong guid) : base(guid) { }
        }

        public class EffectInfoCombo : ComboAsset {
            // wrap
            public EffectParser.EffectInfo Effect;

            public EffectInfoCombo(ulong guid) : base(guid) { }
        }

        public class EntityAsset : ComboAsset {
            public ulong m_modelGUID;
            public ulong m_effectGUID; // todo: STUEffectComponent defined instead of model is like a model in behaviour?
            public ulong m_voiceSet;

            public HashSet<ulong> m_animations;

            public HashSet<ulong> m_effects;
            //public HashSet<ulong> m_animationEffects;

            public List<ChildEntityReference> Children;

            public EntityAsset(ulong guid) : base(guid) {
                m_animations = new HashSet<ulong>();
                m_effects = new HashSet<ulong>();
                // m_animationEffects = new HashSet<ulong>();
            }
        }

        public class ChildEntityReference {
            public ulong m_hardpointGUID;
            public ulong m_identifier;
            public ulong m_defGUID;

            public ChildEntityReference(STUChildEntityDefinition childEntityDefinition, Dictionary<ulong, ulong> replacements) {
                m_defGUID = GetReplacement((ulong) childEntityDefinition.m_child, replacements);
                m_hardpointGUID = childEntityDefinition.m_hardPoint;
                m_identifier = childEntityDefinition.m_49F782CE;
            }
        }

        public class ModelMaterial {
            public readonly ulong m_guid;
            public readonly ulong m_key;

            public ModelMaterial(ulong guid, ulong key) {
                m_guid = guid;
                m_key = key;
            }
        }

        public class ModelLookAsset : ComboAsset {
            public List<ModelMaterial> m_materials = new List<ModelMaterial>(); // id, guid

            public ModelLookAsset(ulong guid) : base(guid) { }
        }

        public class MaterialAsset : ComboAsset {
            public ulong m_materialDataGUID;
            public ulong m_shaderSourceGUID;
            public ulong m_shaderGroupGUID;
            public List<(ulong instance, ulong code, byte[] shaderData)> m_shaders;

            // shader info;
            // main shader = 44, used to be A5
            // golden = 50

            public HashSet<ulong> m_materialIDs;

            public MaterialAsset(ulong guid) : base(guid) {
                m_materialIDs = new HashSet<ulong>();
                m_shaders = new List<(ulong instance, ulong code, byte[] shaderData)>();
            }
        }

        public class MaterialDataAsset : ComboAsset {
            public Dictionary<ulong, uint> m_textureMap;
            public Dictionary<uint, byte[]> m_staticInputMap;

            public MaterialDataAsset(ulong guid) : base(guid) { }
        }

        public class TextureAsset : ComboAsset {
            public bool m_loose;
            public bool? m_processIcon;
            public bool? m_split;
            public string m_fileType;

            public TextureAsset(ulong guid) : base(guid) { }
        }

        public class ModelAsset : ComboAsset {
            public ulong m_skeletonGUID;
            public HashSet<ulong> n_animations;

            public HashSet<ulong> m_modelLooks;

            //public HashSet<IEnumerable<ulong>> m_modelLookSets;
            public HashSet<ulong> m_looseMaterials;

            public ModelAsset(ulong guid) : base(guid) {
                n_animations = new HashSet<ulong>();
                m_modelLooks = new HashSet<ulong>();
                //m_modelLookSets = new HashSet<IEnumerable<ulong>>();
                m_looseMaterials = new HashSet<ulong>();
            }
        }

        public class SubtitleAsset : ComboAsset {
            public HashSet<string> m_text;

            public SubtitleAsset(ulong guid) : base(guid) {
                m_text = new HashSet<string>();
            }

            public void AddText(string text) {
                if (text == null) return;
                m_text.Add(text);
            }
        }

        public class AnimationAsset : ComboAsset {
            public float m_fps;
            public uint m_priority;
            public ulong m_effect;

            public AnimationAsset(ulong guid) : base(guid) { }
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
            if (!@base.m_voiceSets.ContainsKey(voiceSet) || !target.m_voiceSets.ContainsKey(targetVoiceSet)) {
                return false;
            }

            HashSet<ulong> keys = new HashSet<ulong>();
            foreach (KeyValuePair<ulong, HashSet<VoiceLineInstanceInfo>> pair in @base.m_voiceSets[voiceSet].VoiceLineInstances) {
                foreach (VoiceLineInstanceInfo voice in pair.Value) {
                    foreach (ulong guid in voice.SoundFiles) {
                        keys.Add(guid);
                    }
                }
            }

            bool hasData = false;

            // we have to call toarray here to "freeze" the GC stack and allow us to modify the "original" without C# bitching.
            foreach (KeyValuePair<ulong, HashSet<VoiceLineInstanceInfo>> pair in target.m_voiceSets[targetVoiceSet].VoiceLineInstances.ToArray()) {
                HashSet<VoiceLineInstanceInfo> newSet = new HashSet<VoiceLineInstanceInfo>();
                foreach (VoiceLineInstanceInfo voice in pair.Value) {
                    foreach (ulong guid in voice.SoundFiles.ToArray()) { // and here
                        if (!keys.Add(guid)) {
                            voice.SoundFiles.Remove(guid);
                        }
                    }

                    if (voice.SoundFiles.Count > 0) {
                        newSet.Add(voice);
                        hasData = true;
                    }
                }

                target.m_voiceSets[targetVoiceSet].VoiceLineInstances[pair.Key] = newSet;
            }

            return hasData;
        }

        public static ComboInfo Find(ComboInfo info, ulong guid, Dictionary<ulong, ulong> replacements = null, ComboContext context = null) {
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

            if (info.m_fullLog) {
                Logger.DebugLog("Combo", $"Searching in {GetFileName(guid)}");
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
                /*case 0x2: {
                     if (info.Maps.ContainsKey(guid)) break;

                     MapInfoNew mapInfo = new MapInfoNew(guid);
                     info.Maps[guid] = mapInfo;

                     // <read the actual 002>
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
                     }
                    break;
                }*/
                case 0x3: {
                    if (!info.m_processExistingEntities && info.m_entities.ContainsKey(guid)) break;

                    STUEntityDefinition entityDefinition = GetInstance<STUEntityDefinition>(guid);
                    if (entityDefinition == null) break;

                    EntityAsset entityInfo;
                    info.m_entities.TryGetValue(guid, out entityInfo);
                    if (entityInfo == null) {
                        entityInfo = new EntityAsset(guid);
                        info.m_entities[guid] = entityInfo;
                    }

                    ComboContext entityContext = context.Clone();
                    entityContext.Entity = guid;

                    if (context.ChildEntityIdentifier != 0) {
                        if (!info.m_entitiesByIdentifier.ContainsKey(context.ChildEntityIdentifier)) {
                            info.m_entitiesByIdentifier[context.ChildEntityIdentifier] = new HashSet<ulong>();
                        }

                        info.m_entitiesByIdentifier[context.ChildEntityIdentifier].Add(guid);
                    }

                    if (entityDefinition.m_childEntityData != null) {
                        entityInfo.Children = new List<ChildEntityReference>();
                        foreach (STUChildEntityDefinition childEntityDefinition in entityDefinition.m_childEntityData) {
                            if (childEntityDefinition == null) continue;
                            ComboContext childContext = new ComboContext {
                                ChildEntityIdentifier = childEntityDefinition.m_49F782CE
                            };
                            Find(info, (ulong) childEntityDefinition.m_child, replacements, childContext);
                            if (info.m_entities.ContainsKey(GetReplacement((ulong) childEntityDefinition.m_child, replacements))) {
                                // sometimes the entity can't be loaded
                                entityInfo.Children.Add(new ChildEntityReference(childEntityDefinition, replacements));
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
                                entityInfo.m_voiceSet = GetReplacement(voiceSetComponent.m_voiceDefinition, replacements);
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
                    if (entityInfo.m_voiceSet != 0) {
                        foreach (ulong entityAnimation in entityInfo.m_animations) {
                            AnimationAsset entityAnimationInfo = info.m_animations[entityAnimation];
                            if (entityAnimationInfo.m_effect == 0) continue;

                            info.SetEffectVoiceSet(entityAnimationInfo.m_effect, entityInfo.m_voiceSet);
                        }
                    }

                    entityInfo.m_modelGUID = entityContext.Model;
                    entityInfo.m_effectGUID = entityContext.Effect;

                    break;
                }
                case 0x4:
                case 0xF1: {
                    if (info.m_textures.ContainsKey(guid)) break;
                    TextureAsset textureInfo = new TextureAsset(guid);
                    info.m_textures[guid] = textureInfo;

                    if (context.Material == 0) {
                        textureInfo.m_loose = true;
                    }

                    break;
                }
                case 0x6: {
                    if (info.m_animations.ContainsKey(guid)) {
                        if (context.Model != 0) {
                            info.m_models[context.Model].n_animations.Add(guid);
                        }

                        if (context.Entity != 0) {
                            info.m_entities[context.Entity].m_animations.Add(guid);
                        }

                        break;
                    }

                    AnimationAsset animationInfo = new AnimationAsset(guid);

                    ComboContext animationContext = context.Clone();
                    animationContext.Animation = guid;

                    using (Stream animationStream = OpenFile(guid)) {
                        if (animationStream == null) break;
                        ulong effectGuid;
                        // This is ass.
                        using (BinaryReader animationReader = new BinaryReader(animationStream)) {
                            uint priority = animationReader.ReadUInt16();
                            animationStream.Position = 0x18;
                            float fps = animationReader.ReadSingle();
                            animationStream.Position = 0x20;
                            effectGuid = animationReader.ReadUInt64();
                            animationInfo.m_fps = fps;
                            animationInfo.m_priority = priority;
                            animationInfo.m_effect = GetReplacement(effectGuid, replacements);
                        }

                        Find(info, effectGuid, replacements, animationContext);
                    }

                    if (context.Model != 0) {
                        info.m_models[context.Model].n_animations.Add(guid);
                    }

                    if (context.Entity != 0) {
                        info.m_entities[context.Entity].m_animations.Add(guid);
                    }

                    info.m_animations[guid] = animationInfo;
                    break;
                }
                case 0x8:
                case 0x127: {
                    // if (info.m_materials.ContainsKey(guid) &&
                    //     (info.m_materials[guid].m_materialIDs.Contains(context.MaterialID) || context.MaterialID == 0)) break;
                    // // ^ break if material exists and has id, or id is 0

                    teMaterial material = null;
                    try {
                        material = new teMaterial(OpenFile(guid));
                    } catch {
                        break;
                    }

                    MaterialAsset materialInfo;
                    if (!info.m_materials.ContainsKey(guid)) {
                        materialInfo = new MaterialAsset(guid) {
                            m_materialDataGUID = GetReplacement(material.Header.MaterialData, replacements)
                        };
                        info.m_materials[guid] = materialInfo;
                    } else {
                        materialInfo = info.m_materials[guid];
                    }

                    materialInfo.m_materialIDs.Add(context.MaterialID);
                    materialInfo.m_shaderSourceGUID = GetReplacement(material.Header.ShaderSource, replacements);
                    materialInfo.m_shaderGroupGUID = GetReplacement(material.Header.ShaderGroup, replacements);
                    try {
                        if (Program.Flags.ExtractShaders) {
                            teShaderGroup shaderGroup = new teShaderGroup(OpenFile(materialInfo.m_shaderGroupGUID));
                            foreach (teResourceGUID shaderGuid in shaderGroup.Instances) {
                                ulong shaderInstanceGuid = GetReplacement(shaderGuid, replacements);
                                teShaderInstance shaderInstance = new teShaderInstance(OpenFile(shaderInstanceGuid));
                                ulong shaderCodeGuid = GetReplacement(shaderInstance.Header.ShaderCode, replacements);
                                teShaderCode shaderCode = new teShaderCode(OpenFile(shaderCodeGuid));
                                materialInfo.m_shaders.Add((shaderInstanceGuid, shaderCodeGuid, shaderCode.ByteCode));
                            }
                        }
                    } catch {
                        // lol xd
                    }

                    if (context.ModelLook == 0 && context.Model != 0) {
                        info.m_models[context.Model].m_looseMaterials.Add(guid);
                    }

                    ComboContext materialContext = context.Clone();
                    materialContext.Material = guid;
                    Find(info, material.Header.MaterialData, replacements, materialContext);
                    break;
                }
                case 0xC: {
                    if (info.m_models.ContainsKey(guid)) break;
                    ModelAsset modelInfo = new ModelAsset(guid);
                    info.m_models[guid] = modelInfo;
                    break;
                }
                case 0xD:
                case 0x8F: // sorry for breaking order
                case 0x8E: {
                    if (info.m_effects.ContainsKey(guid)) break;
                    if (info.m_animationEffects.ContainsKey(guid)) break;

                    EffectParser.EffectInfo effectInfo = new EffectParser.EffectInfo {
                        GUID = guid
                    };
                    effectInfo.SetupEffect();


                    if (guidType == 0xD || guidType == 0x8E) {
                        info.m_effects[guid] = new EffectInfoCombo(guid) {Effect = effectInfo};
                        if (context.Entity != 0) {
                            info.m_entities[context.Entity].m_effects.Add(guid);
                        }
                    } else if (guidType == 0x8F) {
                        info.m_animationEffects[guid] = new EffectInfoCombo(guid) {Effect = effectInfo};
                        //if (context.Entity != 0) {
                        //    info.Entities[context.Entity].m_animationEffects.Add(guid);
                        //}
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
                                if (!info.m_entitiesByIdentifier.ContainsKey(entityControl.Header.Identifier)) continue;
                                foreach (ulong ceceEntity in info.m_entitiesByIdentifier[entityControl.Header.Identifier]) {
                                    EntityAsset ceceEntityInfo = info.m_entities[ceceEntity];
                                    ceceEntityInfo.m_animations.Add(GetReplacement(entityControl.Header.Animation, replacements));
                                    if (ceceEntityInfo.m_modelGUID != 0) {
                                        info.m_models[ceceEntityInfo.m_modelGUID].n_animations.Add(GetReplacement(entityControl.Header.Animation, replacements));
                                    }
                                }
                            } else if (chunk.Value is teEffectComponentSound soundComponent) {
                                Find(info, soundComponent.Header.Sound, replacements);
                            } else if (chunk.Value is teEffectChunkShaderSetup shaders) {
                                if (lastParticleModel == 0) TankLib.Helpers.Logger.Debug("Combo", "ShaderSetup with no model. textures will get lost");

                                ComboContext ssceContext = new ComboContext {Model = lastParticleModel};
                                Find(info, shaders.Header.Material, replacements, ssceContext);
                                Find(info, shaders.Header.MaterialData, replacements, ssceContext);
                            }

                            if (chunk.Value is teEffectComponentParticle particle) {
                                Find(info, particle.Header.Model, replacements);
                                lastParticleModel = GetReplacement(particle.Header.Model, replacements);
                            } else if (chunk.Value is teEffectComponentRibbonRenderer ribbonRenderer) {
                                Find(info, ribbonRenderer.Header.ModelGUID, replacements);
                                lastParticleModel = GetReplacement(ribbonRenderer.Header.ModelGUID, replacements);
                            } else {
                                lastParticleModel = 0;
                            }
                        }
                    }

                    break;
                }
                case 0x1A: {
                    if (info.m_modelLooks.ContainsKey(guid)) {
                        if (context.Model != 0) {
                            info.m_models[context.Model].m_modelLooks.Add(guid);
                        }

                        break;
                    }

                    STUModelLook modelLook = GetInstance<STUModelLook>(guid);
                    if (modelLook == null) break;

                    ModelLookAsset modelLookInfo = new ModelLookAsset(guid);
                    info.m_modelLooks[guid] = modelLookInfo;

                    ComboContext modelLookContext = context.Clone();
                    modelLookContext.ModelLook = guid;

                    if (modelLook.m_materials != null) {
                        foreach (STUModelMaterial modelLookMaterial in modelLook.m_materials) {
                            FindModelMaterial(info, modelLookMaterial, modelLookInfo, modelLookContext, replacements);
                        }
                    }

                    if (modelLook.m_materialEffects != null) {

                        var matEffectContext = new ComboContext {
                            Model = context.Model
                            // this will be a loose material
                        };

                        foreach (STU_D75EA2E1 materialEffect in modelLook.m_materialEffects) {
                            Find(info, materialEffect.m_materialEffect, replacements, matEffectContext);
                            Find(info, materialEffect.m_82F3DCE0, replacements, matEffectContext);

                            foreach (var material in materialEffect.m_materials) {
                                Find(info, material.m_material, replacements, matEffectContext);
                                Find(info, material.m_5753874F, replacements, matEffectContext);
                            }
                        }
                    }

                    if (modelLook.m_C03306D7 != null) {
                        foreach (var modelRef in modelLook.m_C03306D7) {
                            Find(info, modelRef, replacements);
                        }
                    }

                    if (modelLook.m_05692DC5 != null) {
                        foreach (var anim in modelLook.m_05692DC5) {
                            Find(info, anim.m_animation, replacements, context);
                        }
                    }

                    if (modelLook.m_844B23C0 != null) {
                        foreach (var idk in modelLook.m_844B23C0) {
                            Find(info, idk.m_8A557E94, replacements, context);
                        }
                    }

                    if (context.Model != 0) {
                        info.m_models[context.Model].m_modelLooks.Add(guid);
                    }

                    break;
                }
                case 0x1B: {
                    if (!info.m_doneScripts.Add(guid)) break;

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
                }
                case 0x20: {
                    STUAnimBlendTree blendTree = GetInstance<STUAnimBlendTree>(guid);
                    if (blendTree == null || blendTree.m_animNodes == null) break;
                    foreach (STUAnimNode_Base animNode in blendTree.m_animNodes) {
                        if (animNode is STUAnimNode_Animation animNodeAnimation) {
                            Find(info, animNodeAnimation?.m_animation?.m_value, replacements, context);
                        } else if (animNode is STUAnimNode_AnimationPose2d animNodePose2D) {
                            Find(info, animNodePose2D?.m_animation?.m_value, replacements, context);
                        }
                    }

                    break;
                }
                case 0x21: {
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
                }
                case 0x2C: {
                    if (info.m_sounds.ContainsKey(guid)) break;

                    STUSound sound = GetInstance<STUSound>(guid);
                    if (sound == null) break;

                    SoundInfoNew soundInfo = new SoundInfoNew(guid);
                    info.m_sounds[guid] = soundInfo;

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
                }
                case 0x3F: {
                    if (info.m_soundFiles.ContainsKey(guid)) break;
                    SoundFileAsset soundFileInfo = new SoundFileAsset(guid);
                    info.m_soundFiles[guid] = soundFileInfo;
                    break;
                }
                case 0x43: {
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
                }
                case 0x5F: {
                    if (info.m_voiceSets.ContainsKey(guid)) break;

                    STUVoiceSet voiceSet = GetInstance<STUVoiceSet>(guid);

                    //string firstName = IO.GetString(voiceSet.m_269FC4E9);
                    //string lastName = IO.GetString(voiceSet.m_C0835C08);

                    if (voiceSet == null) break;

                    VoiceSetAsset voiceSetInfo = new VoiceSetAsset(guid);
                    info.m_voiceSets[guid] = voiceSetInfo;

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
                        Find(info, voiceLineInstanceInfo.Subtitle, replacements, context);
                        if (voiceLineInstance.m_voiceLineRuntime != null) {
                            voiceLineInstanceInfo.VoiceLineSet = voiceLineInstance.m_voiceLineRuntime.m_set;
                            voiceLineInstanceInfo.VoiceStimulus = voiceLineInstance.m_voiceLineRuntime.m_stimulus;
                            voiceLineInstanceInfo.ExternalSound = voiceLineInstance.m_voiceLineRuntime.m_externalSound;
                            voiceLineInstanceInfo.Conversations = voiceLineInstance.m_voiceLineRuntime.m_BD1B6F64?.Select(x => x.GUID.GUID).ToArray();
                            voiceLineInstanceInfo.SubtitleRuntime = voiceLineInstance.m_voiceLineRuntime.m_6148094F;
                            Find(info, voiceLineInstanceInfo.ExternalSound, replacements, context);
                            Find(info, voiceLineInstanceInfo.SubtitleRuntime, replacements, context);
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
                }
                case 0x7C: {
                    if (info.m_displayText.ContainsKey(guid)) break;

                    DisplayTextAsset stringInfo = new DisplayTextAsset(guid, GetString(guid));
                    info.m_displayText[guid] = stringInfo;
                    break;
                }
                case 0x71: {
                    if (info.m_subtitles.ContainsKey(guid)) break;

                    var subtitle = GetSubtitle(guid);
                    if (subtitle == null) break;

                    var subtitleSet = new SubtitleAsset(guid);
                    foreach (string s in subtitle.m_strings) {
                        subtitleSet.AddText(s);
                    }

                    info.m_subtitles[guid] = subtitleSet;
                    break;
                }
                case 0xA5: {
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

                        //Find(info, unlockSpray.m_ABFBD552?.m_effect, replacements, context);
                        //Find(info, unlockSpray.m_ABFBD552?.m_effectLook, replacements, context);
                    } else if (cosmetic is STUUnlock_AvatarPortrait unlockIcon) {
                        Find(info, unlockIcon.m_1B25AB90?.m_effect, replacements, context);
                        Find(info, unlockIcon.m_1B25AB90?.m_effectLook, replacements, context);
                    } else if (cosmetic is STUUnlock_POTGAnimation unlockHighlightIntro) {
                        Find(info, unlockHighlightIntro.m_animation, replacements, context);
                    }

                    break;
                }
                case 0xA6: {
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
                }
                case 0xA8: {
                    // hmm, if existing?
                    STUEffectLook effectLook = GetInstance<STUEffectLook>(guid);
                    foreach (teStructuredDataAssetRef<ulong> effectLookMaterialData in effectLook.m_materialData) {
                        Find(info, effectLookMaterialData, replacements, context);
                    }

                    break;
                }
                case 0xB2: {
                    if (info.m_voiceSoundFiles.ContainsKey(guid)) break;
                    SoundFileAsset voiceSoundFileInfo = new SoundFileAsset(guid);
                    info.m_voiceSoundFiles[guid] = voiceSoundFileInfo;
                    break;
                }
                case 0xB3: {
                    if (info.m_materialData.ContainsKey(guid)) break;
                    ComboContext materialDataContext = context.Clone();
                    materialDataContext.MaterialData = guid;

                    MaterialDataAsset materialDataInfo = new MaterialDataAsset(guid);

                    info.m_materialData[guid] = materialDataInfo;

                    teMaterialData materialData = new teMaterialData(OpenFile(guid));

                    if (materialData.Textures != null) {
                        materialDataInfo.m_textureMap = new Dictionary<ulong, uint>();
                        foreach (teMaterialData.Texture matDataTex in materialData.Textures) {
                            Find(info, matDataTex.TextureGUID, replacements, materialDataContext);
                            materialDataInfo.m_textureMap[matDataTex.TextureGUID] = matDataTex.NameHash;
                        }
                    }

                    if (materialData.StaticInputs != null) {
                        materialDataInfo.m_staticInputMap = new Dictionary<uint, byte[]>();
                        foreach (teMaterialDataStaticInput staticinput in materialData.StaticInputs) {
                            materialDataInfo.m_staticInputMap[staticinput.Header.Hash] = staticinput.Data;
                        }
                    }

                    break;
                }
                case 0xBF: {
                    STULineupPose lineupPose = GetInstance<STULineupPose>(guid);
                    if (lineupPose == null) break;

                    Find(info, lineupPose.m_E599EB7C, replacements, context);

                    Find(info, lineupPose.m_0189332F?.m_11E0A658, replacements, context);
                    Find(info, lineupPose.m_BEF008DE?.m_11E0A658, replacements, context);
                    Find(info, lineupPose.m_DE70F501?.m_11E0A658, replacements, context);
                    break;
                }
                default: {
                    if (s_unhandledTypes.Add(guidType)) {
                        Debugger.Log(0, "DataTool", $"[DataTool.FindLogic.Combo]: Unhandled type: {guidType:X3}\r\n");
                    }

                    break;
                }
            }

            return info;
        }

        public static void Find(ComboInfo info, STUStatescriptGraphWithOverrides graphWithOverrides, Dictionary<ulong, ulong> replacements = null, ComboContext context = null) {
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
                Find(info, (ulong) configVarModelLook.m_modelLook, replacements, context);
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

        private static void FindModelMaterial(ComboInfo info, STUModelMaterial modelMaterial, ModelLookAsset modelLookInfo, ComboContext modelLookContext, Dictionary<ulong, ulong> replacements) {
            if (modelMaterial == null || modelMaterial.m_material == 0) return;

            modelLookInfo.m_materials.Add(new ModelMaterial(GetReplacement((ulong) modelMaterial.m_material, replacements), modelMaterial.m_DC05EA3B));

            ComboContext modelMaterialContext = modelLookContext.Clone();
            modelMaterialContext.MaterialID = modelMaterial.m_DC05EA3B;
            Find(info, (ulong) modelMaterial.m_material, replacements, modelMaterialContext);
        }
    }
}