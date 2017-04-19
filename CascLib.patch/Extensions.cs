using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CASCExplorer
{
    public static class Extensions
    {
        public static int ReadInt32BE(this BinaryReader reader)
        {
            byte[] val = reader.ReadBytes(4);
            return val[3] | val[2] << 8 | val[1] << 16 | val[0] << 24;
        }

        public static void Skip(this BinaryReader reader, int bytes)
        {
            reader.BaseStream.Position += bytes;
        }

        public static uint ReadUInt32BE(this BinaryReader reader)
        {
            byte[] val = reader.ReadBytes(4);
            return (uint)(val[3] | val[2] << 8 | val[1] << 16 | val[0] << 24);
        }

        public unsafe static T Read<T>(this BinaryReader reader) where T : struct
        {
            byte[] result = reader.ReadBytes(FastStruct<T>.Size);

            fixed (byte* ptr = result)
                return FastStruct<T>.PtrToStructure(ptr);
        }

        public unsafe static T[] ReadArray<T>(this BinaryReader reader) where T : struct
        {
            int numBytes = (int)reader.ReadInt64();

            byte[] result = reader.ReadBytes(numBytes);

            fixed (byte* ptr = result)
            {
                T[] data = FastStruct<T>.ReadArray((IntPtr)ptr, numBytes);
                reader.BaseStream.Position += (0 - numBytes) & 0x07;
                return data;
            }
        }

        public static short ReadInt16BE(this BinaryReader reader)
        {
            byte[] val = reader.ReadBytes(2);
            return (short)(val[1] | val[0] << 8);
        }

        public static void CopyBytes(this Stream input, Stream output, int bytes)
        {
            byte[] buffer = new byte[32768];
            int read;
            while (bytes > 0 && (read = input.Read(buffer, 0, Math.Min(buffer.Length, bytes))) > 0)
            {
                output.Write(buffer, 0, read);
                bytes -= read;
            }
        }

        public static void ExtractToFile(this Stream input, string path, string name)
        {
            string fullPath = Path.Combine(path, name);
            string dir = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            using (var fileStream = File.Open(fullPath, FileMode.Create))
            {
                input.Position = 0;
                input.CopyTo(fileStream);
            }
        }

        public static string ToHexString(this byte[] data)
        {
            return BitConverter.ToString(data).Replace("-", string.Empty);
        }

        public static bool EqualsTo(this byte[] hash, byte[] other)
        {
            if (hash.Length != other.Length)
                return false;
            for (var i = 0; i < hash.Length; ++i)
                if (hash[i] != other[i])
                    return false;
            return true;
        }

        public static bool EqualsToIgnoreLength(this byte[] array, byte[] other)
        {
            for (var i = 0; i < array.Length; ++i)
                if (array[i] != other[i])
                    return false;
            return true;
        }

        public static byte[] Copy(this byte[] array, int len)
        {
            byte[] ret = new byte[len];
            for (int i = 0; i < len; ++i)
                ret[i] = array[i];
            return ret;
        }

        public static string ToBinaryString(this BitArray bits)
        {
            StringBuilder sb = new StringBuilder(bits.Length);

            for (int i = 0; i < bits.Length; ++i)
            {
                sb.Append(bits[i] ? "1" : "0");
            }

            return sb.ToString();
        }

        public static unsafe bool EqualsTo(this MD5Hash key, byte[] array)
        {
            if (array.Length != 16)
                return false;

            MD5Hash other;

            fixed (byte* ptr = array)
                other = *(MD5Hash*)ptr;

            //for (var i = 0; i < 16; ++i)
            //    if (key.Value[i] != other.Value[i])
            //        return false;

            //return key.EqualsTo(other);
            for (int i = 0; i < 2; ++i)
            {
                ulong keyPart = *(ulong*)(key.Value + i * 8);
                ulong otherPart = *(ulong*)(other.Value + i * 8);

                if (keyPart != otherPart)
                    return key.EqualsTo9(other);
            }
            return true;
        }

        private static unsafe bool EqualsTo9(this MD5Hash key, MD5Hash other)
        {
            ulong keyPart = *(ulong*)(key.Value);
            ulong otherPart = *(ulong*)(other.Value);

            if (keyPart != otherPart)
                return false;

            keyPart = (byte) *(ulong*)(key.Value + 8);
            otherPart = *(ulong*)(other.Value + 8);
            if (keyPart != otherPart)
                return false;
            return true;
        }

        public static unsafe bool EqualsTo(this MD5Hash key, MD5Hash other)
        {
            for (int i = 0; i < 2; ++i)
            {
                ulong keyPart = *(ulong*)(key.Value + i * 8);
                ulong otherPart = *(ulong*)(other.Value + i * 8);

                if (keyPart != otherPart)
                    return false;
            }

            return true;
        }

        public static unsafe string ToHexString(this MD5Hash key)
        {
            byte[] array = new byte[16];

            fixed (byte* aptr = array)
            {
                *(MD5Hash*)aptr = key;
            }

            return array.ToHexString();
        }

        public static unsafe bool IsZeroed(this MD5Hash key)
        {
            for (var i = 0; i < 16; ++i)
                if (key.Value[i] != 0)
                    return false;
            return true;
        }

        public static unsafe MD5Hash ToMD5(this byte[] array)
        {
            if (array.Length != 16)
                throw new ArgumentException("array size != 16");

            fixed (byte* ptr = array)
            {
                return *(MD5Hash*)ptr;
            }
        }
    }

    public static class CStringExtensions
    {
        /// <summary> Reads the NULL terminated string from 
        /// the current stream and advances the current position of the stream by string length + 1.
        /// <seealso cref="BinaryReader.ReadString"/>
        /// </summary>
        public static string ReadCString(this BinaryReader reader)
        {
            return reader.ReadCString(Encoding.UTF8);
        }

        /// <summary> Reads the NULL terminated string from 
        /// the current stream and advances the current position of the stream by string length + 1.
        /// <seealso cref="BinaryReader.ReadString"/>
        /// </summary>
        public static string ReadCString(this BinaryReader reader, Encoding encoding)
        {
            var bytes = new List<byte>();
            byte b;
            while ((b = reader.ReadByte()) != 0)
                bytes.Add(b);
            return encoding.GetString(bytes.ToArray());
        }

        public static void WriteCString(this BinaryWriter writer, string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);
            writer.Write(bytes);
            writer.Write((byte)0);
        }

        public static byte[] ToByteArray(this string str)
        {
            str = str.Replace(" ", string.Empty);

            var res = new byte[str.Length / 2];
            for (int i = 0; i < res.Length; ++i)
            {
                res[i] = Convert.ToByte(str.Substring(i * 2, 2), 16);
            }
            return res;
        }
    }
}
