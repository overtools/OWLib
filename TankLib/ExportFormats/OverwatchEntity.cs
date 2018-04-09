using System;
using System.IO;

namespace TankLib.ExportFormats {
    /// <summary>
    /// OWENTITY format
    /// </summary>
    public class OverwatchEntity : IExportFormat {
        public string Extension => "owentity";
        
        public OverwatchEntity() {
            throw new NotImplementedException();
        }
        
        public void Write(Stream stream) {            
            throw new NotImplementedException();
        }
    }
}