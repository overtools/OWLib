using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Controls;
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

        public PreviewConversation(GUIDEntry guidEntry, Conversation conversation, Lazy<Dictionary<ulong, ulong[]>> conversationVoiceLineMapping) {
            GUIDEntry = guidEntry;

            VoiceLines = conversation.Voicelines
                                         .Select(voiceline => new TankViewConversationLine(voiceline, conversationVoiceLineMapping.Value[voiceline.VoicelineGUID]))
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
                }
                
                NotifyPropertyChanged(nameof(SelectedItem));
            }
        }
        
        
        public Voiceline SelectedVoiceLineItem {
            get => _selectedVoiceline;
            set {
                _selectedVoiceline = value;

                if (value == null) return;

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


        public class TankViewConversationLine : ConversationLine {
            public Voiceline[] Voicelines { get; set; }

            public TankViewConversationLine(ConversationLine line, ulong[] voicelines) : base(line) {
                Voicelines = voicelines?.Select(x => new Voiceline {
                    GUID = x
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
