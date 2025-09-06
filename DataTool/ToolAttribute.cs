using System;
using DataTool.ToolLogic.Extract;
using DataTool.ToolLogic.List;

namespace DataTool;

[AttributeUsage(AttributeTargets.Class)]
public class ToolAttribute : Attribute {
    public string Keyword { get; }
    public string Description { get; set; } = string.Empty;
    public bool IsSensitive { get; set; } = false;
    public Type CustomFlags { get; set; } = null;
    public string Name { get; set; } = string.Empty;
    public string[] Aliases { get; set; }

    public bool UtilNoArchiveNeeded = false;

    public ToolAttribute(string keyword, params string[] aliases) {
        Keyword = keyword;
        Aliases = aliases;
    }
}

/// <summary>
/// Helper Tool attribute for Extract modes that saves you needing to specify the flags
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ExtractToolAttribute : ToolAttribute {
    public ExtractToolAttribute(string keyword, params string[] aliases) : base(keyword, aliases) {
        CustomFlags = typeof(ExtractFlags);
    }
}

/// <summary>
/// Helper Tool attribute for List modes that saves you needing to specify the flags
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ListToolAttribute : ToolAttribute {
    public ListToolAttribute(string keyword, params string[] aliases) : base(keyword, aliases) {
        CustomFlags = typeof(ListFlags);
    }
}