// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System.Text.Json;
using Windows.Storage;

namespace RegistryEditor.Services;

internal static class AppSettingsStore
{
	private const string FileName = "settings.json";
	private static readonly SemaphoreSlim FileLock = new(1, 1);

	public static async Task<AppSettings> LoadAsync()
	{
		await FileLock.WaitAsync();
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
			FileLock.Release();
		}
	}

	public static async Task SaveAsync(AppSettings settings)
	{
		await FileLock.WaitAsync();
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
			FileLock.Release();
		}
	}

	private static string GetFilePath()
		=> Path.Combine(ApplicationData.Current.LocalFolder.Path, FileName);
}
