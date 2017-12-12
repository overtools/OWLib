using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DataTool.Helper;
using OWLib;
using OWLib.Types.Chunk;
using STULib.Types;
using STULib.Types.AnimationList.x020;
using STULib.Types.AnimationList.x021;
using STULib.Types.Generic;
using STULib.Types.STUUnlock;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.FindLogic {
    
    
    public class AnimationInfo : EffectParser.EffectInfo, IEquatable<AnimationInfo> {
        public Common.STUGUID Skeleton;
        public string Name;

        public float FPS = -1;
        public uint Priority = 0;
        
        public override int GetHashCode() {
            return (GUID != null ? GUID.GetHashCode() : 0);
        }

        public bool Equals(AnimationInfo other) {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(GUID, other.GUID);
        }

        public override bool Equals(object obj) {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((AnimationInfo) obj);
        }
    }
    
    public class Animation {
        public static AnimationInfo GetAnimationInfo(HashSet<AnimationInfo> animations, ulong animation, Dictionary<ulong, ulong> replacements) {
            if (replacements.ContainsKey(animation)) animation = replacements[animation];
            AnimationInfo animInfo = animations.FirstOrDefault(x => x.GUID == animation);
            return animInfo;
        }

        public static void SetName(HashSet<AnimationInfo> animations, ulong anim, string name,
            Dictionary<ulong, ulong> reaplacements) {
            if (reaplacements.ContainsKey(anim)) anim = reaplacements[anim];

            AnimationInfo info = animations.FirstOrDefault(x => x.GUID == anim);
            if (info == null) return;

            info.Name = name;
        }
        
        public static void AddGUID(HashSet<AnimationInfo> animations, Common.STUGUID newElement, Common.STUGUID skeleton, Dictionary<ulong, ulong> replacements) {
            if (newElement == null) return;
            
            if (replacements.ContainsKey(newElement)) newElement = new Common.STUGUID(replacements[newElement]);
            AnimationInfo newAnim = new AnimationInfo {GUID = newElement, Skeleton = skeleton};
            newAnim.SetupEffect();

            if (animations.All(x => !Equals(x.GUID, newAnim.GUID))) {
                animations.Add(newAnim);
            } else {
                AnimationInfo existing = animations.FirstOrDefault(x => Equals(x, newAnim));
                if (existing == null) return;
                if (existing.Skeleton == 0) existing.Skeleton = newAnim.Skeleton;
            }
        }

        private static void SetAnimFramerate(HashSet<AnimationInfo> existingAnimations, Common.STUGUID animationGUID, Dictionary<ulong, ulong> replacements, float framerate, uint priority) {
            if (replacements.ContainsKey(animationGUID)) animationGUID = new Common.STUGUID(replacements[animationGUID]);
            
            AnimationInfo animInfo = existingAnimations.FirstOrDefault(x => x.GUID == animationGUID);
            if (animInfo == null) return;

            animInfo.FPS = framerate;
            animInfo.Priority = priority;
        }

        public static HashSet<AnimationInfo> FindChunked(HashSet<AnimationInfo> existingAnimations, HashSet<ModelInfo> models, Common.STUGUID animationGUID,
            Dictionary<ulong, ulong> replacements, ulong parentAnim) {
            if (existingAnimations == null) {
                existingAnimations = new HashSet<AnimationInfo>();
            }

            if (replacements == null) replacements = new Dictionary<ulong, ulong>();
            if (animationGUID == null) return existingAnimations;
            if (replacements.ContainsKey(animationGUID)) animationGUID = new Common.STUGUID(replacements[animationGUID]);

            using (Stream chunkStream = OpenFile(animationGUID)) {
                if (chunkStream == null) {
                    return existingAnimations;
                }
                Chunked chunked = new Chunked(chunkStream, true, ChunkManager.Instance);
                
                // if (GetFileName(parentAnim) == "000000004239.006") Debugger.Break(); // orisa supercharger main
                // if (GetFileName(parentAnim) == "0000000045D6.006") Debugger.Break(); // doomfist one punch - 000000002206.08F
                
                EffectParser parser = new EffectParser(chunked, animationGUID);

                AnimationInfo info = GetAnimationInfo(existingAnimations, parentAnim, replacements);

                ulong lastModel = 0;

                foreach (KeyValuePair<EffectParser.ChunkPlaybackInfo,IChunk> chunk in parser.GetChunks()) {
                    if (chunk.Value == null || chunk.Value.GetType() == typeof(MemoryChunk)) continue;

                    parser.Process(info, chunk);

                    if (chunk.Value.GetType() == typeof(DMCE)) {
                        DMCE dmce = chunk.Value as DMCE;
                        if (dmce == null) continue;
                        HashSet<AnimationInfo> newAnims = new HashSet<AnimationInfo>();
                        Model.FindModels(models, new Common.STUGUID(dmce.Data.Model), replacements);
                        Model.FindModels(models, new Common.STUGUID(dmce.Data.Look), replacements);
                    
                        Dictionary<ulong, List<TextureInfo>> textures = new Dictionary<ulong, List<TextureInfo>>();
                        textures = Texture.FindTextures(textures, new Common.STUGUID(dmce.Data.Look), null, true, replacements);
                    
                        newAnims = FindAnimations(newAnims, models, new Common.STUGUID(dmce.Data.Animation), replacements);
                    
                        Model.AddGUID(models, new Common.STUGUID(dmce.Data.Model), textures, newAnims, replacements);
                    }

                    if (chunk.Value.GetType() == typeof(FECE)) {
                        FECE fece = chunk.Value as FECE;
                        if (fece == null) continue;
                        HashSet<AnimationInfo> fakeAnims = new HashSet<AnimationInfo>();
                        FindChunked(fakeAnims, models, new Common.STUGUID(fece.Data.Effect), replacements, 0);
                    }

                    if (chunk.Value.GetType() == typeof(NECE)) {
                        NECE nece = chunk.Value as NECE;
                        if (nece == null) continue;
                        models = Model.FindModels(models, new Common.STUGUID(nece.Data.key), replacements);
                    }
                    
                    if (chunk.Value.GetType() == typeof(SSCE)) {
                        SSCE ssce = chunk.Value as SSCE;
                        if (ssce == null) continue;
                        Dictionary<ulong, List<TextureInfo>> textures = new Dictionary<ulong, List<TextureInfo>>();
                        textures = Texture.FindTextures(textures, new Common.STUGUID(ssce.Data.material_key), null, true, replacements, GUID.Index(ssce.Data.material_key));
                        if (lastModel != 0) {
                            Model.AddGUID(models, new Common.STUGUID(lastModel), textures, null, replacements);
                        }
                    }

                    if (chunk.Value.GetType() == typeof(RPCE)) {
                        RPCE rpce = chunk.Value as RPCE;
                        if (rpce == null) continue;
                        models = Model.FindModels(models, new Common.STUGUID(rpce.Data.Model), replacements);
                        lastModel = rpce.Data.Model;
                    } else {
                        lastModel = 0;
                    }

                    if (chunk.Value.GetType() == typeof(CECE)) {
                        CECE cece = chunk.Value as CECE;
                        if (cece == null) continue;
                        if (cece.Data.Animation == 0 || cece.Data.EntityVariable == 0) continue;
                        HashSet<AnimationInfo> newAnims = new HashSet<AnimationInfo>();
                        newAnims = FindAnimations(newAnims, models, new Common.STUGUID(cece.Data.Animation), replacements);
                        
                        // if (GUID.Index(cece.Data.Animation) == 0x371B) Debugger.Break();
                        
                        // nooooooooooooooo
                        foreach (ModelInfo model in models) {
                            foreach (KeyValuePair<Common.STUGUID,EntityInfo> entity in model.Entities) {
                                foreach (ChildEntityReference entityReference in entity.Value.Children) {
                                    if (entityReference.Variable == cece.Data.EntityVariable) {
                                        Model.AddGUID(models, new Common.STUGUID(entityReference.Model), new Dictionary<ulong, List<TextureInfo>>(), newAnims, replacements);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return existingAnimations;
        }

        public static HashSet<AnimationInfo> FindAnimations(HashSet<AnimationInfo> existingAnimations, HashSet<ModelInfo> models,
            STUAnimationListSecondaryContainer container, Dictionary<ulong, ulong> replacements = null) {
            
            if (container.GetType() == typeof(STUAnimationListSecondardAnimationContainerA)) {
                STUAnimationListSecondardAnimationContainerA animationContainerA =
                    container as STUAnimationListSecondardAnimationContainerA;
                existingAnimations = FindAnimations(existingAnimations, models, animationContainerA?.AnimationWrapper?.Animation, replacements);
            }
            if (container.GetType() == typeof(STUAnimationListSecondardAnimationContainerB)) {
                STUAnimationListSecondardAnimationContainerB animationContainerB =
                    container as STUAnimationListSecondardAnimationContainerB;
                existingAnimations = FindAnimations(existingAnimations, models, animationContainerB?.AnimationWrapper?.Animation, replacements);
            }
            if (container.GetType() == typeof(STU_BB7A7240)) {
                STU_BB7A7240 listBB7A = container as STU_BB7A7240;
                foreach (STU_74173BA8 listBB7Asub in listBB7A?.m_134EE5BB) {
                    if (listBB7Asub?.m_AF632ACD != null) {
                        existingAnimations = FindAnimations(existingAnimations, models, listBB7Asub.m_AF632ACD, replacements);
                    }
                    // now that I know what this field is called, the recursion issues make sense
                    // if (listBB7Asub?.ParentNode != null) {
                    //     if (listBB7Asub?.ParentNode.GetType() == typeof(STU_BB7A7240)) {
                    //         
                    //     }
                    // }
                    if (listBB7A.m_0DE1BA16 != null) {
                        foreach (STU_40274C18 listBB7A_4027 in listBB7A.m_0DE1BA16) {
                            existingAnimations = FindAnimations(existingAnimations, models, listBB7A_4027.m_AF632ACD);
                        }
                    }
                }
            }
            
            return existingAnimations;
        }

        public static HashSet<AnimationInfo> FindAnimations(HashSet<AnimationInfo> existingAnimations, HashSet<ModelInfo> models, Common.STUGUID animationGUID, Dictionary<ulong, ulong> replacements=null, Common.STUGUID skeleton=null) {
            if (existingAnimations == null) {
                existingAnimations = new HashSet<AnimationInfo>();
            }

            if (replacements == null) replacements = new Dictionary<ulong, ulong>();
            if (animationGUID == null) return existingAnimations;
            if (replacements.ContainsKey(animationGUID)) animationGUID = new Common.STUGUID(replacements[animationGUID]);

            switch (GUID.Type(animationGUID)) {
                case 0x06:
                    AddGUID(existingAnimations, animationGUID, skeleton, replacements);
                    // if (animationGUID.ToString() == "00000000265C.006") Debugger.Break();
                    using (Stream anim = OpenFile(animationGUID)) {
                        if (anim == null) {
                            break;
                        }
                        using (BinaryReader reader = new BinaryReader(anim)) {

                            anim.Position = 0;
                            uint priority = reader.ReadUInt32();
                            anim.Position = 8;
                            float fps = reader.ReadSingle();
                            SetAnimFramerate(existingAnimations, animationGUID, replacements, fps, priority);
                            anim.Position = 0x18L;
                            ulong infokey = reader.ReadUInt64();
                            existingAnimations = FindChunked(existingAnimations, models, new Common.STUGUID(infokey), replacements, animationGUID);
                        }
                    }
                    break;
                case 0x21:
                    STUAnimationListInfo listInfo = GetInstance<STUAnimationListInfo>(animationGUID);
                    foreach (STUAnimationListInfoSub listInfoSubInfo in listInfo.SubInfos) {
                        existingAnimations = FindAnimations(existingAnimations, models, listInfoSubInfo?.SecondaryList,
                            replacements);
                        if (listInfoSubInfo?.AnimationContainer?.Animations != null) {
                            foreach (STUAnimationListAnimationWrapper listAnimationWrapper in listInfoSubInfo.AnimationContainer.Animations) {
                                existingAnimations = FindAnimations(existingAnimations, models, listAnimationWrapper?.Animation,
                                    replacements, listInfoSubInfo.Skeleton);  // todo: is main skeleton?
                            }
                        }
                        if (listInfoSubInfo?.m_9AD6CC25 != null) {
                            if (listInfoSubInfo.m_9AD6CC25.GetType() == typeof(STU_7D00A73D)) {
                                STU_7D00A73D infosub7D00Converted = listInfoSubInfo.m_9AD6CC25 as STU_7D00A73D;
                                if (infosub7D00Converted?.m_083DC038 != null) {
                                    foreach (STU_65DD9C84 sub7D00Sub in infosub7D00Converted.m_083DC038) {
                                        existingAnimations = FindAnimations(existingAnimations, models, sub7D00Sub?.Animation,
                                            replacements, listInfoSubInfo.Skeleton);  // todo: is main skeleton?
                                    }
                                }
                                
                            }
                        }
                        if (listInfoSubInfo?.m_560940DC?.m_6CB79D25 != null) {
                            foreach (STU_BE20B7F5 subBE20 in listInfoSubInfo.m_560940DC.m_6CB79D25) {
                                existingAnimations = FindAnimations(existingAnimations, models, subBE20?.Animation,
                                    replacements, listInfoSubInfo.Skeleton);  // todo: is main skeleton?
                            }
                        }
                    }
                    // if (!Equals(listInfo.SecondaryList, animationGUID)) {
                    //     existingAnimations = FindAnimations(existingAnimations, listInfo.SecondaryList, replacements);
                    // }
                    foreach (Common.STUGUID listInfoReference in listInfo.References) {
                        // if (GUID.Type(listInfoReference) != 0x21) continue;
                        existingAnimations = FindAnimations(existingAnimations, models, listInfoReference, replacements);
                    }
                    STUAnimationListAnimationWrapper[] wrappers =
                        GetAllInstances<STUAnimationListAnimationWrapper>(animationGUID);
                    foreach (STUAnimationListAnimationWrapper animationWrapper in wrappers) {
                        existingAnimations =
                            FindAnimations(existingAnimations, models, animationWrapper?.Animation, replacements);
                    }
                    break;
                case 0x20:
                    STUAnimationListAnimationWrapper[] wrappers2 =
                        GetAllInstances<STUAnimationListAnimationWrapper>(animationGUID);
                    foreach (STUAnimationListAnimationWrapper animationWrapper in wrappers2) {
                        existingAnimations =
                            FindAnimations(existingAnimations, models, animationWrapper?.Animation, replacements);
                    }
                    // STUAnimationListSecondary listSecondary = GetInstance<STUAnimationListSecondary>(animationGUID);
                    // if (listSecondary == null) break;
                    // foreach (STUAnimationListSecondaryContainer listSecondaryContainer in listSecondary.Containers) {
                    //     existingAnimations = FindAnimations(existingAnimations, listSecondaryContainer, replacements);
                    // }
                    // STU_FEB7DB23 listWeird = GetInstance<STU_FEB7DB23>(animationGUID);  // idk what this is but it has animations so I don't care
                    // if (listWeird != null) {
                    //     existingAnimations = FindAnimations(existingAnimations, listWeird.AnimationWrapper?.Animation, replacements);
                    // }
                    break;
                case 0xA5:
                    Cosmetic cosmetic = GetInstance<Cosmetic>(animationGUID);
                    if (cosmetic is Emote) {
                        Emote cosmeticEmote = cosmetic as Emote;
                        existingAnimations = FindAnimations(existingAnimations, models, cosmeticEmote.AnimationList, replacements);
                    } else if (cosmetic is Pose) {
                        Pose cosmeticPose = cosmetic as Pose;
                        existingAnimations = FindAnimations(existingAnimations, models, cosmeticPose.PoseResource, replacements);
                    } else if (cosmetic is HighlightIntro) {
                        HighlightIntro cosmeticHighlightIntro = cosmetic as HighlightIntro;
                        existingAnimations = FindAnimations(existingAnimations, models, cosmeticHighlightIntro.AnimationResource, replacements);
                        SetName(existingAnimations, cosmeticHighlightIntro.AnimationResource, $"HighlightIntro\\{GetString(cosmeticHighlightIntro.CosmeticName)}", replacements);
                    }
                    break;
                case 0xBF:
                    STUPose pose = GetInstance<STUPose>(animationGUID);
                    existingAnimations = FindAnimations(existingAnimations, models, pose.Animation, replacements);
                    foreach (STUPoseSub poseSub in new [] {pose.Sub1, pose.Sub2, pose.Sub3}) {
                        existingAnimations = FindAnimations(existingAnimations, models, poseSub.Animation, replacements);
                    }
                    break;
                default:
                    Debugger.Log(0, "DataTool.FindLogic.Animation", $"[DataTool.FindLogic.Animation] Unhandled type: {GUID.Type(animationGUID):X3}\n");
                    break;
            }
            return existingAnimations;
        }
    }
}