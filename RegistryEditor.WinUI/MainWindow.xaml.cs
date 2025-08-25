// Copyright (c) 2025 0x5BFA.
// Licensed under the MIT License.

using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using RegistryEditor.WinUI.Views;

namespace RegistryEditor.WinUI
{
	public sealed partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			AppWindow.Title = "Registry Valley";
			AppWindow.SetIcon(Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, Constants.AssetPaths.Logo));
			AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
			AppWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
			AppWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
		}

		public void InitializeContent()
		{
			if (Content is not Frame rootFrame)
			{
				rootFrame = new() { CacheSize = 1 };
				Content = rootFrame;
			}

			if (rootFrame.Content is null)
				rootFrame.Navigate(typeof(MainPage), null, new SuppressNavigationTransitionInfo());
		}
	}
}
