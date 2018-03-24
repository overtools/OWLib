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
                "32 32 32",
            };

            List<string> lines = new List<string>();

            float @base = (float)byte.MaxValue / 2.0f; // signed?
            // float @base = (float)byte.MaxValue / 1.0f; // unsigned?

            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 1024; x++)
                {
                    int[] neutral = new int[] { (x % 32), y, (x / 32) }; // 1024x32

                    string s = $"{neutral[0]} {neutral[1]} {neutral[2]} ";

                    float[] rgb = new float[] { (float)lutimage.ReadByte() / @base, (float)lutimage.ReadByte() / @base, (float)lutimage.ReadByte() / @base };
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
