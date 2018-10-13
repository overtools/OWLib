using System;
using System.IO;
using System.Runtime.InteropServices;

namespace TankLib {
    /// <summary>Tank GUID</summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct teResourceGUID {
        /// <summary>GUID Value</summary>
        public ulong GUID;
        
        [Flags]
        public enum AttributeEnum : ulong {
            Index    = 0x00000000FFFFFFFF,
            Locale   = 0x0000001F00000000,
            Reserved = 0x0000006000000000,
            Region   = 0x00000F8000000000,
            Platform = 0x0000F00000000000,
            Type     = 0x0FFF000000000000,
            Engine   = 0xF000000000000000
        };

        public teResourceGUID(ulong guid) {
            GUID = guid;
        }

        public static ulong Attribute(ulong key, AttributeEnum flags) {
            return key & (ulong)flags;
        }

        public static ulong LongKey(ulong key) {
            return Attribute(key, (AttributeEnum)0xFFFFFFFFFFFFUL);
        }

        /// <summary>Unique ID</summary>
        public static uint Index(ulong key) {
            return (uint)Attribute(key, AttributeEnum.Index);
        }

        /// <summary>The locale that this GUID was authored for</summary>
        public static byte Locale(ulong key) {
            return (byte)(Attribute(key, AttributeEnum.Locale) >> 32);
        }

        /// <summary>The reserved component</summary>
        public static byte Reserved(ulong key) {
            return (byte)(Attribute(key, AttributeEnum.Reserved) >> 37);
        }

        /// <summary>Region that this GUID was authored for</summary>
        public static byte Region(ulong key) {
            return (byte)(Attribute(key, AttributeEnum.Region) >> 39);
        }

        /// <summary>Platform that this GUID was authored for</summary>
        public static byte Platform(ulong key) {
            return (byte)(Attribute(key, AttributeEnum.Platform) >> 44);
        }

        /// <summary>Type of this GUID</summary>
        public static ushort Type(ulong key) {
            ulong num = Attribute(key, AttributeEnum.Type) >> 48;

            num = ((num >> 1) & 0x55555555) | ((num & 0x55555555) << 1);
            num = ((num >> 2) & 0x33333333) | ((num & 0x33333333) << 2);
            num = ((num >> 4) & 0x0F0F0F0F) | ((num & 0x0F0F0F0F) << 4);
            num = ((num >> 8) & 0x00FF00FF) | ((num & 0x00FF00FF) << 8);
            num = (num >> 16) | (num << 16);
            num >>= 20;

            return (ushort)(num + 1);
        }

        /// <summary>Type of this GUID, but manged</summary>
        public static ushort MangledType(ulong key) {
            return (ushort)(Attribute(key, AttributeEnum.Type) >> 48);
        }

        /// <summary>Reserved engine component of this GUID</summary>
        public static byte Engine(ulong key) {
            return (byte)(Attribute(key, AttributeEnum.Engine) >> 60);
        }

        /// <summary>Dump info about a guid</summary>
        public static void DumpAttributes(TextWriter @out, ulong key) {
            @out.WriteLine($"Index:    {Index(key):X4}");
            @out.WriteLine($"Locale:   {Locale(key):X1}");
            @out.WriteLine($"Region:   {Region(key):X1}");
            @out.WriteLine($"Reserved: {Reserved(key):X1}");
            @out.WriteLine($"Platform: {Platform(key):X1}");
            @out.WriteLine($"Type:     {Type(key):X3}");
            @out.WriteLine($"Engine:   {Engine(key):X1}");
            @out.WriteLine();
        }
        
        public static implicit operator ulong(teResourceGUID guid) {
            return guid.GUID;
        }
        
        public static explicit operator teResourceGUID(ulong guid) {
            return new teResourceGUID(guid);
        }
        
        /// <summary>String representation of this GUID</summary>
        public override string ToString() {
            return AsString(GUID);
        }

        /// <summary>String representation a GUID</summary>
        public static string AsString(ulong guid) {
            return $"{LongKey(guid):X12}.{Type(guid):X3}";
        }

        public override int GetHashCode() {
            return GUID.GetHashCode();
        }

        public bool Equals(teResourceGUID other) {
            return GUID == other.GUID;
        }
    }
}