using System;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using AssetRipper.TextureDecoder.Bc;
using AssetRipper.TextureDecoder.Rgb;
using JetBrains.Annotations;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using TankLib;

namespace DataTool.ConvertLogic {
    public class TexDecoder {
        public Memory<byte> PixelData { get; set; }
        public int Pixels { get; set; }
        public uint Surfaces { get; set; }
        public teTexture Texture { get; set; }

        public TexDecoder(teTexture texture) {
            Texture = texture;
            Pixels = Texture.Header.Width * Texture.Header.Height * 4;
            Surfaces = texture.Header.Surfaces;

            var format = (TextureTypes.DXGI_PIXEL_FORMAT) texture.Header.Format;
            var allData = Texture.GetData();
            PixelData = new byte[Pixels * Surfaces * 4].AsMemory();
            var size = (int) (allData.Length / Surfaces);

            for (var surface = 0; surface < Surfaces; ++surface) {
                var data = allData.Slice(size * surface, size);
                var interm = PixelData.Slice(Pixels * surface, Pixels).Span;
                switch (format) {
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT: {
                        RgbConverter.RGBAFloatToBGRA32(data, Texture.Header.Width, Texture.Header.Height, interm);
                        break;
                    }
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT: {
                        RgbConverter.RGBAHalfToBGRA32(data, Texture.Header.Width, Texture.Header.Height, interm);
                        break;
                    }
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R16G16_FLOAT: {
                        RgbConverter.RGHalfToBGRA32(data, Texture.Header.Width, Texture.Header.Height, interm);
                        break;
                    }
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R16_FLOAT: {
                        RgbConverter.RHalfToBGRA32(data, Texture.Header.Width, Texture.Header.Height, interm);
                        break;
                    }
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R16G16B16A16_UNORM:
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R16G16B16A16_SNORM:
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R16G16B16A16_UINT:
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R16G16B16A16_SINT: {
                        RgbConverter.RGBA64ToBGRA32(data, Texture.Header.Width, Texture.Header.Height, interm);
                        break;
                    }
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM:
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8G8B8A8_UNORM_SRGB:
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8G8B8A8_UINT:
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8G8B8A8_SINT: {
                        RgbConverter.RGBA32ToBGRA32(data, Texture.Header.Width, Texture.Header.Height, interm);
                        break;
                    }
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8G8_UNORM:
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8G8_UINT:
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8G8_SINT: {
                        RgbConverter.RG16ToBGRA32(data, Texture.Header.Width, Texture.Header.Height, interm);
                        break;
                    }
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8_UNORM:
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8_SNORM:
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8_UINT:
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8_SINT: {
                        RgbConverter.R8ToBGRA32(data, Texture.Header.Width, Texture.Header.Height, interm);
                        break;
                    }
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_BC1_UNORM:
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_BC1_UNORM_SRGB: {
                        BcDecoder.DecompressBC1(data, texture.Header.Width, texture.Header.Height, interm);
                        break;
                    }
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_BC4_UNORM: {
                        BcDecoder.DecompressBC4(data, texture.Header.Width, texture.Header.Height, interm);
                        break;
                    }
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_BC5_UNORM: {
                        BcDecoder.DecompressBC5(data, texture.Header.Width, texture.Header.Height, interm);
                        break;
                    }
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_BC6H_UF16:
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_BC6H_SF16: {
                        BcDecoder.DecompressBC6H(data, texture.Header.Width, texture.Header.Height, format is TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_BC6H_SF16, interm);
                        break;
                    }
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_BC7_UNORM:
                    case TextureTypes.DXGI_PIXEL_FORMAT.DXGI_FORMAT_BC7_UNORM_SRGB: {
                        BcDecoder.DecompressBC7(data, texture.Header.Width, texture.Header.Height, interm);
                        break;
                    }
                }
            }

            // // unshuffle bgra into rgba using Vector256
            // var vectorSize = Vector256<byte>.Count;
            // var vectorCount = Pixels / vectorSize;
            // var vectorRemainder = Pixels % vectorSize;
            // if (Avx2.IsSupported) {
            //     var vectorSpan = MemoryMarshal.Cast<byte, Vector256<byte>>(PixelData.Span[..^vectorRemainder]);
            //     Vector256<byte> Shuffle = Vector256.Create((byte) 2, 1, 0, 3, 6, 5, 4, 7, 10, 9, 8, 11, 14, 13, 12, 15, 18, 17, 16, 19, 22, 21, 20, 23, 26, 25, 24, 27, 30, 29, 28, 31);
            //     for (var i = 0; i < vectorCount; i++) {
            //         vectorSpan[i] = Avx2.Shuffle(vectorSpan[i], Shuffle);
            //     }
            // } else {
            //     vectorCount = 0;
            //     vectorRemainder = Pixels;
            // }
            //
            // // handle remainder
            // if (vectorRemainder > 0) {
            //     var remainder = PixelData.Span[(vectorCount * vectorSize)..];
            //     for (var i = 0; i < remainder.Length; i += 4) {
            //         var b = remainder[i];
            //         var g = remainder[i + 1];
            //         var r = remainder[i + 2];
            //         var a = remainder[i + 3];
            //         remainder[i] = r;
            //         remainder[i + 1] = g;
            //         remainder[i + 2] = b;
            //         remainder[i + 3] = a;
            //     }
            // }
        }

        [CanBeNull]
        public Image<Bgra32> GetFrame(int frame) {
            return frame >= Surfaces ? null : Image.LoadPixelData<Bgra32>(PixelData.Slice(Pixels * frame, Pixels).Span, Texture.Header.Width, Texture.Header.Height);
        }
    }
}