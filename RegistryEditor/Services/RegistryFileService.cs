// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using RegistryEditor.ViewModels;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.Security.Authorization;
using Windows.Win32.System.Registry;

namespace RegistryEditor.Services;

internal sealed record RegistryKeySummary(
	uint SubKeyCount,
	uint ValueCount,
	DateTime LastWriteTime,
	string Owner);

internal static class RegistryFileService
{
	private const string RegeditHeader = "Windows Registry Editor Version 5.00";

	internal static void EnablePrivilege(string privilegeName)
	{
		using SafeFileHandle processHandle = PInvoke.GetCurrentProcess_SafeHandle();
		const TOKEN_ACCESS_MASK desiredAccess = TOKEN_ACCESS_MASK.TOKEN_ADJUST_PRIVILEGES | TOKEN_ACCESS_MASK.TOKEN_QUERY;
		if (!PInvoke.OpenProcessToken(processHandle, desiredAccess, out SafeFileHandle tokenHandle))
		{
			throw new Win32Exception(Marshal.GetLastWin32Error());
		}

		using (tokenHandle)
		{
			if (!PInvoke.LookupPrivilegeValue(null, privilegeName, out LUID luid))
			{
				throw new Win32Exception(Marshal.GetLastWin32Error());
			}

			unsafe
			{
				TOKEN_PRIVILEGES* privileges = stackalloc TOKEN_PRIVILEGES[1];
				privileges->PrivilegeCount = 1;
				privileges->Privileges[0] = new LUID_AND_ATTRIBUTES
				{
					Luid = luid,
					Attributes = TOKEN_PRIVILEGES_ATTRIBUTES.SE_PRIVILEGE_ENABLED,
				};

				if (!PInvoke.AdjustTokenPrivileges(tokenHandle, false, privileges, default, out _))
				{
					throw new Win32Exception(Marshal.GetLastWin32Error());
				}

				int error = Marshal.GetLastWin32Error();
				if (error != 0)
				{
					throw new Win32Exception(error);
				}
			}
		}
	}

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

		if (node.Hive is RegistryHive hive)
		{
			WriteNode(writer, hive, node.SubKeyPath, node.ComputerName);
		}
		else
		{
			foreach (RegistryHive rootHive in new[]
			{
				RegistryHive.ClassesRoot,
				RegistryHive.CurrentUser,
				RegistryHive.LocalMachine,
				RegistryHive.Users,
				RegistryHive.CurrentConfig,
			})
			{
				WriteNode(writer, rootHive, string.Empty, node.ComputerName);
			}
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
			{
				continue;
			}

			if (trimmed.StartsWith("Windows Registry Editor Version", StringComparison.OrdinalIgnoreCase)
				|| trimmed.StartsWith("REGEDIT", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

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
			{
				continue;
			}

			int separator = trimmed.IndexOf('=');
			if (separator <= 0)
			{
				continue;
			}

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
		{
			throw new InvalidOperationException("The selected item is not a registry key.");
		}

		return OpenKey(hive, node.SubKeyPath, node.ComputerName, writable);
	}

	private static RegistryKey OpenKey(RegistryHive hive, string subKeyPath, string? computerName, bool writable)
	{
		RegistryKey baseKey = OpenBaseKey(hive, computerName);
		if (string.IsNullOrEmpty(subKeyPath))
		{
			return baseKey;
		}

		RegistryKey? key = baseKey.OpenSubKey(subKeyPath, writable);
		baseKey.Dispose();
		return key ?? throw new UnauthorizedAccessException($"Unable to open registry key '{subKeyPath}'.");
	}

	internal static RegistryKeySummary ReadKeySummary(RegistryNodeViewModel node)
	{
		using RegistryKey key = OpenKey(node, writable: false);
		uint classLength = 0;
		WIN32_ERROR status = PInvoke.RegQueryInfoKey(
			key.Handle,
			default,
			ref classLength,
			out uint subKeyCount,
			out _,
			out _,
			out uint valueCount,
			out _,
			out _,
			out _,
			out System.Runtime.InteropServices.ComTypes.FILETIME lastWriteTime);
		if (status != WIN32_ERROR.NO_ERROR)
		{
			throw new Win32Exception((int)status);
		}

		long fileTime = unchecked((long)(((ulong)(uint)lastWriteTime.dwHighDateTime << 32) | (uint)lastWriteTime.dwLowDateTime));
		DateTime modified = DateTime.FromFileTimeUtc(fileTime).ToLocalTime();
		return new RegistryKeySummary(subKeyCount, valueCount, modified, ReadOwner(node));
	}

	private static unsafe string ReadOwner(RegistryNodeViewModel node)
	{
		try
		{
			using RegistrySecurityKey key = OpenSecurityKey(node);
			WIN32_ERROR result = PInvoke.GetSecurityInfo(
				key.Handle,
				SE_OBJECT_TYPE.SE_REGISTRY_KEY,
				OBJECT_SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION,
				out PSID owner,
				out _,
				out _,
				out _,
				out PSECURITY_DESCRIPTOR securityDescriptor);
			if (result != WIN32_ERROR.NO_ERROR)
			{
				if (securityDescriptor.Value is not null)
				{
					PInvoke.LocalFree((HLOCAL)(IntPtr)securityDescriptor.Value);
				}

				return "Unavailable";
			}

			try
			{
				if (owner.Value is null)
				{
					return "Not set";
				}

				SecurityIdentifier ownerSid = new((nint)owner.Value);
				try
				{
					return ownerSid.Translate(typeof(NTAccount)).Value ?? ownerSid.Value;
				}
				catch (SystemException)
				{
					return ownerSid.Value;
				}
			}
			finally
			{
				if (securityDescriptor.Value is not null)
				{
					PInvoke.LocalFree((HLOCAL)(IntPtr)securityDescriptor.Value);
				}
			}
		}
		catch (SystemException)
		{
			return "Unavailable";
		}
	}

	internal static RegistrySecurityKey OpenSecurityKey(RegistryNodeViewModel node)
	{
		if (node.Hive is not RegistryHive hive)
		{
			throw new InvalidOperationException("The selected item is not a registry key.");
		}

		using RegistryKey baseKey = OpenBaseKey(hive, node.ComputerName);
		string? subKeyName = string.IsNullOrEmpty(node.SubKeyPath) ? null : node.SubKeyPath;
		REG_SAM_FLAGS keyRead = REG_SAM_FLAGS.KEY_READ;
		REG_SAM_FLAGS securityEditorAccess = keyRead
			| REG_SAM_FLAGS.KEY_WRITE
			| (REG_SAM_FLAGS)TOKEN_ACCESS_MASK.TOKEN_WRITE_DAC
			| (REG_SAM_FLAGS)TOKEN_ACCESS_MASK.TOKEN_WRITE_OWNER;

		WIN32_ERROR status = PInvoke.RegOpenKeyEx(
			baseKey.Handle,
			subKeyName,
			0,
			securityEditorAccess,
			out SafeRegistryHandle keyHandle);
		if (status == WIN32_ERROR.NO_ERROR)
		{
			return new RegistrySecurityKey(keyHandle, canWriteSecurity: true);
		}

		keyHandle.Dispose();
		status = PInvoke.RegOpenKeyEx(
			baseKey.Handle,
			subKeyName,
			0,
			keyRead,
			out keyHandle);
		if (status != WIN32_ERROR.NO_ERROR)
		{
			keyHandle.Dispose();
			throw new Win32Exception((int)status);
		}

		return new RegistrySecurityKey(keyHandle, canWriteSecurity: false);
	}

	internal static RegistryKey OpenBaseKey(RegistryNodeViewModel node)
	{
		if (node.Hive is not RegistryHive hive)
		{
			throw new InvalidOperationException("The selected item is not a registry hive.");
		}

		return OpenBaseKey(hive, node.ComputerName);
	}

	internal static RegistryKey OpenBaseKey(RegistryHive hive, string? computerName)
		=> string.IsNullOrEmpty(computerName)
			? RegistryKey.OpenBaseKey(hive, RegistryView.Default)
			: RegistryKey.OpenRemoteBaseKey(hive, computerName, RegistryView.Default);

	private static void WriteNode(StreamWriter writer, RegistryHive hive, string subKeyPath, string? computerName)
	{
		using RegistryKey key = OpenKey(hive, subKeyPath, computerName, writable: false);
		string path = GetRegistryPath(hive, subKeyPath);

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
			WriteNode(writer, hive, AppendPath(subKeyPath, childName), computerName);
		}
	}

	private static string GetRegistryPath(RegistryHive hive, string subKeyPath)
		=> string.IsNullOrEmpty(subKeyPath)
			? GetHiveName(hive)
			: $"{GetHiveName(hive)}\\{subKeyPath}";

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
		{
			return "\"\"";
		}

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
		{
			return bytes;
		}

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
		{
			return null;
		}

		StringBuilder result = new(line);
		while (result.Length > 0 && result[^1] == '\\')
		{
			result.Length--;
			string? continuation = reader.ReadLine();
			if (continuation is null)
			{
				break;
			}

			result.Append(continuation.TrimStart());
		}

		return result.ToString();
	}

	private static void DeleteKey(string path)
	{
		(RegistryHive hive, string subKeyPath) = ParseRegistryPath(path);
		if (string.IsNullOrEmpty(subKeyPath))
		{
			throw new InvalidOperationException("A registry hive cannot be deleted.");
		}

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
		{
			return baseKey;
		}

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
		{
			return (RegistryValueKind.String, UnescapeString(text[1..^1]), RegistryValueOptions.None);
		}

		if (text.StartsWith("dword:", StringComparison.OrdinalIgnoreCase)
			&& uint.TryParse(text[6..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint dword))
		{
			return (RegistryValueKind.DWord, unchecked((int)dword), RegistryValueOptions.None);
		}

		if (text.StartsWith("hex", StringComparison.OrdinalIgnoreCase))
		{
			return ParseHexValue(text);
		}

		throw new FormatException($"Unsupported registry value '{text}'.");
	}

	private static (RegistryValueKind Kind, object Value, RegistryValueOptions Options) ParseHexValue(string text)
	{
		int colon = text.IndexOf(':');
		if (colon < 0)
		{
			throw new FormatException($"Malformed hexadecimal registry value '{text}'.");
		}

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
