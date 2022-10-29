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

        [DllImport("Ole32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int CoInitializeEx([In, Optional] IntPtr pvReserved, [In] CoInit dwCoInit);

        public static bool Initialized { get; private set; }

        public static void Initialize() {
            if (Initialized) {
                return;
            }

            CoInitializeEx(IntPtr.Zero, CoInit.MultiThreaded | CoInit.SpeedOverMemory);
            Initialized = true;
        }


        public static unsafe Memory<byte> ConvertDDS(Stream ddsSteam, DXGI_FORMAT targetFormat, WICCodecs codec, int? frameNr) {
            Initialize();

            Memory<byte> data = new byte[ddsSteam.Length];
            ddsSteam.Read(data.Span);
            ScratchImage scratch = null;
            try {
                using var dataPin = data.Pin();
                scratch = TexHelper.Instance.LoadFromDDSMemory((IntPtr) dataPin.Pointer, data.Length, DDS_FLAGS.NONE);
                TexMetadata info = scratch.GetMetadata();

                var isMultiFrame = codec == WICCodecs.TIFF;
                if (frameNr != null) {
                    isMultiFrame = false;
                }

                var frame = frameNr ?? 0;

                if (TexHelper.Instance.IsCompressed(info.Format)) {
                    var temp = scratch.Decompress(DXGI_FORMAT.UNKNOWN);

                    scratch.Dispose();
                    scratch = temp;

                    info = scratch.GetMetadata();
                }

                if (targetFormat == DXGI_FORMAT.UNKNOWN) {
                    targetFormat = TexHelper.Instance.BitsPerColor(info.Format) <= 8 ? TexHelper.Instance.IsSRGB(info.Format) ? DXGI_FORMAT.R8G8B8A8_UNORM_SRGB : DXGI_FORMAT.R8G8B8A8_UNORM : DXGI_FORMAT.R16G16B16A16_UNORM;
                }

                if (info.Format != targetFormat) {
                    ScratchImage temp = scratch.Convert(targetFormat, TEX_FILTER_FLAGS.DEFAULT, 0.5f);
                    scratch.Dispose();
                    scratch = temp;
                }

                if (codec > 0) {
                    UnmanagedMemoryStream stream = null;
                    try {
                        if (info.ArraySize == 1 || !isMultiFrame) {
                            stream = scratch.SaveToWICMemory(frame * info.MipLevels, WIC_FLAGS.NONE, TexHelper.Instance.GetWICCodec(codec));
                        } else {
                            stream = scratch.SaveToWICMemory(frame * info.MipLevels,   (frame - info.ArraySize) * info.MipLevels, WIC_FLAGS.ALL_FRAMES, TexHelper.Instance.GetWICCodec(codec));
                        }

                        if (stream == null) {
                            throw new InvalidDataException();
                        }

                        byte[] tex = new byte[stream.Length];
                        stream.Read(tex, 0, tex.Length);
                        scratch.Dispose();
                        return tex;
                    } finally {
                        stream?.Dispose();
                    }
                } else { // save as raw RGBA
                    var image = scratch.GetImage(frame * info.MipLevels);
                    Memory<byte> tex = new byte[image.Width * image.Height * 4];
                    using (var pinned = tex.Pin()) {
                        Buffer.MemoryCopy((void*) image.Pixels, pinned.Pointer, tex.Length, tex.Length);
                    }
                    scratch.Dispose();
                    return tex;
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