using System;
using System.IO;

namespace TankLib.ExportFormats {
    public class SeanimAnimation : IExportFormat {
        public string Extension => "seanim";
        
        public void Write(Stream stream) {
            throw new NotImplementedException();
        }
    }
}