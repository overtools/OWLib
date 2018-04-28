using TankLib.CASC;

namespace TankView.ViewModel
{
    public class GUIDEntry
    {
        public int Size { get; set; }
        public int Offset { get; set; }
        public string Filename { get; set; }
        public string FullPath { get; set; }
        public ContentFlags Flags { get; set; }
        public LocaleFlags Locale { get; set; }
        public MD5Hash Hash { get; set; }
        public ulong GUID { get; set; }
        public ApplicationPackageManifest APM { get; set; }
    }
}
