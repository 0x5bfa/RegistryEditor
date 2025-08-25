using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppLifecycle;
using RegistryValley.App.Services;
using RegistryValley.App.Views;
using System.IO;
using WinUIEx;

namespace RegistryValley.App
{
	public sealed partial class MainWindow : WindowEx
	{
		private bool AlreadyInitialized { get; set; }

		private UserSettingsServices UserSettingsServices { get; } = App.Current.Services.GetRequiredService<UserSettingsServices>();

		public MainWindow()
		{
			InitializeComponent();

			PersistenceId = "RegistryValleyMainWindow";

			Activated += MainWindow_Activated;

			EnsureEarlyWindow();
		}

		private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
		{
			if (!AlreadyInitialized)
				InitializeApplication();
		}

		private void EnsureEarlyWindow()
		{
			AppWindow.Title = "Registry Valley";
			AppWindow.SetIcon(Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, Constants.AssetPaths.Logo));
			AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
			AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
			AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

			MinHeight = 328;
			MinWidth = 516;
		}

		public void InitializeApplication()
		{
			var rootFrame = EnsureWindowIsInitialized();
			Type pageType = UserSettingsServices.SetupCompleted ? typeof(MainPage) : typeof(SetupPage);

			if (rootFrame.Content == null)
			{
				rootFrame.Navigate(pageType, null, new SuppressNavigationTransitionInfo());
			}

			if (UserSettingsServices.SetupCompleted)
			{
				((MainPage)rootFrame.Content).Loaded += (s, e)
					=> DispatcherQueue.TryEnqueue(() => Activate());
			}
			else
			{
				((SetupPage)rootFrame.Content).Loaded += (s, e)
					=> DispatcherQueue.TryEnqueue(() => Activate());
			}

			AlreadyInitialized = true;
		}

		private Frame EnsureWindowIsInitialized()
		{
			if (!(App.Window.Content is Frame rootFrame))
			{
				rootFrame = new() { CacheSize = 1 };
				rootFrame.NavigationFailed += OnNavigationFailed;

				App.Window.Content = rootFrame;
			}

			return rootFrame;
		}

		public void NavigateFrameTo(Type sourcePageType)
		{
			if (App.Window.Content is Frame rootFrame)
			{
				rootFrame.Navigate(sourcePageType);
			}
		}

		private void OnNavigationFailed(object sender, NavigationFailedEventArgs e)
		{
			throw new Exception("Failed to load Page " + e.SourcePageType.FullName);
		}
	}
}
