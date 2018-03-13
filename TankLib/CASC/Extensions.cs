using System;
using System.IO;

namespace TankLib.CASC {
    public static class Extensions {
        /// <summary>Convert byte array of length 16 to MD5 hash type</summary>
        public static unsafe MD5Hash ToMD5(this byte[] array) {
            if (array.Length != 16)
                throw new ArgumentException("array size != 16");

            fixed (byte* ptr = array) {
                return *(MD5Hash*) ptr;
            }
        }

        /// <summary>Read a big endian 32-bit int</summary>
        public static int ReadInt32BE(this BinaryReader reader) {
            byte[] val = reader.ReadBytes(4);
            return val[3] | (val[2] << 8) | (val[1] << 16) | (val[0] << 24);
        }

        /// <summary>Convert byte array to a string</summary>
        public static string ToHexString(this byte[] data) {
            return BitConverter.ToString(data).Replace("-", string.Empty);
        }

        /// <summary>Compare two byte arrays</summary>
        public static bool EqualsTo(this byte[] hash, byte[] other) {
            if (hash.Length != other.Length)
                return false;
            for (int i = 0; i < hash.Length; ++i)
                if (hash[i] != other[i])
                    return false;

            return true;
        }

        /// <summary>Copy bytes from one stream to another</summary>
        public static void CopyBytes(this Stream input, Stream output, int bytes) {
            byte[] buffer = new byte[32768];
            int read;
            while (bytes > 0 && (read = input.Read(buffer, 0, System.Math.Min(buffer.Length, bytes))) > 0) {
                output.Write(buffer, 0, read);
                bytes -= read;
            }
        }

        /// <summary>Advance the stream by <param name="bytes"></param></summary>
        public static void Skip(this BinaryReader reader, int bytes) {
            reader.BaseStream.Position += bytes;
        }

        /// <summary>MD5 hash is zero</summary>
        public static unsafe bool IsZeroed(this MD5Hash key) {
            for (int i = 0; i < 16; ++i)
                if (key.Value[i] != 0)
                    return false;
            return true;
        }

        public static unsafe bool EqualsTo(this MD5Hash key, byte[] array) {
            if (array.Length != 16)
                return false;

            MD5Hash other;

            fixed (byte* ptr = array) {
                other = *(MD5Hash*) ptr;
            }

            for (int i = 0; i < 2; ++i) {
                ulong keyPart = *(ulong*) (key.Value + i * 8);
                ulong otherPart = *(ulong*) (other.Value + i * 8);

                if (keyPart != otherPart)
                    return key.EqualsTo9(array);
            }

            return true;
        }

        public static unsafe bool EqualsTo9(this MD5Hash key, byte[] array) {
            if (array.Length < 9)
                return false;

            MD5Hash other;

            fixed (byte* ptr = array) {
                other = *(MD5Hash*) ptr;
            }

            ulong keyPart = *(ulong*) key.Value;
            ulong otherPart = *(ulong*) other.Value;

            if (keyPart != otherPart)
                return false;

            if (key.Value[8] != other.Value[8])
                return false;

            //for (int i = 0; i < 2; ++i)
            //{
            //    ulong keyPart = *(ulong*)(key.Value + i * 8);
            //    ulong otherPart = *(ulong*)(other.Value + i * 8);

            //    if (keyPart != otherPart)
            //        return false;
            //}

            return true;
        }
    }
}