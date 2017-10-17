using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using OWLib;
using OWLib.Types.Chunk;
using STULib.Types;
using STULib.Types.Generic;
using STULib.Types.StatesciptComponents;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.FindLogic {
    public class FoundModelInfo {
        public HashSet<ModelInfo> Models;
        public Dictionary<Common.STUGUID, HashSet<AnimationInfo>> LooseAnimations;
    }
    public class ModelInfo : IEquatable<ModelInfo> {
        public Common.STUGUID GUID;
        public Common.STUGUID Skeleton;
        public HashSet<AnimationInfo> Animations;
        public HashSet<TextureInfo> Textures;

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

        public static void AddGUID(HashSet<ModelInfo> models, Common.STUGUID newElement, HashSet<TextureInfo> textures, HashSet<AnimationInfo> animations, Dictionary<ulong, ulong> replacements, Common.STUGUID skeleton=null) {
            if (newElement == null) return;

            if (animations == null) animations = new HashSet<AnimationInfo>();
            if (textures == null) textures = new HashSet<TextureInfo>();
            if (replacements.ContainsKey(newElement)) newElement = new Common.STUGUID(replacements[newElement]);
            ModelInfo newModel = new ModelInfo {GUID = newElement, Animations = animations, Textures = textures, Skeleton = skeleton};

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
        
        // public static HashSet<ModelInfo> Find(HashSet<ModelInfo> existingModels, Common.STUInstance instance) {
        //     return null;
        // }

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
                
                DMCE[] dmces = chunked.GetAllOfTypeFlat<DMCE>();
                foreach (DMCE dmce in dmces) {
                    if (dmce.Data.modelKey == 0) continue;
                    
                    Dictionary<ulong, List<TextureInfo>> textures = new Dictionary<ulong, List<TextureInfo>>();
                    textures = Texture.FindTextures(textures, new Common.STUGUID(dmce.Data.materialKey), null, true, replacements);
                    
                    HashSet<AnimationInfo> animations = new HashSet<AnimationInfo>();
                    animations = Animation.FindAnimations(animations, existingModels, new Common.STUGUID(dmce.Data.animationKey), replacements);
                    
                    AddGUID(existingModels, new Common.STUGUID(dmce.Data.modelKey), textures, animations, replacements);
                    
                    // if (animList != null && !animList.ContainsKey(dmce.Data.animationKey) && dmce.Data.animationKey != 0) {
                    //     if (replace.ContainsKey(dmce.Data.animationKey)) {
                    //         animList[replace[dmce.Data.animationKey]] = parent;
                    //         FindAnimationsSoft(replace[dmce.Data.animationKey], sound, animList, replace, parsed, map, handler, models, layers, replace[dmce.Data.animationKey]);
                    //     } else {
                    //         animList[dmce.Data.animationKey] = parent;
                    //         FindAnimationsSoft(dmce.Data.animationKey, sound, animList, replace, parsed, map, handler, models, layers, dmce.Data.animationKey);
                    //     }
                    // }
                }
                
                NECE[] neces = chunked.GetAllOfTypeFlat<NECE>();
                foreach (NECE nece in neces) {
                    if (nece.Data.key > 0) {
                        existingModels = FindModels(existingModels, new Common.STUGUID(nece.Data.key), replacements);
                        // FindModels(nece.Data.key, new List<ulong>(), models, animList, layers, replace, parsed, map, handler, sound);
                    }
                }
                
                // NECE[] neces = chunked.GetAllOfTypeFlat<NECE>();
                // foreach (NECE nece in neces) {
                //     if (nece.Data.key > 0) {
                //         FindModels(nece.Data.key, new List<ulong>(), models, animList, layers, replace, parsed, map, handler, sound);
                //     }
                // }
                // CECE[] ceces = chunked.GetAllOfTypeFlat<CECE>();
                // foreach (CECE cece in ceces) {
                //     if (animList != null && !animList.ContainsKey(cece.Data.animation) && cece.Data.animation != 0) {
                //         animList[cece.Data.animation] = parent;
                //         FindAnimationsSoft(cece.Data.animation, sound, animList, replace, parsed, map, handler, models, layers, cece.Data.animation);
                //     }
                // }
                // SSCE[] ssces = chunked.GetAllOfTypeFlat<SSCE>();
                // foreach (SSCE ssce in ssces) {
                //     if (layers != null) {
                //         FindTexturesAnonymous8(ssce.Data.material_key, layers, replace, parsed, map, handler);
                //         FindTexturesAnonymousB3(ssce.Data.definition_key, layers, replace, parsed, map, handler);
                //     }
                // }
                // RPCE[] prces = chunked.GetAllOfTypeFlat<RPCE>();
                // foreach (RPCE prce in prces) {
                //     if (models != null) {
                //         if (replace.ContainsKey(prce.Data.model_key)) {
                //             models.Add(replace[prce.Data.model_key]);
                //         } else {
                //             models.Add(prce.Data.model_key);
                //         }
                //     }
                // }
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
                    STUStatescriptComponentMaster container = GetInstance<STUStatescriptComponentMaster>(modelGUID);
                    HashSet<AnimationInfo> animationBank = new HashSet<AnimationInfo>();
                    if (container == null) break;
                    foreach (KeyValuePair<ulong, STUStatescriptComponent> statescriptComponent in container.Components) {
                        STUStatescriptComponent component = statescriptComponent.Value;
                        if (component == null) continue;
                        if (component.GetType() == typeof(STUModelComponent)) {
                            STUModelComponent modelComponent = component as STUModelComponent;
                            Dictionary<ulong, List<TextureInfo>> textures = new Dictionary<ulong, List<TextureInfo>>();
                            textures = Texture.FindTextures(textures, modelComponent?.Material, null, true, replacements);
                            
                            HashSet<AnimationInfo> animations = new HashSet<AnimationInfo>();
                            animations = Animation.FindAnimations(animations, existingModels, modelComponent?.AnimationList, replacements);
                            animations = new HashSet<AnimationInfo>(animations.Concat(animationBank));
                            
                            List<TextureInfo> textureList = new List<TextureInfo>();
                            foreach (KeyValuePair<ulong,List<TextureInfo>> pair in textures) {
                                textureList.AddRange(pair.Value);
                            }
                            AddGUID(existingModels, modelComponent?.Model, new HashSet<TextureInfo>(textureList), animations, replacements, modelComponent.Skeleton);
                            // AddGUID(models, newElement, new HashSet<TextureInfo>(textureList), animations, replacements, skeleton);
                            
                            // AddGUID(existingModels, modelComponent?.Model, textures, animations, replacements, modelComponent.Skeleton);
                            // AddGUID(existingModels, modelComponent?.Model, textures, animations, replacements, modelComponent.Skeleton);
                            existingModels = FindModels(existingModels, modelComponent?.Material, replacements);  // get all referenced models
                        }
                        animationBank?.Clear(); // todo: yes?
                        if (component.GetType() == typeof(STUStatescriptSubreferenceComponent)) {  // 003 sub-reference
                            STUStatescriptSubreferenceComponent sub003 = component as STUStatescriptSubreferenceComponent;
                            existingModels = FindModels(existingModels, sub003?.GUIDx003, replacements);
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
                            // todo: assumed: next component is model
                            if (ssAnims?.Animations == null) continue;
                            // HashSet<AnimationInfo> animations = new HashSet<AnimationInfo>();
                            foreach (STUAnimationCoreferenceComponentAnimation ssAnimsFB16 in ssAnims.Animations) {
                                // animations = Animation.FindAnimations(animations, ssAnimsFB16.Animation, replacements);
                                animationBank = Animation.FindAnimations(animationBank, existingModels, ssAnimsFB16.Animation, replacements);
                            }
                        }
                    }
                    if (container.SubModels != null) {
                        foreach (STUSubModelReferenceComponent subModelReference in container.SubModels) {
                            existingModels = FindModels(existingModels, subModelReference?.GUIDx003, replacements);
                        }
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
                    break;
                default:
                    Debugger.Log(0, "DataTool.FindLogic.Model", $"[DataTool.FindLogic.Model] Unhandled type: {GUID.Type(modelGUID):X3}\n");
                    break;
            }
            return existingModels;
        }
    }
}