// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using System.Globalization;
using System.Security;
using RegistryEditor.Services;
using RegistryEditor.Views;
using Windows.ApplicationModel.DataTransfer;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.System.Registry;
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
	private string _valueEditorData = string.Empty;
	private string _valueEditorError = string.Empty;
	private int _integerBaseIndex;
	private bool _hasAttemptedValueSave;
	private bool _isValueDirty;
	private int _keyOverviewRequestId;
	private string _keyOverviewSubKeyCount = "—";
	private string _keyOverviewValueCount = "—";
	private string _keyOverviewModified = "—";
	private string _keyOverviewOwner = "—";
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
	private string _valueDataFontFamilyName = "Cascadia Mono";
	private string _appFontFamilyName = "Segoe UI Variable";
	private FontFamily _valueDataFontFamily = null!;
	private static readonly ElementTheme[] ThemeValues =
	[
		ElementTheme.Default,
		ElementTheme.Light,
		ElementTheme.Dark,
	];

	public RootViewModel()
	{
		_valueDataFontFamily = (FontFamily)Application.Current.Resources["RegistryValueDataFontFamily"];

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

	public AsyncRelayCommand<object?> DeleteValueCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> RenameValueCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> FindCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> DeleteKeyCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> RenameKeyCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> PermissionsCommand { get; private set; } = null!;

	public RelayCommand<object?> CopyKeyCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> ToggleAddressBarCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> SelectThemeCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> DisplayBinaryDataCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> SaveValueChangesCommand { get; private set; } = null!;

	public RelayCommand<object?> DiscardValueChangesCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> RefreshCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> CustomizeFontCommand { get; private set; } = null!;

	public AsyncRelayCommand<object?> AboutCommand { get; private set; } = null!;

	public RegistryNodeViewModel? SelectedNode
	{
		get => _selectedNode;
		set
		{
			if (!SetProperty(ref _selectedNode, value))
			{
				BeginKeyOverviewLoad(value);
				return;
			}

			OnPropertyChanged(nameof(CanEditSelectedValue));
			OnPropertyChanged(nameof(CanSaveValueChanges));
			OnPropertyChanged(nameof(ValueEditorKeyPath));
			OnPropertyChanged(nameof(ValueEditorFieldsVisibility));
			OnPropertyChanged(nameof(ValueEditorUnsupportedVisibility));
			OnPropertyChanged(nameof(CanEditSelectedNode));
			OnPropertyChanged(nameof(CanDeleteSelectedNode));
			OnPropertyChanged(nameof(CanRenameSelectedNode));
			OnPropertyChanged(nameof(CanCopySelectedKey));
			OnPropertyChanged(nameof(CanExportSelectedNode));
			OnPropertyChanged(nameof(CanLoadHive));
			OnPropertyChanged(nameof(CanUnloadHive));
			OnPropertyChanged(nameof(CanDisconnectRemote));
			OnPropertyChanged(nameof(KeyOverviewName));
			OnPropertyChanged(nameof(KeyOverviewPath));
			OnPropertyChanged(nameof(KeyOverviewIcon));
			BeginKeyOverviewLoad(value);
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
			OnPropertyChanged(nameof(ValueEditorName));
			OnPropertyChanged(nameof(ValueEditorType));
			OnPropertyChanged(nameof(ValueEditorIcon));
			OnPropertyChanged(nameof(ValueEditorContentVisibility));
			OnPropertyChanged(nameof(KeyOverviewVisibility));
			OnPropertyChanged(nameof(KeyOverviewActionsVisibility));
			OnPropertyChanged(nameof(ValueActionsVisibility));
			OnPropertyChanged(nameof(ValueEditorFieldsVisibility));
			OnPropertyChanged(nameof(ValueEditorUnsupportedVisibility));
			OnPropertyChanged(nameof(ValueEditorAcceptsReturn));
			OnPropertyChanged(nameof(IsIntegerValue));
			OnPropertyChanged(nameof(IsBinaryIntegerFormat));
			OnPropertyChanged(nameof(IntegerBaseVisibility));
			OnPropertyChanged(nameof(ValueEditorDataTextBoxVisibility));
			OnPropertyChanged(nameof(IntegerBitEditorVisibility));
			OnPropertyChanged(nameof(IntegerBaseIndex));
			LoadValueEditorDraft(value);
			NotifyCommandStates();
		}
	}

	public bool CanEditSelectedNode => SelectedNode?.Hive is not null;

	public bool CanDeleteSelectedNode => SelectedNode is { Hive: not null, IsHiveRoot: false };

	public bool CanRenameSelectedNode => SelectedNode is { Hive: not null, IsHiveRoot: false };

	public bool CanCopySelectedKey => SelectedNode?.Hive is not null;

	public bool CanExportSelectedNode => SelectedNode?.Hive is not null;

	public bool CanEditSelectedValue => IsSupportedValue(SelectedValue) && SelectedNode?.Hive is not null;

	public bool IsValueDirty
	{
		get => _isValueDirty;
		private set
		{
			if (!SetProperty(ref _isValueDirty, value))
				return;

			OnPropertyChanged(nameof(CanSaveValueChanges));
			DiscardValueChangesCommand?.NotifyCanExecuteChanged();
			SaveValueChangesCommand?.NotifyCanExecuteChanged();
		}
	}

	public string ValueEditorData
	{
		get => _valueEditorData;
		set
		{
			if (!SetProperty(ref _valueEditorData, value))
				return;

			UpdateValueEditorState();
		}
	}

	public string ValueEditorError
	{
		get => _valueEditorError;
		private set
		{
			if (!SetProperty(ref _valueEditorError, value))
				return;

			OnPropertyChanged(nameof(ValueEditorErrorVisibility));
		}
	}

	public string ValueEditorName => SelectedValue?.Name ?? string.Empty;

	public string ValueEditorType => SelectedValue?.Kind switch
	{
		RegistryValueKind.String => "REG_SZ (String)",
		RegistryValueKind.ExpandString => "REG_EXPAND_SZ (Expandable String)",
		RegistryValueKind.Binary => "REG_BINARY (Binary)",
		RegistryValueKind.DWord => "REG_DWORD (32-bit integer)",
		RegistryValueKind.MultiString => "REG_MULTI_SZ (Multi-String)",
		RegistryValueKind.QWord => "REG_QWORD (64-bit integer)",
		RegistryValueKind.None => "REG_NONE (Binary data)",
		_ => SelectedValue?.Type ?? string.Empty,
	};

	public string ValueEditorKeyPath => SelectedNode?.Hive is not null ? GetRegistryPath(SelectedNode) : string.Empty;

	public BitmapImage? ValueEditorIcon => SelectedValue?.Icon;

	public string ValueEditorDataHeader => SelectedValue?.Kind switch
	{
		RegistryValueKind.Binary or RegistryValueKind.None => "Bytes:",
		RegistryValueKind.MultiString => "Strings:",
		_ => "Data:",
	};

	public string ValueEditorDataPlaceholder => SelectedValue?.Kind switch
	{
		RegistryValueKind.Binary or RegistryValueKind.None => "00 1A FF",
		RegistryValueKind.MultiString => "One string per line",
		RegistryValueKind.DWord => "0",
		RegistryValueKind.QWord => "0",
		_ => "Enter value data",
	};

	public bool ValueEditorAcceptsReturn => SelectedValue?.Kind is not (RegistryValueKind.DWord or RegistryValueKind.QWord);

	public ObservableCollection<RegistryIntegerByteViewModel> IntegerBitBytes { get; } = [];

	public int IntegerBaseIndex
	{
		get => _integerBaseIndex;
		set
		{
			if (value is < 0 or > 2 || value == _integerBaseIndex)
				return;

			RegistryValueViewModel? selectedValue = SelectedValue;
			ulong number = 0;
			if (selectedValue is { Kind: RegistryValueKind.DWord or RegistryValueKind.QWord }
				&& !TryParseIntegerValue(ValueEditorData, selectedValue.Kind, _integerBaseIndex, out number))
			{
				ValueEditorError = "Enter a valid number before changing the base.";
				OnPropertyChanged();
				return;
			}

			SetProperty(ref _integerBaseIndex, value);
			OnPropertyChanged(nameof(IsBinaryIntegerFormat));
			OnPropertyChanged(nameof(ValueEditorDataTextBoxVisibility));
			OnPropertyChanged(nameof(IntegerBitEditorVisibility));
			if (selectedValue is { Kind: RegistryValueKind.DWord or RegistryValueKind.QWord })
			{
				_valueEditorData = FormatIntegerValue(number, selectedValue.Kind, value);
				OnPropertyChanged(nameof(ValueEditorData));
				UpdateIntegerBitBytes(number, selectedValue.Kind);
			}

			UpdateValueEditorState();
		}
	}

	public bool IsIntegerValue => SelectedValue?.Kind is RegistryValueKind.DWord or RegistryValueKind.QWord;

	public bool IsBinaryIntegerFormat => IsIntegerValue && IntegerBaseIndex == 2;

	public Visibility IntegerBaseVisibility => IsIntegerValue ? Visibility.Visible : Visibility.Collapsed;

	public Visibility ValueEditorDataTextBoxVisibility => IsBinaryIntegerFormat ? Visibility.Collapsed : Visibility.Visible;

	public Visibility IntegerBitEditorVisibility => IsBinaryIntegerFormat ? Visibility.Visible : Visibility.Collapsed;

	public bool CanSaveValueChanges => CanEditSelectedValue
		&& (IsValueDirty || SelectedValue?.RawValue is null);

	public Visibility ValueEditorContentVisibility => SelectedValue is null ? Visibility.Collapsed : Visibility.Visible;

	public Visibility KeyOverviewVisibility => SelectedValue is null ? Visibility.Visible : Visibility.Collapsed;

	public Visibility KeyOverviewActionsVisibility => SelectedValue is null ? Visibility.Visible : Visibility.Collapsed;

	public Visibility ValueActionsVisibility => SelectedValue is null ? Visibility.Collapsed : Visibility.Visible;

	public string KeyOverviewName => SelectedNode?.Name ?? "No key selected";

	public string KeyOverviewPath => SelectedNode is { } node ? GetRegistryPath(node) : string.Empty;

	public BitmapImage? KeyOverviewIcon => SelectedNode?.Icon;

	public string KeyOverviewSubKeyCount => _keyOverviewSubKeyCount;

	public string KeyOverviewValueCount => _keyOverviewValueCount;

	public string KeyOverviewModified => _keyOverviewModified;

	public string KeyOverviewOwner => _keyOverviewOwner;

	private void BeginKeyOverviewLoad(RegistryNodeViewModel? node)
	{
		int requestId = ++_keyOverviewRequestId;
		if (node is null)
		{
			_keyOverviewSubKeyCount = "—";
			_keyOverviewValueCount = "—";
			_keyOverviewModified = "—";
			_keyOverviewOwner = "—";
			NotifyKeyOverview();
			return;
		}

		if (node.Hive is null)
		{
			_keyOverviewSubKeyCount = "—";
			_keyOverviewValueCount = "—";
			_keyOverviewModified = "—";
			_keyOverviewOwner = "—";
			NotifyKeyOverview();
			return;
		}

		_keyOverviewSubKeyCount = "Loading…";
		_keyOverviewValueCount = "Loading…";
		_keyOverviewModified = "Loading…";
		_keyOverviewOwner = "Loading…";
		NotifyKeyOverview();
		_ = LoadKeyOverviewAsync(node, requestId);
	}

	private async Task LoadKeyOverviewAsync(RegistryNodeViewModel node, int requestId)
	{
		try
		{
			RegistryKeySummary summary = await RunRegistryOperationAsync(() => RegistryFileService.ReadKeySummary(node));
			if (!IsCurrentKeyOverviewRequest(node, requestId))
				return;

			_keyOverviewSubKeyCount = summary.SubKeyCount.ToString("N0", CultureInfo.CurrentCulture);
			_keyOverviewValueCount = summary.ValueCount.ToString("N0", CultureInfo.CurrentCulture);
			_keyOverviewModified = summary.LastWriteTime.ToString("G", CultureInfo.CurrentCulture);
			_keyOverviewOwner = summary.Owner;
		}
		catch (Exception)
		{
			if (!IsCurrentKeyOverviewRequest(node, requestId))
				return;

			_keyOverviewSubKeyCount = "Unavailable";
			_keyOverviewValueCount = "Unavailable";
			_keyOverviewModified = "Unavailable";
			_keyOverviewOwner = "Unavailable";
		}

		NotifyKeyOverview();
	}

	private bool IsCurrentKeyOverviewRequest(RegistryNodeViewModel node, int requestId)
		=> requestId == _keyOverviewRequestId && ReferenceEquals(SelectedNode, node);

	private void NotifyKeyOverview()
	{
		OnPropertyChanged(nameof(KeyOverviewSubKeyCount));
		OnPropertyChanged(nameof(KeyOverviewValueCount));
		OnPropertyChanged(nameof(KeyOverviewModified));
		OnPropertyChanged(nameof(KeyOverviewOwner));
	}

	public Visibility ValueEditorFieldsVisibility => CanEditSelectedValue ? Visibility.Visible : Visibility.Collapsed;

	public Visibility ValueEditorUnsupportedVisibility => SelectedValue is not null && !CanEditSelectedValue
		? Visibility.Visible
		: Visibility.Collapsed;

	public Visibility ValueEditorErrorVisibility => string.IsNullOrEmpty(ValueEditorError) ? Visibility.Collapsed : Visibility.Visible;

	public async Task<bool> TrySelectValueAsync(RegistryValueViewModel? value)
	{
		if (ReferenceEquals(SelectedValue, value))
			return true;

		if (!await ConfirmDiscardValueChangesAsync())
			return false;

		SelectedValue = value;
		return true;
	}

	private void LoadValueEditorDraft(RegistryValueViewModel? value)
	{
		_hasAttemptedValueSave = false;
		ValueEditorError = string.Empty;
		_integerBaseIndex = 0;
		OnPropertyChanged(nameof(IntegerBaseIndex));
		OnPropertyChanged(nameof(IsBinaryIntegerFormat));
		OnPropertyChanged(nameof(ValueEditorDataTextBoxVisibility));
		OnPropertyChanged(nameof(IntegerBitEditorVisibility));

		_valueEditorData = value switch
		{
			{ Kind: RegistryValueKind.DWord or RegistryValueKind.QWord } integerValue
				=> GetIntegerValue(integerValue).ToString(
					integerValue.Kind == RegistryValueKind.DWord ? "x8" : "x16",
					CultureInfo.InvariantCulture),
			{ Kind: RegistryValueKind.Binary or RegistryValueKind.None, RawValue: byte[] bytes }
				=> FormatEditableBytes(bytes),
			{ Kind: RegistryValueKind.MultiString, RawValue: string[] strings }
				=> string.Join(Environment.NewLine, strings),
			{ Kind: RegistryValueKind.String or RegistryValueKind.ExpandString, RawValue: string text }
				=> text,
			_ => string.Empty,
		};

		if (value is { Kind: RegistryValueKind.DWord or RegistryValueKind.QWord })
			UpdateIntegerBitBytes(GetIntegerValue(value), value.Kind);
		else
			IntegerBitBytes.Clear();

		OnPropertyChanged(nameof(ValueEditorData));
		OnPropertyChanged(nameof(ValueEditorName));
		OnPropertyChanged(nameof(ValueEditorType));
		OnPropertyChanged(nameof(ValueEditorKeyPath));
		OnPropertyChanged(nameof(ValueEditorIcon));
		OnPropertyChanged(nameof(ValueEditorDataHeader));
		OnPropertyChanged(nameof(ValueEditorDataPlaceholder));
		OnPropertyChanged(nameof(ValueEditorAcceptsReturn));
		OnPropertyChanged(nameof(ValueEditorContentVisibility));
		OnPropertyChanged(nameof(KeyOverviewVisibility));
		OnPropertyChanged(nameof(KeyOverviewActionsVisibility));
		OnPropertyChanged(nameof(ValueActionsVisibility));
		OnPropertyChanged(nameof(ValueEditorFieldsVisibility));
		OnPropertyChanged(nameof(ValueEditorUnsupportedVisibility));
		OnPropertyChanged(nameof(IsIntegerValue));
		OnPropertyChanged(nameof(IntegerBaseVisibility));
		OnPropertyChanged(nameof(CanSaveValueChanges));
		UpdateValueEditorState();
	}

	private void UpdateValueEditorState()
	{
		ValueEditorError = _hasAttemptedValueSave
			&& SelectedValue is { } value
			&& !TryCreateEditedValue(value, ValueEditorData, IntegerBaseIndex, out _, out string error)
			? error
			: string.Empty;
		IsValueDirty = IsValueDraftChanged();
		OnPropertyChanged(nameof(CanSaveValueChanges));
		SaveValueChangesCommand?.NotifyCanExecuteChanged();
	}

	private bool IsValueDraftChanged()
	{
		if (SelectedValue is not { } value || !CanEditSelectedValue)
			return false;

		if (value.RawValue is null)
			return ValueEditorData.Length > 0;

		return !TryCreateEditedValue(value, ValueEditorData, IntegerBaseIndex, out object? editedValue, out _)
			|| !RegistryValuesEqual(value.RawValue, editedValue);
	}

	private void ToggleIntegerBit(int bitPosition)
	{
		if (SelectedValue is not { Kind: RegistryValueKind.DWord or RegistryValueKind.QWord } value
			|| IntegerBaseIndex != 2
			|| !TryParseIntegerValue(ValueEditorData, value.Kind, IntegerBaseIndex, out ulong number))
			return;

		number ^= 1UL << bitPosition;
		_valueEditorData = FormatIntegerValue(number, value.Kind, IntegerBaseIndex);
		OnPropertyChanged(nameof(ValueEditorData));
		UpdateIntegerBitBytes(number, value.Kind);
		UpdateValueEditorState();
	}

	private void UpdateIntegerBitBytes(ulong value, RegistryValueKind kind)
	{
		int bitWidth = GetIntegerBitWidth(kind);
		if (IntegerBitBytes.Count != bitWidth / 8)
		{
			IntegerBitBytes.Clear();
			for (int highBit = bitWidth - 1; highBit >= 0; highBit -= 8)
			{
				int lowBit = highBit - 7;
				ObservableCollection<RegistryIntegerBitViewModel> bits = [];
				for (int bitPosition = highBit; bitPosition >= lowBit; bitPosition--)
				{
					int capturedBitPosition = bitPosition;
					bits.Add(new RegistryIntegerBitViewModel(
						bitPosition,
						(value & (1UL << bitPosition)) != 0,
						() => ToggleIntegerBit(capturedBitPosition)));
				}

				IntegerBitBytes.Add(new RegistryIntegerByteViewModel(highBit, lowBit, bits));
			}
		}

		foreach (RegistryIntegerBitViewModel bit in IntegerBitBytes.SelectMany(byteGroup => byteGroup.Bits))
			bit.IsSet = (value & (1UL << bit.BitPosition)) != 0;
	}

	private void DiscardValueEditorChanges()
		=> LoadValueEditorDraft(SelectedValue);

	private async Task<bool> ConfirmDiscardValueChangesAsync()
	{
		if (!IsValueDirty)
			return true;

		string valueName = SelectedValue?.Name ?? "the selected value";
		if (!await ConfirmAsync("Discard unsaved changes?", $"Discard unsaved changes to '{valueName}'?"))
			return false;

		SelectedValue = null;
		return true;
	}

	private static bool IsSupportedValue(RegistryValueViewModel? value)
		=> value?.Kind switch
		{
			RegistryValueKind.String => value.RawValue is null or string,
			RegistryValueKind.ExpandString => value.RawValue is string,
			RegistryValueKind.MultiString => value.RawValue is string[],
			RegistryValueKind.Binary => value.RawValue is byte[],
			RegistryValueKind.DWord => value.RawValue is int or uint,
			RegistryValueKind.QWord => value.RawValue is long or ulong,
			RegistryValueKind.None => value.RawValue is byte[],
			_ => false,
		};

	private static string FormatEditableBytes(byte[] bytes)
		=> string.Join(" ", bytes.Select(value => value.ToString("x2", CultureInfo.InvariantCulture)));

	private static bool RegistryValuesEqual(object? first, object? second)
	{
		if (first is byte[] firstBytes && second is byte[] secondBytes)
			return firstBytes.AsSpan().SequenceEqual(secondBytes);

		if (first is string[] firstStrings && second is string[] secondStrings)
			return firstStrings.SequenceEqual(secondStrings, StringComparer.Ordinal);

		return Equals(first, second);
	}

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

	public bool IsSystemThemeSelected => _selectedThemeIndex == 0;

	public bool IsLightThemeSelected => _selectedThemeIndex == 1;

	public bool IsDarkThemeSelected => _selectedThemeIndex == 2;

	public FontFamily ValueDataFontFamily
	{
		get => _valueDataFontFamily;
		private set => SetProperty(ref _valueDataFontFamily, value);
	}

	public int SelectedThemeIndex
	{
		get => _selectedThemeIndex;
		set
		{
			if (value < 0
				|| value >= ThemeValues.Length
				|| !SetProperty(ref _selectedThemeIndex, value))
				return;

			OnPropertyChanged(nameof(IsSystemThemeSelected));
			OnPropertyChanged(nameof(IsLightThemeSelected));
			OnPropertyChanged(nameof(IsDarkThemeSelected));

			if (App.Window.Content is FrameworkElement root)
				root.RequestedTheme = ThemeValues[value];
		}
	}

	public async Task LoadSettingsAsync()
	{
		AppSettings settings = await AppSettingsStore.LoadAsync();
		IsAddressBarVisible = settings.ShowAddressBar;
		SelectedThemeIndex = settings.ThemeIndex is >= 0 and < 3 ? settings.ThemeIndex : 0;

		string fontFamilyName = string.IsNullOrWhiteSpace(settings.ValueDataFontFamily)
			? "Cascadia Mono"
			: settings.ValueDataFontFamily.Trim();
		string appFontFamilyName = string.IsNullOrWhiteSpace(settings.AppFontFamily)
			? "Segoe UI Variable"
			: settings.AppFontFamily.Trim();
		ApplyValueDataFont(fontFamilyName);
		ApplyAppFont(appFontFamilyName);
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
		DeleteValueCommand = new(ExecuteDeleteValueCommandAsync, CanDeleteValue);
		RenameValueCommand = new(ExecuteRenameValueCommandAsync, CanRenameValue);
		FindCommand = new(ExecuteFindCommandAsync);
		DeleteKeyCommand = new(ExecuteDeleteKeyCommandAsync, CanDeleteNode);
		RenameKeyCommand = new(ExecuteRenameKeyCommandAsync, CanRenameNode);
		PermissionsCommand = new(ExecutePermissionsCommandAsync, CanKeyNode);
		CopyKeyCommand = new(ExecuteCopyKeyCommand, CanKeyNode);

		ToggleAddressBarCommand = new(ExecuteToggleAddressBarCommandAsync);
		SelectThemeCommand = new(ExecuteSelectThemeCommandAsync);
		DisplayBinaryDataCommand = new(ExecuteDisplayBinaryDataCommandAsync, _ => CanEditSelectedValue);
		SaveValueChangesCommand = new(ExecuteSaveValueChangesCommandAsync, _ => CanSaveValueChanges);
		DiscardValueChangesCommand = new(_ => DiscardValueEditorChanges(), _ => IsValueDirty);
		RefreshCommand = new(ExecuteRefreshCommandAsync, _ => SelectedNode is not null);
		CustomizeFontCommand = new(ExecuteCustomizeFontCommandAsync);
		AboutCommand = new(ExecuteAboutCommandAsync);
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

	private async Task RunRegistryOperationAsync(Action operation, CancellationToken cancellationToken = default)
	{
		BeginLoad();
		try
		{
			await Task.Run(operation, cancellationToken);
		}
		finally
		{
			EndLoad();
		}
	}

	private async Task<TResult> RunRegistryOperationAsync<TResult>(
		Func<TResult> operation,
		CancellationToken cancellationToken = default)
	{
		BeginLoad();
		try
		{
			return await Task.Run(operation, cancellationToken);
		}
		finally
		{
			EndLoad();
		}
	}

	private Task ExecuteImportCommandAsync(object? parameter)
		=> RunCommandAsync("Import failed", async () =>
		{
			if (await PickImportFileAsync() is not { } filePath
				|| !await ConfirmDiscardValueChangesAsync())
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
				|| await RequestTextAsync("Load hive", "Key name", "LoadedHive") is not { } keyName
				|| !await ConfirmDiscardValueChangesAsync())
				return;

			await LoadHiveAsync(parent, filePath, keyName);
		});

	private Task ExecuteUnloadHiveCommandAsync(object? parameter)
		=> RunCommandAsync("Unload hive failed", async () =>
		{
			RegistryNodeViewModel? node = ResolveNode(parameter);
			if (node is null
				|| !await ConfirmDiscardValueChangesAsync()
				|| !await ConfirmAsync("Unload hive", $"Unload '{node.Name}'?"))
				return;

			await UnloadHiveAsync(node);
		});

	private Task ExecuteConnectRemoteCommandAsync(object? parameter)
		=> RunCommandAsync("Connection failed", async () =>
		{
			if (await RequestTextAsync("Connect to network registry", "Computer name", "") is { } computerName
				&& await ConfirmDiscardValueChangesAsync())
				await ConnectRemoteAsync(computerName);
		});

	private Task ExecuteDisconnectRemoteCommandAsync(object? parameter)
		=> RunCommandAsync("Disconnect failed", async () =>
		{
			if (!await ConfirmDiscardValueChangesAsync()
				|| !await ConfirmAsync("Disconnect", "Disconnect from the selected remote computer?"))
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
				&& await RequestTextAsync("New key", "Key name", "New Key") is { } keyName
				&& await ConfirmDiscardValueChangesAsync())
				await CreateKeyAsync(node, keyName);
		});

	private Task CreateValueFromCommandAsync(object? parameter, RegistryValueKind kind)
		=> RunCommandAsync("Registry operation failed", async () =>
		{
			RegistryNodeViewModel? node = ResolveNode(parameter);
			if (node is not null
				&& await RequestTextAsync("New value", "Value name", "New Value") is { } name
				&& await ConfirmDiscardValueChangesAsync())
				await CreateValueAsync(node, name, kind);
		});

	private async Task ExecuteDeleteValueCommandAsync(object? parameter)
	{
		if (ResolveValue(parameter) is not { } value || !CanDeleteValue(value))
			return;

		await RunCommandAsync("Registry operation failed", async () =>
		{
			if (!await ConfirmDiscardValueChangesAsync())
				return;

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
			if (!await ConfirmDiscardValueChangesAsync())
				return;

			if (await RequestTextAsync("Rename value", "New value name", value.RegistryName) is { } newName)
				await RenameValueAsync(value, newName);
		});
	}

	private async Task ExecuteSaveValueChangesCommandAsync(object? parameter)
	{
		if (SelectedValue is not { } value || !CanSaveValueChanges)
			return;

		_hasAttemptedValueSave = true;
		if (!TryCreateEditedValue(value, ValueEditorData, IntegerBaseIndex, out object? editedValue, out string error))
		{
			ValueEditorError = error;
			return;
		}

		await RunCommandAsync("Unable to save value", async () =>
		{
			if (SelectedNode is not { Hive: not null } node)
				throw new InvalidOperationException("Select a registry key first.");

			string registryName = value.RegistryName;
			RegistryValueKind kind = value.Kind;
			await RunRegistryOperationAsync(() =>
			{
				using RegistryKey key = RegistryFileService.OpenKey(node, writable: true);
				key.SetValue(registryName, editedValue!, kind);
			});
			if (ReferenceEquals(SelectedNode, node) && ReferenceEquals(SelectedValue, value))
			{
				_hasAttemptedValueSave = false;
				ValueEditorError = string.Empty;
				IsValueDirty = false;
				await RefreshAsync(node);
			}
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
				&& await ConfirmDiscardValueChangesAsync()
				&& await ConfirmAsync("Delete key", $"Delete '{node.Name}' and all of its subkeys?"))
				await DeleteKeyAsync(node);
		});

	private Task ExecuteRenameKeyCommandAsync(object? parameter)
		=> RunCommandAsync("Registry operation failed", async () =>
		{
			RegistryNodeViewModel? node = ResolveNode(parameter);
			if (node is null
				|| await RequestTextAsync("Rename key", "New key name", node.Name) is not { } newName
				|| !await ConfirmDiscardValueChangesAsync())
				return;

			if (await RenameKeyAsync(node, newName) is { } renamedNode)
				await SelectNodeAsync(renamedNode);
		});

	private Task ExecutePermissionsCommandAsync(object? parameter)
		=> RunCommandAsync("Registry operation failed", async () =>
		{
			if (ResolveNode(parameter) is { } node && await ConfirmDiscardValueChangesAsync())
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

	private Task ExecuteRefreshCommandAsync(object? parameter)
		=> RunCommandAsync("Refresh failed", () => RefreshAsync());

	private Task ExecuteToggleAddressBarCommandAsync(object? parameter)
		=> RunCommandAsync("Settings save failed", async () =>
		{
			SetAddressBarVisibility(!IsAddressBarVisible);
			await SaveSettingsAsync();
		});

	private Task ExecuteSelectThemeCommandAsync(object? parameter)
		=> RunCommandAsync("Settings save failed", async () =>
		{
			if (!int.TryParse(Convert.ToString(parameter, CultureInfo.InvariantCulture), NumberStyles.None, CultureInfo.InvariantCulture, out int index)
				|| index < 0
				|| index >= ThemeValues.Length)
				return;

			SelectedThemeIndex = index;
			await SaveSettingsAsync();
		});

	private Task SaveSettingsAsync()
		=> AppSettingsStore.SaveAsync(CreateSettingsSnapshot());

	private AppSettings CreateSettingsSnapshot(
		string? valueDataFontFamilyName = null,
		string? appFontFamilyName = null)
		=> new()
		{
			ValueDataFontFamily = valueDataFontFamilyName ?? _valueDataFontFamilyName,
			AppFontFamily = appFontFamilyName ?? _appFontFamilyName,
			ThemeIndex = SelectedThemeIndex,
			ShowAddressBar = IsAddressBarVisible,
		};

	private void ApplyValueDataFont(string fontFamilyName)
	{
		_valueDataFontFamilyName = fontFamilyName;
		ValueDataFontFamily = CreateValueDataFontFamily(fontFamilyName);
		Application.Current.Resources["RegistryValueDataFontFamily"] = ValueDataFontFamily;

		foreach (RegistryValueViewModel value in RegistryValues)
			value.SetDataFont(ValueDataFontFamily);
	}

	private void ApplyAppFont(string fontFamilyName)
	{
		_appFontFamilyName = fontFamilyName;
		FontFamily appFontFamily = CreateAppFontFamily(fontFamilyName);
		Application.Current.Resources["RegistryAppFontFamily"] = appFontFamily;
		Application.Current.Resources["ContentControlThemeFontFamily"] = appFontFamily;
	}

	private Task ExecuteCustomizeFontCommandAsync(object? parameter)
		=> RunCommandAsync("Font customization failed", async () =>
		{
			CustomizeFontContentDialog dialog = new()
			{
				XamlRoot = GetXamlRoot(),
			};
			await dialog.LoadAvailableFontFamiliesAsync();
			dialog.Configure(_valueDataFontFamilyName, _appFontFamilyName);

			if (await dialog.ShowAsync() != ContentDialogResult.Primary)
				return;

			string valueDataFontFamilyName = dialog.SelectedValueDataFontFamilyName;
			string appFontFamilyName = dialog.SelectedAppFontFamilyName;
			await AppSettingsStore.SaveAsync(CreateSettingsSnapshot(valueDataFontFamilyName, appFontFamilyName));
			ApplyValueDataFont(valueDataFontFamilyName);
			ApplyAppFont(appFontFamilyName);
		});

	private Task ExecuteAboutCommandAsync(object? parameter)
		=> RunCommandAsync("About failed", async () =>
		{
			AboutContentDialog dialog = new()
			{
				XamlRoot = GetXamlRoot(),
			};
			await dialog.ShowAsync();
		});

	private static FontFamily CreateValueDataFontFamily(string fontFamilyName)
		=> CreateFontFamily(fontFamilyName, "Consolas", "Courier New");

	private static FontFamily CreateAppFontFamily(string fontFamilyName)
		=> CreateFontFamily(fontFamilyName, "Segoe UI Variable", "Segoe UI", "Arial");

	private static FontFamily CreateFontFamily(string fontFamilyName, params string[] fallbackFontFamilyNames)
		=> new(string.Join(
			", ",
			new[] { fontFamilyName }
				.Concat(fallbackFontFamilyNames)
				.Distinct(StringComparer.OrdinalIgnoreCase)));

	private async Task ExecuteBackCommandAsync(object? parameter)
	{
		if (!CanGoBack || !await ConfirmDiscardValueChangesAsync())
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
		if (!CanGoForward || !await ConfirmDiscardValueChangesAsync())
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
		DeleteValueCommand.NotifyCanExecuteChanged();
		RenameValueCommand.NotifyCanExecuteChanged();
		FindCommand.NotifyCanExecuteChanged();
		DeleteKeyCommand.NotifyCanExecuteChanged();
		RenameKeyCommand.NotifyCanExecuteChanged();
		PermissionsCommand.NotifyCanExecuteChanged();
		CopyKeyCommand.NotifyCanExecuteChanged();
		DisplayBinaryDataCommand.NotifyCanExecuteChanged();
		SaveValueChangesCommand.NotifyCanExecuteChanged();
		DiscardValueChangesCommand.NotifyCanExecuteChanged();
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

	public async Task ShowMessageAsync(
		string title,
		string message,
		FontFamily? messageFontFamily = null)
	{
		MessageContentDialog dialog = new()
		{
			XamlRoot = GetXamlRoot(),
		};
		dialog.Configure(title, message, messageFontFamily);
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
			RegistrySecurityEditor.Show(WindowNative.GetWindowHandle(App.Window), node);
			await RefreshAsync(node);
		}
		catch (Exception exception)
		{
			await ShowMessageAsync("Unable to edit access permissions", exception.Message);
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
		await ShowMessageAsync(
			$"{value.Name} ({value.Type})",
			data.Length == 0 ? "(empty)" : data,
			ValueDataFontFamily);
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

	private static int GetIntegerBitWidth(RegistryValueKind kind)
		=> kind switch
		{
			RegistryValueKind.DWord => 32,
			RegistryValueKind.QWord => 64,
			_ => throw new InvalidOperationException("This registry value is not an integer value."),
		};

	private static string FormatIntegerValue(ulong value, RegistryValueKind kind, int numberBaseIndex)
	{
		if (numberBaseIndex == 0)
			return value.ToString(kind == RegistryValueKind.DWord ? "x8" : "x16", CultureInfo.InvariantCulture);

		if (numberBaseIndex == 1)
			return value.ToString(CultureInfo.InvariantCulture);

		int bitWidth = GetIntegerBitWidth(kind);
		char[] bits = new char[bitWidth + (bitWidth / 8) - 1];
		int outputIndex = 0;
		for (int bitPosition = bitWidth - 1; bitPosition >= 0; bitPosition--)
		{
			bits[outputIndex++] = (value & (1UL << bitPosition)) == 0 ? '0' : '1';
			if (bitPosition > 0 && bitPosition % 8 == 0)
				bits[outputIndex++] = ' ';
		}

		return new string(bits);
	}

	private static bool TryParseIntegerValue(string text, RegistryValueKind kind, int numberBaseIndex, out ulong value)
	{
		string input = text.Trim();
		bool parsed;
		switch (numberBaseIndex)
		{
			case 0:
				if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
					input = input[2..];
				parsed = ulong.TryParse(input, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value);
				break;
			case 1:
				parsed = ulong.TryParse(input, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
				break;
			case 2:
				parsed = TryParseBinaryInteger(input, kind, out value);
				break;
			default:
				value = 0;
				return false;
		}

		return parsed && (kind != RegistryValueKind.DWord || value <= uint.MaxValue);
	}

	private static bool TryParseBinaryInteger(string text, RegistryValueKind kind, out ulong value)
	{
		value = 0;
		string input = text.Replace(" ", string.Empty, StringComparison.Ordinal);
		int bitWidth = GetIntegerBitWidth(kind);
		if (input.Length is < 1 || input.Length > bitWidth)
			return false;

		foreach (char bit in input)
		{
			if (bit is not ('0' or '1'))
				return false;

			value = (value << 1) | (uint)(bit - '0');
		}

		return true;
	}

	private static bool TryCreateEditedValue(
		RegistryValueViewModel value,
		string text,
		int numberBaseIndex,
		out object? editedValue,
		out string error)
	{
		error = string.Empty;
		editedValue = value.Kind switch
		{
			RegistryValueKind.String or RegistryValueKind.ExpandString => text,
			RegistryValueKind.MultiString => text.Length == 0
				? Array.Empty<string>()
				: text.Split(["\r\n", "\n"], StringSplitOptions.None),
			RegistryValueKind.Binary or RegistryValueKind.None => ParseByteArray(text, out error),
			RegistryValueKind.DWord when TryParseIntegerValue(text, value.Kind, numberBaseIndex, out ulong dword)
				=> unchecked((int)dword),
			RegistryValueKind.QWord when TryParseIntegerValue(text, value.Kind, numberBaseIndex, out ulong qword)
				=> unchecked((long)qword),
			_ => null,
		};

		if (editedValue is not null || error.Length > 0)
			return error.Length == 0;

		if (value.Kind is RegistryValueKind.DWord or RegistryValueKind.QWord)
		{
			string valueType = value.Kind == RegistryValueKind.DWord ? "DWORD" : "QWORD";
			string valueBase = numberBaseIndex switch
			{
				0 => "hexadecimal",
				1 => "decimal",
				_ => "binary",
			};
			error = $"Enter a valid {valueType} value in {valueBase} format.";
		}
		else
		{
			error = "This registry value type cannot be edited.";
		}

		return false;
	}

	private static byte[]? ParseByteArray(string text, out string error)
	{
		error = string.Empty;
		string[] parts = text.Split([' ', '\t', '\r', '\n', ',', ';', '-'], StringSplitOptions.RemoveEmptyEntries);
		List<byte> bytes = new(parts.Length);
		foreach (string part in parts)
		{
			string token = part.StartsWith("0x", StringComparison.OrdinalIgnoreCase) ? part[2..] : part;
			if (token.Length is < 1 or > 2
				|| !byte.TryParse(token, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out byte value))
			{
				error = $"'{part}' is not a valid byte. Enter each byte as two hexadecimal digits.";
				return null;
			}

			bytes.Add(value);
		}

		return [.. bytes];
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

		try
		{
			IReadOnlyList<RegistrySubKeyData>? children = await RunRegistryOperationAsync(() => ReadSubkeys(node));
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

	private async Task SelectNodeAsync(RegistryNodeViewModel? node, bool recordHistory, bool preserveSelectedValue = false)
	{
		if (!await ConfirmDiscardValueChangesAsync())
			return;

		string? selectedValueName = preserveSelectedValue ? SelectedValue?.RegistryName : null;
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

		try
		{
			IReadOnlyList<RegistryValueData> values = await RunRegistryOperationAsync(
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
					value.RawValue,
					ValueDataFontFamily));

			if (selectedValueName is not null)
				SelectedValue = RegistryValues.FirstOrDefault(value =>
					string.Equals(value.RegistryName, selectedValueName, StringComparison.OrdinalIgnoreCase));

			SetEmptyValuesMessage(
				RegistryValues.Count == 0 ? "No values" : string.Empty,
				isEmpty: RegistryValues.Count == 0);
		}
		catch (OperationCanceledException)
		{
		}
		finally
		{
			ClearValueCancellation(cancellation);
		}
	}

	public async Task RefreshAsync(RegistryNodeViewModel? node = null)
	{
		RegistryNodeViewModel? target = node ?? SelectedNode;
		if (target is null)
			return;

		bool wasSelected = ReferenceEquals(SelectedNode, target);

		if (target.Hive is not null)
		{
			bool isExpanded = target.IsExpanded;
			target.Children.Clear();
			target.AreChildrenLoaded = false;
			target.HasUnrealizedChildren = true;
			await LoadChildrenAsync(target);
			target.IsExpanded = isExpanded;
		}

		if (wasSelected && ReferenceEquals(SelectedNode, target))
			await SelectNodeAsync(target, recordHistory: false, preserveSelectedValue: true);
	}

	private async Task RefreshNodeChildrenAsync(RegistryNodeViewModel node)
	{
		if (node.Hive is null)
			return;

		bool isExpanded = node.IsExpanded;
		node.Children.Clear();
		node.AreChildrenLoaded = false;
		node.HasUnrealizedChildren = true;
		if (isExpanded)
			await LoadChildrenAsync(node);
	}

	private static bool IsWithinSubtree(RegistryNodeViewModel? node, RegistryNodeViewModel root)
	{
		for (RegistryNodeViewModel? current = node; current is not null; current = current.Parent)
		{
			if (ReferenceEquals(current, root))
				return true;
		}

		return false;
	}

	public async Task<RegistryNodeViewModel?> CreateKeyAsync(RegistryNodeViewModel? parent, string name)
	{
		if (parent?.Hive is null)
			throw new InvalidOperationException("Select a registry key first.");

		ValidateKeyName(name);
		await RunRegistryOperationAsync(() =>
		{
			using RegistryKey key = RegistryFileService.OpenKey(parent, writable: true);
			using RegistryKey createdKey = key.CreateSubKey(name, writable: true)
				?? throw new UnauthorizedAccessException("The registry key could not be created.");
		});

		if (ReferenceEquals(SelectedNode, parent))
		{
			await RefreshAsync(parent);
			parent.IsExpanded = true;
			return parent.Children.FirstOrDefault(child => string.Equals(child.Name, name, StringComparison.OrdinalIgnoreCase));
		}

		await RefreshNodeChildrenAsync(parent);
		return null;
	}

	public async Task CreateValueAsync(RegistryNodeViewModel? node, string name, RegistryValueKind kind)
	{
		if (node?.Hive is null)
			throw new InvalidOperationException("Select a registry key first.");

		if (string.IsNullOrWhiteSpace(name))
			throw new ArgumentException("A value name must not be empty.", nameof(name));

		object value = kind switch
		{
			RegistryValueKind.Binary => Array.Empty<byte>(),
			RegistryValueKind.DWord => 0,
			RegistryValueKind.QWord => 0L,
			RegistryValueKind.MultiString => Array.Empty<string>(),
			_ => string.Empty,
		};
		await RunRegistryOperationAsync(() =>
		{
			using RegistryKey key = RegistryFileService.OpenKey(node, writable: true);
			if (key.GetValueNames().Any(valueName => string.Equals(valueName, name, StringComparison.OrdinalIgnoreCase)))
				throw new InvalidOperationException($"The value '{name}' already exists.");

			key.SetValue(name, value, kind);
		});
		if (ReferenceEquals(SelectedNode, node))
			await SelectNodeAsync(node);
	}

	public async Task DeleteValueAsync(RegistryValueViewModel? value)
	{
		if (value?.RawValue is null)
			throw new InvalidOperationException("The selected value cannot be deleted.");

		if (SelectedNode is not { Hive: not null } node)
			throw new InvalidOperationException("Select a registry key first.");

		string registryName = value.RegistryName;
		await RunRegistryOperationAsync(() =>
		{
			using RegistryKey key = RegistryFileService.OpenKey(node, writable: true);
			key.DeleteValue(registryName, throwOnMissingValue: true);
		});
		if (ReferenceEquals(SelectedNode, node))
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

		string oldName = value.RegistryName;
		RegistryValueKind kind = value.Kind;
		object rawValue = value.RawValue;
		await RunRegistryOperationAsync(() =>
		{
			using RegistryKey key = RegistryFileService.OpenKey(node, writable: true);
			if (key.GetValueNames().Any(existingName =>
				string.Equals(existingName, newName, StringComparison.OrdinalIgnoreCase)))
				throw new InvalidOperationException($"The value '{newName}' already exists.");

			key.SetValue(newName, rawValue, kind);
			key.DeleteValue(oldName, throwOnMissingValue: true);
		});
		if (ReferenceEquals(SelectedNode, node))
		{
			await RefreshAsync(node);
			SelectedValue = RegistryValues.FirstOrDefault(candidate =>
				string.Equals(candidate.RegistryName, newName, StringComparison.OrdinalIgnoreCase));
		}
	}

	public async Task DeleteKeyAsync(RegistryNodeViewModel? node)
	{
		if (node?.Hive is null || node.IsHiveRoot || node.Parent?.Hive is null)
			throw new InvalidOperationException("The selected item cannot be deleted.");

		RegistryNodeViewModel parent = node.Parent;
		string keyName = node.Name;
		await RunRegistryOperationAsync(() =>
		{
			using RegistryKey key = RegistryFileService.OpenKey(parent, writable: true);
			key.DeleteSubKeyTree(keyName, throwOnMissingSubKey: true);
		});

		if (IsWithinSubtree(SelectedNode, node))
		{
			await RefreshNodeChildrenAsync(parent);
			SelectedValue = null;
			await SelectNodeAsync(parent, recordHistory: false);
		}
		else if (ReferenceEquals(SelectedNode, parent))
		{
			await RefreshAsync(parent);
		}
		else
		{
			await RefreshNodeChildrenAsync(parent);
		}
	}

	public async Task<RegistryNodeViewModel?> RenameKeyAsync(RegistryNodeViewModel? node, string newName)
	{
		if (node?.Hive is null || node.IsHiveRoot || node.Parent?.Hive is null)
			throw new InvalidOperationException("The selected item cannot be renamed.");

		ValidateKeyName(newName);
		RegistryNodeViewModel parent = node.Parent;
		string oldName = node.Name;
		bool selectionWasAffected = ReferenceEquals(SelectedNode, parent) || IsWithinSubtree(SelectedNode, node);
		WIN32_ERROR status = await RunRegistryOperationAsync(() =>
		{
			using RegistryKey key = RegistryFileService.OpenKey(parent, writable: true);
			return PInvoke.RegRenameKey(key.Handle, oldName, newName);
		});
		if (status != WIN32_ERROR.NO_ERROR)
			throw new Win32Exception((int)status);

		bool selectionStillAffected = ReferenceEquals(SelectedNode, parent) || IsWithinSubtree(SelectedNode, node);
		await RefreshNodeChildrenAsync(parent);
		if (!selectionWasAffected || !selectionStillAffected)
			return null;

		parent.IsExpanded = true;
		return parent.Children.FirstOrDefault(child => string.Equals(child.Name, newName, StringComparison.OrdinalIgnoreCase));
	}

	public async Task<int> ImportAsync(string filePath)
	{
		int importedValueCount = await RunRegistryOperationAsync(() => RegistryFileService.Import(filePath));
		await RefreshAsync();
		return importedValueCount;
	}

	public Task ExportAsync(string filePath, RegistryNodeViewModel node)
		=> RunRegistryOperationAsync(() => RegistryFileService.Export(filePath, node));

	public async Task LoadHiveAsync(RegistryNodeViewModel? parent, string filePath, string keyName)
	{
		if (parent is not { IsHiveRoot: true, IsRemote: false, Hive: RegistryHive.Users or RegistryHive.LocalMachine })
			throw new InvalidOperationException("Select HKEY_USERS or HKEY_LOCAL_MACHINE first.");

		ValidateKeyName(keyName);
		await RunRegistryOperationAsync(() =>
		{
			RegistryFileService.EnablePrivilege("SeRestorePrivilege");
			RegistryFileService.EnablePrivilege("SeBackupPrivilege");
			using RegistryKey hiveKey = RegistryFileService.OpenBaseKey(parent);
			WIN32_ERROR status = PInvoke.RegLoadKey(hiveKey.Handle, keyName, filePath);
			if (status != WIN32_ERROR.NO_ERROR)
				throw new Win32Exception((int)status);
		});

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
		string keyName = node.Name;
		await RunRegistryOperationAsync(() =>
		{
			RegistryFileService.EnablePrivilege("SeRestorePrivilege");
			using RegistryKey hiveKey = RegistryFileService.OpenBaseKey(parent);
			WIN32_ERROR status = PInvoke.RegUnLoadKey(hiveKey.Handle, keyName);
			if (status != WIN32_ERROR.NO_ERROR)
				throw new Win32Exception((int)status);
		});

		_loadedHivePaths.Remove(GetRegistryPath(node));
		if (IsWithinSubtree(SelectedNode, node))
		{
			await RefreshNodeChildrenAsync(parent);
			SelectedValue = null;
			await SelectNodeAsync(parent, recordHistory: false);
		}
		else if (ReferenceEquals(SelectedNode, parent))
		{
			await RefreshAsync(parent);
		}
		else
		{
			await RefreshNodeChildrenAsync(parent);
		}
	}

	public async Task<RegistryNodeViewModel> ConnectRemoteAsync(string computerName)
	{
		string normalizedName = NormalizeComputerName(computerName);
		if (string.Equals(normalizedName, Environment.MachineName, StringComparison.OrdinalIgnoreCase))
			throw new InvalidOperationException("The local computer is already connected.");

		if (RootNodes.Any(node => string.Equals(node.ComputerName, normalizedName, StringComparison.OrdinalIgnoreCase)))
			throw new InvalidOperationException("That computer is already connected.");

		await RunRegistryOperationAsync(() =>
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
		RegistryNodeViewModel? replacement = RootNodes.FirstOrDefault(candidate => !candidate.IsRemote);
		await SelectNodeAsync(replacement);
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

		IReadOnlyList<RegistryValueData> values = await RunRegistryOperationAsync(
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
