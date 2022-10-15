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


        public static unsafe Memory<byte> ConvertDDS(Stream ddsSteam, DXGI_FORMAT targetFormat, WICCodecs codec, int? frameNr) {
            CoInitializeEx(IntPtr.Zero, CoInit.MultiThreaded | CoInit.SpeedOverMemory);

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

                // trust me, this is the only way to do this
                // we basically have to pray that the image is decompressed into a format that the target format supports
                // this is because png will automatically switch between sRGB and Linear, but TIF will always use linear
                // so if we always convert it to a linear target type, PNG loses accuracy.
                if (info.Format != targetFormat && targetFormat != DXGI_FORMAT.UNKNOWN) {
                    ScratchImage temp = scratch.Convert(targetFormat, TEX_FILTER_FLAGS.DEFAULT, 0.5f);
                    scratch.Dispose();
                    scratch = temp;
                }

                if (codec > 0) {
                    return SaveWIC(codec, info, isMultiFrame, scratch);
                } else { // save as raw RGBA
                    var image = scratch.GetImage(0);
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

        private static Memory<byte> SaveWIC(WICCodecs codec, TexMetadata info, bool isMultiFrame, ScratchImage scratch, bool didConvert = false) {
            UnmanagedMemoryStream stream = null;
            // this is fucked beyond belief lol
            try {
                if (info.ArraySize == 1 || !isMultiFrame) {
                    stream = scratch.SaveToWICMemory(0, WIC_FLAGS.NONE, TexHelper.Instance.GetWICCodec(codec));
                } else {
                    stream = scratch.SaveToWICMemory(0, info.ArraySize, WIC_FLAGS.ALL_FRAMES, TexHelper.Instance.GetWICCodec(codec));
                }

                if (stream == null) {
                    throw new InvalidDataException();
                }

                byte[] tex = new byte[stream.Length];
                stream.Read(tex, 0, tex.Length);
                scratch.Dispose();
                return tex;
            } catch {
                // if it crashes then the wic codec doesn't support the format, we gotta convert it to the proper format
                // this is a hacky way to do it, but it works
                // we keep bit depth and sRGB flag the same, but add extra channels
                if (!didConvert) {
                    // if bits < 8 then { if srgb then DXGI_FORMAT_R8G8B8A8_UNORM_SRGB else DXGI_FORMAT_R8G8B8A8_UNORM } else { DXGI_FORMAT_R16G16B16A16_UNORM }
                    ScratchImage temp = scratch.Convert(TexHelper.Instance.BitsPerColor(info.Format) <= 8 ? TexHelper.Instance.IsSRGB(info.Format) ? DXGI_FORMAT.R8G8B8A8_UNORM_SRGB : DXGI_FORMAT.R8G8B8A8_UNORM : DXGI_FORMAT.R16G16B16A16_UNORM, TEX_FILTER_FLAGS.DEFAULT, 0.5f);
                    scratch.Dispose();
                    scratch = temp;
                    return SaveWIC(codec, info, isMultiFrame, scratch, true);
                } else {
                    throw;
                }
            } finally {
                stream?.Dispose();
            }
        }
    }
}