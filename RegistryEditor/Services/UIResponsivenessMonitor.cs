// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Dispatching;
using Microsoft.Extensions.Logging;

namespace RegistryEditor.Services;

internal sealed class UIResponsivenessMonitor : IDisposable
{
	private readonly ILogger<UIResponsivenessMonitor> _logger = AppLogging.CreateLogger<UIResponsivenessMonitor>();
	private readonly DispatcherQueueTimer _heartbeat;
	private readonly Timer _watchdog;
	private long _lastHeartbeat = Stopwatch.GetTimestamp();
	private int _disposed;

	public UIResponsivenessMonitor(DispatcherQueue dispatcher)
	{
		_heartbeat = dispatcher.CreateTimer();
		_heartbeat.Interval = TimeSpan.FromMilliseconds(250);
		_heartbeat.IsRepeating = true;
		_heartbeat.Tick += (_, _) =>
		{
			long now = Stopwatch.GetTimestamp();
			double elapsed = Stopwatch.GetElapsedTime(Interlocked.Exchange(ref _lastHeartbeat, now), now).TotalMilliseconds;
			if (elapsed >= 1000)
			{
				_logger.LogWarning("UI RESUMED heartbeatGapMs={HeartbeatGapMs:F1}", elapsed);
			}
		};
		_heartbeat.Start();

		_watchdog = new Timer(_ =>
		{
			if (Volatile.Read(ref _disposed) != 0)
			{
				return;
			}

			double elapsed = Stopwatch.GetElapsedTime(Volatile.Read(ref _lastHeartbeat)).TotalMilliseconds;
			if (elapsed >= 1000)
			{
				_logger.LogWarning("UI STALL timeSinceLastHeartbeatMs={HeartbeatGapMs:F1}", elapsed);
			}
		},
		null,
		TimeSpan.FromSeconds(1),
		TimeSpan.FromSeconds(1));
	}

	public void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) != 0)
		{
			return;
		}

		_heartbeat.Stop();
		_watchdog.Dispose();
	}
}
