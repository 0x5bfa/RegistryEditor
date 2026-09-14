// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using System.Globalization;
using System.Security;
using System.Security.AccessControl;
using System.Security.Principal;
using RegistryEditor.Services;
using RegistryEditor.Views;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace RegistryEditor.ViewModels;

public sealed class RootViewModel : ObservableObject
{
	private const string ComputerImageUri = "ms-appx:///Assets/Images/Computer.png";
	private const string FolderImageUri = "ms-appx:///Assets/Images/Folder.png";
	private const string BinaryValueImageUri = "ms-appx:///Assets/Images/BinaryValue.png";
	private const string StringValueImageUri = "ms-appx:///Assets/Images/StringValue.png";
	private const string UnknownValueImageUri = "ms-appx:///Assets/Images/UnknownImage.png";
	private const string NoValueText = "(No value)";

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
	private readonly Stack<RegistryNodeViewModel> _backHistory = [];
	private readonly Stack<RegistryNodeViewModel> _forwardHistory = [];
	private RegistryNodeViewModel? _navigationNode;
	private bool _isAddressBarVisible = true;
	private int _selectedThemeIndex;
	private static readonly ElementTheme[] ThemeValues =
	[
		ElementTheme.Default,
		ElementTheme.Light,
		ElementTheme.Dark,
	];

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
		InitializeCommands();
		SelectedNode = computerNode;
		_navigationNode = computerNode;
	}

	public ObservableCollection<RegistryNodeViewModel> RootNodes { get; } = [];

	public ObservableCollection<RegistryBreadcrumbItemViewModel> BreadcrumbItems { get; } = [];

	public ObservableCollection<RegistryValueViewModel> RegistryValues { get; } = [];

	public AsyncRelayCommand<object?> ImportCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> ExportCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> LoadHiveCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> UnloadHiveCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> ConnectRemoteCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> DisconnectRemoteCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> PrintCommand { get; private set; } = null!;

	public RelayCommand<object?> ExitCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> BackCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> ForwardCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> ExpandNodeCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> NavigateToNodeCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> NewKeyCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> NewStringValueCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> NewBinaryValueCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> NewDWordValueCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> NewQWordValueCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> NewMultiStringValueCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> NewExpandableStringValueCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> ModifyValueCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> DeleteValueCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> RenameValueCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> FindCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> DeleteKeyCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> RenameKeyCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> PermissionsCommand { get; private set; } = null!;

	public RelayCommand<object?> CopyKeyCommand { get; private set; } = null!;

	public RelayCommand<object?> ToggleAddressBarCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> DisplayBinaryDataCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> RefreshCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> SettingsCommand { get; private set; } = null!;

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
			NotifyCommandStates();
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
			NotifyCommandStates();
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

	public bool CanGoBack => _backHistory.Count > 0;

	public bool CanGoForward => _forwardHistory.Count > 0;

	public bool IsAddressBarVisible
	{
		get => _isAddressBarVisible;
		private set
		{
			if (!SetProperty(ref _isAddressBarVisible, value))
				return;

			OnPropertyChanged(nameof(AddressBarVisibility));
		}
	}

	public Visibility AddressBarVisibility => IsAddressBarVisible ? Visibility.Visible : Visibility.Collapsed;

	public ObservableCollection<string> ThemeOptions { get; } = ["System default", "Light", "Dark"];

	public int SelectedThemeIndex
	{
		get => _selectedThemeIndex;
		set
		{
			if (!SetProperty(ref _selectedThemeIndex, value)
				|| value < 0
				|| value >= ThemeValues.Length)
				return;

			if (App.Window.Content is FrameworkElement root)
				root.RequestedTheme = ThemeValues[value];
		}
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

	private void InitializeCommands()
	{
		ImportCommand = new(ExecuteImportCommandAsync);
		ExportCommand = new(ExecuteExportCommandAsync, CanExportNode);
		LoadHiveCommand = new(ExecuteLoadHiveCommandAsync, _ => CanLoadHive);
		UnloadHiveCommand = new(ExecuteUnloadHiveCommandAsync, CanUnloadNode);
		ConnectRemoteCommand = new(ExecuteConnectRemoteCommandAsync);
		DisconnectRemoteCommand = new(ExecuteDisconnectRemoteCommandAsync, _ => CanDisconnectRemote);
		PrintCommand = new(ExecutePrintCommandAsync, CanExportNode);
		ExitCommand = new(_ => Close());

		BackCommand = new(ExecuteBackCommandAsync, _ => CanGoBack);
		ForwardCommand = new(ExecuteForwardCommandAsync, _ => CanGoForward);
		ExpandNodeCommand = new(ExecuteExpandNodeCommandAsync, CanExpandNode);
		NavigateToNodeCommand = new(ExecuteNavigateToNodeCommandAsync, CanNavigateToNode);
		NewKeyCommand = new(ExecuteNewKeyCommandAsync, CanEditNode);
		NewStringValueCommand = new(
			parameter => CreateValueFromCommandAsync(parameter, RegistryValueKind.String),
			CanEditNode);
		NewBinaryValueCommand = new(
			parameter => CreateValueFromCommandAsync(parameter, RegistryValueKind.Binary),
			CanEditNode);
		NewDWordValueCommand = new(
			parameter => CreateValueFromCommandAsync(parameter, RegistryValueKind.DWord),
			CanEditNode);
		NewQWordValueCommand = new(
			parameter => CreateValueFromCommandAsync(parameter, RegistryValueKind.QWord),
			CanEditNode);
		NewMultiStringValueCommand = new(
			parameter => CreateValueFromCommandAsync(parameter, RegistryValueKind.MultiString),
			CanEditNode);
		NewExpandableStringValueCommand = new(
			parameter => CreateValueFromCommandAsync(parameter, RegistryValueKind.ExpandString),
			CanEditNode);
		ModifyValueCommand = new(ExecuteModifyValueCommandAsync, CanModifyValue);
		DeleteValueCommand = new(ExecuteDeleteValueCommandAsync, CanDeleteValue);
		RenameValueCommand = new(ExecuteRenameValueCommandAsync, CanRenameValue);
		FindCommand = new(ExecuteFindCommandAsync);
		DeleteKeyCommand = new(ExecuteDeleteKeyCommandAsync, CanDeleteNode);
		RenameKeyCommand = new(ExecuteRenameKeyCommandAsync, CanRenameNode);
		PermissionsCommand = new(ExecutePermissionsCommandAsync, CanKeyNode);
		CopyKeyCommand = new(ExecuteCopyKeyCommand, CanKeyNode);

		ToggleAddressBarCommand = new(_ =>
		{
			SetAddressBarVisibility(!IsAddressBarVisible);
		});
		DisplayBinaryDataCommand = new(ExecuteDisplayBinaryDataCommandAsync, _ => CanEditSelectedValue);
		RefreshCommand = new(ExecuteRefreshCommandAsync, _ => SelectedNode is not null);
		SettingsCommand = new(ExecuteSettingsCommandAsync);
	}

	private bool CanKeyNode(object? parameter)
		=> ResolveNode(parameter)?.Hive is not null;

	private bool CanExpandNode(object? parameter)
		=> ResolveNode(parameter) is { Hive: not null } node
			&& (node.IsExpanded || node.HasUnrealizedChildren || node.Children.Count > 0);

	private static bool CanNavigateToNode(object? parameter)
		=> parameter is RegistryNodeViewModel;

	private bool CanEditNode(object? parameter)
		=> ResolveNode(parameter)?.Hive is not null;

	private bool CanDeleteNode(object? parameter)
		=> ResolveNode(parameter) is { Hive: not null, IsHiveRoot: false };

	private bool CanRenameNode(object? parameter)
		=> CanDeleteNode(parameter);

	private bool CanUnloadNode(object? parameter)
	{
		RegistryNodeViewModel? node = ResolveNode(parameter);
		return node is { IsHiveRoot: false, IsRemote: false }
			&& _loadedHivePaths.Contains(GetRegistryPath(node));
	}

	private bool CanExportNode(object? parameter)
		=> CanKeyNode(parameter);

	private bool CanModifyValue(object? parameter)
	{
		RegistryValueViewModel? value = ResolveValue(parameter);
		return value is not null
			&& value.Kind is RegistryValueKind.String or RegistryValueKind.DWord or RegistryValueKind.QWord
			&& SelectedNode?.Hive is not null;
	}

	private bool CanDeleteValue(object? parameter)
	{
		RegistryValueViewModel? value = ResolveValue(parameter);
		return value?.RawValue is not null && SelectedNode?.Hive is not null;
	}

	private bool CanRenameValue(object? parameter)
	{
		return CanDeleteValue(parameter);
	}

	private RegistryNodeViewModel? ResolveNode(object? parameter)
		=> parameter as RegistryNodeViewModel ?? SelectedNode;

	private RegistryValueViewModel? ResolveValue(object? parameter)
	{
		return parameter as RegistryValueViewModel ?? SelectedValue;
	}

	private async Task RunCommandAsync(string errorTitle, Func<Task> action)
	{
		try
		{
			await action();
		}
		catch (Exception exception)
		{
			await ShowMessageAsync(errorTitle, exception.Message);
		}
	}

	private Task ExecuteImportCommandAsync(object? parameter)
		=> RunCommandAsync("Import failed", async () =>
		{
			if (await PickImportFileAsync() is not { } filePath)
				return;

			int count = await ImportAsync(filePath);
			await ShowMessageAsync(
				"Import",
				$"Imported {count} value(s) from '{Path.GetFileName(filePath)}'.");
		});

	private Task ExecuteExportCommandAsync(object? parameter)
		=> RunCommandAsync("Export failed", async () =>
		{
			RegistryNodeViewModel? node = ResolveNode(parameter);
			if (node?.Hive is null || await PickExportFileAsync() is not { } filePath)
				return;

			await ExportAsync(filePath, node);
			await ShowMessageAsync(
				"Export",
				$"Exported '{GetRegistryPath(node)}' to '{Path.GetFileName(filePath)}'.");
		});

	private Task ExecuteLoadHiveCommandAsync(object? parameter)
		=> RunCommandAsync("Load hive failed", async () =>
		{
			if (SelectedNode is not { } parent
				|| await PickHiveFileAsync() is not { } filePath
				|| await RequestTextAsync("Load hive", "Key name", "LoadedHive") is not { } keyName)
				return;

			await LoadHiveAsync(parent, filePath, keyName);
		});

	private Task ExecuteUnloadHiveCommandAsync(object? parameter)
		=> RunCommandAsync("Unload hive failed", async () =>
		{
			RegistryNodeViewModel? node = ResolveNode(parameter);
			if (node is null || !await ConfirmAsync("Unload hive", $"Unload '{node.Name}'?"))
				return;

			await UnloadHiveAsync(node);
		});

	private Task ExecuteConnectRemoteCommandAsync(object? parameter)
		=> RunCommandAsync("Connection failed", async () =>
		{
			if (await RequestTextAsync("Connect to network registry", "Computer name", "") is { } computerName)
				await ConnectRemoteAsync(computerName);
		});

	private Task ExecuteDisconnectRemoteCommandAsync(object? parameter)
		=> RunCommandAsync("Disconnect failed", async () =>
		{
			if (!await ConfirmAsync("Disconnect", "Disconnect from the selected remote computer?"))
				return;

			await DisconnectRemoteAsync(ResolveNode(parameter));
		});

	private Task ExecutePrintCommandAsync(object? parameter)
		=> RunCommandAsync("Print failed", async () =>
		{
			RegistryNodeViewModel? node = ResolveNode(parameter);
			if (node?.Hive is null)
				return;

			string filePath = Path.Combine(Path.GetTempPath(), $"RegistryEditor-{Guid.NewGuid():N}.reg");
			try
			{
				await ExportAsync(filePath, node);
				await PrintFileAsync(filePath);
			}
			finally
			{
				TryDeleteFile(filePath);
			}
		});

	private async Task ExecuteExpandNodeCommandAsync(object? parameter)
	{
		RegistryNodeViewModel? node = ResolveNode(parameter);
		if (node is null)
			return;

		await RunCommandAsync("Registry operation failed", async () =>
		{
			node.IsExpanded = !node.IsExpanded;
			if (node.IsExpanded)
				await LoadChildrenAsync(node);
		});
	}

	private Task ExecuteNavigateToNodeCommandAsync(object? parameter)
		=> parameter is RegistryNodeViewModel node
			? SelectNodeAsync(node)
			: Task.CompletedTask;

	private Task ExecuteNewKeyCommandAsync(object? parameter)
		=> RunCommandAsync("Registry operation failed", async () =>
		{
			RegistryNodeViewModel? node = ResolveNode(parameter);
			if (node is not null
				&& await RequestTextAsync("New key", "Key name", "New Key") is { } keyName)
				await CreateKeyAsync(node, keyName);
		});

	private Task CreateValueFromCommandAsync(object? parameter, RegistryValueKind kind)
		=> RunCommandAsync("Registry operation failed", async () =>
		{
			RegistryNodeViewModel? node = ResolveNode(parameter);
			if (node is not null
				&& await RequestTextAsync("New value", "Value name", "New Value") is { } name)
				await CreateValueAsync(node, name, kind);
		});

	private async Task ExecuteModifyValueCommandAsync(object? parameter)
	{
		if (ResolveValue(parameter) is { } value)
			await ModifyValueAsync(value);
	}

	private async Task ExecuteDeleteValueCommandAsync(object? parameter)
	{
		if (ResolveValue(parameter) is not { } value || !CanDeleteValue(value))
			return;

		await RunCommandAsync("Registry operation failed", async () =>
		{
			if (await ConfirmAsync("Delete value", $"Delete '{value.Name}'?"))
				await DeleteValueAsync(value);
		});
	}

	private async Task ExecuteRenameValueCommandAsync(object? parameter)
	{
		if (ResolveValue(parameter) is not { } value || !CanRenameValue(value))
			return;

		await RunCommandAsync("Registry operation failed", async () =>
		{
			if (await RequestTextAsync("Rename value", "New value name", value.RegistryName) is { } newName)
				await RenameValueAsync(value, newName);
		});
	}

	private Task ExecuteFindCommandAsync(object? parameter)
		=> RunCommandAsync("Find failed", async () =>
		{
			if (await RequestTextAsync("Find", "Search for a key, value, or data", "") is not { } query)
				return;

			RegistryNodeViewModel? result = await FindAsync(query, parameter as RegistryNodeViewModel);
			if (result is null)
			{
				await ShowMessageAsync("Find", $"No match was found for '{query}'.");
				return;
			}

			result.IsExpanded = true;
			await SelectNodeAsync(result);
		});

	private Task ExecuteDeleteKeyCommandAsync(object? parameter)
		=> RunCommandAsync("Registry operation failed", async () =>
		{
			RegistryNodeViewModel? node = ResolveNode(parameter);
			if (node is not null
				&& await ConfirmAsync("Delete key", $"Delete '{node.Name}' and all of its subkeys?"))
				await DeleteKeyAsync(node);
		});

	private Task ExecuteRenameKeyCommandAsync(object? parameter)
		=> RunCommandAsync("Registry operation failed", async () =>
		{
			RegistryNodeViewModel? node = ResolveNode(parameter);
			if (node is null
				|| await RequestTextAsync("Rename key", "New key name", node.Name) is not { } newName)
				return;

			if (await RenameKeyAsync(node, newName) is { } renamedNode)
				await SelectNodeAsync(renamedNode);
		});

	private Task ExecutePermissionsCommandAsync(object? parameter)
		=> RunCommandAsync("Registry operation failed", async () =>
		{
			if (ResolveNode(parameter) is { } node)
				await EditPermissionsAsync(node);
		});

	private void ExecuteCopyKeyCommand(object? parameter)
	{
		if (ResolveNode(parameter) is { Hive: not null } node)
			CopyText(GetRegistryPath(node));
	}

	private Task ExecuteDisplayBinaryDataCommandAsync(object? parameter)
		=> RunCommandAsync("Unable to display the value", async () =>
		{
			if (SelectedValue is { } value)
				await ShowValueAsync(value);
		});

	public async Task DisplaySelectedValueAsync()
	{
		if (SelectedValue is not { } value)
			return;

		if (value.Kind == RegistryValueKind.String)
		{
			await ModifyValueAsync(value);
			return;
		}

		if (value.Kind is RegistryValueKind.DWord or RegistryValueKind.QWord)
		{
			await ModifyValueAsync(value);
			return;
		}

		await ShowValueAsync(value);
	}

	public async Task ModifyValueAsync(RegistryValueViewModel value)
	{
		if (value.Kind == RegistryValueKind.String)
		{
			await EditStringValueAsync(value);
			return;
		}

		if (value.Kind is RegistryValueKind.DWord or RegistryValueKind.QWord)
			await EditIntegerValueAsync(value);
	}

	private Task ExecuteRefreshCommandAsync(object? parameter)
		=> RunCommandAsync("Refresh failed", () => RefreshAsync());

	private Task ExecuteSettingsCommandAsync(object? parameter)
	{
		return RunCommandAsync("Settings failed", async () =>
		{
			SettingsContentDialog dialog = new(this)
			{
				XamlRoot = GetXamlRoot(),
			};
			await dialog.ShowAsync();
		});
	}

	private async Task ExecuteBackCommandAsync(object? parameter)
	{
		if (!CanGoBack)
			return;

		RegistryNodeViewModel target = _backHistory.Pop();
		if (_navigationNode is not null)
			_forwardHistory.Push(_navigationNode);

		_navigationNode = target;
		NotifyNavigationState();
		await SelectNodeAsync(target, recordHistory: false);
	}

	private async Task ExecuteForwardCommandAsync(object? parameter)
	{
		if (!CanGoForward)
			return;

		RegistryNodeViewModel target = _forwardHistory.Pop();
		if (_navigationNode is not null)
			_backHistory.Push(_navigationNode);

		_navigationNode = target;
		NotifyNavigationState();
		await SelectNodeAsync(target, recordHistory: false);
	}

	private void RecordNavigation(RegistryNodeViewModel? node)
	{
		if (node is null || ReferenceEquals(_navigationNode, node))
			return;

		if (_navigationNode is not null)
			_backHistory.Push(_navigationNode);

		_navigationNode = node;
		_forwardHistory.Clear();
		NotifyNavigationState();
	}

	private void NotifyNavigationState()
	{
		OnPropertyChanged(nameof(CanGoBack));
		OnPropertyChanged(nameof(CanGoForward));
		BackCommand?.NotifyCanExecuteChanged();
		ForwardCommand?.NotifyCanExecuteChanged();
	}

	private void NotifyCommandStates()
	{
		if (ImportCommand is null)
			return;

		ImportCommand.NotifyCanExecuteChanged();
		ExportCommand.NotifyCanExecuteChanged();
		LoadHiveCommand.NotifyCanExecuteChanged();
		UnloadHiveCommand.NotifyCanExecuteChanged();
		ConnectRemoteCommand.NotifyCanExecuteChanged();
		DisconnectRemoteCommand.NotifyCanExecuteChanged();
		PrintCommand.NotifyCanExecuteChanged();
		BackCommand.NotifyCanExecuteChanged();
		ForwardCommand.NotifyCanExecuteChanged();
		ExpandNodeCommand.NotifyCanExecuteChanged();
		NewKeyCommand.NotifyCanExecuteChanged();
		NewStringValueCommand.NotifyCanExecuteChanged();
		NewBinaryValueCommand.NotifyCanExecuteChanged();
		NewDWordValueCommand.NotifyCanExecuteChanged();
		NewQWordValueCommand.NotifyCanExecuteChanged();
		NewMultiStringValueCommand.NotifyCanExecuteChanged();
		NewExpandableStringValueCommand.NotifyCanExecuteChanged();
		ModifyValueCommand.NotifyCanExecuteChanged();
		DeleteValueCommand.NotifyCanExecuteChanged();
		RenameValueCommand.NotifyCanExecuteChanged();
		FindCommand.NotifyCanExecuteChanged();
		DeleteKeyCommand.NotifyCanExecuteChanged();
		RenameKeyCommand.NotifyCanExecuteChanged();
		PermissionsCommand.NotifyCanExecuteChanged();
		CopyKeyCommand.NotifyCanExecuteChanged();
		DisplayBinaryDataCommand.NotifyCanExecuteChanged();
		RefreshCommand.NotifyCanExecuteChanged();
	}

	public async Task<string?> RequestTextAsync(string title, string placeholder, string initialText)
	{
		InputContentDialog dialog = new()
		{
			XamlRoot = GetXamlRoot(),
		};
		dialog.Configure(title, placeholder, initialText);
		ContentDialogResult result = await dialog.ShowAsync();
		return result == ContentDialogResult.Primary ? dialog.Text.Trim() : null;
	}

	public async Task<bool> ConfirmAsync(string title, string message)
	{
		ConfirmationContentDialog dialog = new()
		{
			XamlRoot = GetXamlRoot(),
		};
		dialog.Configure(title, message);
		return await dialog.ShowAsync() == ContentDialogResult.Primary;
	}

	public async Task ShowMessageAsync(string title, string message)
	{
		MessageContentDialog dialog = new()
		{
			XamlRoot = GetXamlRoot(),
		};
		dialog.Configure(title, message);
		await dialog.ShowAsync();
	}

	public async Task<string?> PickImportFileAsync()
	{
		FileOpenPicker picker = new();
		picker.FileTypeFilter.Add(".reg");
		InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.Window));
		StorageFile? file = await picker.PickSingleFileAsync();
		return file?.Path;
	}

	public async Task<string?> PickHiveFileAsync()
	{
		FileOpenPicker picker = new();
		picker.FileTypeFilter.Add(".hiv");
		picker.FileTypeFilter.Add(".dat");
		picker.FileTypeFilter.Add(".*");
		InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.Window));
		StorageFile? file = await picker.PickSingleFileAsync();
		return file?.Path;
	}

	public async Task<string?> PickExportFileAsync()
	{
		FileSavePicker picker = new()
		{
			SuggestedFileName = "registry.reg",
		};
		picker.FileTypeChoices.Add("Registry file", [".reg"]);
		InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.Window));
		StorageFile? file = await picker.PickSaveFileAsync();
		return file?.Path;
	}

	public async Task EditPermissionsAsync(RegistryNodeViewModel node)
	{
		try
		{
			PermissionsContentDialog dialog = new()
			{
				XamlRoot = GetXamlRoot(),
			};
			dialog.Configure(GetRegistryPath(node), GetPermissionRules(node));
			dialog.PrimaryButtonClick += (_, eventArgs) =>
			{
				if (dialog.Account.Length == 0)
				{
					dialog.SetError("Enter an account name.");
					eventArgs.Cancel = true;
					return;
				}

				if (dialog.SelectedPermission is not { } permission)
				{
					dialog.SetError("Select a permission level.");
					eventArgs.Cancel = true;
					return;
				}

				try
				{
					AddPermission(node, dialog.Account, permission, dialog.SelectedAccessType);
				}
				catch (Exception exception)
				{
					dialog.SetError(exception.Message);
					eventArgs.Cancel = true;
				}
			};

			await dialog.ShowAsync();
		}
		catch (Exception exception)
		{
			await ShowMessageAsync("Unable to read access permissions", exception.Message);
		}
	}

	public async Task PrintFileAsync(string filePath)
	{
		Process? process = Process.Start(new ProcessStartInfo
		{
			FileName = "notepad.exe",
			UseShellExecute = true,
			ArgumentList = { "/p", filePath },
		});

		if (process is null)
			throw new InvalidOperationException("The system print helper could not be started.");

		await process.WaitForExitAsync();
	}

	public async Task ShowValueAsync(RegistryValueViewModel value)
	{
		string data = value.RawValue switch
		{
			byte[] bytes => string.Join(" ", bytes.Select(item => item.ToString("x2"))),
			_ => value.Data,
		};
		await ShowMessageAsync($"{value.Name} ({value.Type})", data.Length == 0 ? "(empty)" : data);
	}

	public async Task EditStringValueAsync(RegistryValueViewModel value)
	{
		if (value.Kind != RegistryValueKind.String)
			return;

		await EditRegistryValueAsync(value);
	}

	public async Task EditIntegerValueAsync(RegistryValueViewModel value)
	{
		if (value.Kind is not (RegistryValueKind.DWord or RegistryValueKind.QWord))
			return;

		await EditRegistryValueAsync(value);
	}

	private async Task EditRegistryValueAsync(RegistryValueViewModel value)
	{
		await RunCommandAsync("Registry operation failed", async () =>
		{
			if (SelectedNode is not { Hive: not null } node)
				throw new InvalidOperationException("Select a registry key first.");

			EditValueContentDialog dialog = new()
			{
				XamlRoot = GetXamlRoot(),
			};
			if (value.Kind == RegistryValueKind.String)
				dialog.Configure(value.RegistryName, value.RawValue as string ?? string.Empty);
			else
				dialog.ConfigureInteger(value.RegistryName, GetIntegerValue(value));

			if (await dialog.ShowAsync() != ContentDialogResult.Primary)
				return;

			string newName = dialog.ValueName;
			object newValue;
			if (value.Kind == RegistryValueKind.String)
				newValue = dialog.ValueData;
			else if (value.Kind == RegistryValueKind.DWord)
				newValue = unchecked((int)ParseIntegerValue(dialog.ValueData, value.Kind, dialog.IsHexadecimal));
			else if (value.Kind == RegistryValueKind.QWord)
				newValue = unchecked((long)ParseIntegerValue(dialog.ValueData, value.Kind, dialog.IsHexadecimal));
			else
				throw new InvalidOperationException("This registry value type cannot be edited.");

			using RegistryKey key = RegistryFileService.OpenKey(node, writable: true);
			bool nameChanged = !string.Equals(value.RegistryName, newName, StringComparison.OrdinalIgnoreCase);
			if (nameChanged
				&& key.GetValueNames().Any(existingName =>
					string.Equals(existingName, newName, StringComparison.OrdinalIgnoreCase)))
				throw new InvalidOperationException($"The value '{newName}' already exists.");

			key.SetValue(newName, newValue, value.Kind);
			if (nameChanged)
				key.DeleteValue(value.RegistryName, throwOnMissingValue: true);

			await RefreshAsync(node);
		});
	}

	private static ulong GetIntegerValue(RegistryValueViewModel value)
	{
		if (value.RawValue is null)
			return 0;

		if (value.Kind == RegistryValueKind.DWord)
		{
			if (value.RawValue is int signedValue)
				return unchecked((uint)signedValue);
			if (value.RawValue is uint unsignedValue)
				return unsignedValue;
			return Convert.ToUInt32(value.RawValue, CultureInfo.InvariantCulture);
		}

		if (value.Kind == RegistryValueKind.QWord)
		{
			if (value.RawValue is long signedValue)
				return unchecked((ulong)signedValue);
			if (value.RawValue is ulong unsignedValue)
				return unsignedValue;
			return Convert.ToUInt64(value.RawValue, CultureInfo.InvariantCulture);
		}

		throw new InvalidOperationException("This registry value is not an integer value.");
	}

	private static ulong ParseIntegerValue(string text, RegistryValueKind kind, bool isHexadecimal)
	{
		string input = text.Trim();
		if (isHexadecimal && input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
			input = input[2..];

		bool parsed = isHexadecimal
			? ulong.TryParse(input, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out ulong value)
			: ulong.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
		if (!parsed || (kind == RegistryValueKind.DWord && value > uint.MaxValue))
		{
			string valueType = kind == RegistryValueKind.DWord ? "DWORD" : "QWORD";
			string valueBase = isHexadecimal ? "base 16" : "base 10";
			throw new FormatException($"Enter a valid {valueType} value in {valueBase}.");
		}

		return value;
	}

	public void CopyText(string text)
	{
		DataPackage package = new();
		package.SetText(text);
		Clipboard.SetContent(package);
	}

	public void Close()
		=> App.Window.Close();

	public void SetAddressBarVisibility(bool isVisible)
		=> IsAddressBarVisible = isVisible;

	private static XamlRoot GetXamlRoot()
	{
		if (App.Window is { Content: FrameworkElement content } && content.XamlRoot is { } xamlRoot)
			return xamlRoot;

		throw new InvalidOperationException("The registry editor window is not ready.");
	}

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

	public async Task<IReadOnlyList<RegistryNodeViewModel>> GetBreadcrumbChildrenAsync(int index, bool isRootItem)
	{
		RegistryNodeViewModel? node = isRootItem
			? GetComputerNode(SelectedNode)
			: index >= 0 && index < BreadcrumbItems.Count
				? BreadcrumbItems[index].Node
				: null;

		if (node is null)
			return [];

		await LoadChildrenAsync(node);
		return node.Children.ToArray();
	}

	public Task SelectNodeAsync(RegistryNodeViewModel? node)
	{
		return SelectNodeAsync(node, recordHistory: true);
	}

	private async Task EnsureAncestorsExpandedAsync(RegistryNodeViewModel? node)
	{
		List<RegistryNodeViewModel> ancestors = [];
		for (RegistryNodeViewModel? current = node?.Parent; current is not null; current = current.Parent)
			ancestors.Add(current);

		ancestors.Reverse();
		foreach (RegistryNodeViewModel ancestor in ancestors)
		{
			ancestor.IsExpanded = true;
			await LoadChildrenAsync(ancestor);
		}
	}

	private async Task SelectNodeAsync(RegistryNodeViewModel? node, bool recordHistory)
	{
		if (recordHistory)
			RecordNavigation(node);

		if (node is not null)
			await EnsureAncestorsExpandedAsync(node);

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
			bool isExpanded = target.IsExpanded;
			target.Children.Clear();
			target.AreChildrenLoaded = false;
			target.HasUnrealizedChildren = true;
			await LoadChildrenAsync(target);
			target.IsExpanded = isExpanded;
		}

		await SelectNodeAsync(target, recordHistory: false);
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

	public async Task DeleteValueAsync(RegistryValueViewModel? value)
	{
		if (value?.RawValue is null)
			throw new InvalidOperationException("The selected value cannot be deleted.");

		if (SelectedNode is not { Hive: not null } node)
			throw new InvalidOperationException("Select a registry key first.");

		using RegistryKey key = RegistryFileService.OpenKey(node, writable: true);
		key.DeleteValue(value.RegistryName, throwOnMissingValue: true);
		await RefreshAsync(node);
	}

	public async Task RenameValueAsync(RegistryValueViewModel? value, string newName)
	{
		if (value?.RawValue is null)
			throw new InvalidOperationException("The selected value cannot be renamed.");

		if (SelectedNode is not { Hive: not null } node)
			throw new InvalidOperationException("Select a registry key first.");

		if (string.Equals(value.RegistryName, newName, StringComparison.OrdinalIgnoreCase))
			return;

		using RegistryKey key = RegistryFileService.OpenKey(node, writable: true);
		if (key.GetValueNames().Any(existingName =>
			string.Equals(existingName, newName, StringComparison.OrdinalIgnoreCase)))
			throw new InvalidOperationException($"The value '{newName}' already exists.");

		key.SetValue(newName, value.RawValue, value.Kind);
		key.DeleteValue(value.RegistryName, throwOnMissingValue: true);
		await RefreshAsync(node);
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
		int status = RegistryNative.RegRenameKey(
			key.Handle.DangerousGetHandle(),
			node.Name,
			newName);
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

		List<RegistryNodeViewModel> pathNodes = [];
		for (RegistryNodeViewModel? current = node; current?.Hive is not null; current = current.Parent)
			pathNodes.Add(current);
		pathNodes.Reverse();

		if (pathNodes.Count == 0 || pathNodes[0].Hive is not RegistryHive rootHive)
			return;

		BreadcrumbItems.Add(new RegistryBreadcrumbItemViewModel(
			pathNodes[0],
			GetHiveName(rootHive),
			HasBreadcrumbChildren(pathNodes[0])));

		for (int index = 1; index < pathNodes.Count; index++)
		{
			BreadcrumbItems.Add(new RegistryBreadcrumbItemViewModel(
				pathNodes[index],
				pathNodes[index].Name,
				HasBreadcrumbChildren(pathNodes[index])));
		}

		BreadcrumbPathText = computerName + "\\" + string.Join("\\", BreadcrumbItems.Select(item => item.Text));
	}

	private static bool HasBreadcrumbChildren(RegistryNodeViewModel node)
		=> node.HasUnrealizedChildren || node.Children.Count > 0;

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

			string[] valueNames = key.GetValueNames();
			if (!valueNames.Any(string.IsNullOrEmpty))
				values.Add(CreateUnsetDefaultValue());

			foreach (string valueName in valueNames.OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
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

	private static RegistryValueData CreateUnsetDefaultValue()
		=> new(
			"(Default)",
			"REG_SZ",
			NoValueText,
			StringValueImageUri,
			string.Empty,
			RegistryValueKind.String,
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
			return NoValueText;

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

	public IReadOnlyList<RegistryPermissionRuleViewModel> GetPermissionRules(RegistryNodeViewModel node)
	{
		if (node.Hive is null)
			throw new InvalidOperationException("Select a registry key first.");

		using RegistryKey key = RegistryFileService.OpenKey(node, writable: true);
		RegistrySecurity security = key.GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner);
		return security
			.GetAccessRules(includeExplicit: true, includeInherited: true, typeof(NTAccount))
			.OfType<RegistryAccessRule>()
			.Select(rule => new RegistryPermissionRuleViewModel(
				$"{rule.IdentityReference} — {rule.AccessControlType} — {rule.RegistryRights}"))
			.ToArray();
	}

	public void AddPermission(
		RegistryNodeViewModel node,
		string account,
		RegistryPermissionChoice permission,
		AccessControlType accessType)
	{
		if (node.Hive is null)
			throw new InvalidOperationException("Select a registry key first.");
		if (string.IsNullOrWhiteSpace(account))
			throw new ArgumentException("Enter an account name.", nameof(account));

		using RegistryKey key = RegistryFileService.OpenKey(node, writable: true);
		RegistrySecurity security = key.GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner);
		security.AddAccessRule(new RegistryAccessRule(
			new NTAccount(account.Trim()),
			permission.Rights,
			InheritanceFlags.ContainerInherit,
			PropagationFlags.None,
			accessType));
		key.SetAccessControl(security);
	}

	private static void TryDeleteFile(string path)
	{
		try
		{
			if (File.Exists(path))
				File.Delete(path);
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
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
