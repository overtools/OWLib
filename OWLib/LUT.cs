using System.Collections.Generic;
using System.IO;

namespace OWLib
{
    public class LUT
    {
        public static string SPILUT1024x32(Stream lutimage)
        {
            List<string> realLines = new List<string>
            {
                "SPILUT 1.0",
                "3 3",
                "256 256 256",
            };

            List<string> lines = new List<string>();

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 1024; x++)
                {
                    // int[] neutral = new int[] { (x % 64) * 4, (y % 64) * 4, (y / 2 + x / 16) }; // 512x512
                    int[] neutral = new int[] { (x % 32) * 8, y * 8, (x / 32) * 8 }; // 1024x32
                    // int[] neutral = new int[] { (x % 16) * 16, y * 16, (x / 16) * 16 }; // 256x16

                    string s = $"{neutral[0]} {neutral[1]} {neutral[2]} ";

                    float[] rgb = new float[] { (float)lutimage.ReadByte() / (float)neutral[0], (float)lutimage.ReadByte() / (float)neutral[1], (float)lutimage.ReadByte() / (float)neutral[2] };
                    lutimage.ReadByte(); // alpha.

                    s += $"{rgb[0]} {rgb[1]} {rgb[2]}";

                    lines.Add(s);
                }
            }

            lines.Sort(); // sanity, i guess.
            realLines.AddRange(lines);

            return string.Join("\n", lines);
        }
    }
}
