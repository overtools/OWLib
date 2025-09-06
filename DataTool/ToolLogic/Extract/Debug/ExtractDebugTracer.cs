using System;
using System.IO;
using DataTool.Flag;
using DataTool.SaveLogic;

namespace DataTool.ToolLogic.Extract.Debug;

[Tool("extract-debug-tracer", Description = "Extract tracer (debug)", CustomFlags = typeof(ExtractFlags), IsSensitive = true)]
public class ExtractDebugTracer : ITool {
    public void Parse(ICLIFlags toolFlags) {
        GetTracer(toolFlags);
    }

    public void GetTracer(ICLIFlags toolFlags) {
        const string container = "Tracer";

        string basePath;
        if (toolFlags is ExtractFlags flags) {
            basePath = flags.OutputPath;
        } else {
            throw new Exception("no output path");
        }

        string path = Path.Combine(basePath, container);

        FindLogic.Combo.ComboInfo comboInfo = new FindLogic.Combo.ComboInfo();
        FindLogic.Combo.Find(comboInfo, 0x0400000000002BBD); // tracer new
        var context = new Combo.SaveContext(comboInfo);
        Combo.Save(flags, path, context);
    }
}