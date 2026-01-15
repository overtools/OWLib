using System;
using System.Globalization;
using System.Threading;
using Avalonia;

namespace TankView;

public static class Program {
	// Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	// yet and stuff might break.
	[STAThread]
	public static void Main(string[] args) {
		Thread.CurrentThread.CurrentCulture =
			Thread.CurrentThread.CurrentUICulture =
				CultureInfo.DefaultThreadCurrentCulture =
					CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

		BuildAvaloniaApp()
		   .StartWithClassicDesktopLifetime(args);
	}

	// Avalonia configuration, don't remove; also used by visual designer.
	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
		             .UsePlatformDetect()
		             .WithInterFont()
		             .LogToTrace();
}
