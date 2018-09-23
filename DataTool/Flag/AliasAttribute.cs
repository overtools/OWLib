using System;

namespace DataTool.Flag {
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class AliasAttribute : Attribute {
        public string Alias;
        
        public AliasAttribute() {}

        public AliasAttribute(string alias) {
            Alias = alias;
        }
        
        public new string ToString() {
            return Alias;
        }
    }
}
