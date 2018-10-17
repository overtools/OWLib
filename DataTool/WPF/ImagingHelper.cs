using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Windows.Media;

namespace DataTool.WPF {
    public static class ImagingHelper {
        private static ConcurrentDictionary<string, int> CachedLineHeight = new ConcurrentDictionary<string, int>();

        public static int LineHeight(string font, double dpi = 16) {
            return CachedLineHeight.ContainsKey($"{dpi}-{font}") ? CachedLineHeight[$"{dpi}-{font}"] : LineHeight(new FontFamily(font), dpi);
        }
        
        public static int LineHeight(FontFamily family, double dpi = 16) {
            if (CachedLineHeight.ContainsKey($"{dpi}-{family.FamilyNames.First()}")) {
                return CachedLineHeight[$"{dpi}-{family.FamilyNames.First()}"];
            }
            var height = (int) Math.Ceiling(dpi * family.LineSpacing);
            CachedLineHeight[$"{dpi}-{family.FamilyNames.First()}"] = height;
            return height;
        }

        public static double CalculateSizeAS(double value, double axis, double target) {
            return value * (target / axis);
        }
    }
}
