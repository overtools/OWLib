using System;

namespace DataTool {
    [AttributeUsage(AttributeTargets.Class)]
    public class ToolAttribute : Attribute {
        public string Keyword;
        public string Description = null;
        public ushort[] TrackTypes = null;
        public bool IsSensitive = false;
        public Type CustomFlags = null;

        public ToolAttribute(string keyword) {
            Keyword = keyword;
        }
    }
}
