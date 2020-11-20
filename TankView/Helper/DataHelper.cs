using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using DataTool.DataModels;
using DataTool.DataModels.Hero;
using DataTool.Helper;
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
            String,
            MapHeader,
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

            if (type == 0x09F) {
                return DataType.MapHeader;
            }
            
            if (type == 0x075) {
                return DataType.Hero;
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

        public static byte[] ConvertDDS(ulong guid, DXGI_FORMAT targetFormat, System.Drawing.Imaging.ImageFormat imageFormat, int frame) {
            try {
                if (GetDataType(guid) != DataType.Image) {
                    return null;
                }

                teTexture texture = LoadTexture(guid);
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
        
        private static object GetSubtitle(GUIDEntry value) {
            var subtitle = new teSubtitleThing(IOHelper.OpenFile(value));
            return string.Join("\n", subtitle.m_strings);
        }
    }
}
