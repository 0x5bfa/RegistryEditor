// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using RegistryEditor.Views;

namespace RegistryEditor;

public sealed partial class MainWindow : Window
{
	public RootView RootView { get; }

	public MainWindow()
	{
		InitializeComponent();

		AppWindow.Title = "Registry Editor";
		ExtendsContentIntoTitleBar = true;
		AppWindow.SetIcon(System.IO.Path.Combine(
			Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
			"Assets/Branding/AppLogo.ico"));

		RootView = new RootView();
		Content = RootView;
	}

	public Task InitializeAsync()
		=> RootView.ViewModel.LoadSettingsAsync();
}
