// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Win32;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using RegistryEditor.ViewModels;

namespace RegistryEditor.Services;

internal static partial class RegistryNative
{
	private const uint TokenAdjustPrivileges = 0x0020;
	private const uint TokenQuery = 0x0008;
	private const uint SePrivilegeEnabled = 0x0002;

	[LibraryImport("kernel32.dll", EntryPoint = "GetCurrentProcess")]
	private static partial nint GetCurrentProcess();

	[LibraryImport("kernel32.dll", EntryPoint = "CloseHandle", SetLastError = true)]
	private static partial int CloseHandle(nint handle);

	[LibraryImport("advapi32.dll", EntryPoint = "OpenProcessToken", SetLastError = true)]
	private static partial int OpenProcessToken(nint processHandle, uint desiredAccess, out nint tokenHandle);

	[LibraryImport("advapi32.dll", EntryPoint = "LookupPrivilegeValueW", StringMarshalling = StringMarshalling.Utf16, SetLastError = true)]
	private static partial int LookupPrivilegeValue(string? systemName, string name, out Luid luid);

	[LibraryImport("advapi32.dll", EntryPoint = "AdjustTokenPrivileges", SetLastError = true)]
	private static partial int AdjustTokenPrivileges(
		nint tokenHandle,
		int disableAllPrivileges,
		in TokenPrivileges newState,
		uint bufferLength,
		nint previousState,
		nint returnLength);

	[LibraryImport("advapi32.dll", EntryPoint = "RegLoadKeyW", StringMarshalling = StringMarshalling.Utf16)]
	internal static partial int RegLoadKey(nint hKey, string subKey, string fileName);

	[LibraryImport("advapi32.dll", EntryPoint = "RegUnLoadKeyW", StringMarshalling = StringMarshalling.Utf16)]
	internal static partial int RegUnLoadKey(nint hKey, string subKey);

	[LibraryImport("advapi32.dll", EntryPoint = "RegRenameKey", StringMarshalling = StringMarshalling.Utf16)]
	internal static partial int RegRenameKey(nint hKey, string newName);

	internal static nint GetPredefinedHiveHandle(RegistryHive hive)
		=> hive switch
		{
			RegistryHive.LocalMachine => new nint(-2147483646),
			RegistryHive.Users => new nint(-2147483645),
			_ => throw new ArgumentOutOfRangeException(nameof(hive)),
		};

	internal static void EnablePrivilege(string privilegeName)
	{
		const uint desiredAccess = TokenAdjustPrivileges | TokenQuery;
		if (OpenProcessToken(GetCurrentProcess(), desiredAccess, out nint tokenHandle) == 0)
			throw new Win32Exception(Marshal.GetLastWin32Error());

		try
		{
			if (LookupPrivilegeValue(null, privilegeName, out Luid luid) == 0)
				throw new Win32Exception(Marshal.GetLastWin32Error());

			TokenPrivileges privileges = new()
			{
				PrivilegeCount = 1,
				Luid = luid,
				Attributes = SePrivilegeEnabled,
			};
			if (AdjustTokenPrivileges(tokenHandle, disableAllPrivileges: 0, in privileges, (uint)Marshal.SizeOf<TokenPrivileges>(), previousState: 0, returnLength: 0) == 0)
				throw new Win32Exception(Marshal.GetLastWin32Error());

			int error = Marshal.GetLastWin32Error();
			if (error != 0)
				throw new Win32Exception(error);
		}
		finally
		{
			CloseHandle(tokenHandle);
		}
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct Luid
	{
		public uint LowPart;
		public int HighPart;
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct TokenPrivileges
	{
		public uint PrivilegeCount;
		public Luid Luid;
		public uint Attributes;
	}
}

internal static class RegistryFileService
{
	private const string RegeditHeader = "Windows Registry Editor Version 5.00";

	public static void Export(string filePath, RegistryNodeViewModel node)
	{
		ArgumentException.ThrowIfNullOrEmpty(filePath);
		ArgumentNullException.ThrowIfNull(node);

		using StreamWriter writer = new(
			filePath,
			append: false,
			encoding: new UnicodeEncoding(bigEndian: false, byteOrderMark: true));

		writer.WriteLine(RegeditHeader);
		writer.WriteLine();

		if (node.Hive is null)
		{
			foreach (RegistryNodeViewModel child in node.Children.Where(child => child.Hive is not null))
				WriteNode(writer, child);
		}
		else
		{
			WriteNode(writer, node);
		}
	}

	public static int Import(string filePath)
	{
		ArgumentException.ThrowIfNullOrEmpty(filePath);

		using StreamReader reader = new(
			filePath,
			Encoding.UTF8,
			detectEncodingFromByteOrderMarks: true);

		string? currentKeyPath = null;
		int importedValueCount = 0;
		string? line;

		while ((line = ReadLogicalLine(reader)) is not null)
		{
			string trimmed = line.Trim();
			if (trimmed.Length == 0 || trimmed[0] is ';' or '#')
				continue;

			if (trimmed.StartsWith("Windows Registry Editor Version", StringComparison.OrdinalIgnoreCase)
				|| trimmed.StartsWith("REGEDIT", StringComparison.OrdinalIgnoreCase))
				continue;

			if (trimmed.StartsWith("[-", StringComparison.Ordinal) && trimmed.EndsWith(']'))
			{
				DeleteKey(trimmed[2..^1]);
				currentKeyPath = null;
				continue;
			}

			if (trimmed[0] == '[' && trimmed[^1] == ']')
			{
				currentKeyPath = trimmed[1..^1];
				using RegistryKey ignored = OpenKey(currentKeyPath, writable: true);
				continue;
			}

			if (currentKeyPath is null)
				continue;

			int separator = trimmed.IndexOf('=');
			if (separator <= 0)
				continue;

			string valueName = ParseValueName(trimmed[..separator]);
			string valueText = trimmed[(separator + 1)..].Trim();
			using RegistryKey key = OpenKey(currentKeyPath, writable: true);

			if (valueText == "-")
			{
				key.DeleteValue(valueName, throwOnMissingValue: false);
				continue;
			}

			(RegistryValueKind kind, object value, _) = ParseValue(valueText);
			key.SetValue(valueName, value, kind);
			importedValueCount++;
		}

		return importedValueCount;
	}

	internal static RegistryKey OpenKey(RegistryNodeViewModel node, bool writable)
	{
		if (node.Hive is not RegistryHive hive)
			throw new InvalidOperationException("The selected item is not a registry key.");

		RegistryKey baseKey = OpenBaseKey(hive, node.ComputerName);
		if (string.IsNullOrEmpty(node.SubKeyPath))
			return baseKey;

		RegistryKey? key = baseKey.OpenSubKey(node.SubKeyPath, writable);
		baseKey.Dispose();
		return key ?? throw new UnauthorizedAccessException($"Unable to open registry key '{node.SubKeyPath}'.");
	}

	internal static RegistryKey OpenBaseKey(RegistryNodeViewModel node)
	{
		if (node.Hive is not RegistryHive hive)
			throw new InvalidOperationException("The selected item is not a registry hive.");

		return OpenBaseKey(hive, node.ComputerName);
	}

	internal static RegistryKey OpenBaseKey(RegistryHive hive, string? computerName)
		=> string.IsNullOrEmpty(computerName)
			? RegistryKey.OpenBaseKey(hive, RegistryView.Default)
			: RegistryKey.OpenRemoteBaseKey(hive, computerName, RegistryView.Default);

	private static void WriteNode(StreamWriter writer, RegistryNodeViewModel node)
	{
		using RegistryKey key = OpenKey(node, writable: false);
		string path = GetRegistryPath(node);

		writer.WriteLine($"[{path}]");
		foreach (string valueName in key.GetValueNames().OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
		{
			RegistryValueKind kind = key.GetValueKind(valueName);
			object? value = key.GetValue(valueName, defaultValue: null, RegistryValueOptions.DoNotExpandEnvironmentNames);
			writer.WriteLine($"{FormatValueName(valueName)}={FormatValue(value, kind)}");
		}

		writer.WriteLine();
		foreach (string childName in key.GetSubKeyNames().OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
		{
			RegistryNodeViewModel child = node.Children.FirstOrDefault(candidate =>
				string.Equals(candidate.Name, childName, StringComparison.OrdinalIgnoreCase))
				?? new RegistryNodeViewModel(childName, node.Hive, AppendPath(node.SubKeyPath, childName),
					"ms-appx:///Assets/Images/Folder.png", hasUnrealizedChildren: true, node.ComputerName, node);

			WriteNode(writer, child);
		}
	}

	private static string GetRegistryPath(RegistryNodeViewModel node)
		=> node.Hive is RegistryHive hive
			? string.IsNullOrEmpty(node.SubKeyPath)
				? GetHiveName(hive)
				: $"{GetHiveName(hive)}\\{node.SubKeyPath}"
			: string.Empty;

	private static string AppendPath(string parent, string child)
		=> string.IsNullOrEmpty(parent) ? child : $"{parent}\\{child}";

	private static string GetHiveName(RegistryHive hive) => hive switch
	{
		RegistryHive.ClassesRoot => "HKEY_CLASSES_ROOT",
		RegistryHive.CurrentUser => "HKEY_CURRENT_USER",
		RegistryHive.LocalMachine => "HKEY_LOCAL_MACHINE",
		RegistryHive.Users => "HKEY_USERS",
		RegistryHive.CurrentConfig => "HKEY_CURRENT_CONFIG",
		_ => throw new ArgumentOutOfRangeException(nameof(hive)),
	};

	private static string FormatValueName(string name)
		=> string.IsNullOrEmpty(name) ? "@" : $"\"{EscapeString(name)}\"";

	private static string FormatValue(object? value, RegistryValueKind kind)
	{
		if (value is null)
			return "\"\"";

		return kind switch
		{
			RegistryValueKind.String => $"\"{EscapeString(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty)}\"",
			RegistryValueKind.ExpandString => FormatHex(value, kind),
			RegistryValueKind.DWord => $"dword:{Convert.ToUInt32(value, CultureInfo.InvariantCulture):x8}",
			_ => FormatHex(value, kind),
		};
	}

	private static string FormatHex(object value, RegistryValueKind kind)
	{
		byte[] bytes = GetBytes(value, kind);
		string prefix = kind == RegistryValueKind.Binary ? "hex" : $"hex({(int)kind:x})";
		return $"{prefix}:{string.Join(',', bytes.Select(value => value.ToString("x2", CultureInfo.InvariantCulture)))}";
	}

	private static byte[] GetBytes(object value, RegistryValueKind kind)
	{
		if (value is byte[] bytes)
			return bytes;

		return kind switch
		{
			RegistryValueKind.DWord => BitConverter.GetBytes(Convert.ToUInt32(value, CultureInfo.InvariantCulture)),
			RegistryValueKind.QWord => BitConverter.GetBytes(Convert.ToUInt64(value, CultureInfo.InvariantCulture)),
			RegistryValueKind.MultiString when value is string[] strings
				=> Encoding.Unicode.GetBytes(string.Join('\0', strings) + "\0\0"),
			_ => Encoding.Unicode.GetBytes((Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty) + "\0"),
		};
	}

	private static string? ReadLogicalLine(StreamReader reader)
	{
		string? line = reader.ReadLine();
		if (line is null)
			return null;

		StringBuilder result = new(line);
		while (result.Length > 0 && result[^1] == '\\')
		{
			result.Length--;
			string? continuation = reader.ReadLine();
			if (continuation is null)
				break;

			result.Append(continuation.TrimStart());
		}

		return result.ToString();
	}

	private static void DeleteKey(string path)
	{
		(RegistryHive hive, string subKeyPath) = ParseRegistryPath(path);
		if (string.IsNullOrEmpty(subKeyPath))
			throw new InvalidOperationException("A registry hive cannot be deleted.");

		int separator = subKeyPath.LastIndexOf('\\');
		string parentPath = separator < 0 ? string.Empty : subKeyPath[..separator];
		string keyName = separator < 0 ? subKeyPath : subKeyPath[(separator + 1)..];
		using RegistryKey baseKey = OpenBaseKey(hive, computerName: null);
		using RegistryKey? parent = string.IsNullOrEmpty(parentPath)
			? baseKey
			: baseKey.OpenSubKey(parentPath, writable: true);
		parent?.DeleteSubKeyTree(keyName, throwOnMissingSubKey: false);
	}

	private static RegistryKey OpenKey(string path, bool writable)
	{
		(RegistryHive hive, string subKeyPath) = ParseRegistryPath(path);
		RegistryKey baseKey = OpenBaseKey(hive, computerName: null);
		if (string.IsNullOrEmpty(subKeyPath))
			return baseKey;

		RegistryKey? key = writable
			? baseKey.CreateSubKey(subKeyPath, writable: true)
			: baseKey.OpenSubKey(subKeyPath, writable: false);
		baseKey.Dispose();
		return key ?? throw new UnauthorizedAccessException($"Unable to open registry key '{path}'.");
	}

	private static (RegistryHive Hive, string SubKeyPath) ParseRegistryPath(string path)
	{
		string[] parts = path.Split('\\', 2, StringSplitOptions.TrimEntries);
		RegistryHive hive = parts[0].ToUpperInvariant() switch
		{
			"HKEY_CLASSES_ROOT" or "HKCR" => RegistryHive.ClassesRoot,
			"HKEY_CURRENT_USER" or "HKCU" => RegistryHive.CurrentUser,
			"HKEY_LOCAL_MACHINE" or "HKLM" => RegistryHive.LocalMachine,
			"HKEY_USERS" or "HKU" => RegistryHive.Users,
			"HKEY_CURRENT_CONFIG" or "HKCC" => RegistryHive.CurrentConfig,
			_ => throw new FormatException($"Unsupported registry root '{parts[0]}'."),
		};

		return (hive, parts.Length == 1 ? string.Empty : parts[1]);
	}

	private static string ParseValueName(string text)
	{
		text = text.Trim();
		return text == "@" ? string.Empty : UnescapeString(text.Trim('"'));
	}

	private static (RegistryValueKind Kind, object Value, RegistryValueOptions Options) ParseValue(string text)
	{
		if (text.Length >= 2 && text[0] == '"' && text[^1] == '"')
			return (RegistryValueKind.String, UnescapeString(text[1..^1]), RegistryValueOptions.None);

		if (text.StartsWith("dword:", StringComparison.OrdinalIgnoreCase)
			&& uint.TryParse(text[6..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint dword))
			return (RegistryValueKind.DWord, unchecked((int)dword), RegistryValueOptions.None);

		if (text.StartsWith("hex", StringComparison.OrdinalIgnoreCase))
			return ParseHexValue(text);

		throw new FormatException($"Unsupported registry value '{text}'.");
	}

	private static (RegistryValueKind Kind, object Value, RegistryValueOptions Options) ParseHexValue(string text)
	{
		int colon = text.IndexOf(':');
		if (colon < 0)
			throw new FormatException($"Malformed hexadecimal registry value '{text}'.");

		string prefix = text[..colon].ToLowerInvariant();
		RegistryValueKind kind = prefix switch
		{
			"hex" => RegistryValueKind.Binary,
			_ when prefix.StartsWith("hex(", StringComparison.Ordinal) && prefix.EndsWith(')')
				=> (RegistryValueKind)int.Parse(prefix[4..^1], NumberStyles.HexNumber, CultureInfo.InvariantCulture),
			_ => throw new FormatException($"Unsupported hexadecimal registry value '{text}'."),
		};

		byte[] bytes = text[(colon + 1)..]
			.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
			.Select(value => byte.Parse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture))
			.ToArray();

		return kind switch
		{
			RegistryValueKind.DWord when bytes.Length >= sizeof(uint)
				=> (kind, unchecked((int)BitConverter.ToUInt32(bytes, 0)), RegistryValueOptions.None),
			RegistryValueKind.QWord when bytes.Length >= sizeof(ulong)
				=> (kind, unchecked((long)BitConverter.ToUInt64(bytes, 0)), RegistryValueOptions.None),
			RegistryValueKind.String or RegistryValueKind.ExpandString
				=> (kind, DecodeString(bytes), RegistryValueOptions.DoNotExpandEnvironmentNames),
			RegistryValueKind.MultiString
				=> (kind, DecodeMultiString(bytes), RegistryValueOptions.None),
			_ => (kind, bytes, RegistryValueOptions.None),
		};
	}

	private static string DecodeString(byte[] bytes)
		=> Encoding.Unicode.GetString(bytes).TrimEnd('\0');

	private static string[] DecodeMultiString(byte[] bytes)
		=> DecodeString(bytes)
			.Split('\0', StringSplitOptions.RemoveEmptyEntries);

	private static string EscapeString(string value)
		=> value
			.Replace("\\", "\\\\", StringComparison.Ordinal)
			.Replace("\"", "\\\"", StringComparison.Ordinal)
			.Replace("\r", "\\r", StringComparison.Ordinal)
			.Replace("\n", "\\n", StringComparison.Ordinal);

	private static string UnescapeString(string value)
		=> value
			.Replace("\\r", "\r", StringComparison.Ordinal)
			.Replace("\\n", "\n", StringComparison.Ordinal)
			.Replace("\\\"", "\"", StringComparison.Ordinal)
			.Replace("\\\\", "\\", StringComparison.Ordinal);
}
