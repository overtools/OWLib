using System;

namespace DataTool {
    [AttributeUsage(AttributeTargets.Class)]
    public class ToolAttribute : Attribute {
        public string Keyword;
        public string Description = null;
        public ushort[] TrackTypes = null;
        public bool HasCustomFlags = false;
        public bool IsSensitive = false;

        public ToolAttribute(string Keyword) {
            this.Keyword = Keyword;
        }
    }
}
