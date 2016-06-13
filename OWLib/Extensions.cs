using System;
using System.IO;
using System.Runtime.InteropServices;
using OWLib.Types;

namespace OWLib {
  public static class Extensions {
    public static TextureType TextureTypeFromHeaderByte(byte type) {
      if(type == 70 || type == 71 || type == 72) {
        return TextureType.DXT1;
      }

      if(type == 73 || type == 74 || type == 75) {
        return TextureType.DXT3;
      }

      if(type == 76 || type == 77 || type == 78) {
        return TextureType.DXT5;
      }

      if(type == 79 || type == 80 || type == 81) {
        return TextureType.ATI1;
      }

      if(type == 82 || type == 83 || type == 84) {
        return TextureType.ATI2;
      }

      return TextureType.Unknown;
    }

    public static uint ByteSize(this TextureType T) {
      if(T == TextureType.DXT5) {
        return 16;
      } else {
        return 8;
      }
    }

    public static DDSPixelFormat ToPixelFormat(this TextureType T) {
      DDSPixelFormat ret = new DDSPixelFormat {
        size = 32,
        flags = 4,
        fourCC = (uint)T,
        bitCount = 32,
        redMask = 0x0000FF00,
        greenMask = 0x00FF0000,
        blueMask = 0xFF000000,
        alphaMask = 0x000000FF
      };
      return ret;
    }

    public static TextureType Format(this TextureHeader header) {
      return TextureTypeFromHeaderByte((byte)header.format);
    }

    public static T Read<T>(this BinaryReader reader) where T : struct {
      int size = Marshal.SizeOf<T>();
      byte[] buf = reader.ReadBytes(size);
      IntPtr ptr = Marshal.AllocHGlobal(size);
      Marshal.Copy(buf, 0, ptr, size);
      T obj = Marshal.PtrToStructure<T>(ptr);
      Marshal.FreeHGlobal(ptr);
      return obj;
    }

    public static void Write<T>(this BinaryWriter writer, T obj) where T : struct {
      int size = Marshal.SizeOf<T>();
      byte[] buf = new byte[size];
      IntPtr ptr = Marshal.AllocHGlobal(size);
      Marshal.StructureToPtr<T>(obj, ptr, true);
      Marshal.Copy(ptr, buf, 0, size);
      Marshal.FreeHGlobal(ptr);
      writer.Write(buf, 0, size);
    }

    public static string ToHex(this byte[] data) {
      return BitConverter.ToString(data).Replace("-", string.Empty);
    }
    
    public static bool IsCubemap(this TextureHeader dds) {
      return (dds.type & TEXTURE_FLAGS.CUBEMAP) == TEXTURE_FLAGS.CUBEMAP;
    }

    public static bool IsMultisurface(this TextureHeader dds) {
      return (dds.type & TEXTURE_FLAGS.MULTISURFACE) == TEXTURE_FLAGS.MULTISURFACE;
    }

    public static bool IsWorld(this TextureHeader dds) {
      return (dds.type & TEXTURE_FLAGS.WORLD) == TEXTURE_FLAGS.WORLD;
    }

    public static DDSHeader Modify(this DDSHeader d, TextureHeader header, uint mips, uint surfaces) {
      DDSHeader dds = d;
      if(surfaces > 1) {
        dds.caps1 = 0x1000 | 0x8;
      }
      if(header.IsCubemap()) {
        dds.caps2 = 0x200;
      }
      if(mips > 1) {
        dds.mipmapCount = mips;
        dds.caps1 = (0x8 | 0x1000 | 0x400000);
      }
      return dds;
    }

    public static DDSHeader ToDDSHeader(this TextureHeader header, bool modify = true) {
      DDSHeader ret = new DDSHeader {
        magic = 0x20534444,
        size = 124,
        flags = (0x1 | 0x2 | 0x4 | 0x1000 | 0x20000),
        height = header.height,
        width = header.width,
        linearSize = 0,
        depth = 0,
        mipmapCount = 1,
        format = header.Format().ToPixelFormat(),
        caps1 = 0x1000,
        caps2 = 0,
        caps3 = 0,
        caps4 = 0,
        reserved2 = 0
      };
      if(modify) {
        ret = ret.Modify(header, header.mips, header.surfaces);
      }
      return ret;
    }

    public static DDSHeader ToDDSHeader(this RawTextureheader rawHeader, TextureHeader header) {
      DDSHeader ret = header.ToDDSHeader(false);
      ret = ret.Modify(header, rawHeader.mips, rawHeader.surfaces);
      return ret;
    }

    public unsafe static string ToHex(this MD5Hash hash) {
      byte[] array = new byte[16];
      fixed (byte* ptr = array)
      {
        *(MD5Hash*)ptr = hash;
      }
      return array.ToHex();
    }
    
    public unsafe static OpenTK.Matrix4 ToOpenTK(this Matrix4B matrix) {
      unsafe
      {
        return new OpenTK.Matrix4(
          matrix.Value[0], matrix.Value[1], matrix.Value[2], matrix.Value[3],
          matrix.Value[4], matrix.Value[5], matrix.Value[6], matrix.Value[7],
          matrix.Value[8], matrix.Value[9], matrix.Value[10], matrix.Value[11],
          matrix.Value[12], matrix.Value[13], matrix.Value[14], matrix.Value[15]
        );
      }
    }

    public unsafe static OpenTK.Matrix3x4 ToOpenTK(this Matrix3x4B matrix) {
      unsafe
      {
        return new OpenTK.Matrix3x4(
          matrix.Value[0], matrix.Value[1], matrix.Value[2], matrix.Value[3],
          matrix.Value[4], matrix.Value[5], matrix.Value[6], matrix.Value[7],
          matrix.Value[8], matrix.Value[9], matrix.Value[10], matrix.Value[11]
        );
      }
    }
  }
}
