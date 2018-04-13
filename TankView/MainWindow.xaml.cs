using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TankView.ViewResources;

namespace TankView
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public RsrcNGDPPatchHosts NGDPPatchHosts { get; private set; }

        public MainWindow()
        {
            NGDPPatchHosts = new RsrcNGDPPatchHosts();
            if (!NGDPPatchHosts.Any(x => x.Active))
            {
                NGDPPatchHosts[0].Active = true;
            }

            InitializeComponent();
            DataContext = this;
            // FolderView.ItemsSource = ;
            // FolderItemList.ItemsSource = ;
        }

        public string ModuloTitle {
            get {
                return "TankView";
            }
        }

        private void Exit(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void NGDPHostChange(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem)
            {
                foreach (var node in NGDPPatchHosts.Where(x => x.Active && x.GetHashCode() != (menuItem.DataContext as PatchHost).GetHashCode()))
                {
                    node.Active = false;
                }

                CollectionViewSource.GetDefaultView(NGDPPatchHosts).Refresh();
            }
        }
    }
}
