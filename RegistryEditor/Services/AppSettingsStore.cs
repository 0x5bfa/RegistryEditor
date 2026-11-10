// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System.Text.Json;
using Windows.Storage;

namespace RegistryEditor.Services;

internal static class AppSettingsStore
{
	private const string FileName = "settings.json";
	private static readonly SemaphoreSlim _fileLock = new(1, 1);

	public static async Task<AppSettings> LoadAsync()
	{
		await _fileLock.WaitAsync();
		try
		{
			try
			{
				byte[] json = await File.ReadAllBytesAsync(GetFilePath());
				return JsonSerializer.Deserialize(json, AppSettingsJsonContext.Default.AppSettings) ?? new AppSettings();
			}
			catch (FileNotFoundException)
			{
				return new AppSettings();
			}
			catch (DirectoryNotFoundException)
			{
				return new AppSettings();
			}
			catch (JsonException)
			{
				return new AppSettings();
			}
		}
		finally
		{
			_fileLock.Release();
		}
	}

	public static async Task SaveAsync(AppSettings settings)
	{
		await _fileLock.WaitAsync();
		try
		{
			string filePath = GetFilePath();
			string temporaryFilePath = $"{filePath}.tmp";
			byte[] json = JsonSerializer.SerializeToUtf8Bytes(settings, AppSettingsJsonContext.Default.AppSettings);
			await File.WriteAllBytesAsync(temporaryFilePath, json);
			File.Move(temporaryFilePath, filePath, overwrite: true);
		}
		finally
		{
			_fileLock.Release();
		}
	}

	private static string GetFilePath()
	{
		return Path.Combine(ApplicationData.Current.LocalFolder.Path, FileName);
	}
}
