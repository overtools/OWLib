using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using DataTool.WPF;
using Microsoft.WindowsAPICodePack.Dialogs;
using TankView.Helper;
using TankView.Properties;
using TankView.ViewModel;
using TACTLib.Client;
using TACTLib.Client.HandlerArgs;
using TACTLib.Core.Product.Tank;
// ReSharper disable MemberCanBeMadeStatic.Local

namespace TankView {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged {
        public SynchronizationContext ViewContext { get; }

        public NGDPPatchHosts NGDPPatchHosts { get; set; }
        public RecentLocations RecentLocations { get; set; }
        public ProgressInfo ProgressInfo { get; set; }
        public CASCSettings CASCSettings { get; set; }
        public AppSettings AppSettings { get; set; }
        public GUIDCollection GUIDTree { get; set; } = new GUIDCollection();
        public ProductLocations ProductAgent { get; set; }

        public string SearchText { get; set; } = string.Empty;

        public static ClientCreateArgs ClientArgs = new ClientCreateArgs();

        private bool ready = true;
 
        public bool IsReady {
            get => ready;
            set {
                ready = value;
                if (value) {
                    _progressWorker.ReportProgress(0, "Idle");
                }

                NotifyPropertyChanged(nameof(IsReady));
                NotifyPropertyChanged(nameof(IsDataReady));
                NotifyPropertyChanged(nameof(IsDataToolSafe));
            }
        }

        public bool IsDataReady => IsReady && GUIDTree?.Data?.Folders.Count > 1;

        public bool IsDataToolSafe => IsDataReady && DataTool.Program.TankHandler?.MainContentManifest?.HashList != null;  // todo

        private ProgressWorker _progressWorker = new ProgressWorker();

        public event PropertyChangedEventHandler PropertyChanged;

        public void NotifyPropertyChanged(string name) {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public MainWindow() {
            ClientArgs.HandlerArgs = new ClientCreateArgs_Tank();

            ViewContext = SynchronizationContext.Current;

            NGDPPatchHosts = new NGDPPatchHosts();
            RecentLocations = new RecentLocations();
            ProgressInfo = new ProgressInfo();
            CASCSettings = new CASCSettings();
            AppSettings = new AppSettings();
            ProductAgent = new ProductLocations();

            _progressWorker.OnProgress += UpdateProgress;

            if (!NGDPPatchHosts.Any(x => x.Active)) {
                NGDPPatchHosts[0].Active = true;
            }

            InitializeComponent();
            
            DataContext = this;

            // FolderView.ItemsSource = ;
            // FolderItemList.ItemsSource = ;
        }
        
        GridViewColumnHeader _lastHeaderClicked = null;
        private ListSortDirection _lastDirection = ListSortDirection.Descending; 

        void GridViewColumnHeaderClickedHandler(object sender, RoutedEventArgs e) {  
            var headerClicked = e.OriginalSource as GridViewColumnHeader;
    
            if (headerClicked == null) return;
            if (headerClicked.Role == GridViewColumnHeaderRole.Padding) return;
            
            ListSortDirection direction;
            if (headerClicked != _lastHeaderClicked)  {  
                direction = ListSortDirection.Descending;  
            } else {
                direction = _lastDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
            }  
      
            var columnBinding = headerClicked.Column.DisplayMemberBinding as Binding;
            var sortBy = columnBinding?.Path.Path ?? headerClicked.Column.Header as string;
    
            GUIDTree.OrderBy(sortBy, direction);
      
            if (direction == ListSortDirection.Ascending) {  
                headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowUp"] as DataTemplate;  
            } else {  
                headerClicked.Column.HeaderTemplate = Resources["HeaderTemplateArrowDown"] as DataTemplate;  
            }  
    
            if (_lastHeaderClicked != null && _lastHeaderClicked != headerClicked) {  
                _lastHeaderClicked.Column.HeaderTemplate = null;  
            }  
      
            _lastHeaderClicked = headerClicked;  
            _lastDirection = direction;
        }

        private void GUIDSearch(object sender, TextChangedEventArgs e) {
            if (GUIDTree == null || e.Handled) return;
            GUIDTree.Search = (e.Source as TextBox)?.Text;
            e.Handled = true;
        }

        private void UpdateProgress(object sender, ProgressChangedEventArgs @event) {
            ViewContext.Send(x => {
                if (!(x is ProgressChangedEventArgs evt)) return;
                if (evt.UserState != null && evt.UserState is string state) {
                    ProgressInfo.State = state;
                }

                ProgressInfo.Percentage = evt.ProgressPercentage;
            }, @event);
        }

        public string ModuloTitle => "TankView";

        private void Exit(object sender, RoutedEventArgs e) {
            Environment.Exit(0);
        }

        private void NGDPHostChange(object sender, RoutedEventArgs e) {
            if (!(sender is MenuItem menuItem)) return;
            foreach (PatchHost node in NGDPPatchHosts.Where(x => x.Active && x.GetHashCode() != ((PatchHost) menuItem.DataContext).GetHashCode())) {
                node.Active = false;
            }

            CollectionViewSource.GetDefaultView(NGDPPatchHosts).Refresh();
        }

        private void OpenNGDP(object sender, RoutedEventArgs e) {
            throw new NotImplementedException(nameof(OpenNGDP));
        }

        private void OpenCASC(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog {
                IsFolderPicker = true,
                EnsurePathExists = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                OpenCASC(dialog.FileName);
            }
        }

        private void OpenRecent(object sender, RoutedEventArgs e) {
            if (!(sender is MenuItem menuItem)) return;
            string path = menuItem.DataContext as string;
            RecentLocations.Add(path);
            CollectionViewSource.GetDefaultView(RecentLocations).Refresh();

            if (path?.StartsWith("ngdp://") == true) {
                OpenNGDP(path);
            } else {
                OpenCASC(path);
            }
        }

        private void OpenAgent(object sender, RoutedEventArgs e) {
            if (sender is MenuItem menuItem) {
                OpenCASC(menuItem.Tag as string);
            }
        }

        private void OpenNGDP(string path) {
            throw new NotImplementedException(nameof(OpenNGDP));
            
#pragma warning disable 162
            // ReSharper disable once HeuristicUnreachableCode
            PrepareTank(path);

            Task.Run(delegate {
                try {
                    DataTool.Program.Client = new ClientHandler(null, ClientArgs);

                    ClientArgs.Online = true;

                    DataTool.Program.TankHandler = DataTool.Program.Client.ProductHandler as ProductHandler_Tank;
                } catch (Exception e) {
                    MessageBox.Show(e.Message, "Error while loading CASC", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    IsReady = true;
                    if (Debugger.IsAttached) {
                        throw;
                    }
                } finally {
                    GCSettings.LatencyMode = GCLatencyMode.Interactive;
                    GC.Collect();
                    DataTool.Program.InitTrackedFiles();
                }
            });
#pragma warning restore 162
        }

        private void OpenCASC(string path) {
            PrepareTank(path);

            Task.Run(delegate {
                try {
                    DataTool.Program.Client = new ClientHandler(path, ClientArgs);

                    DataTool.Program.TankHandler = DataTool.Program.Client.ProductHandler as ProductHandler_Tank;

                    BuildTree();
                } catch (Exception e) {
                    MessageBox.Show(e.Message, "Error while loading CASC", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                    if (Debugger.IsAttached) {
                        throw;
                    }
                } finally {
                    GCSettings.LatencyMode = GCLatencyMode.Interactive;
                    GC.Collect();
                    if (Settings.Default.LoadManifest) {
                        DataTool.Program.InitTrackedFiles();
                    }
                    ViewContext.Send(delegate { IsReady = true; NotifyPropertyChanged(nameof(IsReady)); }, null);
                }

                if (DataTool.Program.Client?.AgentProduct?.Uid != null && DataTool.Program.Client.AgentProduct.Uid != "prometheus") {
                    MessageBox.Show($"The branch \"{DataTool.Program.Client.AgentProduct.Uid}\" is not supported!\nThis might result in failure to load.\nProceed with caution.", "Unsupported Branch", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                }
            });
        }

        private void PrepareTank(string path) {
            RecentLocations.Add(path);
            CollectionViewSource.GetDefaultView(RecentLocations).Refresh();

            _progressWorker.ReportProgress(0, $"Preparing to load {path}");
            
            IsReady = false;

            DataTool.Program.Client = null;
            DataTool.Program.TankHandler = null;
            GUIDTree?.Dispose();
            GUIDTree = new GUIDCollection();
            NotifyPropertyChanged(nameof(GUIDTree));
            GCSettings.LatencyMode = GCLatencyMode.Batch;
        }

        private void BuildTree() {
            GUIDTree?.Dispose();
            GUIDTree = new GUIDCollection(DataTool.Program.Client, DataTool.Program.TankHandler, _progressWorker);
            NotifyPropertyChanged(nameof(GUIDTree));
        }

        private void ChangeActiveNode(object sender, RoutedEventArgs e) {
            if (e.Handled) {
                return;
            }

            if (!(sender is TreeViewItem item)) return;
            if (item.DataContext is Folder folder) GUIDTree.SelectedEntries = folder.Files;
            NotifyPropertyChanged(nameof(GUIDTree));
            e.Handled = true;
        }

        private void OpenOrFocusSimView(object sender, RoutedEventArgs e) {
            var view = Application.Current.Windows.OfType<DataToolSimView>().FirstOrDefault();
            if (view != null) {
                view.Show();
                view.Focus();
                return;
            }
            
            var instance = Application.Current.Windows.OfType<DataToolListView>().FirstOrDefault() ?? new DataToolListView { };
            instance.Show();
            instance.Focus();
        }

        private void ExtractFiles(object sender, RoutedEventArgs e) {
            var files = FolderItemList.SelectedItems.OfType<GUIDEntry>().ToArray();
            if (files.Length == 0) {
                files = FolderItemList.Items.OfType<GUIDEntry>().ToArray();
            }

            if (files.Length == 0) {
                return;
            }

            CommonOpenFileDialog dialog = new CommonOpenFileDialog {
                IsFolderPicker = true,
                EnsurePathExists = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                ExtractFiles(dialog.FileName, files);
            }
        }

        private void ExtractFiles(string outPath, IEnumerable<GUIDEntry> files) {
            IsReady = false;

            IEnumerable<GUIDEntry> guidEntries = files as GUIDEntry[] ?? files.ToArray();
            HashSet<string> directories = new HashSet<string>(guidEntries.Select(x => Path.Combine(outPath, Path.GetDirectoryName(x.FullPath.Substring(1)) ?? string.Empty)));
            foreach (string directory in directories) {
                if (!Directory.Exists(directory)) {
                    Directory.CreateDirectory(directory);
                }
            }

            Task.Run(delegate {
                int c = 0;
                int t = guidEntries.Count();
                _progressWorker?.ReportProgress(0, "Saving files...");
                Parallel.ForEach(guidEntries, new ParallelOptions {
                    MaxDegreeOfParallelism = 4
                }, (entry) => {
                    c++;
                    if (c % ((int) (t * 0.005) + 1) == 0) {
                        _progressWorker?.ReportProgress((int) ((c / (float) t) * 100));
                    }

                    try {
                        using (Stream i = IOHelper.OpenFile(entry))
                        using (Stream o = File.OpenWrite(Path.Combine(outPath, entry.FullPath.Substring(1)))) {
                            i.CopyTo(o);
                        }
                    } catch {
                        // ignored
                    }
                });

                ViewContext.Send(delegate { IsReady = true; }, null);
            });
        }

        private void ExtractFolder(Folder folder, ref List<GUIDEntry> files) {
            files.AddRange(folder.Files);
            foreach (Folder f in folder.Folders) {
                ExtractFolder(f, ref files);
            }
        }

        private void ExtractFolder(string outPath, Folder folder) {
            List<GUIDEntry> files = new List<GUIDEntry>();
            ExtractFolder(folder, ref files);
            ExtractFiles(outPath, files);
        }

        private void ExtractFolder(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog {
                IsFolderPicker = true,
                EnsurePathExists = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                ExtractFolder(dialog.FileName, (sender as FrameworkElement)?.DataContext as Folder);
            }
        }

        private bool HasShown = false;
        private void FirstChance(object sender, EventArgs e) {
            if (HasShown) return;
            
            HasShown = true;

            if (Debugger.IsAttached) {
                IsEnabled = true;
                
                // Use to auto load a dir at startup, useful or dev
                // OpenCASC("");
                return;
            }
            
            new AboutPage(this).Show();
            Hide();
        }
    }
}
