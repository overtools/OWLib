using System.IO;
using TankLib.CASC;
using TankLib.CASC.Handlers;
using TankView.ViewModel;

namespace TankView.Helper
{
    public static class IOHelper
    {
        public static Stream OpenFile(MD5Hash loadHash)
        {
            if (MainWindow.CASC.EncodingHandler.GetEntry(loadHash, out EncodingEntry encoding) == true)
            {
                Stream s = MainWindow.CASC.OpenFile(encoding.Key);
                s.Position = 0;
                return s;
            }
            return null;
        }
        public static Stream OpenFile(MD5Hash loadHash, long offset, int size)
        {
            Stream file = OpenFile(loadHash);
            if(file == null)
            {
                return null;
            }
            file.Position = offset;
            byte[] data = new byte[size];
            file.Read(data, 0, size);
            MemoryStream ms = new MemoryStream(data)
            {
                Position = 0
            };
            return ms;
        }

        public static Stream OpenFile(ApplicationPackageManifest.Types.PackageRecord packageRecord)
        {
            if (packageRecord.Flags.HasFlag(ContentFlags.Bundle))
            {
                return OpenFile(packageRecord.LoadHash, packageRecord.Offset, (int)packageRecord.Size);
            }
            else
            {
                return OpenFile(packageRecord.LoadHash);
            }
        }

        public static Stream OpenFile(GUIDEntry entry)
        {
            if (entry.Flags.HasFlag(ContentFlags.Bundle))
            {
                return OpenFile(entry.Hash, entry.Offset, (int)entry.Size);
            }
            else
            {
                return OpenFile(entry.Hash);
            }
        }
    }
}
