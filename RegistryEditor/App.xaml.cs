// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

namespace RegistryEditor;

public partial class App : Application
{
	public static MainWindow Window { get; private set; } = null!;

	public App()
	{
		InitializeComponent();
	}

	protected override async void OnLaunched(LaunchActivatedEventArgs args)
	{
		Window = new MainWindow();
		await Window.InitializeAsync();
		Window.Activate();
	}
}
