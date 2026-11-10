// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using RegistryEditor.Services;
using Windows.Storage;

namespace RegistryEditor;

public partial class App : Application
{
	private readonly ILogger<App> _logger;

	public static MainWindow Window { get; private set; } = null!;

	public App()
	{
		AppLogging.Initialize(Path.Combine(ApplicationData.Current.LocalFolder.Path, "logs"));
		_logger = AppLogging.CreateLogger<App>();
		UnhandledException += (_, args) =>
		{
			_logger.LogCritical(args.Exception, "Unhandled UI exception");
			AppLogging.FlushAsync().Wait(TimeSpan.FromSeconds(2));
		};
		AppDomain.CurrentDomain.UnhandledException += (_, args) =>
		{
			_logger.LogCritical(args.ExceptionObject as Exception, "Unhandled runtime exception terminating={IsTerminating}", args.IsTerminating);
			AppLogging.FlushAsync().Wait(TimeSpan.FromSeconds(2));
		};
		TaskScheduler.UnobservedTaskException += (_, args) => _logger.LogError(args.Exception, "Unobserved task exception");
		InitializeComponent();
	}

	protected override async void OnLaunched(LaunchActivatedEventArgs args)
	{
		using var operation = _logger.Measure("Application.Launch", level: LogLevel.Information);
		Window = new MainWindow();
		UIResponsivenessMonitor monitor = new(Window.DispatcherQueue);
		Window.Closed += (_, _) =>
		{
			monitor.Dispose();
			AppLogging.Shutdown();
		};
		await Window.InitializeAsync();
		Window.Activate();
	}
}
