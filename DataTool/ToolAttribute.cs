using System;

namespace DataTool {
    [AttributeUsage(AttributeTargets.Class)]
    public class ToolAttribute : Attribute {
        public string Keyword { get; }
        public string Description { get; set; } = string.Empty;
        public bool IsSensitive { get; set; } = false;
        public Type CustomFlags { get; set; } = null;
        public string Name { get; set; } = string.Empty;
        
        public ToolAttribute(string keyword) {
            Keyword = keyword;
        }
    }
}
