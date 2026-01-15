using System.Reflection;
using Avalonia.Controls;
using TankLib;

namespace TankView;

public partial class MainWindow : Window {
	public MainWindow() {
		InitializeComponent();
		Status.Text = $"{Assembly.GetExecutingAssembly().GetName().Name} v{Util.GetVersion(typeof(Program).Assembly)}";
	}
}
