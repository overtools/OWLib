#nullable enable
using TankLib;
using TankLib.STU.Types;
using static DataTool.Helper.STUHelper;
using static DataTool.Helper.IO;

namespace DataTool.DataModels;

public class ReportResponse {
    public teResourceGUID GUID { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }

    public ReportResponse(STU_71C0D73D stu, ulong key = default) {
        Init(stu, key);
    }

    private void Init(STU_71C0D73D? reportResponse, ulong key = default) {
        if (reportResponse == null) return;

        GUID = (teResourceGUID) key;
        Title = GetString(reportResponse.m_BEBA3AEF);
        Description = GetString(reportResponse.m_C65AA24E);
    }

    public static ReportResponse? Load(ulong key) {
        var stu = GetInstance<STU_71C0D73D>(key);
        if (stu == null) return null;
        return new ReportResponse(stu, key);
    }
}