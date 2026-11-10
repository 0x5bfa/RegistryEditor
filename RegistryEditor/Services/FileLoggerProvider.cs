// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading.Channels;

namespace RegistryEditor.Services;

internal sealed partial class FileLoggerProvider : ILoggerProvider, ISupportExternalScope
{
	private readonly Channel<Entry> _entries = Channel.CreateUnbounded<Entry>(new UnboundedChannelOptions
	{
		SingleReader = true,
		AllowSynchronousContinuations = false,
	});
	private readonly Task _writerTask;
	private IExternalScopeProvider _scopes = new LoggerExternalScopeProvider();
	private int _disposed;

	public FileLoggerProvider(string directory)
	{
		StreamWriter? writer = null;
		try
		{
			Directory.CreateDirectory(directory);
			string path = Path.Combine(directory, $"app-{DateTime.Now:yyyyMMdd-HHmmss-fff}-{Environment.ProcessId}.log");
			writer = new StreamWriter(new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite))
			{
				AutoFlush = true,
			};
			LogFilePath = path;
		}
		catch (Exception exception)
		{
			Debug.WriteLine($"Unable to create application log: {exception}");
		}
		_writerTask = Task.Run(() => WriteEntriesAsync(writer));
	}

	public string? LogFilePath { get; }

	public ILogger CreateLogger(string categoryName)
	{
		return new FileLogger(this, categoryName);
	}

	public void SetScopeProvider(IExternalScopeProvider scopeProvider)
	{
		_scopes = scopeProvider;
	}

	public Task FlushAsync()
	{
		TaskCompletionSource completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
		return _entries.Writer.TryWrite(new Entry(null, completion)) ? completion.Task : _writerTask;
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
		{
			return;
		}

		_entries.Writer.TryComplete();
		try
		{
			_writerTask.Wait(TimeSpan.FromSeconds(2));
		}
		catch (Exception exception)
		{
			Debug.WriteLine($"Unable to finish application log: {exception}");
		}
	}

	private async Task WriteEntriesAsync(StreamWriter? writer)
	{
		try
		{
			await foreach (Entry entry in _entries.Reader.ReadAllAsync().ConfigureAwait(false))
			{
				if (entry.Completion is { } completion)
				{
					completion.SetResult();
					continue;
				}
				Debug.WriteLine(entry.Line);
				try
				{
					writer?.WriteLine(entry.Line);
				}
				catch (Exception exception)
				{
					Debug.WriteLine($"Unable to write application log: {exception}");
				}
			}
		}
		finally
		{
			try
			{
				writer?.Dispose();
			}
			catch (Exception exception)
			{
				Debug.WriteLine($"Unable to close application log: {exception}");
			}
		}
	}

	private sealed record Entry(string? Line, TaskCompletionSource? Completion = null);

	private sealed class FileLogger(FileLoggerProvider provider, string category) : ILogger
	{
		public IDisposable? BeginScope<TState>(TState state) where TState : notnull
		{
			return provider._scopes.Push(state);
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return logLevel != LogLevel.None && Volatile.Read(ref provider._disposed) == 0;
		}

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
			Func<TState, Exception?, string> formatter)
		{
			if (!IsEnabled(logLevel))
			{
				return;
			}

			List<string> scopes = [];
			provider._scopes.ForEachScope((scope, items) => items.Add(scope?.ToString() ?? ""), scopes);
			string scopeText = scopes.Count == 0 ? "" : $" [{string.Join(" => ", scopes)}]";
			string line = $"{DateTimeOffset.Now:O} [{logLevel}] [T{Environment.CurrentManagedThreadId}] [{category}] [E{eventId.Id}]{scopeText} {formatter(state, exception)}";
			if (exception is not null)
			{
				line += $"{Environment.NewLine}{exception}";
			}

			provider._entries.Writer.TryWrite(new Entry(line));
		}
	}
}
