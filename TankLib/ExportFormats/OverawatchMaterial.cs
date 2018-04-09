using System;
using System.IO;

namespace TankLib.ExportFormats {
    /// <summary>
    /// OWMAT format
    /// </summary>
    public class OverawatchMaterial : IExportFormat {
        public string Extension => "owmat";
        
        public void Write(Stream stream) {
            throw new NotImplementedException();
        }
    }
}