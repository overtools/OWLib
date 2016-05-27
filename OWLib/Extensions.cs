using System;
using System.IO;
using System.Runtime.InteropServices;

namespace OWLib {
  public static class Extensions {
    public static TextureType TextureTypeFromHeaderByte(byte type) {
      if(type == 71 || type == 72) {
        return TextureType.DXT1;
      }

      if(type == 78) {
        return TextureType.DXT5;
      }

      if(type == 80) {
        return TextureType.ATI1;
      }

      if(type == 83) {
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
        size      = 32,
        flags     = 0x4,
        fourCC    = (uint)T,
        bitCount  = 32,
        redMask   = 0x0000FF00,
        greenMask = 0x00FF0000,
        blueMask  = 0xFF000000,
        alphaMask = 0x000000FF
      };
      return ret;
    }

    public static TextureType Format(this TextureHeader header) {
      return TextureTypeFromHeaderByte(header.format);
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

    public static DDSHeader ToDDSHeader(this TextureHeader header) {
      DDSHeader ret = new DDSHeader {
        magic       = 0x20534444,
        size        = 124,
        flags       = 659463,
        height      = header.height,
        width       = header.width,
        linearSize  = 0,
        depth       = 0,
        mipmapCount = 1,
        format      = header.Format().ToPixelFormat(),
        caps1       = 0x1000,
        caps2       = 0,
        caps3       = 0,
        caps4       = 0,
        reserved2   = 0
      };
      return ret;
    }

    public unsafe static string ToHex(this MD5Hash hash) {
      byte[] array = new byte[16];
      fixed(byte* ptr = array)
      {
        *(MD5Hash*)ptr = hash;
      }
      return array.ToHex();
    }
  }
}
