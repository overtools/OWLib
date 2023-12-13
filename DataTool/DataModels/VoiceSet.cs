using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using TankLib;
using TankLib.STU;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;

namespace DataTool.DataModels {
    public class VoiceSet {
        public Dictionary<ulong, VoiceLineInstance> VoiceLines { get; set; }
        public Dictionary<ulong, HashSet<ulong>> Stimuli { get; set; }

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
            if (voiceSet == null || voiceSet.m_voiceLineInstances == null) return;
            VoiceLines = new Dictionary<ulong, VoiceLineInstance>(voiceSet.m_voiceLineInstances.Length);
            Stimuli = new Dictionary<ulong, HashSet<ulong>>();

            for (int i = 0; i < voiceSet.m_voiceLineInstances.Length; i++) {
                STUVoiceLineInstance instance = voiceSet.m_voiceLineInstances[i];
                ulong voiceLineGuid = instance.GetVoiceLineGUID();

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

    public class VoiceLineInstance {
        [CanBeNull] public teResourceGUID[] VoiceSounds { get; set; }
        [CanBeNull] public teResourceGUID[] Conversations { get; set; }

        [CanBeNull] internal STUCriteriaContainer Conditions;
        internal STUVoiceLineInstance STU;

        public VoiceLineInstance(STUVoiceLineInstance instance) {
            STU = instance;

            if (instance.m_AF226247 != null) {
                var voiceSounds = new List<teResourceGUID>();
                foreach (var soundFile in new[] {
                    instance.m_AF226247.m_1485B834, instance.m_AF226247.m_798027DE,
                    instance.m_AF226247.m_A84AA2B5, instance.m_AF226247.m_D872E45C
                }) {
                    if (soundFile != null) {
                        voiceSounds.Add(soundFile.m_3C099E86);
                    }
                }

                VoiceSounds = voiceSounds.ToArray();
            }

            if (instance.m_voiceLineRuntime != null) {
                if (instance.m_voiceLineRuntime.m_BD1B6F64 != null)
                    Conversations = instance.m_voiceLineRuntime.m_BD1B6F64.Select(x => x.GUID).ToArray();

                if (instance.m_voiceLineRuntime.m_criteria != null) {
                    Conditions = instance.m_voiceLineRuntime.m_criteria;
                }
            }
        }
    }
}