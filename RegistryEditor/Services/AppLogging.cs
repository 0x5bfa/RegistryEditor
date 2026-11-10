// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;

namespace RegistryEditor.Services;

internal static class AppLogging
{
	private static readonly ILoggerFactory Factory = LoggerFactory.Create(builder =>
	{
#if DEBUG
		builder.SetMinimumLevel(LogLevel.Debug);
#else
		builder.SetMinimumLevel(LogLevel.Information);
#endif
	});
	private static FileLoggerProvider? _fileProvider;
	private static int _stopped;

	public static void Initialize(string directory)
	{
		if (_fileProvider is not null)
			return;
		_fileProvider = new FileLoggerProvider(directory);
		Factory.AddProvider(_fileProvider);
		Factory.CreateLogger("Application").LogInformation("Session started pid={ProcessId} log={LogFilePath}",
			Environment.ProcessId, _fileProvider.LogFilePath);
	}

	public static ILogger<T> CreateLogger<T>() => Factory.CreateLogger<T>();

	public static Task FlushAsync() => _fileProvider?.FlushAsync() ?? Task.CompletedTask;

	public static void Shutdown()
	{
		if (Interlocked.Exchange(ref _stopped, 1) != 0)
			return;
		Factory.CreateLogger("Application").LogInformation("Session ended");
		Factory.Dispose();
	}
}
