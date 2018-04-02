using System.IO;

namespace TankLib.ExportFormats {
    public interface IExportFormat {
        /// <summary>File extension</summary>
        string Extension { get; }
        
        /// <summary>Write format to a Stream</summary>
        /// <param name="stream">Target Stream</param>
        void Write(Stream stream);
    }
}