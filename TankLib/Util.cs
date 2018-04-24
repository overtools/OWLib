using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace TankLib {
    public class Util {
        /// <summary>
        /// Get TankLib version info
        /// </summary>
        /// <returns>Version string</returns>
        public static string GetVersion() {
            Assembly asm = Assembly.GetAssembly(typeof(teResourceGUID));
            return GetVersion(asm);
        }

        public const uint BYTE_MASK_1 = 0xFF;
        public const uint BYTE_MASK_2 = 0xFFFF;
        public const uint BYTE_MASK_3 = 0xFFFFFF;

        /// <summary>
        /// Converts 4 chars
        /// </summary>
        /// <returns>int value</returns>
        public static int GetMagicBytes(params byte[] chars)
        {
            if (chars.Length > 4)
            {
                return 0;
            }

            IEnumerable<byte> v = chars;

            if (chars.Length == 3)
            {
                v = v.Prepend((byte)0);
            }
            else if (chars.Length < 4)
            {
                v = Enumerable.Repeat((byte)0, chars.Length - 4).Concat(v);
            }

            v = v.Reverse();

            unsafe
            {
                fixed (byte* chs = v.ToArray())
                {
                    return Marshal.ReadInt32((IntPtr)chs);
                }
            }
        }

        /// <summary>
        /// Converts 4 chars
        /// </summary>
        /// <returns>be int value</returns>
        public static int GetMagicBytesBE(params byte[] chars)
        {
            if (chars.Length > 4)
            {
                return 0;
            }

            IEnumerable<byte> v = chars;

            if (chars.Length == 3)
            {
                v = v.Append((byte)0);
            }
            else if (chars.Length < 4)
            {
                v = v.Concat(Enumerable.Repeat((byte)0, chars.Length - 4));
            }

            unsafe
            {
                fixed (byte* chs = v.ToArray())
                {
                    return Marshal.ReadInt32((IntPtr)chs);
                }
            }
        }

        public static int GetMagicBytes(params char[] chars) => GetMagicBytes(chars.Select(x => (byte)x).ToArray());
        public static int GetMagicBytesBE(params char[] chars) => GetMagicBytesBE(chars.Select(x => (byte)x).ToArray());

        /// <summary>
        /// Get assembly version info
        /// </summary>
        /// <param name="asm">Assembly to get info about</param>
        /// <returns>Version string</returns>
        public static string GetVersion(Assembly asm) {
            AssemblyInformationalVersionAttribute attrib = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            AssemblyFileVersionAttribute file = asm.GetCustomAttribute<AssemblyFileVersionAttribute>();
            if (attrib == null) {
                return file.Version;
            }
            return file.Version + "-git-" + attrib.InformationalVersion;
        }   
    }
}