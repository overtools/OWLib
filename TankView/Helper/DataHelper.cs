using System;
using System.Diagnostics;
using System.IO;
using DataTool.WPF.IO;
using DirectXTexNet;
using TankLib;
using TankView.ViewModel;

namespace TankView.Helper {
    public static class DataHelper {
        public enum DataType {
            Unknown,
            Image,
            Sound,
            Model,
            String
        };

        public static DataType GetDataType(GUIDEntry value) {
            if (value == null || value.GUID == 0) {
                return DataType.Unknown;
            }

            ushort type = teResourceGUID.Type(value.GUID);
            return GetDataType(type);
        }

        public static DataType GetDataType(ushort type) {
            if (type == 0x004 || type == 0x0F1) {
                return DataType.Image;
            }

            if (type == 0x03F || type == 0x0B2 || type == 0x0BB) {
                return DataType.Sound;
            }

            if (type == 0x00C) {
                return DataType.Model;
            }

            if (type == 0x07C || type == 0x0A9 || type == 0x071) {
                return DataType.String;
            }

            return DataType.Unknown;
        }

        internal static object ConvertSound(GUIDEntry value) {
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

        public static byte[] ConvertDDS(GUIDEntry value, DXGI_FORMAT targetFormat, DDSConverter.ImageFormat imageFormat, int frame) {
            try {
                if (GetDataType(value) != DataType.Image) {
                    return null;
                }

                teTexture texture = LoadTexture(value);
                Stream ms = texture.SaveToDDS(1);

                return DDSConverter.ConvertDDS(ms, targetFormat, imageFormat, frame);
            } catch {
                // ignored
            }

            return null;
        }

        public static void SaveImage(GUIDEntry value, Stream fileStream, Stream outStream) {
            if (GetDataType(value) != DataType.Image) {
                return;
            }
            
            teTexture texture = LoadTexture(value, fileStream);
            texture.SaveToDDS(outStream, false, 1);
        }

        internal static teTexture LoadTexture(GUIDEntry value, Stream fileStream = null) {
            teTexture texture = new teTexture(fileStream ?? IOHelper.OpenFile(value));
            if (texture.PayloadRequired) {
                ulong payload = texture.GetPayloadGUID(value.GUID, 0);
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
        
        private static object GetSubtitle(GUIDEntry value) {
            var subtitle = new teSubtitleThing(IOHelper.OpenFile(value));
            return string.Join("\n", subtitle.m_strings);
        }
    }
}
