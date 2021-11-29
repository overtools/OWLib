using System;
using System.Windows;
using AdonisUI;
using TankView.Properties;

namespace TankView {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App {
        protected override void OnStartup(StartupEventArgs startupEventArgs) {
            // SetDarkMode(Settings.Default.DarkMode);
        }

        public void SetDarkMode(bool enableDarkMode) {
            ResourceLocator.SetColorScheme(Resources, enableDarkMode ? ResourceLocator.DarkColorScheme : ResourceLocator.LightColorScheme);
        }
    }
}
