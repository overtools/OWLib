using System;
using System.IO;

namespace TankLib.ExportFormats {
    /// <summary>
    /// OWEFFECT format
    /// </summary>
    public class OverwatchEffect : IExportFormat {
        public string Extension => "oweffect";
        
        public OverwatchEffect() {
            throw new NotImplementedException();
        }
        
        public void Write(Stream stream) {            
            throw new NotImplementedException();
        }
    }
}