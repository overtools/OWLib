using System.Collections.Generic;
using System.Diagnostics;
using Newtonsoft.Json;
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels {
    [JsonObject(MemberSerialization.OptOut)]
    public class VoiceSet {
        public Dictionary<ulong, VoiceLineInstance> VoiceLines;

        public Dictionary<ulong, HashSet<ulong>> Stimuli;

        public VoiceSet(STUHero hero) {
            STUVoiceSetComponent voiceSetComponent = GetInstance<STUVoiceSetComponent>(hero.m_gameplayEntity);

            if (voiceSetComponent?.m_voiceDefinition == null) {
                Debugger.Log(0, "DataTool.DataModels.VoiceSet", "Hero VoiceSet not found");
                return;
            }
            STUVoiceSet set = GetInstance<STUVoiceSet>(voiceSetComponent.m_voiceDefinition);
            if (set == null) return;
            
            Init(set);
        }

        public VoiceSet(STUVoiceSet voiceSet) {
            Init(voiceSet);
        }

        private void Init(STUVoiceSet voiceSet) {
            if (voiceSet.m_voiceLineInstances == null) return;
            VoiceLines = new Dictionary<ulong, VoiceLineInstance>(voiceSet.m_voiceLineInstances.Length);
            Stimuli = new Dictionary<ulong, HashSet<ulong>>();

            for (int i = 0; i < voiceSet.m_voiceLineInstances.Length; i++) {
                ulong voiceLineGuid = voiceSet.m_voiceLineGuids[i];
                STUVoiceLineInstance instance = voiceSet.m_voiceLineInstances[i];
                
                VoiceLineInstance instanceModel = new VoiceLineInstance(instance);

                VoiceLines[voiceLineGuid] = instanceModel;

                if (instance.m_voiceLineRuntime != null) {
                    ulong stimuli = instance.m_voiceLineRuntime.m_stimulus;
                    if (stimuli != 0) {
                        if (!Stimuli.ContainsKey(stimuli)) {
                            Stimuli[stimuli] = new HashSet<ulong>();
                        }
                        Stimuli[stimuli].Add(voiceLineGuid);
                    }
                }
            }
        }
    }

    [JsonObject(MemberSerialization.OptOut)]
    public class VoiceLineInstance {
        public List<ulong> VoiceSounds;
        public teResourceGUID VoiceConversation;

        [JsonIgnore]
        public STUVoiceLineInstance STU;  // todo: ideally not here
        
        // todo: more fields and stuff.
        
        public VoiceLineInstance(STUVoiceLineInstance instance) {
            STU = instance;
            
            if (instance.m_AF226247 != null) {
                VoiceSounds = new List<ulong>();
                foreach (var soundFile in new[] {
                    instance.m_AF226247.m_1485B834, instance.m_AF226247.m_798027DE,
                    instance.m_AF226247.m_A84AA2B5, instance.m_AF226247.m_D872E45C
                }) {
                    if (soundFile != null) {
                        VoiceSounds.Add(soundFile.m_3C099E86);
                    }
                }
            }

            if (instance.m_voiceLineRuntime != null) {
                VoiceConversation = instance.m_voiceLineRuntime.m_voiceConversation;
            }
        }
    }
}