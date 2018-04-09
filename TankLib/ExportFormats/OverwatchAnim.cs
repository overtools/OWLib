using System;
using System.IO;

namespace TankLib.ExportFormats {
    /// <summary>
    /// OWANIM format
    /// </summary>
    public class OverwatchAnim : IExportFormat {
        public string Extension => "owanim";
        
        public OverwatchAnim() {
            throw new NotImplementedException();
        }
        
        public void Write(Stream stream) {            
            throw new NotImplementedException();
        }
    }
}