using System;

namespace OWLib {
    public static class Util {
        public static string GetEnumName(Type t, object value, string fallback = "{0}") {
            string v = Enum.GetName(t, value);
            if (v == null) {
                v = string.Format(fallback, value.ToString());
            }
            return v;
        }
    }
}
