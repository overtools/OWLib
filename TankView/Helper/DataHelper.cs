using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using DataTool.DataModels;
using DataTool.DataModels.Hero;
using DataTool.DataModels.Voice;
using DataTool.Helper;
using DirectXTexNet;
using TankLib;
using TankLib.STU.Types;
using TankView.ViewModel;

namespace TankView.Helper {
    public static class DataHelper {
        public enum DataType {
            Unknown,
            Image,
            Sound,
            Model,
            String,
            MapHeader,
            Conversation,
            Hero
        };

        public static ulong? GetGuid(object value) {
            if (value is teResourceGUID teGuid)
                return teGuid;

            if (value is GUIDEntry geGuid)
                return geGuid;

            return null;
        }

        public static DataType GetDataType(GUIDEntry value) {
            if (value == null || value.GUID == 0) {
                return DataType.Unknown;
            }

            ushort type = teResourceGUID.Type(value.GUID);
            return GetDataType(type);
        }

        public static DataType GetDataType(ulong? guid) {
            if (guid == null || guid == 0) {
                return DataType.Unknown;
            }

            ushort type = teResourceGUID.Type(guid.Value);
            return GetDataType(type);
        }

        public static DataType GetDataType(ushort type) {
            switch (type) {
                case 0x004:
                case 0x0F1:
                    return DataType.Image;
                case 0x03F:
                case 0x0B2:
                case 0x0BB:
                    return DataType.Sound;
                case 0x07C:
                case 0x0A9:
                case 0x071:
                    return DataType.String;
                case 0x09F:
                    return DataType.MapHeader;
                case 0x00C:
                    return DataType.Model;
                case 0x075:
                    return DataType.Hero;
                case 0x0D0:
                    return DataType.Conversation;
                default:
                    return DataType.Unknown;
            }
        }

        internal static object ConvertSound(ulong value) {
            MemoryStream ms = new MemoryStream();
            try {
                DataTool.SaveLogic.Combo.ConvertSoundFile(IOHelper.OpenFile(value), ms);
            } catch (Exception ex) {
                Debugger.Log(0, "[TankView.DataHelper.ConvertSound]", $"Error converting sound! {ex.Message}\n");
                // ignored
            }

            ms.Position = 0;
            return ms;
        }

        public static Memory<byte> ConvertDDS(ulong guid, DXGI_FORMAT targetFormat, WICCodecs imageFormat, out int width, out int height) {
            width = 0;
            height = 0;

            try {
                if (GetDataType(guid) != DataType.Image) {
                    return default;
                }

                teTexture texture = LoadTexture(guid);
                if(texture == null || texture.Header.Width == 0 || texture.Header.Height == 0) {
                    return default;
                }
                width = texture.Header.Width;
                height = texture.Header.Height;
                Stream ms = texture.SaveToDDS(1);

                return DDSConverter.ConvertDDS(ms, targetFormat, imageFormat, texture.Header.Surfaces - 1);
            } catch {
                // ignored
            }

            return default;
        }

        public static void SaveImage(GUIDEntry value, Stream fileStream, Stream outStream) {
            if (GetDataType(value) != DataType.Image) {
                return;
            }

            teTexture texture = LoadTexture(value.GUID, fileStream);
            texture.SaveToDDS(outStream, false, 1);
        }

        internal static teTexture LoadTexture(ulong guid, Stream fileStream = null) {
            teTexture texture = new teTexture(fileStream ?? IOHelper.OpenFile(guid));
            if (texture.PayloadRequired) {
                ulong payload = texture.GetPayloadGUID(guid, 0);
                if (IOHelper.HasFile(payload)) {
                    texture.LoadPayload(IOHelper.OpenFile(payload), 0);
                } else {
                    return null;
                }
            }

            return texture;
        }

        internal static object GetString(GUIDEntry value) {
            if (teResourceGUID.Type(value.GUID) == 0x071) {
                return GetSubtitle(value);
            }

            try {
                teString str = new teString(IOHelper.OpenFile(value));
                return str.Value;
            } catch {
                return string.Empty;
            }
        }

        internal static MapHeader GetMap(GUIDEntry value) {
            if (value == null || value.GUID == 0)
                return null;

            return new MapHeader(value.GUID);
        }

        internal static Hero GetHero(GUIDEntry value) {
            if (value == null || value.GUID == 0)
                return null;

            return new Hero(value.GUID);
        }

        internal static Conversation GetConversation(GUIDEntry value) {
            if (value == null || value.GUID == 0)
                return null;

            return new Conversation(value);
        }

        private static object GetSubtitle(GUIDEntry value) {
            var subtitle = new teSubtitleThing(IOHelper.OpenFile(value));
            return string.Join("\n", subtitle.m_strings);
        }

        internal static Dictionary<ulong, ulong[]> GenerateVoicelineConversationMapping(Dictionary<ushort, HashSet<ulong>> trackedFiles, ProgressWorker worker) {
            var @return = new Dictionary<ulong, ulong[]>();
            var size = trackedFiles[0x5F].Count;
            var i = 0;
            foreach (var guid in trackedFiles[0x5F]) {
                i++;
                worker?.ReportProgress((int) (((float) i / (float) size) * 100));
                var voiceSet = new VoiceSet(STUHelper.GetInstance<STUVoiceSet>(guid));
                if (voiceSet.VoiceLines == null) continue;

                foreach (var voiceSetVoiceLine in voiceSet.VoiceLines) {
                    @return[voiceSetVoiceLine.Key] = voiceSetVoiceLine.Value.VoiceSounds;
                }
            }

            return @return;
        }

        internal static Dictionary<ulong, string> GenerateVoicelineSubtitleMapping(Dictionary<ushort, HashSet<ulong>> trackedFiles, ProgressWorker worker) {
            var @return = new Dictionary<ulong, string>();
            var size = trackedFiles[0x5F].Count;
            var i = 0;
            foreach (ulong guid in trackedFiles[0x5F]) {
                i++;
                worker?.ReportProgress((int) (((float) i / (float) size) * 100));

                var voiceSet = STUHelper.GetInstance<STUVoiceSet>(guid);
                if (voiceSet?.m_voiceLineInstances == null) {
                    continue;
                }

                foreach (var instance in voiceSet.m_voiceLineInstances) {
                    if (instance.m_AF226247 == null) continue;

                    var subtitle = IO.GetSubtitleString(instance.m_43C90056);
                    if (string.IsNullOrEmpty(subtitle)) continue;

                    if (!string.IsNullOrEmpty(subtitle)) {
                        foreach (var soundFile in new[] {
                            instance.m_AF226247.m_1485B834, instance.m_AF226247.m_798027DE,
                            instance.m_AF226247.m_A84AA2B5, instance.m_AF226247.m_D872E45C
                        }) {
                            if (soundFile != null) {
                                @return[soundFile.m_3C099E86] = subtitle;
                            }
                        }
                    }
                }
            }

            return @return;
        }
    }
}
