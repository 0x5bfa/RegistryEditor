// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using System.Runtime.CompilerServices;
using System.Security;

namespace RegistryEditor.ViewModels;

public sealed class RootViewModel : INotifyPropertyChanged
{
	private const string ComputerImageUri = "ms-appx:///Assets/Images/Computer.png";
	private const string FolderImageUri = "ms-appx:///Assets/Images/Folder.png";
	private const string BinaryValueImageUri = "ms-appx:///Assets/Images/BinaryValue.png";
	private const string StringValueImageUri = "ms-appx:///Assets/Images/StringValue.png";
	private const string UnknownValueImageUri = "ms-appx:///Assets/Images/UnknownImage.png";

	private CancellationTokenSource? _valueLoadingCancellation;
	private RegistryNodeViewModel? _selectedNode;
	private string _breadcrumbRootText = "Computer";
	private string _breadcrumbPathText = "Computer";
	private string _valuesEmptyText = "Select a registry key";
	private bool _isValuesEmpty = true;
	private bool _isLoading;
	private int _activeLoads;

	public RootViewModel()
	{
		RegistryNodeViewModel computerNode = CreateNode(
			"Computer",
			hive: null,
			subKeyPath: string.Empty,
			ComputerImageUri,
			hasUnrealizedChildren: false);
		computerNode.IsExpanded = true;

		computerNode.Children.Add(CreateNode("HKEY_CLASSES_ROOT", RegistryHive.ClassesRoot, string.Empty, FolderImageUri, hasUnrealizedChildren: true));
		computerNode.Children.Add(CreateNode("HKEY_CURRENT_USER", RegistryHive.CurrentUser, string.Empty, FolderImageUri, hasUnrealizedChildren: true));
		computerNode.Children.Add(CreateNode("HKEY_LOCAL_MACHINE", RegistryHive.LocalMachine, string.Empty, FolderImageUri, hasUnrealizedChildren: true));
		computerNode.Children.Add(CreateNode("HKEY_USERS", RegistryHive.Users, string.Empty, FolderImageUri, hasUnrealizedChildren: true));
		computerNode.Children.Add(CreateNode("HKEY_CURRENT_CONFIG", RegistryHive.CurrentConfig, string.Empty, FolderImageUri, hasUnrealizedChildren: true));

		RootNodes.Add(computerNode);
		SelectedNode = computerNode;
	}

	public ObservableCollection<RegistryNodeViewModel> RootNodes { get; } = [];

	public ObservableCollection<RegistryBreadcrumbItemViewModel> BreadcrumbItems { get; } = [];

	public ObservableCollection<RegistryValueViewModel> RegistryValues { get; } = [];

	public RegistryNodeViewModel? SelectedNode
	{
		get => _selectedNode;
		set => SetProperty(ref _selectedNode, value);
	}

	public string BreadcrumbRootText
	{
		get => _breadcrumbRootText;
		private set => SetProperty(ref _breadcrumbRootText, value);
	}

	public string BreadcrumbPathText
	{
		get => _breadcrumbPathText;
		private set => SetProperty(ref _breadcrumbPathText, value);
	}

	public string ValuesEmptyText
	{
		get => _valuesEmptyText;
		private set => SetProperty(ref _valuesEmptyText, value);
	}

	public Visibility ValuesEmptyVisibility => _isValuesEmpty ? Visibility.Visible : Visibility.Collapsed;

	public Visibility LoadingVisibility => _isLoading ? Visibility.Visible : Visibility.Collapsed;

	public event PropertyChangedEventHandler? PropertyChanged;

	public async Task LoadChildrenAsync(RegistryNodeViewModel node)
	{
		if (node.Hive is null || !node.HasUnrealizedChildren || node.AreChildrenLoaded || node.IsChildrenLoading)
			return;

		node.IsChildrenLoading = true;
		BeginLoad();

		try
		{
			IReadOnlyList<RegistrySubKeyData>? children = await Task.Run(() => ReadSubkeys(node));
			if (children is null)
				return;

			foreach (RegistrySubKeyData child in children)
				node.Children.Add(CreateNode(child.Name, node.Hive, child.Path, FolderImageUri, child.HasChildren));

			node.AreChildrenLoaded = true;
			node.HasUnrealizedChildren = false;
		}
		catch (UnauthorizedAccessException)
		{
		}
		catch (SecurityException)
		{
		}
		catch (IOException)
		{
		}
		finally
		{
			node.IsChildrenLoading = false;
			EndLoad();
		}
	}

	public async Task SelectNodeAsync(RegistryNodeViewModel? node)
	{
		SelectedNode = node;
		UpdateBreadcrumb(node);

		_valueLoadingCancellation?.Cancel();
		CancellationTokenSource cancellation = new();
		_valueLoadingCancellation = cancellation;
		RegistryValues.Clear();

		if (node?.Hive is null)
		{
			SetEmptyValuesMessage("Select a registry key", isEmpty: true);
			ClearValueCancellation(cancellation);
			return;
		}

		SetEmptyValuesMessage("Loading values...", isEmpty: true);
		BeginLoad();

		try
		{
			IReadOnlyList<RegistryValueData> values = await Task.Run(
				() => ReadValues(node, cancellation.Token),
				cancellation.Token);

			if (!ReferenceEquals(_valueLoadingCancellation, cancellation))
				return;

			foreach (RegistryValueData value in values)
				RegistryValues.Add(new RegistryValueViewModel(
					value.Name,
					value.Type,
					value.Data,
					CreateBitmapImage(value.IconUri)));

			SetEmptyValuesMessage(
				RegistryValues.Count == 0 ? "No values" : string.Empty,
				isEmpty: RegistryValues.Count == 0);
		}
		catch (OperationCanceledException)
		{
		}
		finally
		{
			EndLoad();
			ClearValueCancellation(cancellation);
		}
	}

	public async Task NavigateToPathAsync(string? path)
	{
		string[] segments = (path ?? string.Empty)
			.Trim()
			.Trim('\\')
			.Split('\\', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		if (RootNodes.FirstOrDefault() is not { } computerNode)
			return;

		if (segments.Length is 0 || string.Equals(segments[0], computerNode.Name, StringComparison.OrdinalIgnoreCase))
		{
			if (segments.Length is 1 or 0)
			{
				await SelectNodeAsync(computerNode);

				return;
			}

			segments = segments[1..];
		}

		if (computerNode.Children.FirstOrDefault(node => string.Equals(node.Name, segments[0], StringComparison.OrdinalIgnoreCase)) is not { } currentNode)
			return;

		for (int index = 1; index < segments.Length; index++)
		{
			await LoadChildrenAsync(currentNode);
			currentNode = currentNode.Children.FirstOrDefault(node => string.Equals(node.Name, segments[index], StringComparison.OrdinalIgnoreCase));
			if (currentNode is null)
				return;
		}

		await SelectNodeAsync(currentNode);
	}

	public Task NavigateToBreadcrumbAsync(int index, bool isRootItem)
	{
		if (isRootItem)
			return NavigateToPathAsync("Computer");

		if (index < 0 || index >= BreadcrumbItems.Count)
			return Task.CompletedTask;

		string path = "Computer\\" + string.Join("\\", BreadcrumbItems.Take(index + 1).Select(item => item.Text));

		return NavigateToPathAsync(path);
	}

	private void UpdateBreadcrumb(RegistryNodeViewModel? node)
	{
		if (node?.Hive is not RegistryHive hive)
		{
			BreadcrumbRootText = "Computer";
			BreadcrumbPathText = "Computer";
			BreadcrumbItems.Clear();
			return;
		}

		BreadcrumbRootText = "Computer";
		BreadcrumbItems.Clear();
		BreadcrumbItems.Add(new RegistryBreadcrumbItemViewModel(
			GetHiveName(hive),
			!string.IsNullOrEmpty(node.SubKeyPath)));

		string[] segments = string.IsNullOrEmpty(node.SubKeyPath)
			? []
			: node.SubKeyPath.Split('\\');

		for (int index = 0; index < segments.Length; index++)
		{
			BreadcrumbItems.Add(new RegistryBreadcrumbItemViewModel(
				segments[index],
				index != segments.Length - 1));
		}

		BreadcrumbPathText = "Computer\\" + string.Join("\\", BreadcrumbItems.Select(item => item.Text));
	}

	private void SetEmptyValuesMessage(string text, bool isEmpty)
	{
		ValuesEmptyText = text;
		if (_isValuesEmpty == isEmpty)
			return;

		_isValuesEmpty = isEmpty;
		OnPropertyChanged(nameof(ValuesEmptyVisibility));
	}

	private void BeginLoad()
	{
		_activeLoads++;
		if (_activeLoads != 1)
			return;

		_isLoading = true;
		OnPropertyChanged(nameof(LoadingVisibility));
	}

	private void EndLoad()
	{
		if (_activeLoads == 0)
			return;

		_activeLoads--;
		if (_activeLoads != 0)
			return;

		_isLoading = false;
		OnPropertyChanged(nameof(LoadingVisibility));
	}

	private void ClearValueCancellation(CancellationTokenSource cancellation)
	{
		if (ReferenceEquals(_valueLoadingCancellation, cancellation))
			_valueLoadingCancellation = null;

		cancellation.Dispose();
	}

	private static RegistryNodeViewModel CreateNode(
		string name,
		RegistryHive? hive,
		string subKeyPath,
		string iconUri,
		bool hasUnrealizedChildren)
		=> new(name, hive, subKeyPath, iconUri, hasUnrealizedChildren);

	private static IReadOnlyList<RegistrySubKeyData>? ReadSubkeys(RegistryNodeViewModel node)
	{
		try
		{
			using RegistryKey baseKey = RegistryKey.OpenBaseKey(node.Hive!.Value, RegistryView.Default);
			using RegistryKey? subKey = string.IsNullOrEmpty(node.SubKeyPath)
				? null
				: baseKey.OpenSubKey(node.SubKeyPath, writable: false);
			RegistryKey key = subKey ?? baseKey;

			List<RegistrySubKeyData> children = [];
			foreach (string subKeyName in key.GetSubKeyNames().OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
			{
				string path = string.IsNullOrEmpty(node.SubKeyPath)
					? subKeyName
					: $"{node.SubKeyPath}\\{subKeyName}";

				bool hasChildren = true;
				try
				{
					using RegistryKey? childKey = key.OpenSubKey(subKeyName, writable: false);
					hasChildren = childKey?.SubKeyCount > 0;
				}
				catch (UnauthorizedAccessException)
				{
				}
				catch (SecurityException)
				{
				}

				children.Add(new RegistrySubKeyData(subKeyName, path, hasChildren));
			}

			return children;
		}
		catch (UnauthorizedAccessException)
		{
			return null;
		}
		catch (SecurityException)
		{
			return null;
		}
		catch (IOException)
		{
			return null;
		}
	}

	private static IReadOnlyList<RegistryValueData> ReadValues(
		RegistryNodeViewModel node,
		CancellationToken cancellationToken)
	{
		List<RegistryValueData> values = [];

		try
		{
			using RegistryKey baseKey = RegistryKey.OpenBaseKey(node.Hive!.Value, RegistryView.Default);
			using RegistryKey? subKey = string.IsNullOrEmpty(node.SubKeyPath)
				? null
				: baseKey.OpenSubKey(node.SubKeyPath, writable: false);
			RegistryKey key = subKey ?? baseKey;

			foreach (string valueName in key.GetValueNames().OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
			{
				cancellationToken.ThrowIfCancellationRequested();
				values.Add(ReadValue(key, valueName));
			}
		}
		catch (OperationCanceledException)
		{
			throw;
		}
		catch (UnauthorizedAccessException)
		{
		}
		catch (SecurityException)
		{
		}
		catch (IOException)
		{
		}

		return values;
	}

	private static RegistryValueData ReadValue(RegistryKey key, string valueName)
	{
		try
		{
			RegistryValueKind kind = key.GetValueKind(valueName);
			object? value = key.GetValue(valueName, defaultValue: null, RegistryValueOptions.DoNotExpandEnvironmentNames);

			return new RegistryValueData(
				string.IsNullOrEmpty(valueName) ? "(Default)" : valueName,
				GetValueTypeName(kind),
				FormatValue(kind, value),
				GetValueImageUri(kind));
		}
		catch (UnauthorizedAccessException)
		{
			return CreateUnreadableValue(valueName);
		}
		catch (SecurityException)
		{
			return CreateUnreadableValue(valueName);
		}
		catch (IOException)
		{
			return CreateUnreadableValue(valueName);
		}
	}

	private static RegistryValueData CreateUnreadableValue(string valueName)
		=> new(
			string.IsNullOrEmpty(valueName) ? "(Default)" : valueName,
			"REG_UNKNOWN",
			"(Unable to read)",
			UnknownValueImageUri);

	private static string GetValueTypeName(RegistryValueKind kind) => kind switch
	{
		RegistryValueKind.None => "REG_NONE",
		RegistryValueKind.String => "REG_SZ",
		RegistryValueKind.ExpandString => "REG_EXPAND_SZ",
		RegistryValueKind.Binary => "REG_BINARY",
		RegistryValueKind.DWord => "REG_DWORD",
		RegistryValueKind.MultiString => "REG_MULTI_SZ",
		RegistryValueKind.QWord => "REG_QWORD",
		RegistryValueKind.Unknown => "REG_UNKNOWN",
		_ => $"REG_{kind.ToString().ToUpperInvariant()}",
	};

	private static string GetValueImageUri(RegistryValueKind kind) => kind switch
	{
		RegistryValueKind.String or RegistryValueKind.ExpandString or RegistryValueKind.MultiString => StringValueImageUri,
		RegistryValueKind.Binary or RegistryValueKind.DWord or RegistryValueKind.QWord => BinaryValueImageUri,
		_ => UnknownValueImageUri,
	};

	private static string FormatValue(RegistryValueKind kind, object? value)
	{
		if (value is null)
			return "(value not set)";

		return kind switch
		{
			RegistryValueKind.Binary when value is byte[] bytes => FormatBinaryValue(bytes),
			RegistryValueKind.DWord => FormatDWordValue(value),
			RegistryValueKind.QWord => FormatQWordValue(value),
			RegistryValueKind.MultiString when value is string[] strings => string.Join("; ", strings),
			_ => value.ToString() ?? string.Empty,
		};
	}

	private static string FormatBinaryValue(byte[] value)
	{
		const int maximumDisplayedBytes = 256;
		string formatted = string.Join(" ", value.Take(maximumDisplayedBytes).Select(byteValue => byteValue.ToString("x2")));
		return value.Length > maximumDisplayedBytes ? $"{formatted} …" : formatted;
	}

	private static string FormatDWordValue(object value)
	{
		uint number = value switch
		{
			int signedNumber => unchecked((uint)signedNumber),
			uint unsignedNumber => unsignedNumber,
			_ => Convert.ToUInt32(value),
		};

		return $"0x{number:x8} ({number})";
	}

	private static string FormatQWordValue(object value)
	{
		ulong number = value switch
		{
			long signedNumber => unchecked((ulong)signedNumber),
			ulong unsignedNumber => unsignedNumber,
			_ => Convert.ToUInt64(value),
		};

		return $"0x{number:x16} ({number})";
	}

	private static string GetHiveName(RegistryHive hive) => hive switch
	{
		RegistryHive.ClassesRoot => "HKEY_CLASSES_ROOT",
		RegistryHive.CurrentUser => "HKEY_CURRENT_USER",
		RegistryHive.LocalMachine => "HKEY_LOCAL_MACHINE",
		RegistryHive.Users => "HKEY_USERS",
		RegistryHive.CurrentConfig => "HKEY_CURRENT_CONFIG",
		_ => hive.ToString(),
	};

	private static BitmapImage CreateBitmapImage(string uri) => new(new Uri(uri));

	private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(storage, value))
			return false;

		storage = value;
		OnPropertyChanged(propertyName);
		return true;
	}

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

	private sealed record RegistrySubKeyData(string Name, string Path, bool HasChildren);

	private sealed record RegistryValueData(string Name, string Type, string Data, string IconUri);
}
