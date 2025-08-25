// Copyright (c) 2025 0x5BFA.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using RegistryEditor.WinUI.ViewModels;

namespace RegistryEditor.WinUI
{
	public partial class App : Application
	{
		public new static App Current => (App)Application.Current;

		public IServiceProvider Services { get; }

		public static MainWindow Window { get; private set; } = null!;

		public static IntPtr WindowHandle { get; private set; }

		public App()
		{
			InitializeComponent();

			Services = ConfigureServices();
		}

		private static IServiceProvider ConfigureServices()
		{
			return new ServiceCollection()
				//.AddSingleton<Utils.ILogger>(new SerilogWrapperLogger(Serilog.Log.Logger))
				//.AddSingleton<ToastService>()
				.AddSingleton<IMessenger>(StrongReferenceMessenger.Default)
				// ViewModels
				.AddTransient<Services.UserSettingsServices>()
				// ViewModels
				.AddTransient<ViewModels.Dialogs.ValueAddingDialogViewModel>()
				.AddTransient<ViewModels.Dialogs.ValueEditingDialogViewModel>()
				.AddTransient<ViewModels.Properties.GeneralViewModel>()
				.AddTransient<ViewModels.Properties.MainPropertyViewModel>()
				.AddTransient<ViewModels.Properties.SecurityAdvancedViewModel>()
				.AddTransient<ViewModels.Properties.SecurityViewModel>()
				.AddTransient<ViewModels.UserControls.TreeViewViewModel>()
				.AddSingleton<MainViewModel>()
				.AddTransient<SettingsViewModel>()
				.AddSingleton<ValuesViewerViewModel>()
				.BuildServiceProvider();
		}

		protected override void OnLaunched(LaunchActivatedEventArgs e)
		{
			Window = new MainWindow();
			Window.Activate();
			WindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(Window);

			Window.InitializeContent();
		}
	}
}
