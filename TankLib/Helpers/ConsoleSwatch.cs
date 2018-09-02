using System;
using System.Drawing;
using System.Runtime.InteropServices;
using TankLib.Math;
// ReSharper disable IdentifierTypo

namespace TankLib.Helpers {
    public static class ConsoleSwatch {
        public enum XTermColor : byte {
            Black = 0,
            Maroon,
            Green,
            Olive,
            Navy,
            Purple,
            Teal,
            Silver,
            Grey,
            Red,
            Lime,
            Yellow,
            Blue,
            Fuchsia,
            Aqua,
            White,
            Grey1,
            NavyBlue,
            DarkBlue,
            Blue1,
            Blue2,
            Blue3,
            DarkGreen,
            DeepSkyBlue,
            DeepSkyBlue1,
            DeepSkyBlue2,
            DodgerBlue,
            DodgerBlue1,
            Green1,
            SpringGreen,
            Turquoise,
            DeepSkyBlue3,
            DeepSkyBlue4,
            DodgerBlue2,
            Green2,
            SpringGreen1,
            DarkCyan,
            LightSeaGreen,
            DeepSkyBlue5,
            DeepSkyBlue6,
            Green3,
            SpringGreen2,
            SpringGreen3,
            Cyan,
            DarkTurquoise,
            Turquoise1,
            Green4,
            SpringGreen4,
            SpringGreen5,
            MediumSpringGreen,
            Cyan1,
            Cyan2,
            DarkRed,
            DeepPink,
            Purple1,
            Purple2,
            Purple3,
            BlueViolet,
            Orange,
            Grey2,
            MediumPurple,
            SlateBlue,
            SlateBlue1,
            RoyalBlue,
            Chartreuse,
            DarkSeaGreen,
            PaleTurquoise,
            SteelBlue,
            SteelBlue1,
            CornflowerBlue,
            Chartreuse1,
            DarkSeaGreen1,
            CadetBlue,
            CadetBlue1,
            SkyBlue,
            SteelBlue2,
            Chartreuse2,
            PaleGreen,
            SeaGreen,
            Aquamarine,
            MediumTurquoise,
            SteelBlue3,
            Chartreuse3,
            SeaGreen1,
            SeaGreen2,
            SeaGreen3,
            Aquamarine1,
            DarkSlateGray,
            DarkRed1,
            DeepPink1,
            DarkMagenta,
            DarkMagenta1,
            DarkViolet,
            Purple4,
            Orange1,
            LightPink,
            Plum,
            MediumPurple1,
            MediumPurple2,
            SlateBlue2,
            Yellow1,
            Wheat,
            Grey3,
            LightSlateGrey,
            MediumPurple3,
            LightSlateBlue,
            Yellow2,
            DarkOliveGreen,
            DarkSeaGreen2,
            LightSkyBlue,
            LightSkyBlue1,
            SkyBlue1,
            Chartreuse4,
            DarkOliveGreen1,
            PaleGreen1,
            DarkSeaGreen3,
            DarkSlateGray1,
            SkyBlue2,
            Chartreuse5,
            LightGreen,
            LightGreen1,
            PaleGreen2,
            Aquamarine2,
            DarkSlateGray2,
            Red1,
            DeepPink2,
            MediumVioletRed,
            Magenta,
            DarkViolet1,
            Purple5,
            DarkOrange,
            IndianRed,
            HotPink,
            MediumOrchid,
            MediumOrchid1,
            MediumPurple4,
            DarkGoldenrod,
            LightSalmon,
            RosyBrown,
            Grey4,
            MediumPurple5,
            MediumPurple6,
            Gold,
            DarkKhaki,
            NavajoWhite,
            Grey5,
            LightSteelBlue,
            LightSteelBlue1,
            Yellow3,
            DarkOliveGreen2,
            DarkSeaGreen4,
            DarkSeaGreen5,
            LightCyan,
            LightSkyBlue2,
            GreenYellow,
            DarkOliveGreen3,
            PaleGreen3,
            DarkSeaGreen6,
            DarkSeaGreen7,
            PaleTurquoise1,
            Red2,
            DeepPink3,
            DeepPink4,
            Magenta1,
            Magenta2,
            Magenta3,
            DarkOrange1,
            IndianRed1,
            HotPink1,
            HotPink2,
            Orchid,
            MediumOrchid2,
            Orange2,
            LightSalmon1,
            LightPink1,
            Pink,
            Plum1,
            Violet,
            Gold1,
            LightGoldenrod,
            Tan,
            MistyRose,
            Thistle,
            Plum2,
            Yellow4,
            Khaki,
            LightGoldenrod1,
            LightYellow,
            Grey6,
            LightSteelBlue2,
            Yellow5,
            DarkOliveGreen4,
            DarkOliveGreen5,
            DarkSeaGreen8,
            Honeydew,
            LightCyan1,
            Red3,
            DeepPink5,
            DeepPink6,
            DeepPink7,
            Magenta4,
            Magenta5,
            OrangeRed,
            IndianRed2,
            IndianRed3,
            HotPink3,
            HotPink4,
            MediumOrchid3,
            DarkOrange2,
            Salmon,
            LightCoral,
            PaleVioletRed,
            Orchid1,
            Orchid2,
            Orange3,
            SandyBrown,
            LightSalmon2,
            LightPink2,
            Pink1,
            Plum3,
            Gold2,
            LightGoldenrod2,
            LightGoldenrod3,
            NavajoWhite1,
            MistyRose1,
            Thistle1,
            Yellow6,
            LightGoldenrod4,
            Khaki1,
            Wheat1,
            Cornsilk,
            Grey7,
            Grey8,
            Grey9,
            Grey10,
            Grey11,
            Grey12,
            Grey13,
            Grey14,
            Grey15,
            Grey16,
            Grey17,
            Grey18,
            Grey19,
            Grey20,
            Grey21,
            Grey22,
            Grey23,
            Grey24,
            Grey25,
            Grey26,
            Grey27,
            Grey28,
            Grey29,
            Grey30,
            Grey31
        };

        public enum DOSColor {
            Black = 0,
            DarkBlue = 1,
            DarkGreen = 2,
            DarkCyan = 3,
            DarkRed = 4,
            DarkMagenta = 5,
            DarkYellow = 6,
            Gray = 7,
            DarkGray = 8,
            Blue = 9,
            Green = 10,
            Cyan = 11,
            Red = 12,
            Magenta = 13,
            Yellow = 14,
            White = 15
        }

        public static ConsoleColor AsConsoleColor(this DOSColor color) {
            return (ConsoleColor) color;
        }

        public static DOSColor AsDOSColor(this ConsoleColor color) {
            return (DOSColor) color;
        }

        public static XTermColor AsXTermColor(this DOSColor color) {
            // ReSharper disable once SwitchStatementMissingSomeCases
            switch (color) {
                case DOSColor.DarkGray:
                    return XTermColor.Grey;
                case DOSColor.DarkYellow:
                    return XTermColor.Yellow;
                case DOSColor.Yellow:
                    return XTermColor.LightYellow;
                case DOSColor.Gray:
                    return XTermColor.LightSlateGrey;
                default:
                    return Enum.TryParse(color.ToString(), out XTermColor col) ? col : XTermColor.DarkSlateGray;
            }
        }

        public static string ToForeground(this XTermColor color) {
            return $"\x1b[38;5;{(byte) color}m";
        }

        public static string ToBackground(this XTermColor color) {
            return $"\x1b[48;5;{(byte) color}m";
        }

        public static string ToForeground(this Color color) {
            return $"\x1b[38;2;{color.R};{color.G};{color.B}m";
        }

        public static string ToBackground(this Color color) {
            return $"\x1b[48;2;{color.R};{color.G};{color.B}m";
        }

        public static string ToForeground(this teColorRGB color) {
            return $"\x1b[38;2;{(int) (color.R * 255.0f)};{(int) (color.G * 255.0f)};{(int) (color.B * 255.0f)}m";
        }

        public static string ToBackground(this teColorRGB color) {
            return $"\x1b[48;2;{(int) (color.R * 255.0f)};{(int) (color.G * 255.0f)};{(int) (color.B * 255.0f)}m";
        }

        public static string ToForeground(this teColorRGBA color) {
            return $"\x1b[38;2;{(int) (color.R * 255.0f)};{(int) (color.G * 255.0f)};{(int) (color.B * 255.0f)}m";
        }

        public static string ToBackground(this teColorRGBA color) {
            return $"\x1b[48;2;{(int) (color.R * 255.0f)};{(int) (color.G * 255.0f)};{(int) (color.B * 255.0f)}m";
        }

        public const string ColorReset = "\x1b[0m";

        public static bool IsVTEnabled { get; private set; }
        public static bool IsVTCapable { get; private set; } = Environment.OSVersion.Version.Major >= 6;

        public static bool EnableVT() {
            if (IsVTEnabled) {
                return true;
            }

            if (!IsVTCapable) {
                return false;
            }

            unsafe {
                IntPtr hOut = GetStdHandle(STD_OUTPUT_HANDLE);
                if (hOut == INVALID_HANDLE_VALUE) {
                    IsVTCapable = false;
                    return false;
                }

                int dwMode = 0;
                if (!GetConsoleMode(hOut, &dwMode)) {
                    IsVTCapable = false;
                    return false;
                }

                dwMode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
                if (!SetConsoleMode(hOut, dwMode)) {
                    IsVTCapable = false;
                    return false;
                }

                IsVTEnabled = true;
                return true;
            }
        }

        [DllImport("Kernel32.dll")]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("Kernel32.dll")]
        private static extern unsafe bool GetConsoleMode(IntPtr hConsoleHandle, int* lpMode);

        [DllImport("Kernel32.dll")]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, int dwMode);

        private static readonly IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        private const int STD_OUTPUT_HANDLE = -11;
        private const int ENABLE_VIRTUAL_TERMINAL_PROCESSING = 4;
    }
}
