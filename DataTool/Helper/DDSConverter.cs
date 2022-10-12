using System;
using System.IO;
using System.Runtime.InteropServices;
using DirectXTexNet;

namespace DataTool.Helper {
    public static class DDSConverter {
        [Flags]
        public enum CoInit : uint {
            MultiThreaded = 0x00,
            ApartmentThreaded = 0x02,
            DisableOLE1DDE = 0x04,
            SpeedOverMemory = 0x08
        }

        public enum Codec {
            // abstract formats
            RAW,
            // WIC formats
            TIFF,
            PNG,
            JPEG,
            // other formats
            TGA,
            HDR,
        }

        [DllImport("Ole32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int CoInitializeEx([In, Optional] IntPtr pvReserved, [In] CoInit dwCoInit);


        public static unsafe Memory<byte> ConvertDDS(Stream ddsSteam, DXGI_FORMAT targetFormat, Codec codec, int? frameNr) {
            CoInitializeEx(IntPtr.Zero, CoInit.MultiThreaded | CoInit.SpeedOverMemory);

            Memory<byte> data = new byte[ddsSteam.Length];
            ddsSteam.Read(data.Span);
            ScratchImage scratch = null;
            try {
                using var dataPin = data.Pin();
                scratch = TexHelper.Instance.LoadFromDDSMemory((IntPtr) dataPin.Pointer, data.Length, DDS_FLAGS.NONE);
                TexMetadata info = scratch.GetMetadata();

                var isMultiFrame = codec == Codec.TIFF;
                if (frameNr != null) {
                    isMultiFrame = false;
                }

                var frame = frameNr ?? 0;

                if (TexHelper.Instance.IsCompressed(info.Format)) {
                    ScratchImage temp;
                    try {
                        temp = scratch.Decompress(targetFormat);
                    } catch {
                        temp = scratch.Decompress(DXGI_FORMAT.UNKNOWN);
                    }

                    scratch.Dispose();
                    scratch = temp;

                    info = scratch.GetMetadata();
                }

                if (info.Format != targetFormat) {
                    ScratchImage temp = scratch.Convert(targetFormat, TEX_FILTER_FLAGS.DEFAULT, 0.5f);
                    scratch.Dispose();
                    scratch = temp;
                }

                switch (codec) {
                    case Codec.RAW: {
                        var image = scratch.GetImage(0);
                        Memory<byte> tex = new byte[image.SlicePitch];
                        using (var pinned = tex.Pin()) {
                            Buffer.MemoryCopy((void*) image.Pixels, pinned.Pointer, tex.Length, tex.Length);
                        }
                        scratch.Dispose();
                        return tex;
                    }

                    case Codec.TGA: {
                        using var stream = scratch.SaveToTGAMemory(frame);
                        Memory<byte> tex = new byte[stream.Length];
                        stream.Read(tex.Span);
                        scratch.Dispose();
                        return tex;
                    }

                    case Codec.HDR: {
                        using var stream = scratch.SaveToHDRMemory(frame);
                        Memory<byte> tex = new byte[stream.Length];
                        stream.Read(tex.Span);
                        scratch.Dispose();
                        return tex;
                    }

                    default: {
                        var wic = codec switch {
                            Codec.TIFF => WICCodecs.TIFF,
                            Codec.PNG => WICCodecs.PNG,
                            Codec.JPEG => WICCodecs.JPEG,
                            _ => WICCodecs.TIFF
                        };
                        UnmanagedMemoryStream stream;
                        if (info.ArraySize == 1 || !isMultiFrame) {
                            stream = scratch.SaveToWICMemory(frame, WIC_FLAGS.NONE, TexHelper.Instance.GetWICCodec(wic));
                        } else {
                            stream = scratch.SaveToWICMemory(0, info.ArraySize, WIC_FLAGS.ALL_FRAMES, TexHelper.Instance.GetWICCodec(wic));
                        }
                        Memory<byte> tex = new byte[stream.Length];
                        stream.Read(tex.Span);
                        scratch.Dispose();
                        return tex;
                    }
                }
            }  catch {
                // ignored
            } finally {
                if (scratch != null && scratch.IsDisposed == false) {
                    scratch.Dispose();
                }
            }

            return default;
        }
    }
}