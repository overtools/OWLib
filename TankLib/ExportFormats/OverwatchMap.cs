using System;
using System.IO;

namespace TankLib.ExportFormats {
    /// <summary>
    /// OWMAP format
    /// </summary>
    public class OverwatchMap : IExportFormat {
        public string Extension => "owmap";
        
        public OverwatchMap() {
            throw new NotImplementedException();
        }
        
        public void Write(Stream stream) {            
            throw new NotImplementedException();
        }
    }
}