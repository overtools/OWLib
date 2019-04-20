using System.Collections.Generic;
using DataTool.DataModels;
using DataTool.Flag;
using DataTool.JSON;
using static DataTool.Program;
using static DataTool.Helper.Logger;

namespace DataTool.ToolLogic.List.Misc {
    [Tool("list-report-responses", CustomFlags = typeof(ListFlags), IsSensitive = true)]
    public class ListReportResponses : JSONTool, ITool {
        public void Parse(ICLIFlags toolFlags) {
            var data = GetData();
            
            if (toolFlags is ListFlags flags)
                if (flags.JSON) {
                    OutputJSON(data, flags);
                    return;
                }

            foreach (var response in data) {
                Log($"Title: {response.Title ?? "N/A"}");
                Log($"Description: {response.Description ?? "N/A"}");
                Log("\n");
            }
        }

        private static List<ReportResponse> GetData() {
            var responses = new List<ReportResponse>();

            foreach (ulong key in TrackedFiles[0xEB]) {
                var reportResponse = new ReportResponse(key);
                if (reportResponse.GUID != 0)
                    responses.Add(reportResponse);
            }

            return responses;
        }
    }
}