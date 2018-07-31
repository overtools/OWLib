using System.Collections.Generic;
using System.IO;

namespace DataTool.ConvertLogic {
    public static class LUT {
        public static string SPILUT1024x32(Stream lutimage) {
            List<string> realLines = new List<string>
            {
                "SPILUT 1.0",
                "3 3",
                "32 32 32",
            };

            SortedList<int, string> lines = new SortedList<int, string>();

            float @base = byte.MaxValue;

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 1024; x++)
                {
                    int[] neutral = { x % 32, y, x / 32 }; // 1024x32

                    string s = $"{neutral[0]} {neutral[1]} {neutral[2]} ";

                    float[] rgb = { lutimage.ReadByte() / @base, lutimage.ReadByte() / @base, lutimage.ReadByte() / @base };
                    lutimage.ReadByte(); // alpha.

                    s += $"{rgb[0]} {rgb[1]} {rgb[2]}";

                    lines.Add((neutral[0] << 16) + (neutral[1] << 8) + neutral[2], s);
                }
            }
            
            realLines.AddRange(lines.Values);

            return string.Join("\n", realLines);
        }
    }
}