using System;
using System.IO;
using DirectXTexNet;
using static DataTool.WPF.IO.Native;

namespace DataTool.WPF.IO {
    public static class DDSConverter {
        public enum ImageFormat {
            TGA,
            PNG,
            TIF,
            GIF,
            JPEG,
            BMP
        }
        
        public static unsafe byte[] ConvertDDS(Stream ddsSteam, DXGI_FORMAT targetFormat, ImageFormat imageFormat, int frame) {
            try {
                CoInitializeEx(IntPtr.Zero, CoInit.MultiThreaded | CoInit.SpeedOverMemory);

                byte[] data = new byte[ddsSteam.Length];
                ddsSteam.Read(data, 0, data.Length);
                ScratchImage scratch = null;
                try {
                    fixed (byte* dataPin = data) {
                        scratch = TexHelper.Instance.LoadFromDDSMemory((IntPtr) dataPin, data.Length, DDS_FLAGS.NONE);
                        TexMetadata info = scratch.GetMetadata();
                        if (TexHelper.Instance.IsCompressed(info.Format)) {
                            ScratchImage temp = scratch.Decompress(frame, DXGI_FORMAT.UNKNOWN);
                            scratch.Dispose();
                            scratch = temp;
                        }

                        info = scratch.GetMetadata();

                        if (info.Format != targetFormat) {
                            ScratchImage temp = scratch.Convert(targetFormat, TEX_FILTER_FLAGS.DEFAULT, 0.5f);
                            scratch.Dispose();
                            scratch = temp;
                        }

                        UnmanagedMemoryStream stream = null;
                        if (imageFormat == ImageFormat.TGA) {
                            stream = scratch.SaveToTGAMemory(frame < 0 ? 0 : frame);
                        } else {
                            WICCodecs codec = WICCodecs.PNG;
                            bool isMultiFrame = false;
                            switch (imageFormat) {
                                case ImageFormat.BMP:
                                    codec = WICCodecs.BMP;
                                    break;
                                case ImageFormat.GIF:
                                    codec = WICCodecs.GIF;
                                    isMultiFrame = true;
                                    break;
                                case ImageFormat.JPEG:
                                    codec = WICCodecs.JPEG;
                                    break;
                                case ImageFormat.PNG:
                                    codec = WICCodecs.PNG;
                                    break;
                                case ImageFormat.TIF:
                                    codec = WICCodecs.TIFF;
                                    isMultiFrame = true;
                                    break;
                                case ImageFormat.TGA:
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException(nameof(imageFormat), imageFormat, null);
                            }

                            if (frame < 0) {
                                if (!isMultiFrame) {
                                    frame = 0;
                                } else {
                                    stream = scratch.SaveToWICMemory(0, info.ArraySize, WIC_FLAGS.ALL_FRAMES, TexHelper.Instance.GetWICCodec(codec));
                                }
                            }

                            if (frame >= 0) {
                                stream = scratch.SaveToWICMemory(frame, WIC_FLAGS.NONE, TexHelper.Instance.GetWICCodec(codec));
                            }
                        }

                        if (stream == null) {
                            return null;
                        }

                        byte[] tex = new byte[stream.Length];
                        stream.Read(tex, 0, tex.Length);
                        scratch.Dispose();
                        return tex;
                    }
                } catch {
                    if (scratch != null && scratch.IsDisposed == false) {
                        scratch.Dispose();
                    }
                }
            } catch {
                // ignored
            }

            return null;
        }
    }
}
