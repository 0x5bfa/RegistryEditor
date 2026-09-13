// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using System.Runtime.CompilerServices;
using System.Security;
using RegistryEditor.Services;

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
	private RegistryValueViewModel? _selectedValue;
	private string _breadcrumbRootText = "Computer";
	private string _breadcrumbPathText = "Computer";
	private string _valuesEmptyText = "Select a registry key";
	private bool _isValuesEmpty = true;
	private bool _isLoading;
	private int _activeLoads;
	private readonly HashSet<string> _loadedHivePaths = new(StringComparer.OrdinalIgnoreCase);

	public RootViewModel()
	{
		RegistryNodeViewModel computerNode = CreateNode(
			"Computer",
			hive: null,
			subKeyPath: string.Empty,
			ComputerImageUri,
			hasUnrealizedChildren: false);
		computerNode.IsExpanded = true;

		AddHiveNodes(computerNode);

		RootNodes.Add(computerNode);
		SelectedNode = computerNode;
	}

	public ObservableCollection<RegistryNodeViewModel> RootNodes { get; } = [];

	public ObservableCollection<RegistryBreadcrumbItemViewModel> BreadcrumbItems { get; } = [];

	public ObservableCollection<RegistryValueViewModel> RegistryValues { get; } = [];

	public RegistryNodeViewModel? SelectedNode
	{
		get => _selectedNode;
		set
		{
			if (!SetProperty(ref _selectedNode, value))
				return;

			OnPropertyChanged(nameof(CanEditSelectedNode));
			OnPropertyChanged(nameof(CanDeleteSelectedNode));
			OnPropertyChanged(nameof(CanRenameSelectedNode));
			OnPropertyChanged(nameof(CanCopySelectedKey));
			OnPropertyChanged(nameof(CanExportSelectedNode));
			OnPropertyChanged(nameof(CanLoadHive));
			OnPropertyChanged(nameof(CanUnloadHive));
			OnPropertyChanged(nameof(CanDisconnectRemote));
		}
	}

	public RegistryValueViewModel? SelectedValue
	{
		get => _selectedValue;
		set
		{
			if (!SetProperty(ref _selectedValue, value))
				return;

			OnPropertyChanged(nameof(CanEditSelectedValue));
		}
	}

	public bool CanEditSelectedNode => SelectedNode?.Hive is not null;

	public bool CanDeleteSelectedNode => SelectedNode is { Hive: not null, IsHiveRoot: false };

	public bool CanRenameSelectedNode => SelectedNode is { Hive: not null, IsHiveRoot: false };

	public bool CanCopySelectedKey => SelectedNode?.Hive is not null;

	public bool CanExportSelectedNode => SelectedNode?.Hive is not null;

	public bool CanEditSelectedValue => SelectedValue is not null && SelectedNode?.Hive is not null;

	public bool CanLoadHive
		=> SelectedNode is { IsHiveRoot: true, IsRemote: false, Hive: RegistryHive.Users or RegistryHive.LocalMachine };

	public bool CanUnloadHive
		=> SelectedNode is { IsHiveRoot: false, IsRemote: false }
			&& _loadedHivePaths.Contains(GetRegistryPath(SelectedNode));

	public bool CanDisconnectRemote => GetComputerNode(SelectedNode)?.IsRemote is true;

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
				node.Children.Add(CreateNode(
					child.Name,
					node.Hive,
					child.Path,
					FolderImageUri,
					child.HasChildren,
					computerName: node.ComputerName,
					parent: node));

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
		SelectedValue = null;

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
					CreateBitmapImage(value.IconUri),
					value.RegistryName,
					value.Kind,
					value.RawValue));

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

	public async Task RefreshAsync(RegistryNodeViewModel? node = null)
	{
		RegistryNodeViewModel? target = node ?? SelectedNode;
		if (target is null)
			return;

		if (target.Hive is not null)
		{
			target.Children.Clear();
			target.AreChildrenLoaded = false;
			target.HasUnrealizedChildren = true;
			await LoadChildrenAsync(target);
			target.IsExpanded = true;
		}

		await SelectNodeAsync(target);
	}

	public async Task<RegistryNodeViewModel?> CreateKeyAsync(RegistryNodeViewModel? parent, string name)
	{
		if (parent?.Hive is null)
			throw new InvalidOperationException("Select a registry key first.");

		ValidateKeyName(name);
		using RegistryKey key = RegistryFileService.OpenKey(parent, writable: true);
		using RegistryKey createdKey = key.CreateSubKey(name, writable: true)
			?? throw new UnauthorizedAccessException("The registry key could not be created.");

		await RefreshAsync(parent);
		parent.IsExpanded = true;
		return parent.Children.FirstOrDefault(child => string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase));
	}

	public async Task CreateValueAsync(RegistryNodeViewModel? node, string name, RegistryValueKind kind)
	{
		if (node?.Hive is null)
			throw new InvalidOperationException("Select a registry key first.");

		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("A value name must not be empty.", nameof(name));

		using RegistryKey key = RegistryFileService.OpenKey(node, writable: true);
		if (key.GetValueNames().Any(valueName => string.Equals(valueName, name, StringComparison.OrdinalIgnoreCase)))
			throw new InvalidOperationException($"The value '{name}' already exists.");

		object value = kind switch
		{
			RegistryValueKind.Binary => Array.Empty<byte>(),
			RegistryValueKind.DWord => 0,
			RegistryValueKind.QWord => 0L,
			RegistryValueKind.MultiString => Array.Empty<string>(),
			_ => string.Empty,
		};
		key.SetValue(name, value, kind);
		await SelectNodeAsync(node);
	}

	public async Task DeleteKeyAsync(RegistryNodeViewModel? node)
	{
		if (node?.Hive is null || node.IsHiveRoot || node.Parent?.Hive is null)
			throw new InvalidOperationException("The selected item cannot be deleted.");

		RegistryNodeViewModel parent = node.Parent;
		using RegistryKey key = RegistryFileService.OpenKey(parent, writable: true);
		key.DeleteSubKeyTree(node.Name, throwOnMissingSubKey: true);
		await RefreshAsync(parent);
	}

	public async Task<RegistryNodeViewModel?> RenameKeyAsync(RegistryNodeViewModel? node, string newName)
	{
		if (node?.Hive is null || node.IsHiveRoot || node.Parent?.Hive is null)
			throw new InvalidOperationException("The selected item cannot be renamed.");

		ValidateKeyName(newName);
		RegistryNodeViewModel parent = node.Parent;
		using RegistryKey key = RegistryFileService.OpenKey(parent, writable: true);
		int status = RegistryNative.RegRenameKey(key.Handle.DangerousGetHandle(), newName);
		if (status != 0)
			throw new Win32Exception(status);

		await RefreshAsync(parent);
		parent.IsExpanded = true;
		return parent.Children.FirstOrDefault(child => string.Equals(child.Name, newName, StringComparison.OrdinalIgnoreCase));
	}

	public async Task<int> ImportAsync(string filePath)
	{
		int importedValueCount = await Task.Run(() => RegistryFileService.Import(filePath));
		await RefreshAsync();
		return importedValueCount;
	}

	public Task ExportAsync(string filePath, RegistryNodeViewModel node)
		=> Task.Run(() => RegistryFileService.Export(filePath, node));

	public async Task LoadHiveAsync(RegistryNodeViewModel? parent, string filePath, string keyName)
	{
		if (parent is not { IsHiveRoot: true, IsRemote: false, Hive: RegistryHive.Users or RegistryHive.LocalMachine })
			throw new InvalidOperationException("Select HKEY_USERS or HKEY_LOCAL_MACHINE first.");

		ValidateKeyName(keyName);
		RegistryNative.EnablePrivilege("SeRestorePrivilege");
		RegistryNative.EnablePrivilege("SeBackupPrivilege");
		int status = RegistryNative.RegLoadKey(
			RegistryNative.GetPredefinedHiveHandle(parent.Hive!.Value),
			keyName,
			filePath);
		if (status != 0)
			throw new Win32Exception(status);

		_loadedHivePaths.Add($"{GetRegistryPath(parent)}\\{keyName}");
		await RefreshAsync(parent);
	}

	public async Task UnloadHiveAsync(RegistryNodeViewModel? node)
	{
		if (node is not { IsHiveRoot: false, IsRemote: false }
			|| node.Parent is not { Hive: RegistryHive.Users or RegistryHive.LocalMachine }
			|| !_loadedHivePaths.Contains(GetRegistryPath(node)))
			throw new InvalidOperationException("Select a loaded hive first.");

		RegistryNodeViewModel parent = node.Parent;
		RegistryNative.EnablePrivilege("SeRestorePrivilege");
		int status = RegistryNative.RegUnLoadKey(
			RegistryNative.GetPredefinedHiveHandle(parent.Hive!.Value),
			node.Name);
		if (status != 0)
			throw new Win32Exception(status);

		_loadedHivePaths.Remove(GetRegistryPath(node));
		await RefreshAsync(parent);
	}

	public async Task<RegistryNodeViewModel> ConnectRemoteAsync(string computerName)
	{
		string normalizedName = NormalizeComputerName(computerName);
		if (string.Equals(normalizedName, Environment.MachineName, StringComparison.OrdinalIgnoreCase))
			throw new InvalidOperationException("The local computer is already connected.");

		if (RootNodes.Any(node => string.Equals(node.ComputerName, normalizedName, StringComparison.OrdinalIgnoreCase)))
			throw new InvalidOperationException("That computer is already connected.");

		await Task.Run(() =>
		{
			using RegistryKey key = RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, normalizedName, RegistryView.Default);
		});

		RegistryNodeViewModel computerNode = CreateNode(
			normalizedName,
			hive: null,
			subKeyPath: string.Empty,
			ComputerImageUri,
			hasUnrealizedChildren: false,
			computerName: normalizedName);
		computerNode.IsExpanded = true;
		AddHiveNodes(computerNode);
		RootNodes.Add(computerNode);
		await SelectNodeAsync(computerNode);
		return computerNode;
	}

	public async Task DisconnectRemoteAsync(RegistryNodeViewModel? node)
	{
		RegistryNodeViewModel? computerNode = GetComputerNode(node);
		if (computerNode?.IsRemote is not true)
			throw new InvalidOperationException("Select a remote computer first.");

		RootNodes.Remove(computerNode);
		SelectedNode = RootNodes.FirstOrDefault(candidate => !candidate.IsRemote);
		await SelectNodeAsync(SelectedNode);
	}

	public async Task<RegistryNodeViewModel?> FindAsync(
		string query,
		RegistryNodeViewModel? startNode = null,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(query))
			return null;

		IEnumerable<RegistryNodeViewModel> searchRoots = startNode is null
			? RootNodes.ToArray()
			: [startNode];
		foreach (RegistryNodeViewModel root in searchRoots)
		{
			RegistryNodeViewModel? result = await FindInNodeAsync(root, query.Trim(), cancellationToken);
			if (result is not null)
				return result;
		}

		return null;
	}

	public async Task NavigateToPathAsync(string? path)
	{
		string[] segments = (path ?? string.Empty)
			.Trim()
			.Trim('\\')
			.Split('\\', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

		if (RootNodes.FirstOrDefault() is not { } localComputerNode)
			return;

		RegistryNodeViewModel computerNode = localComputerNode;
		int segmentIndex = 0;
		if (segments.Length > 0)
		{
			if (string.Equals(segments[0], localComputerNode.Name, StringComparison.OrdinalIgnoreCase))
			{
				segmentIndex = 1;
			}
			else if (RootNodes.FirstOrDefault(node =>
				string.Equals(node.Name, segments[0], StringComparison.OrdinalIgnoreCase)
				|| string.Equals(node.ComputerName, segments[0], StringComparison.OrdinalIgnoreCase)) is { } namedComputer)
			{
				computerNode = namedComputer;
				segmentIndex = 1;
			}
		}

		if (segmentIndex >= segments.Length)
		{
			await SelectNodeAsync(computerNode);
			return;
		}

		if (computerNode.Children.FirstOrDefault(node => string.Equals(node.Name, segments[segmentIndex], StringComparison.OrdinalIgnoreCase)) is not { } currentNode)
			return;

		for (int index = segmentIndex + 1; index < segments.Length; index++)
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
			return NavigateToPathAsync(GetComputerNode(SelectedNode)?.Name ?? "Computer");

		if (index < 0 || index >= BreadcrumbItems.Count)
			return Task.CompletedTask;

		string computerName = GetComputerNode(SelectedNode)?.Name ?? "Computer";
		string path = computerName + "\\" + string.Join("\\", BreadcrumbItems.Take(index + 1).Select(item => item.Text));

		return NavigateToPathAsync(path);
	}

	private void UpdateBreadcrumb(RegistryNodeViewModel? node)
	{
		if (node?.Hive is not RegistryHive hive)
		{
			string rootComputerName = GetComputerNode(node)?.Name ?? "Computer";
			BreadcrumbRootText = rootComputerName;
			BreadcrumbPathText = rootComputerName;
			BreadcrumbItems.Clear();
			return;
		}

		string computerName = GetComputerNode(node)?.Name ?? "Computer";
		BreadcrumbRootText = computerName;
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

		BreadcrumbPathText = computerName + "\\" + string.Join("\\", BreadcrumbItems.Select(item => item.Text));
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
		bool hasUnrealizedChildren,
		string? computerName = null,
		RegistryNodeViewModel? parent = null)
		=> new(name, hive, subKeyPath, iconUri, hasUnrealizedChildren, computerName, parent);

	private static IReadOnlyList<RegistrySubKeyData>? ReadSubkeys(RegistryNodeViewModel node)
	{
		try
		{
			using RegistryKey baseKey = RegistryFileService.OpenBaseKey(node);
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
			using RegistryKey baseKey = RegistryFileService.OpenBaseKey(node);
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
				GetValueImageUri(kind),
				valueName,
				kind,
				value);
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
			UnknownValueImageUri,
			valueName,
			RegistryValueKind.Unknown,
			null);

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

	public string GetRegistryPath(RegistryNodeViewModel node)
	{
		if (node.Hive is not RegistryHive hive)
			return node.Name;

		string path = string.IsNullOrEmpty(node.SubKeyPath)
			? GetHiveName(hive)
			: $"{GetHiveName(hive)}\\{node.SubKeyPath}";

		return node.IsRemote ? $"{node.ComputerName}\\{path}" : path;
	}

	private void AddHiveNodes(RegistryNodeViewModel computerNode)
	{
		computerNode.Children.Add(CreateNode("HKEY_CLASSES_ROOT", RegistryHive.ClassesRoot, string.Empty, FolderImageUri, hasUnrealizedChildren: true, computerNode.ComputerName, computerNode));
		computerNode.Children.Add(CreateNode("HKEY_CURRENT_USER", RegistryHive.CurrentUser, string.Empty, FolderImageUri, hasUnrealizedChildren: true, computerNode.ComputerName, computerNode));
		computerNode.Children.Add(CreateNode("HKEY_LOCAL_MACHINE", RegistryHive.LocalMachine, string.Empty, FolderImageUri, hasUnrealizedChildren: true, computerNode.ComputerName, computerNode));
		computerNode.Children.Add(CreateNode("HKEY_USERS", RegistryHive.Users, string.Empty, FolderImageUri, hasUnrealizedChildren: true, computerNode.ComputerName, computerNode));
		computerNode.Children.Add(CreateNode("HKEY_CURRENT_CONFIG", RegistryHive.CurrentConfig, string.Empty, FolderImageUri, hasUnrealizedChildren: true, computerNode.ComputerName, computerNode));
	}

	private async Task<RegistryNodeViewModel?> FindInNodeAsync(
		RegistryNodeViewModel node,
		string query,
		CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (node.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
			return node;

		if (node.Hive is null)
		{
			foreach (RegistryNodeViewModel child in node.Children.ToArray())
			{
				RegistryNodeViewModel? result = await FindInNodeAsync(child, query, cancellationToken);
				if (result is not null)
					return result;
			}

			return null;
		}

		IReadOnlyList<RegistryValueData> values = await Task.Run(
			() => ReadValues(node, cancellationToken),
			cancellationToken);
		if (values.Any(value => value.Name.Contains(query, StringComparison.OrdinalIgnoreCase)
			|| value.Data.Contains(query, StringComparison.OrdinalIgnoreCase)))
			return node;

		await LoadChildrenAsync(node);
		foreach (RegistryNodeViewModel child in node.Children.ToArray())
		{
			RegistryNodeViewModel? result = await FindInNodeAsync(child, query, cancellationToken);
			if (result is not null)
				return result;
		}

		return null;
	}

	private static RegistryNodeViewModel? GetComputerNode(RegistryNodeViewModel? node)
	{
		while (node?.Parent is not null)
			node = node.Parent;

		return node;
	}

	private static string NormalizeComputerName(string computerName)
	{
		string normalized = computerName.Trim().TrimStart('\\');
		if (normalized.Length == 0 || normalized.Contains('\\'))
			throw new ArgumentException("Enter a computer name.", nameof(computerName));

		return normalized;
	}

	private static void ValidateKeyName(string name)
	{
		if (string.IsNullOrWhiteSpace(name) || name.Contains('\\'))
			throw new ArgumentException("A key name must not be empty or contain a backslash.", nameof(name));
	}

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

	private sealed record RegistryValueData(
		string Name,
		string Type,
		string Data,
		string IconUri,
		string RegistryName,
		RegistryValueKind Kind,
		object? RawValue);
}
