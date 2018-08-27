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

            IEnumerable<byte> v = chars.AsEnumerable();

            if (chars.Length == 3)
            {
                v = Enumerable.Append(v, (byte)0);
            }
            else if (chars.Length < 4)
            {
                v = Enumerable.Concat(v, Enumerable.Repeat((byte)0, chars.Length - 4));
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

        public static List<Type> GetAssemblyTypes<T>(Assembly assembly) {
            List<Type> types = assembly.GetTypes().Where(type => type != typeof(T) && typeof(T).IsAssignableFrom(type)).ToList();
            return types;
        }
    }

    [Flags]
    public enum TestByteFlags : byte {
        F00000001 = 0x1,
        F00000002 = 0x2,
        F00000004 = 0x4,
        F00000008 = 0x8,
        F00000010 = 0x10,
        F00000020 = 0x20,
        F00000040 = 0x40,
        F00000080 = 0x80
    }
}