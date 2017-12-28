using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using OWLib;
using OWLib.Types.Chunk;

namespace DataTool.Helper {
    public class EffectParser {
        public Chunked Chunk;
        public ulong GUID;

        public EffectParser(Chunked chunked, ulong guid) {
            Chunk = chunked;
            GUID = guid;
        }

        public class ChunkPlaybackTimeInfo : IEquatable<ChunkPlaybackTimeInfo> {
            public float StartTime;
            public float EndTime;

            public bool Equals(ChunkPlaybackTimeInfo other) {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return StartTime.Equals(other.StartTime) && EndTime.Equals(other.EndTime);
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((ChunkPlaybackTimeInfo) obj);
            }

            public override int GetHashCode() {
                unchecked {
                    return (StartTime.GetHashCode() * 397) ^ EndTime.GetHashCode();
                }
            }
        }

        public class ChunkPlaybackInfo : IEquatable<ChunkPlaybackInfo> {
            public ChunkPlaybackTimeInfo TimeInfo;
            public ulong Hardpoint;
            public IChunk PreviousChunk;

            public ChunkPlaybackInfo(PMCEInfo component, IChunk previousChunk) {
                if (component != null) {
                    TimeInfo = new ChunkPlaybackTimeInfo {StartTime = component.StartTime, EndTime = component.EndTime};
                    Hardpoint = component.Hardpoint;
                } else {
                    Hardpoint = 0;
                }
                
                PreviousChunk = previousChunk;
            }

            public bool Equals(ChunkPlaybackInfo other) {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return Equals(TimeInfo, other.TimeInfo) && Hardpoint == other.Hardpoint && Equals(PreviousChunk, other.PreviousChunk);
            }

            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != GetType()) return false;
                return Equals((ChunkPlaybackInfo) obj);
            }

            public override int GetHashCode() {
                unchecked {
                    int hashCode = (TimeInfo != null ? TimeInfo.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ Hardpoint.GetHashCode();
                    hashCode = (hashCode * 397) ^ (PreviousChunk != null ? PreviousChunk.GetHashCode() : 0);
                    return hashCode;
                }
            }
        }

        public EffectInfo ProcessAll(Dictionary<ulong, ulong> replacements) {
            EffectInfo ret = new EffectInfo {GUID = GUID};
            ret.SetupEffect();
            foreach (KeyValuePair<ChunkPlaybackInfo,IChunk> chunk in GetChunks()) {
                Process(ret, chunk, replacements);
            }
            return ret;
        }

        public IEnumerable<KeyValuePair<ChunkPlaybackInfo, IChunk>> GetChunks() {
            PMCEInfo lastComponent = null;
            IChunk lastChunk = null;
            // TCFE effect = Chunk.GetAllOfType<TCFE>().First().Value;
            for (int i = 0; i < Chunk.Chunks.Count; i++) {
                if (Chunk?.Chunks[i]?.GetType() == typeof(EffectChunkComponent)) {
                    EffectChunkComponent pmce = Chunk.Chunks[i] as EffectChunkComponent;
                    if (pmce == null) continue;
                    lastComponent = new PMCEInfo {Hardpoint = pmce.Data.Hardpoint, StartTime = pmce.StartTime, EndTime = pmce.EndTime};
                    // if (effect.Hardpoints == null) continue;
                    // if (effect.Hardpoints.Length <= pmce.Data.Index) continue;
                    continue;
                }

                ChunkPlaybackInfo playbackInfo = new ChunkPlaybackInfo(lastComponent, lastChunk);

                yield return new KeyValuePair<ChunkPlaybackInfo, IChunk>(playbackInfo, Chunk.Chunks[i]);

                lastComponent = null;
                lastChunk = Chunk.Chunks[i];
            }
        }

        public static void AddDMCE(EffectInfo effect, DMCE dmce, ChunkPlaybackInfo playbackInfo, Dictionary<ulong, ulong> replacements) {
            DMCEInfo newInfo = new DMCEInfo {
                Model = dmce.Data.Model,
                Material = dmce.Data.Look,
                Animation = dmce.Data.Animation,
                PlaybackInfo = playbackInfo
            };
            if (replacements.ContainsKey(newInfo.Model)) newInfo.Model = replacements[newInfo.Model];
            if (replacements.ContainsKey(newInfo.Material)) newInfo.Material = replacements[newInfo.Material];
            if (replacements.ContainsKey(newInfo.Animation)) newInfo.Animation = replacements[newInfo.Animation];
            effect.DMCEs.Add(newInfo);
        }

        public static void AddCECE(EffectInfo effect, CECE cece, ChunkPlaybackInfo playbackInfo, Dictionary<ulong, ulong> replacements) {
            CECEInfo newInfo = new CECEInfo {
                Animation = cece.Data.Animation,
                PlaybackInfo = playbackInfo,
                Action = cece.Data.Action,
                EntityVariable = cece.Data.EntityVariable
            };
            if (replacements.ContainsKey(newInfo.Animation)) newInfo.Animation = replacements[newInfo.Animation];
            if (replacements.ContainsKey(newInfo.EntityVariable)) newInfo.EntityVariable = replacements[newInfo.EntityVariable];
            effect.CECEs.Add(newInfo);
        }

        public static void AddOSCE(EffectInfo effect, OSCE osce, ChunkPlaybackInfo playbackInfo, Dictionary<ulong, ulong> replacements) {
            OSCEInfo newInfo = new OSCEInfo {PlaybackInfo = playbackInfo, Sound = osce.Data.Sound};
            if (replacements.ContainsKey(newInfo.Sound)) newInfo.Sound = replacements[newInfo.Sound];
            effect.OSCEs.Add(newInfo);
        }
        
        public static void AddFECE(EffectInfo effect, ulong guid, EffectInfo subEffect, ChunkPlaybackInfo playbackInfo, Dictionary<ulong, ulong> replacements) {
            FECEInfo newInfo = new FECEInfo {PlaybackInfo = playbackInfo, Effect = subEffect, GUID=guid};
            if (replacements.ContainsKey(newInfo.GUID)) newInfo.GUID = replacements[newInfo.GUID];
            effect.FECEs.Add(newInfo);
        }
        
        public static void AddNECE(EffectInfo effect, NECE nece, ChunkPlaybackInfo playbackInfo, Dictionary<ulong, ulong> replacements) {
            NECEInfo newInfo = new NECEInfo {PlaybackInfo = playbackInfo, GUID = nece.Data.Entity, Variable = nece.Data.EntityVariable};
            if (replacements.ContainsKey(newInfo.GUID)) newInfo.GUID = replacements[newInfo.GUID];
            if (replacements.ContainsKey(newInfo.Variable)) newInfo.Variable = replacements[newInfo.Variable];
            effect.NECEs.Add(newInfo);
        }
        
        public static void AddRPCE(EffectInfo effect, RPCE rpce, ChunkPlaybackInfo playbackInfo, Dictionary<ulong, ulong> replacements) {
            RPCEInfo newInfo = new RPCEInfo {PlaybackInfo = playbackInfo, Model = rpce.Data.Model};
            if (replacements.ContainsKey(newInfo.Model)) newInfo.Model = replacements[newInfo.Model];
            effect.RPCEs.Add(newInfo);
        }

        public static void AddSSCE(EffectInfo effectInfo, SSCE ssce, Type lastType, Dictionary<ulong, ulong> replacements) {
            ulong def = ssce.Data.TextureDefinition;
            ulong mat = ssce.Data.Material;
            if (replacements.ContainsKey(def)) def = replacements[def];
            if (replacements.ContainsKey(mat)) mat = replacements[mat];
            if (lastType == typeof(RPCE)) {
                RPCEInfo rpceInfo = effectInfo.RPCEs.Last();
                rpceInfo.TextureDefiniton = def;
                rpceInfo.Material = mat;
            }
        }
        
        public static void AddSVCE(EffectInfo effect, SVCE svce, ChunkPlaybackInfo playbackInfo, Dictionary<ulong, ulong> replacements) {
            SVCEInfo newInfo = new SVCEInfo {PlaybackInfo = playbackInfo, VoiceStimulus = svce.Data.VoiceStimulus};
            if (replacements.ContainsKey(newInfo.VoiceStimulus)) newInfo.VoiceStimulus = replacements[newInfo.VoiceStimulus];
            effect.SVCEs.Add(newInfo);
        }

        public void Process(EffectInfo effectInfo, KeyValuePair<ChunkPlaybackInfo, IChunk> chunk, Dictionary<ulong, ulong> replacements) {
            // todo: STUVoiceStimulus has f3099f20/m_volume
            // probably more stuff too
            
            
            // hey have some notes about particles:
            // 000000003CEC.006 - 000000001D3D.08F = ana - guardian:
            //     one RPCE, 61 chunks
            //     seems to be at correct position with rpce at rot: x=90
            
            // 000000003796.006 - 000000001A31.08F = genji - warrior's salute:
            //     one RPCE, 64 chunks.
            
            // VCCE might be a texture/material transform
            // A B C D = R G B A
            // see 'extract-debug-vcce'
            
            
            if (effectInfo == null) return;
            if (chunk.Value == null) return;
            if (replacements == null) replacements = new Dictionary<ulong, ulong>();
            if (chunk.Value.GetType() == typeof(TCFE)) {
                TCFE tcfe = chunk.Value as TCFE;
                if (tcfe == null) return; 
                effectInfo.EffectLength = tcfe.Data.EndTime1;
            }
            if (chunk.Value.GetType() == typeof(DMCE)) {
                DMCE dmce = chunk.Value as DMCE;
                if (dmce == null) return;
                AddDMCE(effectInfo, dmce, chunk.Key, replacements);
            }
            if (chunk.Value.GetType() == typeof(CECE)) {
                CECE cece = chunk.Value as CECE;
                if (cece == null) return;
                AddCECE(effectInfo, cece, chunk.Key, replacements);
            }
            if (chunk.Value.GetType() == typeof(OSCE)) {
                OSCE osce = chunk.Value as OSCE;
                if (osce == null) return;
                AddOSCE(effectInfo, osce, chunk.Key, replacements);
            }
            if (chunk.Value.GetType() == typeof(FECE)) {
                FECE fece = chunk.Value as FECE;
                if (fece == null) return;
                EffectInfo feceInfo = null;
                ulong effectKey = fece.Data.Effect;
                if (replacements.ContainsKey(fece.Data.Effect)) effectKey = replacements[fece.Data.Effect];
                using (Stream feceStream = IO.OpenFile(effectKey)) {
                    if (feceStream != null) {
                        using (Chunked feceChunkednew = new Chunked(feceStream)) {
                            EffectParser sub = new EffectParser(feceChunkednew, fece.Data.Effect);
                            feceInfo = sub.ProcessAll(replacements);
                        }
                    }
                }
                
                AddFECE(effectInfo, fece.Data.Effect, feceInfo, chunk.Key, replacements);
            }
            if (chunk.Value.GetType() == typeof(NECE)) {
                NECE nece = chunk.Value as NECE;
                if (nece == null) return;
                AddNECE(effectInfo, nece, chunk.Key, replacements);
            }
            if (chunk.Value.GetType() == typeof(RPCE)) {
                RPCE rpce = chunk.Value as RPCE;
                if (rpce == null) return;
                AddRPCE(effectInfo, rpce, chunk.Key, replacements);
            }
            if (chunk.Value.GetType() == typeof(SSCE)) {
                SSCE ssce = chunk.Value as SSCE;
                if (ssce == null) return;

                AddSSCE(effectInfo, ssce, chunk.Key.PreviousChunk?.GetType(), replacements);
            }
            if (chunk.Value.GetType() == typeof(SVCE)) {
                SVCE svce = chunk.Value as SVCE;
                if (svce == null) return;

                AddSVCE(effectInfo, svce, chunk.Key, replacements);
            }
        }

        public class EffectChunkInfo {
            public ChunkPlaybackInfo PlaybackInfo;
        }

        public class DMCEInfo : EffectChunkInfo {
            public ulong Model;
            public ulong Material;
            public ulong Animation;
        }

        public class CECEInfo : EffectChunkInfo {
            public ulong Animation;
            public ulong EntityVariable;
            public CECE.CECEAction Action;
        }

        public class OSCEInfo : EffectChunkInfo {
            public ulong Sound;
        }
        
        public class FECEInfo : EffectChunkInfo {
            public EffectInfo Effect;
            public ulong GUID;
        }
        
        public class NECEInfo : EffectChunkInfo {
            public ulong GUID;
            public ulong Variable;
        }

        public class RPCEInfo : EffectChunkInfo {
            public ulong Model;

            public ulong Material;
            public ulong TextureDefiniton;
        }
        
        public class SVCEInfo : EffectChunkInfo {
            public ulong VoiceStimulus;
        }
        
        public class PMCEInfo : EffectChunkInfo {
            public float StartTime;
            public float EndTime;
            public ulong Hardpoint;
        }

        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public class EffectInfo {
            public List<DMCEInfo> DMCEs;
            public List<CECEInfo> CECEs;
            public List<OSCEInfo> OSCEs;
            public List<FECEInfo> FECEs;
            public List<NECEInfo> NECEs;
            public List<RPCEInfo> RPCEs;
            public List<SVCEInfo> SVCEs;
            public ulong GUID;

            public ulong SoundMaster; // 05F for VoiceStimuli

            public float EffectLength; // seconds
            
            // todo: many more chunks
            // todo: OSCE / 02C is controlled by the bnk?

            public void SetupEffect() {
                DMCEs = new List<DMCEInfo>();
                CECEs = new List<CECEInfo>();
                OSCEs = new List<OSCEInfo>();
                NECEs = new List<NECEInfo>();
                FECEs = new List<FECEInfo>();
                RPCEs = new List<RPCEInfo>();
                SVCEs = new List<SVCEInfo>();
            }
        }
    }
}