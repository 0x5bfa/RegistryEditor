// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace RegistryEditor.Services;

internal static class LoggerTimingExtensions
{
	public static LogMeasurement Measure(this ILogger logger, string name, string details = "", LogLevel level = LogLevel.Debug)
		=> new(logger, name, details, level);
}

internal sealed class LogMeasurement : IDisposable
{
	private static int _nextId;
	private readonly ILogger _logger;
	private readonly string _name;
	private readonly LogLevel _level;
	private readonly IDisposable? _scope;
	private readonly long _started;
	private int _disposed;

	public LogMeasurement(ILogger logger, string name, string details, LogLevel level)
	{
		Id = Interlocked.Increment(ref _nextId);
		_logger = logger;
		_name = name;
		_level = level;
		_scope = logger.BeginScope("{Operation}#{OperationId}", name, Id);
		logger.Log(level, "{Operation} BEGIN {Details}", name, details);
		_started = Stopwatch.GetTimestamp();
	}

	public int Id { get; }

	public void Write(string message, LogLevel? level = null, Exception? exception = null)
		=> _logger.Log(level ?? _level, exception, "[{OperationId}] {Operation} elapsedMs={ElapsedMs:F1} {Message}",
			Id, _name, Stopwatch.GetElapsedTime(_started).TotalMilliseconds, message);

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
			return;
		Write("END");
		_scope?.Dispose();
	}
}
