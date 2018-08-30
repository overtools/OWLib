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
using System.Windows.Shapes;

namespace TankView {
    /// <summary>
    /// Interaction logic for DataToolSimView.xaml
    /// </summary>
    public partial class DataToolSimView : Window {
        public DataToolSimView() {
            InitializeComponent();
        }

        public void PostInitialize() {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            Activate();
            Show();
        }

        private void ShowMaps(object sender, RoutedEventArgs e) { }

        private void ShowHeroes(object sender, RoutedEventArgs e) { }

        private void ShowNPCs(object sender, RoutedEventArgs e) { }

        private void ShowGeneralItems(object sender, RoutedEventArgs e) { }
    }
}
