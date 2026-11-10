// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using RegistryEditor.Services;

namespace RegistryEditor.Collections;

public sealed class BulkObservableCollection<T> : ObservableCollection<T>
{
	private static readonly ILogger<BulkObservableCollection<T>> Logger = AppLogging.CreateLogger<BulkObservableCollection<T>>();

	/// <summary>
	/// Appends items using a single collection add notification.
	/// </summary>
	public void AddRange(IEnumerable<T> items)
	{
		ArgumentNullException.ThrowIfNull(items);
		CheckReentrancy();

		long started = Stopwatch.GetTimestamp();
		T[] newItems = items.ToArray();
		if (newItems.Length == 0)
		{
			return;
		}

		int startingIndex = Count;
		foreach (T item in newItems)
		{
			Items.Add(item);
		}

		OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
		OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
		long notifying = Stopwatch.GetTimestamp();
		OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems, startingIndex));
		double notifyMilliseconds = Stopwatch.GetElapsedTime(notifying).TotalMilliseconds;
		if (notifyMilliseconds >= 100 || newItems.Length >= 256)
		{
			Logger.Log(notifyMilliseconds >= 100 ? LogLevel.Warning : LogLevel.Debug,
				"AddRange.NotifyAdd itemType={ItemType} count={Count} startIndex={StartingIndex} notifyMs={NotifyMs:F1} totalMs={TotalMs:F1}",
				typeof(T).Name, newItems.Length, startingIndex, notifyMilliseconds, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
		}
	}
}
