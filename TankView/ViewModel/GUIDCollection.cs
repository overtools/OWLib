using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TankLib;
using TankLib.CASC;
using TankLib.CASC.Handlers;
using TankLib.CASC.Helpers;
using TankView.Helper;
using static TankLib.CASC.ApplicationPackageManifest.Types;

namespace TankView.ViewModel
{
    public class GUIDCollection : INotifyPropertyChanged
    {
        private CASCConfig Config;
        private CASCHandler CASC;
        private ProgressReportSlave Slave;

        private GUIDEntry _top;
        public GUIDEntry TopSelectedEntry {
            get {
                return _top;
            } set {
                ImageSource = ImageHelper.ConvertDDS(value, DirectXTexNet.DXGI_FORMAT.R8G8B8A8_UNORM, ImageHelper.ImageFormat.PNG, 0);
                _top = value;
                NotifyPropertyChanged(nameof(TopSelectedEntry));
            }
        }


        private BitmapSource _frame = null;

        private byte[] _image = null;
        public byte[] ImageSource {
            get {
                return _image;
            } set {
                if (value != null)
                {
                    MemoryStream stream = new MemoryStream(value);
                    PngBitmapDecoder decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
                    _frame = decoder.Frames[0];
                    double dpi = 96;
                    int width = _frame.PixelWidth;
                    int height = _frame.PixelHeight;

                    int stride = width * 4; // 4 bytes per pixel
                    byte[] pixelData = new byte[stride * height];
                    _frame.CopyPixels(pixelData, stride, 0);

                    _frame = BitmapSource.Create(width, height, dpi, dpi, _frame.Format, null, pixelData, stride);
                }
                _image = value;
                NotifyPropertyChanged(nameof(ValidImage));
                NotifyPropertyChanged(nameof(ImageWidth));
                NotifyPropertyChanged(nameof(ImageHeight));
                NotifyPropertyChanged(nameof(ImageSource));
            }
        }

        public bool ValidImage {
            get {
                return _image != null;
            }
        }

        public double ImageWidth {
            get {
                return _frame?.Width ?? 0;
            }
        }

        public double ImageHeight {
            get {
                return _frame?.Height ?? 0;
            }
        }

        private List<GUIDEntry> _selected = null;
        public List<GUIDEntry> SelectedEntries {
            get {
                return _selected;
            } set {
                _selected = value.OrderBy(x => x.Filename).ToList();
                NotifyPropertyChanged(nameof(SelectedEntries));
            }
        }

        public List<Folder> Root {
            get {
                return new List<Folder>
                {
                    Data
                };
            }
        }

        public Folder Data = new Folder("/", "/");

        public GUIDCollection(CASCConfig Config, CASCHandler CASC, ProgressReportSlave Slave)
        {
            this.Config = Config;
            this.CASC = CASC;
            this.Slave = Slave;

            long total = CASC.RootHandler.RootFiles.Count + CASC.RootHandler.APMFiles.SelectMany(x => x.FirstOccurence).LongCount();

            Slave?.ReportProgress(0, "Building file tree...");

            long c = 0;

            foreach(KeyValuePair<string, MD5Hash> entry in this.CASC.RootHandler.RootFiles.OrderBy(x => x.Key).ToArray())
            {
                c++;
                Slave?.ReportProgress((int)(((float)c / (float)total) * 100));
                AddEntry(entry.Key, 0, null, entry.Value, 0, 0, ContentFlags.None, LocaleFlags.None);
            }

            foreach(ApplicationPackageManifest apm in this.CASC.RootHandler.APMFiles.OrderBy(x => x.Name).ToArray())
            {
                foreach (KeyValuePair<ulong, PackageRecord> record in apm.FirstOccurence.OrderBy(x => x.Key).ToArray())
                {
                    c++;
                    if (c % 10000 == 0)
                    {
                        Slave?.ReportProgress((int)(((float)c / (float)total) * 100));
                    }
                    AddEntry($"files/{Path.GetFileNameWithoutExtension(apm.Name)}/{teResourceGUID.Type(record.Key):X3}", record.Key, apm, record.Value.LoadHash, (int)record.Value.Size, (int)record.Value.Offset, record.Value.Flags, apm.Locale);
                }
            }

            Slave?.ReportProgress(0, "Sorting tree...");
            long t = GetSize(Root);
            Sort(Root, 0, t);

            NotifyPropertyChanged(nameof(Data));
            NotifyPropertyChanged(nameof(Root));

            try
            {
                SelectedEntries = Data["RetailClient"]?.Files;
            }
            catch(KeyNotFoundException)
            {
                //
            }
        }

        private long GetSize(List<Folder> root)
        {
            long i = 0;
            foreach (Folder folder in root)
            {
                i += folder.Folders.LongCount();
                GetSize(folder.Folders);
            }
            return i;
        }

        private void Sort(List<Folder> parent, long c, long t)
        {
            c++;
            Slave?.ReportProgress((int)(((float)c / (float)t) * 100));
            foreach (Folder folder in parent)
            {
                folder.Folders = folder.Folders.OrderBy(x => x.Name).ToList();
                Sort(folder.Folders, c, t);
            }
        }

        private void AddEntry(string path, ulong guid, ApplicationPackageManifest apm, MD5Hash hash, int size, int offset, ContentFlags flags, LocaleFlags locale)
        {
            string dir = guid != 0 ? path : Path.GetDirectoryName(path);
            string filename = guid != 0 ? teResourceGUID.AsString(guid) : Path.GetFileName(path);

            Folder d = Data;

            foreach (string part in dir.Split(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!d.HasFolder(part))
                {
                    d.Add(part);
                }
                d = d[part];
            }

            if (size == 0 && guid == 0)
            {
                if (CASC.EncodingHandler.GetEntry(hash, out EncodingEntry enc))
                {
                    size = enc.Size;
                }
            }

            d.Files.Add(new GUIDEntry
            {
                Filename = filename,
                GUID = guid,
                FullPath = Path.Combine(d.FullPath, filename),
                Size = size,
                Offset = offset,
                Flags = flags,
                Locale = locale,
                Hash = hash,
                APM = apm
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
