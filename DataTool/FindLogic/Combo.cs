using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DataTool.Helper;
using OWLib;
using OWLib.Types;
using OWLib.Types.Chunk;
using STULib.Types;
using STULib.Types.AnimationList.x021;
using STULib.Types.Generic;
using STULib.Types.Statescript.Components;
using STULib.Types.STUUnlock;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;
using static DataTool.Program;

namespace DataTool.FindLogic {
    public static class Combo {
        public class ComboInfo {
            // keep everything at top level, stops us from doing the same things again.
            // everything here is unsorted, but we can use GUIDs as references.
            public Dictionary<ulong, EntityInfoNew> Entities;
            public Dictionary<ulong, HashSet<ulong>> EntitiesByVar;
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

            public ComboConfig Config = new ComboConfig();
            public ComboSaveConfig SaveConfig = new ComboSaveConfig();

            public ComboInfo() {
                Entities = new Dictionary<ulong, EntityInfoNew>();
                EntitiesByVar = new Dictionary<ulong, HashSet<ulong>>();
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
        }

        public class ComboConfig {
            public bool DoExistingEntities = false;
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
                return OWLib.GUID.Index(GUID).ToString("X12");
            }
        }
        
        public class ComboNameable : ComboType {
            public string Name;

            public ComboNameable(ulong guid) : base(guid) {
                if (GUID == 0) return;
                uint type = OWLib.GUID.Type(GUID);
                uint index = OWLib.GUID.Index(GUID);
                if (!GUIDTable.ContainsKey(type)) return;
                if (GUIDTable[type].ContainsKey(index)) {
                    Name = GUIDTable[type][index];
                }
            }

            public override string GetName() {
                return GetValidFilename(Name) ?? GetFileName(GUID);
            }

            public override string GetNameIndex() {
                return GetValidFilename(Name) ?? OWLib.GUID.Index(GUID).ToString("X12");
            }
        }
        
        public class SoundFileInfo : ComboType {
            public SoundFileInfo(ulong guid) : base(guid) { }
        }

        public class SoundInfoNew : ComboType {
            public Dictionary<uint, ulong> Sounds;
            public ulong Bank;
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
        
        public class EffectInfoCombo : ComboType {
            // wrap
            public EffectParser.EffectInfo Effect;

            public EffectInfoCombo(ulong guid) : base(guid) { }
        }

        public class EntityInfoNew : ComboNameable {
            public ulong Model;
            public ulong Effect; // todo: STUEffectComponent defined instead of model is like a model in behaviour?
            public HashSet<ulong> Animations;
            
            public List<ChildEntityReferenceNew> Children;

            public EntityInfoNew(ulong guid) : base(guid) {
                Animations = new HashSet<ulong>();
            }
        }

        public class ChildEntityReferenceNew {
            public ulong Hardpoint;
            public ulong Variable;
            public ulong GUID;

            public ChildEntityReferenceNew(STUChildEntityDefinition childEntityDefinition, Dictionary<ulong, ulong> replacements) {
                GUID = GetReplacement(childEntityDefinition.Entity, replacements);
                Hardpoint = childEntityDefinition.HardPoint;
                Variable = childEntityDefinition.Variable;
            }
        }

        public class ModelLookInfo : ComboNameable {
            public HashSet<ulong> Materials;  // id, guid
            public ModelLookInfo(ulong guid) : base(guid) { }
        }

        public class MaterialInfo : ComboType {
            public ulong MaterialData;
            public ulong Shader;
            
            // shader info;
            // main shader = 44, used to be A5
            // golden = 50
            
            // ReSharper disable once InconsistentNaming
            public HashSet<ulong> IDs;  
            // dear blizz
            // ...
            // WHY DO YOU HAVE THE SAME MATERIAL MULTIPLE TIMES WITH DIFFERENT IDS. ONE OF THEM ISN'T EVEN USED
            // AHHHHHHHHHHHHHHHHHHH
            
            public MaterialInfo(ulong guid) : base(guid) { }
        }

        public class MaterialDataInfo : ComboType {
            public Dictionary<ulong, ImageDefinition.ImageType> Textures;

            public MaterialDataInfo(ulong guid) : base(guid) { }
        }

        public class TextureInfoNew : ComboNameable {
            public bool UseData;
            public ulong DataGUID => (GUID & 0xFFFFFFFFUL) | 0x100000000UL | 0x0320000000000000UL;
            public bool Loose;
            public TextureInfoNew(ulong guid) : base(guid) { }
        }

        public class ModelInfoNew : ComboType {
            public ulong Skeleton;
            public HashSet<ulong> Animations;
            public HashSet<ulong> ModelLooks;
            public HashSet<ulong> LooseMaterials;

            public ModelInfoNew(ulong guid) : base(guid) {
                Animations = new HashSet<ulong>();
                ModelLooks = new HashSet<ulong>();
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
            public ulong EntityVariable;

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
            
            // Debugger break area:
            // if (GetFileName(guid) == "000000000F6D.00C") Debugger.Break(); // tracer chest blue spinny thing

            uint guidType = GUID.Type(guid);
            if (guidType == 0 || guidType == 1) return info;
            switch (guidType) {
                case 0x3:
                    if (info.Config == null || info.Config.DoExistingEntities == false) {
                        if (info.Entities.ContainsKey(guid)) break;
                    }
                    STUEntityDefinition entityDefinition = GetInstance<STUEntityDefinition>(guid);
                    if (entityDefinition == null) break;
                    
                    EntityInfoNew entityInfo = new EntityInfoNew(guid);
                    info.Entities[guid] = entityInfo;

                    ComboContext entityContext = context.Clone();
                    entityContext.Entity = guid;

                    if (context.EntityVariable != 0) {
                        if (!info.EntitiesByVar.ContainsKey(context.EntityVariable)) {
                            info.EntitiesByVar[context.EntityVariable] = new HashSet<ulong>();
                        }
                        info.EntitiesByVar[context.EntityVariable].Add(guid);
                    }
                    
                    if (entityDefinition.Children != null) {
                        entityInfo.Children = new List<ChildEntityReferenceNew>();
                        foreach (STUChildEntityDefinition childEntityDefinition in entityDefinition.Children) {
                            if (childEntityDefinition == null) continue;
                            ComboContext childContext = new ComboContext {
                                EntityVariable = childEntityDefinition.Variable
                            };
                            Find(info, childEntityDefinition.Entity, replacements, childContext);
                            if (info.Entities.ContainsKey(GetReplacement(childEntityDefinition.Entity, replacements))) {  // sometimes the entity can't be loaded
                                entityInfo.Children.Add(new ChildEntityReferenceNew(childEntityDefinition, replacements));
                            }
                        }
                    }
                    
                    if (entityDefinition.Components != null) {
                        STUEntityComponent[] components = entityDefinition.Components.Values
                            .OrderBy(x => x?.GetType() != typeof(STUModelComponent) && 
                                          x?.GetType() != typeof(STUEffectComponent)).ToArray();
                        // STUModelComponent first because we need model for context
                        // STUEffectComponent second(ish) because we need effect for context
                        foreach (STUEntityComponent component in components) {
                            if (component == null) continue;
                            if (component.GetType() == typeof(STUModelComponent)) {
                                STUModelComponent modelComponent = component as STUModelComponent;
                                if (modelComponent == null) continue;
                                entityContext.Model = GetReplacement(modelComponent.Model, replacements);

                                Find(info, modelComponent.Model, replacements, entityContext);
                                Find(info, modelComponent.Look, replacements, entityContext);
                                Find(info, modelComponent.AnimBlendTreeSet, replacements, entityContext);
                            } else if (component.GetType() == typeof(STUEffectComponent)) {
                                STUEffectComponent effectComponent = component as STUEffectComponent;
                                if (effectComponent == null) continue;
                                entityContext.Effect = GetReplacement(effectComponent.Effect, replacements);
                                Find(info, effectComponent.Effect, replacements, entityContext);
                            // } else if (component.GetType() == typeof(STUStatescript01B)) {
                            //     STUStatescript01B ss01B = component as STUStatescript01B;
                            //     if (ss01B == null) continue;
                            //     Find(info, ss01B.GUIDx01B, replacements, context);
                            //     foreach (STU_61386B75 stu61386B75 in ss01B.m_3BD16B9E) {
                            //         Find(info, stu61386B75?.GUIDx01B, replacements, context);
                            //     }
                            } else if (component.GetType() == typeof(STUAnimationCoreferenceComponent)) {
                                STUAnimationCoreferenceComponent ssAnims = component as STUAnimationCoreferenceComponent;
                                if (ssAnims?.Animations == null) continue;
                                foreach (STUAnimationCoreferenceComponentAnimation ssAnim in ssAnims.Animations) {
                                    Find(info, ssAnim.Animation, replacements, entityContext);
                                }
                            } else if (component.GetType() == typeof(STUUnlockComponent)) {
                                STUUnlockComponent ssUnlock = component as STUUnlockComponent;
                                Find(info, ssUnlock.Unlock, replacements, entityContext);
                            } if (component.GetType() == typeof(STUFirstPersonComponent)) {
                                STUFirstPersonComponent firstPersonComponent = component as STUFirstPersonComponent;
                                Find(info, firstPersonComponent?.Entity, replacements);  // clean context
                            }
                            if (component.GetType() == typeof(STUSecondaryEffectComponent)) {
                                STUSecondaryEffectComponent secondaryEffectComponent = component as STUSecondaryEffectComponent;
                                Find(info, secondaryEffectComponent?.Effect, replacements, entityContext);
                            }
                        }
                    }

                    entityInfo.Model = entityContext.Model;
                    entityInfo.Effect = entityContext.Effect;
                    
                    break;
                case 0x4:
                    if (info.Textures.ContainsKey(guid)) break;
                    TextureInfoNew textureInfo = new TextureInfoNew(guid);
                    ulong dataKey = (guid & 0xFFFFFFFFUL) | 0x100000000UL | 0x0320000000000000UL;
                    bool useData = Files.ContainsKey(dataKey);
                    textureInfo.UseData = useData;
                    info.Textures[guid] = textureInfo;

                    if (context.Material == 0) {
                        textureInfo.Loose = true;
                    }
                    
                    break;
                case 0x6:
                    if (info.Animations.ContainsKey(guid)) break;
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
                    if (info.Materials.ContainsKey(guid) && (info.Materials[guid].IDs.Contains(context.MaterialID) || context.MaterialID == 0)) break;
                    // ^ break if material exists and has id, or id is 0
                    Material material = new Material(OpenFile(guid), 0);
                    MaterialInfo materialInfo;
                    if (!info.Materials.ContainsKey(guid)) {
                        materialInfo = new MaterialInfo(guid) {
                            MaterialData = GetReplacement(material.Header.ImageDefinition, replacements),
                            IDs = new HashSet<ulong>()
                        };
                        info.Materials[guid] = materialInfo;
                    } else {
                        materialInfo = info.Materials[guid];
                    }
                    materialInfo.IDs.Add(context.MaterialID);
                    materialInfo.Shader = material.Header.Shader;

                    if (context.ModelLook == 0 && context.Model != 0) {
                        info.Models[context.Model].LooseMaterials.Add(guid);
                    }
                    
                    ComboContext materialContext = context.Clone();
                    materialContext.Material = guid;
                    Find(info, material.Header.ImageDefinition, replacements, materialContext);
                    break;
                case 0xC:
                    if (info.Models.ContainsKey(guid)) break;
                    ModelInfoNew modelInfo = new ModelInfoNew(guid);
                    info.Models[guid] = modelInfo;
                    break;
                case 0xD:
                case 0x8F:  // sorry for breaking order
                    if (info.Effects.ContainsKey(guid)) break;
                    if (info.AnimationEffects.ContainsKey(guid)) break;
                    
                    EffectParser.EffectInfo effectInfo = new EffectParser.EffectInfo();
                    effectInfo.GUID = guid;
                    effectInfo.SetupEffect();

                    using (Stream effectStream = OpenFile(guid)) {
                        if (effectStream == null) break;
                        using (Chunked effectChunked = new Chunked(effectStream, true, ChunkManager.Instance)) {
                            EffectParser parser = new EffectParser(effectChunked, guid);
                            ulong lastModel = 0;
                            
                            foreach (KeyValuePair<EffectParser.ChunkPlaybackInfo, IChunk> chunk in parser.GetChunks()) {
                                if (chunk.Value == null || chunk.Value.GetType() == typeof(MemoryChunk)) continue;

                                parser.Process(effectInfo, chunk, replacements);
                                
                                if (chunk.Value.GetType() == typeof(DMCE)) {
                                    DMCE dmce = chunk.Value as DMCE;
                                    if (dmce == null) continue;
                                    ComboContext dmceContext = new ComboContext {Model = GetReplacement(dmce.Data.Model, replacements)};
                                    Find(info, dmce.Data.Model, replacements, dmceContext);
                                    Find(info, dmce.Data.Look, replacements, dmceContext);
                                    Find(info, dmce.Data.Animation, replacements, dmceContext);
                                } else if (chunk.Value.GetType() == typeof(FECE)) {
                                    FECE fece = chunk.Value as FECE;
                                    if (fece == null) continue;
                                    Find(info, fece.Data.Effect, replacements);  // clean context
                                } else if (chunk.Value.GetType() == typeof(NECE)) {
                                    NECE nece = chunk.Value as NECE;
                                    if (nece == null) continue;
                                    ComboContext neceContext =
                                        new ComboContext {EntityVariable = nece.Data.EntityVariable};
                                    Find(info, nece.Data.Entity, replacements, neceContext);
                                } else if (chunk.Value.GetType() == typeof(SSCE)) {
                                    SSCE ssce = chunk.Value as SSCE;
                                    if (ssce == null) continue;
                                    ComboContext ssceContext = new ComboContext();
                                    if (lastModel != 0) ssceContext.Model = lastModel;
                                    Find(info, ssce.Data.Material, replacements, ssceContext);
                                    Find(info, ssce.Data.TextureDefinition, replacements, ssceContext);
                                } else if (chunk.Value.GetType() == typeof(CECE)) {
                                    CECE cece = chunk.Value as CECE;
                                    if (cece == null) continue;
                                    Find(info, cece.Data.Animation, replacements);
                                    if (!info.EntitiesByVar.ContainsKey(cece.Data.EntityVariable)) continue;
                                    if (cece.Data.Animation == 0) continue;
                                    foreach (ulong ceceEntity in info.EntitiesByVar[cece.Data.EntityVariable]) {
                                        EntityInfoNew ceceEntityInfo = info.Entities[ceceEntity];
                                        ceceEntityInfo.Animations.Add(cece.Data.Animation);
                                        if (ceceEntityInfo.Model != 0) {
                                            info.Models[ceceEntityInfo.Model].Animations.Add(cece.Data.Animation);
                                        }
                                    }
                                }
                                
                                if (chunk.Value.GetType() == typeof(OSCE)) {
                                    OSCE osce = chunk.Value as OSCE;
                                    if (osce == null) continue;
                                    Find(info, osce.Data.Sound, replacements);
                                }

                                if (chunk.Value.GetType() == typeof(RPCE)) {
                                    RPCE rpce = chunk.Value as RPCE;
                                    if (rpce == null) continue;
                                    Find(info, rpce.Data.Model, replacements);
                                    lastModel = GetReplacement(rpce.Data.Model, replacements);
                                } else {
                                    lastModel = 0;
                                }
                            }
                        }
                    }


                    if (guidType == 0xD) {
                        info.Effects[guid] = new EffectInfoCombo(guid) {Effect = effectInfo};
                    } else if (guidType == 0x8F) {
                        info.AnimationEffects[guid] = new EffectInfoCombo(guid) {Effect = effectInfo};
                    }
                    
                    break;
                    
                case 0x1A:
                    if (info.ModelLooks.ContainsKey(guid)) break;
                    
                    STUModelLook modelLook = GetInstance<STUModelLook>(guid);
                    if (modelLook == null) break;
                    
                    ModelLookInfo modelLookInfo = new ModelLookInfo(guid);
                    info.ModelLooks[guid] = modelLookInfo;
                    
                    ComboContext modelLookContext = context.Clone();
                    
                    if (context.Model != 0) {
                        info.Models[context.Model].ModelLooks.Add(guid);
                    }
                    
                    modelLookContext.ModelLook = guid;
                    if (modelLook.Materials != null) {
                        modelLookInfo.Materials = new HashSet<ulong>();
                        foreach (STUModelMaterial modelLookMaterial in modelLook.Materials) {
                            if (modelLookMaterial == null || modelLookMaterial.Material == 0) continue;
                            modelLookInfo.Materials.Add(GetReplacement(modelLookMaterial.Material, replacements));
                            ComboContext modelLookMaterialContext = modelLookContext.Clone();
                            modelLookMaterialContext.MaterialID = modelLookMaterial.ID;
                            Find(info, modelLookMaterial.Material, replacements, modelLookMaterialContext);
                        }
                    }

                    break;
                case 0x20:
                    // todo: how do blend trees work
                    // STUAnimBlendTree blendTree = GetInstance<STUAnimBlendTree>(guid);
                    // foreach (STUAnimNode_Base blendTreeAnimNode in blendTree.AnimNodes) {
                    //     if (blendTreeAnimNode is STUAnimNode_Animation animNodeAnimation) {
                    //         Find(info, animNodeAnimation.Animation?.Value, replacements, context);
                    //     }
                    // }
                    
                    STUAnimationListAnimationWrapper[] wrappers2 =
                        GetAllInstances<STUAnimationListAnimationWrapper>(guid);
                    foreach (STUAnimationListAnimationWrapper animationWrapper in wrappers2) {
                        Find(info, animationWrapper?.Value, replacements, context);
                    }
                    break;
                case 0x21:
                    STUAnimBlendTreeSet blendTreeSet = GetInstance<STUAnimBlendTreeSet>(guid);
                    if (blendTreeSet == null) break;
                    foreach (STUAnimBlendTreeSet_BlendTreeItem blendTreeItem in blendTreeSet.BlendTreeItems) {
                        if (blendTreeItem?.AnimationContainer?.Animations != null) {
                            foreach (STUAnimationListAnimationWrapper listAnimationWrapper in blendTreeItem.AnimationContainer.Animations) {
                                Find(info, listAnimationWrapper?.Value, replacements, context);
                            }
                        }
                        Find(info, blendTreeItem?.SecondaryList, replacements, context);
                        if (blendTreeItem?.m_9AD6CC25 != null) {
                            if (blendTreeItem.m_9AD6CC25.GetType() == typeof(STU_7D00A73D)) {
                                STU_7D00A73D infosub7D00Converted = blendTreeItem.m_9AD6CC25 as STU_7D00A73D;
                                if (infosub7D00Converted?.m_083DC038 != null) {
                                    foreach (STU_65DD9C84 sub7D00Sub in infosub7D00Converted.m_083DC038) {
                                       Find(info, sub7D00Sub?.Animation, replacements, context);
                                    }
                                }
                                
                            }
                        }
                        if (blendTreeItem?.OnFinished?.m_6CB79D25 != null) {
                            foreach (STU_BE20B7F5 blendTreeSetOnFinishedThing in blendTreeItem.OnFinished.m_6CB79D25) {
                                Find(info, blendTreeSetOnFinishedThing?.Animation, replacements, context);
                            }
                        }
                    }
                    // erm, k
                    foreach (Common.STUGUID listInfoReference in blendTreeSet.References) {
                        Find(info, listInfoReference, replacements, context);
                    }
                    // STUAnimationListAnimationWrapper[] wrappers =
                    //     GetAllInstances<STUAnimationListAnimationWrapper>(guid);
                    // foreach (STUAnimationListAnimationWrapper animationWrapper in wrappers) {
                    //     Find(info, animationWrapper?.Value, replacements, context);
                    // }
                    break;
                case 0x2C:
                    if (info.Sounds.ContainsKey(guid)) break;
                    STUSound sound = GetInstance<STUSound>(guid);
                    SoundInfoNew soundInfo = new SoundInfoNew(guid);
                    info.Sounds[guid] = soundInfo;

                    if (sound?.Inner?.Soundbank != null) {
                        Find(info, sound.Inner.Soundbank, replacements, context);
                        soundInfo.Bank = GetReplacement(sound.Inner.Soundbank, replacements);

                        soundInfo.Sounds = new Dictionary<uint, ulong>();
                        for (int i = 0; i < sound.Inner.IDs.Length; i++) {
                            ulong soundFileRef = sound.Inner.Sounds[i];
                            soundInfo.Sounds[sound.Inner.IDs[i]] = GetReplacement(soundFileRef, replacements);
                            Find(info, soundFileRef, replacements, context);
                        }
                    }
                    break;
                case 0x3F:
                    if (info.SoundFiles.ContainsKey(guid)) break;
                    SoundFileInfo soundFileInfo = new SoundFileInfo(guid);
                    info.SoundFiles[guid] = soundFileInfo;
                    break;
                case 0x43:
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
                                    if (actionParameter.Key == ConvertLogic.Sound.BankObjectEventAction
                                            .EventActionParameterType.Play) {
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
                    
                    break;
                case 0xA5:
                    // hmm, if existing?
                    Cosmetic cosmetic = GetInstance<Cosmetic>(guid);

                    if (cosmetic.GetType() == typeof(Spray)) {
                        Spray sprayCosmetic = (Spray) cosmetic;
                        Find(info, sprayCosmetic.Effect2?.Effect, replacements, context);
                        Find(info, sprayCosmetic.Effect2?.EffectLook, replacements, context);
                        Find(info, sprayCosmetic.Effect?.EffectLook, replacements, context);
                        Find(info, sprayCosmetic.Effect?.Effect, replacements, context);
                    } else if (cosmetic.GetType() == typeof(PlayerIcon)) {
                        PlayerIcon playerIconCosmetic = (PlayerIcon) cosmetic;
                        Find(info, playerIconCosmetic.Effect?.EffectLook, replacements, context);
                        Find(info, playerIconCosmetic.Effect?.Effect, replacements, context);
                    } else if (cosmetic.GetType() == typeof(HighlightIntro)) {
                        HighlightIntro cosmeticHighlightIntro = (HighlightIntro) cosmetic;
                        Find(info, cosmeticHighlightIntro.Animation, replacements, context);
                    } else if (cosmetic.GetType() == typeof(Emote)) {
                        Emote cosmeticEmote = (Emote) cosmetic;
                        Find(info, cosmeticEmote.BlendTreeSet, replacements, context);
                    }

                    break;
                case 0xA8:
                    // hmm, if existing?
                    STUEffectLook effectLook = GetInstance<STUEffectLook>(guid);
                    if (effectLook == null) break;
                    foreach (Common.STUGUID materialData in effectLook.MaterialDatas) {
                        Find(info, materialData, replacements, context);
                    }
                    break;
                case 0xB3:
                    if (info.MaterialDatas.ContainsKey(guid)) break;
                    ComboContext materialDataContext = context.Clone();
                    materialDataContext.MaterialData = guid;
                    
                    MaterialDataInfo materialDataInfo = new MaterialDataInfo(guid);

                    info.MaterialDatas[guid] = materialDataInfo;
                    ImageDefinition def = new ImageDefinition(OpenFile(guid));
                    if (def.Layers != null) {
                        materialDataInfo.Textures = new Dictionary<ulong, ImageDefinition.ImageType>();
                        foreach (ImageLayer layer in def.Layers) {
                            Find(info, layer.Key, replacements, materialDataContext);
                            materialDataInfo.Textures[layer.Key] = layer.Type;
                        }
                    }
                    
                    break;
                default:
                    Debugger.Log(0, "DataTool", $"[DataTool.FindLogic.Combo]: Unhandled type: {guidType:X3}\r\n");
                    break;
            }

            return info;
        }
    }
}