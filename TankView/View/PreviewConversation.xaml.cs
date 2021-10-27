using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using DataTool.DataModels.Voice;
using TankLib;
using TankView.Helper;
using TankView.ViewModel;
using TankView.Properties;

namespace TankView.View {
    public partial class PreviewConversation : INotifyPropertyChanged, IDisposable {
        public GUIDEntry GUIDEntry { get; set; }
        public TankViewConversationLine[] VoiceLines { get; set; }
        public PreviewDataSound SoundPreviewControl { get; set; } = new PreviewDataSound();

        public PreviewConversation(GUIDEntry guidEntry, Conversation conversation, Dictionary<ulong, ulong[]> conversationVoiceLineMapping) {
            GUIDEntry = guidEntry;

            VoiceLines = conversation.Voicelines
                .Select(voiceline => new TankViewConversationLine(voiceline, conversationVoiceLineMapping))
                .OrderBy(x => x.Position)
                .ToArray();

            InitializeComponent();
        }

        private TankViewConversationLine _selected;
        private Voiceline _selectedVoiceline;

        public TankViewConversationLine SelectedItem {
            get => _selected;
            set {
                _selected = value;

                if (value == null) return;

                if (value.Voicelines?.Length == 1) {
                    PlayAudio(value.Voicelines[0].GUID);
                } else if (value.Voicelines?.Length == 0) {
                    SoundPreviewControl.SetAudioError("Error: This sound does not exist?");
                } else if (value.Voicelines?.Length > 1) {
                    SoundPreviewControl.SetAudioError("Error: Sound contains multiple voicelines??!");
                } else {
                    SoundPreviewControl.SetAudioError("Error: Unable to play this sound");
                }

                NotifyPropertyChanged(nameof(SelectedItem));
            }
        }


        public Voiceline SelectedVoiceLineItem {
            get => _selectedVoiceline;
            set {
                _selectedVoiceline = value;

                if (value == null) {
                    return;
                }

                PlayAudio(value.GUID);
                NotifyPropertyChanged(nameof(SelectedVoiceLineItem));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void PlayAudio(ulong guid) {
            try {
                var sound = DataHelper.ConvertSound(guid);
                SoundPreviewControl.SetAudio((Stream) sound);

                if (Settings.Default.AutoPlay) {
                    SoundPreviewControl.Play(null, null);
                }

                NotifyPropertyChanged(nameof(SoundPreviewControl));
            } catch {
                Debugger.Break();
            }
        }


        public class TankViewConversationLine {
            public Voiceline[] Voicelines { get; set; }
            public ulong Position { get; set; }
            public teResourceGUID GUID { get; set; }
            public teResourceGUID VoicelineGUID { get; set; }
            public string Subtitle { get; set; }

            public TankViewConversationLine(ConversationLine line, IReadOnlyDictionary<ulong, ulong[]> voicelineMapping) {
                voicelineMapping.TryGetValue(line.VoicelineGUID, out var voicelines);
                GUID = line.GUID;
                VoicelineGUID = line.VoicelineGUID;
                Position = line.Position;
                // conversations can technically contain multiple lines? we dont support this but it's pretty uncommon
                Subtitle = GUIDCollection.VoicelineSubtitleMapping[voicelines?.FirstOrDefault() ?? 0];
                Voicelines = (voicelines ?? Array.Empty<ulong>()).Select(x => new Voiceline {
                    GUID = x,
                }).ToArray();
            }
        }

        public class Voiceline {
            public ulong GUID { get; set; }
            public string GUIDString => teResourceGUID.AsString(GUID);
        }

        public void Dispose() {
            SoundPreviewControl?.Dispose();
        }
    }
}