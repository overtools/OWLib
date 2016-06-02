using System;
using System.IO;
using OWLib.Types;

namespace OWLib {
  public class TextureLinear {
    private TextureHeader header;
    private DXGI_PIXEL_FORMAT format;
    private uint size;
    private ulong[] color;
    private bool loaded = false;

    public TextureHeader Header => header;
    public DXGI_PIXEL_FORMAT Format => format;
    public uint Size => size;
    public ulong[] Color => color;
    public bool Loaded => loaded;

    public enum TEXTURE_STYLE : byte {
      UNKNOWN = 0,
      TYPELESS = 1,
      FLOAT = 2,
      UNSIGNED = 3,
      SIGNED = 4
    };

    public TextureLinear(Stream imageStream) {
      using(BinaryReader imageReader = new BinaryReader(imageStream)) {
        header = imageReader.Read<TextureHeader>();
        if(header.dataSize == 0) {
          return;
        }
        size = header.dataSize;
        format = header.format;

        uint[] bits = new uint[4] { 0, 0, 0, 0 };
        TEXTURE_STYLE style = 0;

        if(format >= DXGI_PIXEL_FORMAT.DXGI_FORMAT_R32G32B32A32_TYPELESS && format <= DXGI_PIXEL_FORMAT.DXGI_FORMAT_R32G32B32A32_SINT) {
          bits[0] = bits[1] = bits[2] = bits[3] = 32;
        } else if(format >= DXGI_PIXEL_FORMAT.DXGI_FORMAT_R32G32B32_TYPELESS && format <= DXGI_PIXEL_FORMAT.DXGI_FORMAT_R32G32B32_SINT) {
          bits[0] = bits[1] = bits[2] = 32;
        } else if(format >= DXGI_PIXEL_FORMAT.DXGI_FORMAT_R16G16B16A16_TYPELESS && format <= DXGI_PIXEL_FORMAT.DXGI_FORMAT_R16G16B16A16_SINT) {
          bits[0] = bits[1] = bits[2] = bits[3] = 16;
        } else if(format >= DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8G8B8A8_TYPELESS && format <= DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8G8B8A8_SINT) {
          bits[0] = bits[1] = bits[2] = bits[3] = 8;
        } else {
          return;
        }

        switch(format) {
          case DXGI_PIXEL_FORMAT.DXGI_FORMAT_R32G32B32A32_TYPELESS:
          case DXGI_PIXEL_FORMAT.DXGI_FORMAT_R32G32B32_TYPELESS:
          case DXGI_PIXEL_FORMAT.DXGI_FORMAT_R16G16B16A16_TYPELESS:
          case DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8G8B8A8_TYPELESS:
            style = TEXTURE_STYLE.TYPELESS;
            break;
          case DXGI_PIXEL_FORMAT.DXGI_FORMAT_R32G32B32A32_FLOAT:
          case DXGI_PIXEL_FORMAT.DXGI_FORMAT_R32G32B32_FLOAT:
          case DXGI_PIXEL_FORMAT.DXGI_FORMAT_R16G16B16A16_FLOAT:
            style = TEXTURE_STYLE.FLOAT;
            break;
          case DXGI_PIXEL_FORMAT.DXGI_FORMAT_R32G32B32A32_UINT:
          case DXGI_PIXEL_FORMAT.DXGI_FORMAT_R32G32B32_UINT:
          case DXGI_PIXEL_FORMAT.DXGI_FORMAT_R16G16B16A16_UINT:
          case DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8G8B8A8_UINT:
            style = TEXTURE_STYLE.UNSIGNED;
            break;
          case DXGI_PIXEL_FORMAT.DXGI_FORMAT_R32G32B32A32_SINT:
          case DXGI_PIXEL_FORMAT.DXGI_FORMAT_R32G32B32_SINT:
          case DXGI_PIXEL_FORMAT.DXGI_FORMAT_R16G16B16A16_SINT:
          case DXGI_PIXEL_FORMAT.DXGI_FORMAT_R8G8B8A8_SINT:
            style = TEXTURE_STYLE.SIGNED;
            break;
        }

        if(style == TEXTURE_STYLE.UNKNOWN) {
          return;
        }

        imageStream.Seek(128, SeekOrigin.Begin);
        uint pixels = (uint)(header.width * header.height);
        color = new ulong[pixels];

        for(int i = 0; i < pixels; ++i) {
          ulong pixel = 0;
          for(int j = 0; j < 4; ++j) {
            uint bit = bits[j];
            ushort v;
            if(bit == 8) {
              byte value = imageReader.ReadByte();
              v = (ushort)((value / byte.MaxValue) * ushort.MaxValue);
            } else if(bit == 16) {
              ushort value = imageReader.ReadUInt16();
              if(style == TEXTURE_STYLE.FLOAT) {
                v = (ushort)((float)Half.ToHalf(value) * ushort.MaxValue);
              } else {
                v = value;
              }
            } else if(bit == 32) {
              if(style == TEXTURE_STYLE.FLOAT) {
                v = (ushort)(imageReader.ReadSingle() * ushort.MaxValue);
              } else {
                v = (ushort)((imageReader.ReadUInt32() / uint.MaxValue) * ushort.MaxValue);
              }
            } else {
              v = ushort.MaxValue;
            }
            ulong x = (ulong)v << (j * 12);
            pixel += x;
          }
          color[i] = pixel;
        }
      }
      loaded = true;
    }
  }
}
