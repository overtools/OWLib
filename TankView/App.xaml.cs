using System.Windows;
using AdonisUI;
using TankLib.TACT;

namespace TankView {
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App {
        protected override void OnStartup(StartupEventArgs startupEventArgs) {
            LoadHelper.PreLoad();
            // SetDarkMode(Settings.Default.DarkMode);
        }

        public void SetDarkMode(bool enableDarkMode) {
            ResourceLocator.SetColorScheme(Resources, enableDarkMode ? ResourceLocator.DarkColorScheme : ResourceLocator.LightColorScheme);
        }
    }
}
