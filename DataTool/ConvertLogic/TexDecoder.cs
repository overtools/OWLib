using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using AssetRipper.TextureDecoder.Bc;
using AssetRipper.TextureDecoder.Rgb;
using AssetRipper.TextureDecoder.Rgb.Formats;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TankLib;

namespace DataTool.ConvertLogic;

public class TexDecoder {
    internal struct GrayscaleR32<T>(T r) : IColor<ColorR<T>, T> where T : unmanaged, INumberBase<T>, IMinMaxValue<T> {
        public override string ToString() {
            return $"{{ R: {R} }}";
        }

        public static bool HasRedChannel => true;
        public static bool HasGreenChannel => true;
        public static bool HasBlueChannel => true;
        public static bool HasAlphaChannel => true;
        public static bool ChannelsAreFullyUtilized => true;
        public static Type ChannelType => typeof(T);

        public readonly void GetChannels(out T r, out T g, out T b, out T a) {
            r = g = b = R;
            a = A;
        }

        public void SetChannels(T r, T g, T b, T a) {
            R = G = B = r;
            A = a;
        }

        public T R { get; set; } = r;
        public T B { get; set; } = r;
        public T G { get; set; } = r;
        public T A { get; set; } = r;

        public static ColorR<T> Black => new(T.MinValue);

        public static ColorR<T> White => new(T.MaxValue);
    }

    public readonly byte[] PixelData;
    public uint Surfaces { get; set; }
    private teTexture Texture { get; set; }

    private readonly int BytesPerOutputSurface;

    public TexDecoder(teTexture texture, bool grayscale) {
        Texture = texture;
        Surfaces = texture.Header.Surfaces;

        var format = (TextureTypes.DXGI_PIXEL_FORMAT) texture.Header.Format;
        var inputData = Texture.GetData();
        var bytesPerInputSurface = (int) (inputData.Length / Surfaces);

        // data is:
        // slice0mip0
        // slice0mip1
        // slice1mip0
        // slice1mip1
        // etc
        // so bytesPerInputSurface is sneakily extracting the slice pitch
        // data for lower mips is simply skipped over

        BytesPerOutputSurface = Texture.Header.Width * Texture.Header.Height * Unsafe.SizeOf<Rgba32>();
        PixelData = new byte[BytesPerOutputSurface * Surfaces];

        for (var surface = 0; surface < Surfaces; ++surface) {
            var surfaceInputData = inputData.Slice(bytesPerInputSurface * surface, bytesPerInputSurface);
            var surfaceOutputData = PixelData.AsSpan(BytesPerOutputSurface * surface, BytesPerOutputSurface);
            switch (format) {
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT: {
                    RgbConverter.Convert<ColorRGBA<float>, float, ColorRGBA<byte>, byte>(surfaceInputData, Texture.Header.Width, Texture.Header.Height, surfaceOutputData);
                    break;
                }
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT: {
                    RgbConverter.Convert<ColorRGBA<Half>, Half, ColorRGBA<byte>, byte>(surfaceInputData, Texture.Header.Width, Texture.Header.Height, surfaceOutputData);
                    break;
                }
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R16G16_FLOAT: {
                    RgbConverter.Convert<ColorRG<Half>, Half, ColorRGBA<byte>, byte>(surfaceInputData, Texture.Header.Width, Texture.Header.Height, surfaceOutputData);
                    break;
                }
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R16_FLOAT: {
                    RgbConverter.Convert<ColorR<Half>, Half, ColorRGBA<byte>, byte>(surfaceInputData, Texture.Header.Width, Texture.Header.Height, surfaceOutputData);
                    break;
                }
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R16G16B16A16_UNORM:
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R16G16B16A16_SNORM:
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R16G16B16A16_UINT:
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R16G16B16A16_SINT: {
                    RgbConverter.Convert<ColorRGBA<ushort>, ushort, ColorRGBA<byte>, byte>(surfaceInputData, Texture.Header.Width, Texture.Header.Height, surfaceOutputData);
                    break;
                }
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM:
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM_SRGB:
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8G8B8A8_UINT:
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8G8B8A8_SINT: {
                    RgbConverter.Convert<ColorRGBA<byte>, byte, ColorRGBA<byte>, byte>(surfaceInputData, Texture.Header.Width, Texture.Header.Height, surfaceOutputData);
                    break;
                }
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8G8_UNORM:
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8G8_UINT:
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8G8_SINT: {
                    RgbConverter.Convert<ColorRG<byte>, byte, ColorRGBA<byte>, byte>(surfaceInputData, Texture.Header.Width, Texture.Header.Height, surfaceOutputData);
                    break;
                }
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8_UNORM:
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8_SNORM:
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8_UINT:
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8_SINT: {
                    RgbConverter.Convert<ColorR<byte>, byte, ColorRGBA<byte>, byte>(surfaceInputData, Texture.Header.Width, Texture.Header.Height, surfaceOutputData);
                    break;
                }
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_BC1_UNORM:
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB: {
                    Bc1.Decompress<ColorRGBA<byte>, byte>(surfaceInputData, texture.Header.Width, texture.Header.Height, surfaceOutputData);
                    break;
                }
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_BC2_UNORM:
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_BC2_UNORM_SRGB: {
                    Bc2.Decompress<ColorRGBA<byte>, byte>(surfaceInputData, texture.Header.Width, texture.Header.Height, surfaceOutputData);
                    break;
                }
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_BC3_UNORM:
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_BC3_UNORM_SRGB: {
                    Bc3.Decompress<ColorRGBA<byte>, byte>(surfaceInputData, texture.Header.Width, texture.Header.Height, surfaceOutputData);
                    break;
                }
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_BC4_UNORM: {
                    Bc4.Decompress<ColorRGBA<byte>, byte>(surfaceInputData, texture.Header.Width, texture.Header.Height, surfaceOutputData);
                    break;
                }
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_BC5_UNORM: {
                    Bc5.Decompress<ColorRGBA<byte>, byte>(surfaceInputData, texture.Header.Width, texture.Header.Height, surfaceOutputData);
                    break;
                }
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_BC6H_UF16:
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_BC6H_SF16: {
                    Bc6h.Decompress<ColorRGBA<byte>, byte>(surfaceInputData, texture.Header.Width, texture.Header.Height, format is TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_BC6H_SF16, surfaceOutputData);
                    break;
                }
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_BC7_UNORM:
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_BC7_UNORM_SRGB: {
                    Bc7.Decompress<ColorRGBA<byte>, byte>(surfaceInputData, texture.Header.Width, texture.Header.Height, surfaceOutputData);
                    break;
                }
                default:
                    throw new NotImplementedException($"Unsupported format {format}");
            }

            switch (format) {
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R16_FLOAT:
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8_UNORM:
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8_SNORM:
                //case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8_UINT:
                //case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8_SINT:
                case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_BC4_UNORM: {
                    if (grayscale) {
                        RgbConverter.Convert<ColorRGBA<byte>, byte, GrayscaleR32<byte>, byte>(surfaceOutputData, Texture.Header.Width, Texture.Header.Height, surfaceOutputData);
                    }
                    break;
                }
            }
        }
    }

    public Image<Rgba32> GetSheet() {
        return Image.LoadPixelData<Rgba32>(PixelData, Texture.Header.Width, (int) (Texture.Header.Height * Surfaces));
    }

    public Image<Rgba32> GetFrame(int frame) {
        if (frame >= Surfaces) throw new ArgumentOutOfRangeException(nameof(frame));

        return Image.LoadPixelData<Rgba32>(PixelData.AsSpan(BytesPerOutputSurface * frame, BytesPerOutputSurface), Texture.Header.Width, Texture.Header.Height);
    }

    public Image<Rgba32> GetFrames() {
        var image = new Image<Rgba32>(Texture.Header.Width, Texture.Header.Height);
        for (int i = 0; i < Surfaces; i++) {
            var img = GetFrame(i);
            image.Frames.AddFrame(img!.Frames.RootFrame);
        }

        image.Frames.RemoveFrame(0); // root is garbage :3
        return image;
    }
}