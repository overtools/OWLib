using System.IO;

namespace TankLib.ExportFormats {
    public class OverawatchMaterial : IExportFormat {
        public string Extension => "owmat";
        
        public void Write(Stream stream) {
            throw new System.NotImplementedException();
        }
    }
}