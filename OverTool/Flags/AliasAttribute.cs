using System;

namespace OverTool.Flags {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true, Inherited = true)]
    public class AliasAttribute : Attribute {
        public string Alias;

        public new string ToString() {
            return Alias;
        }
    }
}
