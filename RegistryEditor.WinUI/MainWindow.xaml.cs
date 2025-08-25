// Copyright (c) 2025 0x5BFA.
// Licensed under the MIT License.

using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using RegistryEditor.WinUI.Services;
using RegistryEditor.WinUI.Views;

namespace RegistryEditor.WinUI
{
	public sealed partial class MainWindow : Window
	{
		private readonly UserSettingsServices UserSettingsServices = App.Current.Services.GetRequiredService<UserSettingsServices>();

		private bool AlreadyInitialized { get; set; }

		public MainWindow()
		{
			InitializeComponent();

			Activated += MainWindow_Activated;

			AppWindow.Title = "Registry Valley";
			AppWindow.SetIcon(Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, Constants.AssetPaths.Logo));
			AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
			AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
			AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
			AppWindow.Resize(new(516, 328));
		}

		private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
		{
			if (!AlreadyInitialized)
			{
				if (App.Window.Content is not Frame rootFrame)
				{
					rootFrame = new() { CacheSize = 1 };
					App.Window.Content = rootFrame;
				}

				Type pageType = UserSettingsServices.SetupCompleted ? typeof(MainPage) : typeof(SetupPage);

				if (rootFrame.Content is null)
					rootFrame.Navigate(pageType, null, new SuppressNavigationTransitionInfo());

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
		}

		public void NavigateFrameTo(Type sourcePageType)
		{
			if (App.Window.Content is Frame rootFrame)
				rootFrame.Navigate(sourcePageType);
		}
	}
}
