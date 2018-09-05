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
using Microsoft.WindowsAPICodePack.Dialogs;
using TankView.Helper;
using TankView.ViewModel;
using TACTLib.Client;
using TACTLib.Client.HandlerArgs;
using TACTLib.Config;
using TACTLib.Core;
using TACTLib.Core.Product;
using TACTLib.Core.Product.Tank;

namespace TankView {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged {
        public SynchronizationContext ViewContext { get; }

        public NGDPPatchHosts NGDPPatchHosts { get; set; }
        public RecentLocations RecentLocations { get; set; }
        public ProgressInfo ProgressInfo { get; set; }
        public CASCSettings CASCSettings { get; set; }
        public GUIDCollection GUIDTree { get; set; } = new GUIDCollection();
        public ProductLocations ProductAgent { get; set; }

        public static ClientCreateArgs ClientArgs = new ClientCreateArgs();

        public static ClientHandler Client;
        public static ProductHandler_Tank TankHandler;

        private bool ready = true;

        public bool IsReady {
            get => ready;
            set {
                ready = value;
                if (value) {
                    ProgressSlave.ReportProgress(0, "Idle");
                }

                NotifyPropertyChanged(nameof(IsReady));
                NotifyPropertyChanged(nameof(IsDataReady));
            }
        }

        public bool IsDataReady => IsReady;

        private ProgressSlave ProgressSlave = new ProgressSlave();

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
            ProductAgent = new ProductLocations();

            ProgressSlave.OnProgress += UpdateProgress;

            if (!NGDPPatchHosts.Any(x => x.Active)) {
                NGDPPatchHosts[0].Active = true;
            }

            InitializeComponent();
            DataContext = this;
            // FolderView.ItemsSource = ;
            // FolderItemList.ItemsSource = ;
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
            RecentLocations.Add(path);
            CollectionViewSource.GetDefaultView(RecentLocations).Refresh();
            throw new NotImplementedException(nameof(OpenNGDP));
        }

        private void OpenCASC(string path) {
            RecentLocations.Add(path);
            CollectionViewSource.GetDefaultView(RecentLocations).Refresh();

            IsReady = false;

            Client = null;
            TankHandler = null;
            GUIDTree?.Dispose();
            GUIDTree = new GUIDCollection();
            NotifyPropertyChanged(nameof(GUIDTree));
            GCSettings.LatencyMode = GCLatencyMode.Batch;

            Task.Run(delegate {
                         try {
                             Client = new ClientHandler(path, ClientArgs);

                             if (Client.AgentProduct.Uid != null && Client.AgentProduct.Uid != "prometheus") {
                                 MessageBox.Show($"The branch \"{Client.AgentProduct.Uid}\" is not supported!\nThis might result in failure to load.\nProceed with caution.", "Unsupported Branch", MessageBoxButton.OK, MessageBoxImage.Warning, MessageBoxResult.OK);
                             }

                             TankHandler = Client.ProductHandler as ProductHandler_Tank;;

                             BuildTree();
                         } catch (Exception e) {
                             MessageBox.Show(e.Message, "Error while loading CASC", MessageBoxButton.OK, MessageBoxImage.Error, MessageBoxResult.OK);
                             if (Debugger.IsAttached) {
                                 throw;
                             }
                         } finally {
                             GCSettings.LatencyMode = GCLatencyMode.Interactive;
                             GC.Collect();
                         }

                         ViewContext.Send(new SendOrPostCallback(delegate { IsReady = true; }), null);
                     });
        }

        private void BuildTree() {
            GUIDTree?.Dispose();
            GUIDTree = new GUIDCollection(Client, TankHandler, ProgressSlave);
            NotifyPropertyChanged(nameof(GUIDTree));
        }

        private void ChangeActiveNode(object sender, RoutedEventArgs e) {
            if (e.Handled) {
                return;
            }

            if (sender is TreeViewItem item) {
                Folder folder = item.DataContext as Folder;
                GUIDTree.SelectedEntries = folder.Files;
                NotifyPropertyChanged(nameof(GUIDTree));
                e.Handled = true;
            }
        }

        private void OpenOrFocusSimView(object sender, RoutedEventArgs e) {
            var instance = Application.Current.Windows.OfType<DataToolSimView>().FirstOrDefault();
            if (instance == null) {
                instance = new DataToolSimView();
            }

            instance.Owner = this;
            instance.PostInitialize();
        }

        private void ExtractFiles(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog {
                IsFolderPicker = true,
                EnsurePathExists = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                ExtractFiles(dialog.FileName, FolderItemList.SelectedItems.OfType<GUIDEntry>());
            }
        }

        private void ExtractFiles(string outPath, IEnumerable<GUIDEntry> files) {
            IsReady = false;

            HashSet<string> directories = new HashSet<string>(files.Select(x => Path.Combine(outPath, Path.GetDirectoryName(x.FullPath.Substring(1)))));
            foreach (string directory in directories) {
                if (!Directory.Exists(directory)) {
                    Directory.CreateDirectory(directory);
                }
            }

            Task.Run(delegate {
                         int c = 0;
                         int t = files.Count();
                         ProgressSlave?.ReportProgress(0, "Saving files...");
                         Parallel.ForEach(files, new ParallelOptions {
                             MaxDegreeOfParallelism = 4
                         }, (entry) => {
                                c++;
                                if (c % ((int) (t * 0.005) + 1) == 0) {
                                    ProgressSlave?.ReportProgress((int) (((float) c / (float) t) * 100));
                                }

                                try {
                                    using (Stream i = IOHelper.OpenFile(entry))
                                    using (Stream o = File.OpenWrite(Path.Combine(outPath, entry.FullPath.Substring(1)))) {
                                        i.CopyTo(o);
                                    }
                                } catch { }
                            });

                         ViewContext.Send(new SendOrPostCallback(delegate { IsReady = true; }), null);
                     });
        }

        private void ExtractFolder(string outPath, Folder folder, ref List<GUIDEntry> files) {
            files.AddRange(folder.Files);
            foreach (Folder f in folder.Folders) {
                ExtractFolder(outPath, f, ref files);
            }
        }

        private void ExtractFolder(string outPath, Folder folder) {
            List<GUIDEntry> files = new List<GUIDEntry>();
            ExtractFolder(outPath, folder, ref files);
            ExtractFiles(outPath, files);
        }

        private void ExtractFolder(object sender, RoutedEventArgs e) {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog {
                IsFolderPicker = true,
                EnsurePathExists = true
            };
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok) {
                ExtractFolder(dialog.FileName, (sender as FrameworkElement).DataContext as Folder);
            }
        }
    }
}
