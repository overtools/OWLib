using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DataTool.Helper;
using OWLib;
using STULib;
using STULib.Types;
using STULib.Types.Generic;
using STULib.Types.Statescript.Components;
using STULib.Types.Statescript.ConfigVar;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.FindLogic {
    public class ChildEntityReference : IEquatable<ChildEntityReference> {
        public Common.STUGUID GUID;
        public Common.STUGUID Hardpoint;
        public Common.STUGUID Variable;  // the entity referenced by this
        public Common.STUGUID Model;

        public ChildEntityReference(STUChildEntityDefinition def, Common.STUGUID model) {
            GUID = def.Entity;
            Hardpoint = def.HardPoint;
            Variable = def.Variable;
            Model = model;
        }

        public bool Equals(ChildEntityReference other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(GUID, other.GUID) && Equals(Hardpoint, other.Hardpoint) && Equals(Variable, other.Variable);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ChildEntityReference) obj);
        }

        public override int GetHashCode() {
            unchecked {
                int hashCode = (GUID != null ? GUID.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Hardpoint != null ? Hardpoint.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Variable != null ? Variable.GetHashCode() : 0);
                return hashCode;
            }
        }
    }

    public class EntityInfo {
        public ulong GUID;
        public HashSet<ChildEntityReference> Children;
        public HashSet<AnimationInfo> Animations;

        public ulong Model;

        public EntityInfo() {}

        public EntityInfo(ulong guid) {
            GUID = guid;
        }
    }
    
    public class ModelInfo : IEquatable<ModelInfo> {
        public ulong GUID;
        public Common.STUGUID Skeleton;
        public HashSet<AnimationInfo> Animations;
        public HashSet<TextureInfo> Textures;

        public ModelInfo(ulong guid) {
            GUID = guid;
            Animations = new HashSet<AnimationInfo>();
            Textures = new HashSet<TextureInfo>();
        }

        public Dictionary<Common.STUGUID, EntityInfo> Entities;  // eww, please. Future: somewhere nice for this
        // todo: is everything an entity? even random map props?

        public bool Equals(ModelInfo other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(GUID, other.GUID) && Equals(Animations, other.Animations);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ModelInfo) obj);
        }

        public override int GetHashCode() {
            unchecked {
                return ((GUID != null ? GUID.GetHashCode() : 0) * 397) ^ (Animations != null ? Animations.GetHashCode() : 0);
            }
        }
    }
    
    public class Model {
        public static void AddGUID(HashSet<ModelInfo> models, Common.STUGUID newElement, Dictionary<ulong, List<TextureInfo>> textures, HashSet<AnimationInfo> animations, Dictionary<ulong, ulong> replacements, Common.STUGUID skeleton=null) {
            List<TextureInfo> textureList = new List<TextureInfo>();
            foreach (KeyValuePair<ulong,List<TextureInfo>> pair in textures) {
                textureList.AddRange(pair.Value);
            }
            AddGUID(models, newElement, new HashSet<TextureInfo>(textureList), animations, replacements, skeleton);
        }
        
        public static void AddGUID(HashSet<ModelInfo> models, Common.STUGUID newElement, List<TextureInfo> textures, HashSet<AnimationInfo> animations, Dictionary<ulong, ulong> replacements, Common.STUGUID skeleton=null) {
            AddGUID(models, newElement, new HashSet<TextureInfo>(textures), animations, replacements, skeleton);
        }

        public static ModelInfo GetModelInfo(HashSet<ModelInfo> models, ulong model, Dictionary<ulong, ulong> replacements) {
            if (model == 0) return null;
            if (replacements.ContainsKey(model)) model = new Common.STUGUID(replacements[model]);
            ModelInfo modelInfo = models.FirstOrDefault(x => x.GUID == model);
            return modelInfo;
        }

        public static void AddGUID(HashSet<ModelInfo> models, Common.STUGUID newElement, HashSet<TextureInfo> textures, HashSet<AnimationInfo> animations, Dictionary<ulong, ulong> replacements, Common.STUGUID skeleton=null) {
            if (newElement == null) return;

            if (animations == null) animations = new HashSet<AnimationInfo>();
            if (textures == null) textures = new HashSet<TextureInfo>();
            if (replacements != null) {
                if (replacements.ContainsKey(newElement)) newElement = new Common.STUGUID(replacements[newElement]);
            }
            ModelInfo newModel = new ModelInfo(newElement) {Animations = animations, Textures = textures, 
                Skeleton = skeleton, Entities = new Dictionary<Common.STUGUID, EntityInfo>()};

            if (models.All(x => !Equals(x.GUID, newModel.GUID))) {
                models.Add(newModel);
            } else {
                ModelInfo existing = models.FirstOrDefault(x => Equals(x.GUID, newModel.GUID));
                if (existing == null) return;
                if (existing.Skeleton == null) existing.Skeleton = newModel.Skeleton;
                foreach (AnimationInfo newModelAnimation in newModel.Animations) {
                    if (!existing.Animations.Contains(newModelAnimation)) {
                        existing.Animations.Add(newModelAnimation);
                    }
                }
                foreach (TextureInfo newModelTexture in newModel.Textures) {
                    if (!existing.Textures.Contains(newModelTexture)) {
                        existing.Textures.Add(newModelTexture);
                    }
                }
            }
        }

        public static void AddEntity(HashSet<ModelInfo> models, Common.STUGUID entityGUID, Common.STUGUID modelGUID, Dictionary<ulong, ulong> replacements, HashSet<AnimationInfo> animations) {
            if (replacements.ContainsKey(entityGUID)) entityGUID = new Common.STUGUID(replacements[entityGUID]);
            if (replacements.ContainsKey(modelGUID)) modelGUID = new Common.STUGUID(replacements[modelGUID]);
            ModelInfo model = models.FirstOrDefault(x => x.GUID.Equals(modelGUID));
            if (model == null) return;

            if (!model.Entities.ContainsKey(entityGUID)) {
                model.Entities[entityGUID] = new EntityInfo {
                    Children = new HashSet<ChildEntityReference>(),
                    GUID = entityGUID,
                    Model = modelGUID,
                    Animations = animations
                };
            } else {
                model.Entities[entityGUID].Animations = animations;
            }
        }
        
        public static void AddEntityChild(HashSet<ModelInfo> models, Common.STUGUID entityGUID, Common.STUGUID modelGUID, 
            STUChildEntityDefinition definition, Dictionary<ulong, ulong> replacements, Common.STUGUID childModel) {
            if (replacements.ContainsKey(entityGUID)) entityGUID = new Common.STUGUID(replacements[entityGUID]);
            if (replacements.ContainsKey(modelGUID)) modelGUID = new Common.STUGUID(replacements[modelGUID]);
            ModelInfo model = models.FirstOrDefault(x => x.GUID.Equals(modelGUID));
            if (model == null) return;

            if (model.Entities.ContainsKey(entityGUID)) {
                model.Entities[entityGUID].Children.Add(new ChildEntityReference(definition, childModel));
            }
        }

        public static HashSet<ModelInfo> FindChunked(HashSet<ModelInfo> existingModels, Common.STUGUID modelGUID,
            Dictionary<ulong, ulong> replacements = null) {
            if (existingModels == null) {
                existingModels = new HashSet<ModelInfo>();
            }

            if (replacements == null) replacements = new Dictionary<ulong, ulong>();
            if (modelGUID == null) return existingModels;
            if (replacements.ContainsKey(modelGUID)) modelGUID = new Common.STUGUID(replacements[modelGUID]);

            using (Stream chunkStream = OpenFile(modelGUID)) {
                if (chunkStream == null) {
                    return existingModels;
                }
                Chunked chunked = new Chunked(chunkStream, true, ChunkManager.Instance);
                
                EffectParser parser = new EffectParser(chunked, modelGUID);
                parser.ProcessAll(replacements);
                Animation.FindChunked(null, existingModels, modelGUID, replacements, 0);
                
                // all of the effect stuff is handled by FindLogic.Animation.FindChunked
            }
            return existingModels;
        }

        public static HashSet<ModelInfo> FindModels(HashSet<ModelInfo> existingModels, Common.STUGUID modelGUID, Dictionary<ulong, ulong> replacements=null) {
            if (existingModels == null) {
                existingModels = new HashSet<ModelInfo>();
            }

            if (replacements == null) replacements = new Dictionary<ulong, ulong>();
            if (modelGUID == null) return existingModels;
            if (replacements.ContainsKey(modelGUID)) modelGUID = new Common.STUGUID(replacements[modelGUID]);

            switch (GUID.Type(modelGUID)) {
                case 0x03:
                    STUEntityDefinition entityDefinition = GetInstance<STUEntityDefinition>(modelGUID);
                    if (entityDefinition == null) break;
                    Common.STUGUID entityModel = null;
                    Common.STUGUID entitySound = null;
                    HashSet<AnimationInfo> animations = new HashSet<AnimationInfo>();
                    
                    foreach (KeyValuePair<ulong, STUEntityComponent> statescriptComponent in entityDefinition.Components.OrderBy(x => x.Value?.GetType() != typeof(STUModelComponent))) {
                        STUEntityComponent component = statescriptComponent.Value;
                        if (component == null) continue;
                        if (component.GetType() == typeof(STUModelComponent)) {
                            STUModelComponent modelComponent = component as STUModelComponent;
                            Dictionary<ulong, List<TextureInfo>> textures = new Dictionary<ulong, List<TextureInfo>>();
                            textures = Texture.FindTextures(textures, modelComponent?.Look, null, true, replacements);
                            
                            entityModel = modelComponent?.Model;
                            
                            AddEntity(existingModels, modelGUID, entityModel, replacements, new HashSet<AnimationInfo>());
                            
                            if (entityDefinition.Children != null) {
                                foreach (STUChildEntityDefinition entityChild in entityDefinition.Children) {
                                    existingModels = FindModels(existingModels, entityChild?.Entity, replacements);
                                    if (entityModel != null && entityChild != null) {
                                        STUModelComponent childModelComponent = GetInstance<STUModelComponent>(entityChild.Entity);
                                        AddEntityChild(existingModels, modelGUID, entityModel, entityChild, replacements, childModelComponent?.Model);
                                    }
                                }
                            }
                            
                            animations = Animation.FindAnimations(animations, existingModels, modelComponent?.AnimBlendTreeSet, replacements);
                            
                            List<TextureInfo> textureList = new List<TextureInfo>();
                            foreach (KeyValuePair<ulong,List<TextureInfo>> pair in textures) {
                                textureList.AddRange(pair.Value);
                            }
                            AddGUID(existingModels, modelComponent?.Model, new HashSet<TextureInfo>(textureList), animations, replacements, modelComponent.Skeleton);
                            
                            AddEntity(existingModels, modelGUID, entityModel, replacements, animations);
                            
                            // AddGUID(models, newElement, new HashSet<TextureInfo>(textureList), animations, replacements, skeleton);
                            
                            existingModels = FindModels(existingModels, modelComponent?.Look, replacements);  // get all referenced models
                        }
                        if (component.GetType() == typeof(STUEntityVoiceMaster)) {
                            STUEntityVoiceMaster soundMaster = component as STUEntityVoiceMaster;
                            entitySound = soundMaster.VoiceMaster;
                        }
                        if (component.GetType() == typeof(STUFirstPersonComponent)) {  // 003 sub-reference
                            STUFirstPersonComponent sub003 = component as STUFirstPersonComponent;
                            existingModels = FindModels(existingModels, sub003?.Entity, replacements);
                        }
                        if (component.GetType() == typeof(STUSecondaryEffectComponent)) {
                            STUSecondaryEffectComponent secondaryEffectComponent = component as STUSecondaryEffectComponent;
                            existingModels = FindModels(existingModels, secondaryEffectComponent?.Effect, replacements);
                        }
                        if (component.GetType() == typeof(STUEffectComponent)) {
                            STUEffectComponent effectComponent = component as STUEffectComponent;
                            existingModels = FindModels(existingModels, effectComponent?.Effect, replacements);
                        }
                        if (component.GetType() == typeof(STUStatescript01B)) {
                            STUStatescript01B ss01B = component as STUStatescript01B;
                            if (ss01B == null) continue;
                            existingModels = FindModels(existingModels, ss01B.GUIDx01B, replacements);
                            foreach (STU_61386B75 stu61386B75 in ss01B.m_3BD16B9E) {
                                existingModels = FindModels(existingModels, stu61386B75?.GUIDx01B, replacements);
                            }
                        }
                        if (component.GetType() == typeof(STUAnimationCoreferenceComponent)) {
                            STUAnimationCoreferenceComponent ssAnims = component as STUAnimationCoreferenceComponent;
                            if (ssAnims?.Animations == null) continue;
                            foreach (STUAnimationCoreferenceComponentAnimation ssAnim in ssAnims.Animations) {
                                animations = Animation.FindAnimations(animations, existingModels, ssAnim.Animation, replacements);
                            }
                        }
                        if (component.GetType() == typeof(STUUnlockComponent)) {
                            STUUnlockComponent ssUnlock = component as STUUnlockComponent;
                            animations = Animation.FindAnimations(animations, existingModels, ssUnlock.Unlock, replacements);
                        }
                    }
                    if (entitySound != null) {
                        foreach (AnimationInfo animation in animations) {
                            animation.SoundMaster = entitySound;
                        }
                    }
                    if (entityModel != null) {  // we want all anims
                        AddGUID(existingModels, entityModel, new Dictionary<ulong, List<TextureInfo>>(), animations, replacements);
                        AddEntity(existingModels, modelGUID, entityModel, replacements, animations);
                    }
                    
                    break;
                case 0x0D:
                    existingModels = FindChunked(existingModels, modelGUID, replacements);
                    break;
                case 0x1A:
                    // pre-process material and prepare for model or add new textures
                    STUModelLook modelLook = GetInstance<STUModelLook>(modelGUID);
                    Dictionary<ulong, List<TextureInfo>> matTextures = new Dictionary<ulong, List<TextureInfo>>();
                    matTextures = Texture.FindTextures(matTextures, modelGUID, null, true, replacements);
                    foreach (Common.STUGUID modelReference in modelLook.ModelReferences) {
                        AddGUID(existingModels, modelReference, matTextures, null, replacements);
                    }
                    break;
                case 0x0C:
                    AddGUID(existingModels, modelGUID, new Dictionary<ulong, List<TextureInfo>>(), null, replacements);
                    // existingModels = FindChunked(existingModels, modelGUID, replacements);
                    break;
                case 0x1B:
                    ISTU stuTemp = OpenSTUSafe(modelGUID);
                    // STUStatescriptDataStoreBase[] dataStores = GetAllInstances<STUStatescriptDataStoreBase>(modelGUID);
                    foreach (STUStatescriptDataStore01B statescriptDataStore01B in stuTemp.Instances.OfType<STUStatescriptDataStore01B>()) {
                        existingModels = FindModels(existingModels, statescriptDataStore01B.GUIDx01B, replacements);
                    }
                    foreach (STUStatescriptDataStoreComponent statescriptDataStoreComponent in stuTemp.Instances.OfType<STUStatescriptDataStoreComponent>()) {
                        existingModels = FindModels(existingModels, statescriptDataStoreComponent.Component, replacements);
                    }
                    foreach (STUStatescriptDataStoreComponent2 statescriptDataStoreComponent in stuTemp.Instances.OfType<STUStatescriptDataStoreComponent2>()) {
                        existingModels = FindModels(existingModels, statescriptDataStoreComponent.Entity, replacements);
                    }
                    foreach (STUStatescriptDataStoreMaterial statescriptDataStoreMaterial in stuTemp.Instances.OfType<STUStatescriptDataStoreMaterial>()) {
                        existingModels = FindModels(existingModels, statescriptDataStoreMaterial.ModelLook, replacements);
                    }
                    foreach (STUConfigVarEffect statescriptDataStoreEffect in stuTemp.Instances.OfType<STUConfigVarEffect>()) {
                        existingModels = FindModels(existingModels, statescriptDataStoreEffect.Effect, replacements);
                    }
                    break;
                default:
                    Debugger.Log(0, "DataTool.FindLogic.Model", $"[DataTool.FindLogic.Model] Unhandled type: {GUID.Type(modelGUID):X3}\n");
                    break;
            }
            return existingModels;
        }
    }
}