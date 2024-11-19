#nullable enable
namespace DataTool.DataModels {
    public class UXDisplayText {
        public string? Value { get; set; }

        public UXDisplayText(ulong guid) {
            Value = Helper.IO.GetString(guid);
        }
    }
}
