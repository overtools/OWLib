using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using DirectXTexNet;
using TankLib;
using TankLib.CASC;
using TankLib.CASC.Handlers;
using TankLib.CASC.Helpers;
using TankView.Helper;
using TankView.Properties;
using TankView.View;
using static TankLib.CASC.ApplicationPackageManifest.Types;

namespace TankView.ViewModel
{
    public class GUIDCollection : INotifyPropertyChanged, IDisposable
    {
        private CASCConfig Config;
        private CASCHandler CASC;
        private ProgressReportSlave Slave;

        private GUIDEntry _top;
        public GUIDEntry TopSelectedEntry {
            get {
                return _top;
            } set {
                UpdateControl(value);
                _top = value;
                NotifyPropertyChanged(nameof(TopSelectedEntry));
            }
        }

        private void UpdateControl(GUIDEntry value)
        {
            if (PreviewControl is IDisposable disposable)
            {
                disposable.Dispose();
            }
            if (PreviewSource is IDisposable disposable2)
            {
                disposable2.Dispose();
            }
            if (!ShowPreview)
            {
                PreviewSource = null;
                PreviewControl = null;
            }
            switch (DataHelper.GetDataType(value))
            {
                case DataHelper.DataType.Image:
                    {
                        PreviewSource = DataHelper.ConvertDDS(value, DXGI_FORMAT.R8G8B8A8_UNORM, DataHelper.ImageFormat.PNG, 0);
                        PreviewControl = new PreviewDataImage();
                    }
                    break;
                case DataHelper.DataType.Sound:
                    {
                        PreviewSource = DataHelper.ConvertSound(value);
                        PreviewControl = new PreviewDataSound();
                        (PreviewControl as PreviewDataSound).SetAudio(PreviewSource as Stream);
                    }
                    break;
                case DataHelper.DataType.Model:
                    {
                        PreviewSource = null;
                        PreviewControl = new PreviewDataModel();
                    }
                    break;
                case DataHelper.DataType.String:
                    {
                        PreviewSource = DataHelper.GetString(value);
                        PreviewControl = new PreviewDataString();
                    }
                    break;
                default:
                    {
                        PreviewSource = null;
                        PreviewControl = null;
                    }
                    break;
            }
        }

        private Control _control = null;

        public Control PreviewControl {
            get {
                return _control;
            } set {
                _control = value;
                NotifyPropertyChanged(nameof(PreviewControl));
            }
        }

        private BitmapSource _frame = null;
        
        public bool ShowPreview {
            get {
                return Settings.Default.ShowPreview;
            }
            set {
                Settings.Default.ShowPreview = value;
                Settings.Default.Save();
                UpdateControl(_top);
                NotifyPropertyChanged(nameof(ShowPreview));
                NotifyPropertyChanged(nameof(ListRow));
                NotifyPropertyChanged(nameof(PreviewRow));
            }
        }

        public GridLength ListRow {
            get {
                if (ShowPreview)
                {
                    return new GridLength(250, GridUnitType.Pixel);
                }
                return new GridLength(1, GridUnitType.Star);
            }
        }

        public GridLength PreviewRow {
            get {
                if (ShowPreview)
                {
                    return new GridLength(1, GridUnitType.Star);
                }
                return new GridLength(0);
            }
        }

        private object _previewData = null;
        public object PreviewSource {
            get {
                return _previewData;
            } set {
                _frame = null;
                if (value != null)
                {
                    switch (DataHelper.GetDataType(_top))
                    {
                        case DataHelper.DataType.Image:
                            {
                                MemoryStream stream = new MemoryStream((byte[])value);
                                PngBitmapDecoder decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
                                _frame = decoder.Frames[0];
                            }
                            break;
                    }
                }
                _previewData = value;
                NotifyPropertyChanged(nameof(ImageWidth));
                NotifyPropertyChanged(nameof(ImageHeight));
                NotifyPropertyChanged(nameof(PreviewSource));
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

        public GUIDCollection() { }

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
                    ushort typeVal = teResourceGUID.Type(record.Key);
                    string typeStr = typeVal.ToString("X3");
                    DataHelper.DataType typeData = DataHelper.GetDataType(typeVal);
                    if(typeData != DataHelper.DataType.Unknown)
                    {
                        typeStr = $"{typeStr} ({typeData.ToString()})";
                    }
                    AddEntry($"files/{Path.GetFileNameWithoutExtension(apm.Name)}/{typeStr}", record.Key, apm, record.Value.LoadHash, (int)record.Value.Size, (int)record.Value.Offset, record.Value.Flags, apm.Locale);
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

        public void Dispose()
        {
            if (PreviewControl is IDisposable disposable)
            {
                disposable.Dispose();
            }
            if (PreviewSource is IDisposable disposable2)
            {
                disposable2.Dispose();
            }
        }
    }
}
