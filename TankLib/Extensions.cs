using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TankLib {
    public static class Extensions {
        #region BinaryReader
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
            Marshal.StructureToPtr(obj, ptr, true);
            Marshal.Copy(ptr, buf, 0, size);
            Marshal.FreeHGlobal(ptr);
            writer.Write(buf, 0, size);
        }
        
        public static long Position(this BinaryReader reader) => reader.BaseStream.Position;

        // Needs testing.
        public static string ReadString(this BinaryReader reader, int size) {
            byte[] bytes = reader.ReadBytes(size);
            return Encoding.UTF8.GetString(bytes);
        }

        public static long Seek(this BinaryReader reader, long offset, SeekOrigin origin = SeekOrigin.Begin)
            => reader.BaseStream.Seek(offset, origin);

        public static long Size(this BinaryReader reader)
            => reader.BaseStream.Length;
        #endregion

        #region Random Utils
        public static string ReverseXor(this string s) {
            char[] charArray = s.ToCharArray();
            int len = s.Length - 1;

            for (int i = 0; i < len; i++, len--)
            {
                charArray[i] ^= charArray[len];
                charArray[len] ^= charArray[i];
                charArray[i] ^= charArray[len];
            }

            return new string(charArray);
        }
        
        public static unsafe ulong SwapBytes(this ulong value, int b1, int b2) {
            byte* data = (byte*)&value;
            byte a = *(data + b1);
            byte b = *(data + b2);
            *(data + b1) = b;
            *(data + b2) = a;
            return value;
        }
        
        public static byte[] ToByteArray(this string str) {
            str = str.Replace(" ", string.Empty);

            byte[] res = new byte[str.Length / 2];
            for (int i = 0; i < res.Length; ++i)
            {
                res[i] = Convert.ToByte(str.Substring(i * 2, 2), 16);
            }
            return res;
        }
        #endregion

        #region Textures
        public static TextureTypes.DDSPixelFormat ToPixelFormat(this TextureTypes.TextureType T) {
            TextureTypes.DDSPixelFormat ret = new TextureTypes.DDSPixelFormat {
                Size = 32,
                Flags = 4,
                FourCC = (uint)T,
                BitCount = 32,
                RedMask = 0x0000FF00,
                GreenMask = 0x00FF0000,
                BlueMask = 0xFF000000,
                AlphaMask = 0x000000FF
            };
            if (T == TextureTypes.TextureType.ATI2) {
                ret.Flags |= 0x80000000;
            }
            return ret;
        }
        
        public static uint ByteSize(this TextureTypes.TextureType T) {
            return T == TextureTypes.TextureType.DXT5 ? 16u : 8u;
        }
        #endregion
    }
}