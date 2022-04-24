using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DataTool.DataModels;
using DataTool.DataModels.Hero;
using DataTool.Helper;
using DirectXTexNet;
using JetBrains.Annotations;
using TankLib;
using TankView.Helper;
using TankView.Properties;
using TankView.View;
using TACTLib.Client;
using TACTLib.Container;
using TACTLib.Core.Product.Tank;
using TankView.ObjectModel;
using Logger = TACTLib.Logger;

namespace TankView.ViewModel {
    public class GUIDCollection : INotifyPropertyChanged, IDisposable {
        public static readonly string ApplicationDataPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "overtools", "TankView");
        private HashSet<ulong> PreviousBuildGuids = new HashSet<ulong>();
        private readonly ClientHandler Client;
        private readonly ProductHandler_Tank Tank;
        private readonly ProgressWorker _worker;
        internal static Dictionary<ushort, HashSet<ulong>> TrackedFiles = new Dictionary<ushort, HashSet<ulong>>();

        private GUIDEntry _top;
        public string GUIDStr;

        public string GUIDString => teResourceGUID.AsString(_top.GUID);

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

        private void UpdateControl([CanBeNull] GUIDEntry value) {
            if (PreviewControl is IDisposable disposable) {
                disposable.Dispose();
            }

            if (PreviewSource is IDisposable disposable2) {
                disposable2.Dispose();
            }

            if (!ShowPreview || value == null) {
                PreviewSource = null;
                PreviewControl = null;
                return;
            }

            switch (DataHelper.GetDataType(value)) {
                case DataHelper.DataType.Image: {
                    PreviewSource = new RGBABitmapSource(DataHelper.ConvertDDS(value.GUID, DXGI_FORMAT.R8G8B8A8_UNORM, 0, out var width, out var height), width, height);
                    PreviewControl = new PreviewDataImage();
                }
                    break;
                case DataHelper.DataType.Sound: {
                    PreviewSource = DataHelper.ConvertSound(value);
                    PreviewControl = new PreviewDataSound();
                    ((PreviewDataSound) PreviewControl).SetAudio(PreviewSource as Stream);

                    if (EnableAutoPlay) {
                        ((PreviewDataSound) PreviewControl).Play(null, null);
                    }
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
                case DataHelper.DataType.MapHeader: {
                    PreviewSource = null;
                    PreviewControl = new PreviewDataMapHeader(value, DataHelper.GetMap(value));
                }
                    break;
                case DataHelper.DataType.Hero: {
                    PreviewSource = null;
                    PreviewControl = new PreviewHeroData(value, DataHelper.GetHero(value));
                }
                    break;
                case DataHelper.DataType.Conversation: {
                    PreviewSource = null;
                    PreviewControl = new PreviewConversation(value, DataHelper.GetConversation(value), ConversationVoiceLineMapping);
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

        public bool ShowPreview {
            get => Settings.Default.ShowPreview;
            set {
                Settings.Default.ShowPreview = value;
                Settings.Default.Save();
                UpdateControl(_top);
                NotifyPropertyChanged(nameof(ShowPreview));
                NotifyPropertyChanged(nameof(ListRow));
                NotifyPropertyChanged(nameof(PreviewRow));
                NotifyPropertyChanged(nameof(PreviewRowMin));
            }
        }

        public bool EnableAutoPlay {
            get => Settings.Default.AutoPlay;
            set {
                Settings.Default.AutoPlay = value;
                Settings.Default.Save();
                NotifyPropertyChanged(nameof(EnableAutoPlay));
            }
        }

        public bool OnlyShowNewFiles {
            get => Settings.Default.OnlyShowNewFiles;
            set {
                Settings.Default.OnlyShowNewFiles = value;
                NotifyPropertyChanged(nameof(OnlyShowNewFiles));
                NotifyPropertyChanged(nameof(SelectedEntries));
            }
        }

        public bool ShowPreviewList => DataHelper.GetDataType(_selected?.FirstOrDefault()) == DataHelper.DataType.Image;

        public GridLength ListRow => ShowPreview ? new GridLength(250, GridUnitType.Pixel) : new GridLength(1, GridUnitType.Star);

        public GridLength PreviewRow => ShowPreview ? new GridLength(1, GridUnitType.Star) : new GridLength(0);

        public double PreviewRowMin => ShowPreview ? 50 : 0;

        private object _previewData = null;

        public object PreviewSource {
            get => _previewData;
            set {
                _previewData = value;
                NotifyPropertyChanged(nameof(PreviewSource));
            }
        }

        public int ImageWidth { get; set; }
        public int ImageHeight { get; set; }

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

        private GUIDEntry[] _selected = null;

        public GUIDEntry[] SelectedEntries {
            get {
                var selectedWithSearch = _selected;

                if (_selected == null) {
                    return null;
                }

                if (!string.IsNullOrEmpty(searchQuery) || OnlyShowNewFiles) {
                    var newResults = new List<GUIDEntry>();
                    foreach (var x in _selected) {
                        if (OnlyShowNewFiles && !x.IsNew) {
                            continue;
                        }

                        if (string.IsNullOrEmpty(searchQuery) && OnlyShowNewFiles) {
                            newResults.Add(x);
                            continue;
                        }

                        if (CultureInfo.CurrentUICulture.CompareInfo.IndexOf(x.Filename, searchQuery, CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreWidth) > -1 ||
                            CultureInfo.CurrentUICulture.CompareInfo.IndexOf(x.StringValue ?? string.Empty, searchQuery, CompareOptions.IgnoreCase | CompareOptions.IgnoreSymbols | CompareOptions.IgnoreWidth) > -1) {
                            newResults.Add(x);
                        }
                    }

                    selectedWithSearch = newResults.ToArray();
                }

                switch (_orderBy) {
                    case "Size":
                        return selectedWithSearch.OrderByWithDirection(x => x.Size, _orderDescending).ToArray();
                    case "FileName":
                        return selectedWithSearch.OrderByWithDirection(x => x.GUID, _orderDescending).ToArray();
                    case "Value":
                        return selectedWithSearch.OrderByWithDirection(x => x.StringValue, _orderDescending).ToArray();
                    case "#":
                    default:
                        return selectedWithSearch;
                }
            }
            set {
                _selected = value.OrderBy(x => x?.Filename).ToArray();
                NotifyPropertyChanged(nameof(SelectedEntries));
                NotifyPropertyChanged(nameof(ShowPreviewList));
            }
        }

        public List<Folder> Root => new List<Folder> {
            Data
        };

        public Folder Data = new Folder("/", "/", true);

        public GUIDCollection() { }

        public GUIDCollection(ClientHandler client, ProductHandler_Tank tank, ProgressWorker worker) {
            Client = client;
            Tank = tank;
            _worker = worker;

            int totalHashList = tank.m_assets.Count;
            long total = tank.m_rootFiles.Length + totalHashList;

            worker?.ReportProgress(0, "Building file tree...");

            long c = 0;

            foreach (var entry in Tank.m_rootFiles.OrderBy(x => x.FileName).ToArray()) {
                c++;
                worker?.ReportProgress((int) (((float) c / (float) total) * 100));
                AddEntry(entry.FileName, 0, entry.MD5, 0, "None");
            }

            LookupAndGeneratePreviousBuildGuids(client, tank);

            foreach (var asset in Tank.m_assets) {
                var type = teResourceGUID.Type(asset.Key);
                if (!TrackedFiles.TryGetValue(type, out var typeMap)) {
                    typeMap            = new HashSet<ulong>();
                    TrackedFiles[type] = typeMap;
                }

                typeMap.Add(asset.Key);
            }

            worker?.ReportProgress(0, "Generating Conversation mappings...");
            ConversationVoiceLineMapping = DataHelper.GenerateVoicelineConversationMapping(TrackedFiles, worker);

            worker?.ReportProgress(0, "Generating Voiceline mappings...");
            VoicelineSubtitleMapping = DataHelper.GenerateVoicelineSubtitleMapping(TrackedFiles, worker);

            worker?.ReportProgress(0, "Building file tree...");

            if (totalHashList != default) {
                foreach (ContentManifestFile contentManifest in new [] {Tank.m_rootContentManifest, Tank.m_textContentManifest, Tank.m_speechContentManifest}) {
                    if (contentManifest == null) continue;
                    foreach (var record in contentManifest.m_hashList) {
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
                SelectedEntries = Data.Folders.FirstOrDefault(x => x.Name.EndsWith("Client"))?.Files.ToArray() ?? Array.Empty<GUIDEntry>();
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
                ContentKey = ckey,
                StringValue = GetValue(guid),
                IsNew = !PreviousBuildGuids.Contains(guid)
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

        public string GetValue(ulong guid) {
            var dataType = DataHelper.GetDataType(guid);
            var guidType = teResourceGUID.Type(guid);


            if (guidType == 0xB2) {
                return VoicelineSubtitleMapping.TryGetValue(guid, out var subtitle) ? subtitle : null;
            }

            switch (dataType) {
                case DataHelper.DataType.String:
                    return IO.GetString(guid);
                case DataHelper.DataType.MapHeader:
                    return MapHeader.GetName(guid);
                case DataHelper.DataType.Hero: {
                    return Hero.GetName(guid);
                }
            }

            return null;
        }

        public readonly Dictionary<ulong, ulong[]> ConversationVoiceLineMapping = new Dictionary<ulong, ulong[]>();
        public static Dictionary<ulong, string> VoicelineSubtitleMapping = new Dictionary<ulong, string>();

        private void LookupAndGeneratePreviousBuildGuids(ClientHandler client, ProductHandler_Tank tank) {
            try {
                // store the current guids for the current version if it hasn't already been saved
                var buildVersion = uint.Parse(client.InstallationInfo.Values["Version"].Split('.').Last());
                var guidFilePath = Path.Join(ApplicationDataPath, $"{buildVersion}.guids");
                var doesGuidFileExist = File.Exists(guidFilePath);
                if (!doesGuidFileExist) {
                    List<ulong> guids = Tank.m_assets.Select(x => x.Key).ToList();
                    Diff.WriteBinaryGUIDs(guidFilePath, guids);
                }

                GetPreviousBuildGuids();
            } catch (Exception ex) {
                Logger.Debug("TankView", $"Error saving guids! {ex.Message}");
            }
        }

        public void GetPreviousBuildGuids() {
            try {
                // find the latest guid that isnt current build and use that as the previous version
                // todo: ideally allow choosing from any of the previous guids?
                var files = Directory.GetFiles(ApplicationDataPath, "*.guids");

                var lastBuildGuid = files
                    .Select(Path.GetFileName)
                    .Select(x => x.Split(".").FirstOrDefault())
                    .Select(x => {
                        if (uint.TryParse(x, out var buildNumber))
                            return buildNumber;

                        return (uint?) null;
                    })
                    .Where(x => x != null)
                    .OrderByDescending(x => x) // is this even needed? will the files always be ordered due to the increasing numbers??
                    .Skip(1) // first will always be the current build?
                    .FirstOrDefault();

                if (lastBuildGuid != null) {
                    PreviousBuildGuids = Diff.ReadGUIDs(Path.Join(ApplicationDataPath, $"{lastBuildGuid}.guids"));
                }

            } catch (Exception ex) {
                Logger.Debug("TankView", $"Error getting previous build guids! {ex.Message}");
            }
        }
    }
}
