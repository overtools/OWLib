using System.IO;

namespace TankLib.ExportFormats {
    public interface IExportFormat {
        string Extension { get; }
        void Write(Stream stream);
    }
}