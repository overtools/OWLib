using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using DataTool.WPF;
using DataTool.WPF.IO;
using DirectXTexNet;
using TankLib;
using TankView.Helper;
using TankView.Properties;
using TankView.View;
using TACTLib.Client;
using TACTLib.Container;
using TACTLib.Core.Product.Tank;

namespace TankView.ViewModel {
    public class GUIDCollection : INotifyPropertyChanged, IDisposable {
        private readonly ClientHandler Client;
        private readonly ProductHandler_Tank Tank;
        private readonly ProgressWorker _worker;

        private GUIDEntry _top;

        public GUIDEntry TopSelectedEntry {
            get => _top;
            set {
                try {
                    UpdateControl(value);
                } catch {
                    // ignored
                }

                _top = value;
                NotifyPropertyChanged(nameof(TopSelectedEntry));
            }
        }

        private void UpdateControl(GUIDEntry value) {
            if (PreviewControl is IDisposable disposable) {
                disposable.Dispose();
            }

            if (PreviewSource is IDisposable disposable2) {
                disposable2.Dispose();
            }

            if (!ShowPreview) {
                PreviewSource = null;
                PreviewControl = null;
            }

            switch (DataHelper.GetDataType(value)) {
                case DataHelper.DataType.Image: {
                    PreviewSource = DataHelper.ConvertDDS(value, DXGI_FORMAT.R8G8B8A8_UNORM, DDSConverter.ImageFormat.PNG, 0);
                    PreviewControl = new PreviewDataImage();
                }
                    break;
                case DataHelper.DataType.Sound: {
                    PreviewSource = DataHelper.ConvertSound(value);
                    PreviewControl = new PreviewDataSound();
                    ((PreviewDataSound) PreviewControl).SetAudio(PreviewSource as Stream);
                }
                    break;
                case DataHelper.DataType.Model: {
                    PreviewSource = null;
                    PreviewControl = new PreviewDataModel();
                }
                    break;
                case DataHelper.DataType.String: {
                    PreviewSource = DataHelper.GetString(value);
                    PreviewControl = new PreviewDataString();
                }
                    break;
                case DataHelper.DataType.Unknown:
                    break;
                default: {
                    PreviewSource = null;
                    PreviewControl = null;
                }
                    break;
            }
        }

        private Control _control;

        public Control PreviewControl {
            get => _control;
            set {
                _control = value;
                NotifyPropertyChanged(nameof(PreviewControl));
            }
        }

        private BitmapSource _frame;

        public bool ShowPreview {
            get => Settings.Default.ShowPreview;
            set {
                Settings.Default.ShowPreview = value;
                Settings.Default.Save();
                UpdateControl(_top);
                NotifyPropertyChanged(nameof(ShowPreview));
                NotifyPropertyChanged(nameof(ListRow));
                NotifyPropertyChanged(nameof(PreviewRow));
            }
        }

        public GridLength ListRow => ShowPreview ? new GridLength(250, GridUnitType.Pixel) : new GridLength(1, GridUnitType.Star);

        public GridLength PreviewRow => ShowPreview ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

        private object _previewData = null;

        public object PreviewSource {
            get => _previewData;
            set {
                _frame = null;
                if (value != null) {
                    switch (DataHelper.GetDataType(_top)) {
                        case DataHelper.DataType.Image: {
                            MemoryStream stream = new MemoryStream((byte[]) value);
                            PngBitmapDecoder decoder = new PngBitmapDecoder(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.None);
                            _frame = decoder.Frames[0];
                        }
                            break;
                        case DataHelper.DataType.Unknown:
                            break;
                        case DataHelper.DataType.Sound:
                            break;
                        case DataHelper.DataType.Model:
                            break;
                        case DataHelper.DataType.String:
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                _previewData = value;
                NotifyPropertyChanged(nameof(ImageWidth));
                NotifyPropertyChanged(nameof(ImageHeight));
                NotifyPropertyChanged(nameof(PreviewSource));
            }
        }

        public double ImageWidth => _frame?.Width ?? 0;

        public double ImageHeight => _frame?.Height ?? 0;

        private string searchQuery = string.Empty;

        public string Search {
            get => searchQuery;
            set {
                searchQuery = value?.Trim() ?? string.Empty;
                NotifyPropertyChanged(nameof(SelectedEntries));
            }
        }

        private string _orderBy = "FileName";
        private bool _orderDescending = true;

        public void OrderBy(string orderBy, ListSortDirection direction) {
            _orderBy = orderBy;
            _orderDescending = direction == ListSortDirection.Descending;
            NotifyPropertyChanged(nameof(SelectedEntries));
        }

        private List<GUIDEntry> _selected = null;

        public List<GUIDEntry> SelectedEntries {
            get {
                var selectedWithSearch = !string.IsNullOrWhiteSpace(searchQuery) ? _selected.Where(x => CultureInfo.CurrentUICulture.CompareInfo.IndexOf(x.Filename, searchQuery, CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreWidth) > -1).ToList() : _selected;

                switch (_orderBy) {
                    case "Size":
                        return selectedWithSearch.OrderByWithDirection(x => x.Size, _orderDescending).ToList();
                    case "FileName":
                        return selectedWithSearch.OrderByWithDirection(x => x.GUID, _orderDescending).ToList();
                    case "#":
                    default:
                        return selectedWithSearch;
                }
            }
            set {
                _selected = value.OrderBy(x => x?.Filename).ToList();
                NotifyPropertyChanged(nameof(SelectedEntries));
            }
        }

        public List<Folder> Root => new List<Folder> {
            Data
        };

        public Folder Data = new Folder("/", "/");

        public GUIDCollection() { }

        public GUIDCollection(ClientHandler client, ProductHandler_Tank tank, ProgressWorker worker) {
            Client = client;
            Tank = tank;
            _worker = worker;

            int totalHashList = tank.Assets.Count;
            long total = tank.RootFiles.Length + totalHashList;

            worker?.ReportProgress(0, "Building file tree...");

            long c = 0;

            foreach (var entry in Tank.RootFiles.OrderBy(x => x.FileName).ToArray()) {
                c++;
                worker?.ReportProgress((int) (((float) c / (float) total) * 100));
                AddEntry(entry.FileName, 0, entry.MD5, 0, "None");
            }

            if (totalHashList != default) {
                foreach (ContentManifestFile contentManifest in new [] {Tank.MainContentManifest, Tank.SpeechContentManifest}) {
                    foreach (var record in contentManifest.HashList) {
                        c++;
                        if (c % 10000 == 0) {
                            worker?.ReportProgress((int) (((float) c / (float) total) * 100));
                        }

                        ushort typeVal = teResourceGUID.Type(record.GUID);
                        string typeStr = typeVal.ToString("X3");
                        DataHelper.DataType typeData = DataHelper.GetDataType(typeVal);
                        if (typeData != DataHelper.DataType.Unknown) {
                            typeStr = $"{typeStr} ({typeData.ToString()})";
                        }

                        // todo: add cmf name again?
                        AddEntry($"files/{typeStr}", record.GUID, record.ContentKey, (int) record.Size, "None");
                    }
                }
            }

            worker?.ReportProgress(0, "Sorting tree...");
            long t = GetSize(Root);
            Sort(Root, 0, t);

            NotifyPropertyChanged(nameof(Data));
            NotifyPropertyChanged(nameof(Root));

            try {
                SelectedEntries = Data.Folders.FirstOrDefault(x => x.Name.EndsWith("Client"))?.Files ?? new List<GUIDEntry>();
            } catch (KeyNotFoundException) {
                //
            }
        }

        private long GetSize(List<Folder> root) {
            long i = 0;
            foreach (Folder folder in root) {
                i += folder.Folders.LongCount();
                GetSize(folder.Folders);
            }

            return i;
        }

        private void Sort(List<Folder> parent, long c, long t) {
            c++;
            _worker?.ReportProgress((int) (((float) c / (float) t) * 100));
            foreach (Folder folder in parent) {
                folder.Folders = folder.Folders.OrderBy(x => x.Name).ToList();
                Sort(folder.Folders, c, t);
            }
        }

        private void AddEntry(string path, ulong guid, CKey ckey, int size, string locale) {
            string dir = guid != 0 ? path : Path.GetDirectoryName(path);

            string filename = guid != 0 ? teResourceGUID.AsString(guid) : Path.GetFileName(path);

            Folder d = Data;

            foreach (string part in dir.Split(new char[] {Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar}, StringSplitOptions.RemoveEmptyEntries)) {
                if (!d.HasFolder(part)) {
                    d.Add(part);
                }

                d = d[part];
            }

            if (size == 0 && guid == 0) {
                if (Client.EncodingHandler.TryGetEncodingEntry(ckey, out var info)) {
                    size = info.GetSize();
                }
            }

            d.Files.Add(new GUIDEntry {
                Filename = filename,
                GUID = guid,
                FullPath = Path.Combine(d.FullPath, filename),
                Size = size,
                Locale = locale,
                ContentKey = ckey
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void Dispose() {
            if (PreviewControl is IDisposable disposable) {
                disposable.Dispose();
            }

            if (PreviewSource is IDisposable disposable2) {
                disposable2.Dispose();
            }
        }
    }
}
