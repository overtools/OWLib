using System;
using System.IO;
using System.Runtime.InteropServices;
using DirectXTexNet;

namespace DataTool.ConvertLogic {
    public sealed class DDSConverter : IDisposable {
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

        public ScratchImage Image { get; set; }
        public TexMetadata Info { get; set; }

        public unsafe DDSConverter(Stream ddsSteam, DXGI_FORMAT targetFormat, bool force8bpc) {
            Initialize();

            Memory<byte> data = new byte[ddsSteam.Length];
            var offset = 0;
            while (offset < data.Length) {
                offset += ddsSteam.Read(data.Span[offset..]);
            }

            try {
                using var dataPin = data.Pin();
                Image = TexHelper.Instance.LoadFromDDSMemory((IntPtr) dataPin.Pointer, data.Length, DDS_FLAGS.NONE);
                Info = Image.GetMetadata();


                if (TexHelper.Instance.IsCompressed(Info.Format)) {
                    var temp = Image.Decompress(DXGI_FORMAT.UNKNOWN);

                    Image.Dispose();
                    Image = temp;

                    Info = Image.GetMetadata();
                }

                if (targetFormat == DXGI_FORMAT.UNKNOWN) {
                    targetFormat = force8bpc || TexHelper.Instance.BitsPerColor(Info.Format) <= 8 ? TexHelper.Instance.IsSRGB(Info.Format) ? DXGI_FORMAT.R8G8B8A8_UNORM_SRGB : DXGI_FORMAT.R8G8B8A8_UNORM : DXGI_FORMAT.R16G16B16A16_UNORM;
                }

                if (Info.Format != targetFormat) {
                    ScratchImage temp = Image.Convert(targetFormat, TEX_FILTER_FLAGS.DEFAULT, 0.5f);
                    Image.Dispose();
                    Image = temp;

                    Info = Image.GetMetadata();
                }
            } catch {
                if (Image is { IsDisposed: false }) {
                    Image.Dispose();
                }
                throw;
            }
        }

        public unsafe Stream GetFrame(WICCodecs codec, int frame, int count) {
            if (count < 1) {
                count = 1;
            }

            if (codec > 0) {
                UnmanagedMemoryStream stream = null;
                try {
                    count = codec == WICCodecs.TIFF ? count : 1;

                    if(count + frame > Info.ArraySize) {
                        throw new ArgumentOutOfRangeException(nameof(frame));
                    }

                    if (Info.ArraySize == 1 || count == 1) {
                        stream = Image.SaveToWICMemory(frame * Info.MipLevels, WIC_FLAGS.NONE, TexHelper.Instance.GetWICCodec(codec));
                    } else {
                        stream = Image.SaveToWICMemory(frame * Info.MipLevels, count * Info.MipLevels, WIC_FLAGS.NONE, TexHelper.Instance.GetWICCodec(codec));
                    }

                    if (stream == null) {
                        throw new InvalidDataException();
                    }

                    return stream;
                } catch {
                    stream?.Dispose();
                    throw;
                }
            }

            // save as raw RGBA
            if (count > 1) {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            var image = Image.GetImage(frame * Info.MipLevels);
            return new UnmanagedMemoryStream((byte*) image.Pixels, image.Width * image.Height * (TexHelper.Instance.BitsPerPixel(Info.Format) / 8));
        }

        public void Dispose() {
            if (Image is { IsDisposed: false }) {
                Image.Dispose();
            }

            Image = default;
            Info = default;
        }
    }
}