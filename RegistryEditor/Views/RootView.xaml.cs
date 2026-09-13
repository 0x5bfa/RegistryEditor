// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using RegistryEditor.ViewModels;
using RegistryEditor.Controls;
using RegistryBreadcrumbBar = RegistryEditor.Controls.BreadcrumbBar;
using RegistryBreadcrumbBarItemClickedEventArgs = RegistryEditor.Controls.BreadcrumbBarItemClickedEventArgs;
using Microsoft.Win32;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Security.AccessControl;
using System.Security;
using System.Security.Principal;
using System.Text;
using RegistryEditor.Services;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace RegistryEditor.Views;

public sealed partial class RootView : UserControl
{
	private const string FavoritesRegistryPath = @"Software\Microsoft\Windows\CurrentVersion\Applets\Regedit\Favorites";
	private readonly RootViewModel _viewModel;
	private readonly HashSet<string> _favorites = new(StringComparer.OrdinalIgnoreCase);
	private RegistryNodeViewModel? _contextNode;
	private bool _treePaneIsWide;

	public RootViewModel ViewModel => _viewModel;

	public RootView()
		: this(new RootViewModel())
	{
	}

	public RootView(RootViewModel viewModel)
	{
		ArgumentNullException.ThrowIfNull(viewModel);

		InitializeComponent();
		_viewModel = viewModel;
		LoadFavorites();
		UpdateMenuState();
	}

	private void RootView_Loaded(object sender, RoutedEventArgs args)
		=> UpdateMenuState();

	private async void RegistryTreeView_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
	{
		if (sender.ContainerFromNode(args.Node) is { } container
			&& sender.ItemFromContainer(container) is RegistryNodeViewModel node)
			await ViewModel.LoadChildrenAsync(node);
	}

	private async void RegistryTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
	{
		RegistryNodeViewModel? selectedNode = args.AddedItems
			.OfType<RegistryNodeViewModel>()
			.FirstOrDefault();

		await ViewModel.SelectNodeAsync(selectedNode);
		UpdateMenuState();
	}

	private async void NavigationOmnibar_QuerySubmitted(Omnibar sender, OmnibarQuerySubmittedEventArgs args)
	{
		if (ReferenceEquals(args.Mode, PathOmnibarMode))
			await ViewModel.NavigateToPathAsync(args.Text);
	}

	private async void PathBreadcrumbBar_ItemClicked(RegistryBreadcrumbBar sender, RegistryBreadcrumbBarItemClickedEventArgs args)
		=> await ViewModel.NavigateToBreadcrumbAsync(args.Index, args.IsRootItem);

	private void RegistryValueListView_SelectionChanged(object sender, SelectionChangedEventArgs args)
	{
		ViewModel.SelectedValue = args.AddedItems.OfType<RegistryValueViewModel>().FirstOrDefault();
		UpdateMenuState();
	}

	private async void RegistryValueListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs args)
		=> await ShowSelectedValueAsync();

	private void RegistryTreeItem_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		if (sender is not TreeViewItem { DataContext: RegistryNodeViewModel node })
			return;

		_contextNode = node;
		ViewModel.SelectedNode = node;
		UpdateMenuState();
	}

	private void RegistryNodeContextFlyout_Opening(object sender, object args)
	{
		if (sender is not MenuFlyout flyout)
			return;

		RegistryNodeViewModel? node = _contextNode ?? ViewModel.SelectedNode;
		bool hasKey = node?.Hive is not null;
		bool canEdit = node?.Hive is not null;
		bool canDeleteOrRename = node is { Hive: not null, IsHiveRoot: false };

		foreach (MenuFlyoutItemBase item in flyout.Items)
		{
			if (item is MenuFlyoutSubItem subItem && string.Equals(subItem.Tag as string, "new", StringComparison.Ordinal))
			{
				subItem.IsEnabled = canEdit;
				continue;
			}

			if (item is not MenuFlyoutItem menuItem || menuItem.Tag is not string tag)
				continue;

			menuItem.IsEnabled = tag switch
			{
				"expand" => hasKey,
				"find" => true,
				"delete" or "rename" => canDeleteOrRename,
				"export" or "permissions" or "copy" => hasKey,
				_ => true,
			};
			if (tag == "expand" && node is not null)
				menuItem.Text = node.IsExpanded ? "Collapse" : "Expand";
		}
	}

	private async void RegistryNodeMenuItem_Click(object sender, RoutedEventArgs args)
	{
		if (sender is not MenuFlyoutItem { Tag: string action })
			return;

		await ExecuteNodeActionAsync(action, ViewModel.SelectedNode);
	}

	private async void ImportMenuItem_Click(object sender, RoutedEventArgs args)
	{
		if (await PickImportFileAsync() is not { } file)
			return;

		try
		{
			int count = await ViewModel.ImportAsync(file.Path);
			await ShowMessageAsync("Import", $"Imported {count} value(s) from '{file.Name}'.");
		}
		catch (Exception exception)
		{
			await ShowErrorAsync("Import failed", exception);
		}
	}

	private async void ExportMenuItem_Click(object sender, RoutedEventArgs args)
	{
		try
		{
			await ExportNodeAsync(ViewModel.SelectedNode);
		}
		catch (Exception exception)
		{
			await ShowErrorAsync("Export failed", exception);
		}
	}

	private async Task ExportNodeAsync(RegistryNodeViewModel? node)
	{
		if (node?.Hive is null)
			return;

		if (await PickExportFileAsync() is not { } file)
			return;

		await ViewModel.ExportAsync(file.Path, node);
		await ShowMessageAsync("Export", $"Exported '{ViewModel.GetRegistryPath(node)}' to '{file.Name}'.");
	}

	private async void LoadHiveMenuItem_Click(object sender, RoutedEventArgs args)
	{
		if (ViewModel.SelectedNode is not { } node || await PickHiveFileAsync() is not { } file)
			return;

		string? keyName = await ShowInputDialogAsync("Load hive", "Key name", "LoadedHive");
		if (keyName is null)
			return;

		try
		{
			await ViewModel.LoadHiveAsync(node, file.Path, keyName);
		}
		catch (Exception exception)
		{
			await ShowErrorAsync("Load hive failed", exception);
		}
	}

	private async void UnloadHiveMenuItem_Click(object sender, RoutedEventArgs args)
	{
		if (ViewModel.SelectedNode is not { } node || !await ConfirmAsync("Unload hive", $"Unload '{node.Name}'?"))
			return;

		try
		{
			await ViewModel.UnloadHiveAsync(node);
		}
		catch (Exception exception)
		{
			await ShowErrorAsync("Unload hive failed", exception);
		}
	}

	private async void ConnectMenuItem_Click(object sender, RoutedEventArgs args)
	{
		string? computerName = await ShowInputDialogAsync("Connect to network registry", "Computer name", "");
		if (computerName is null)
			return;

		try
		{
			await ViewModel.ConnectRemoteAsync(computerName);
			UpdateMenuState();
		}
		catch (Exception exception)
		{
			await ShowErrorAsync("Connection failed", exception);
		}
	}

	private async void DisconnectMenuItem_Click(object sender, RoutedEventArgs args)
	{
		if (!ViewModel.CanDisconnectRemote || !await ConfirmAsync("Disconnect", "Disconnect from the selected remote computer?"))
			return;

		try
		{
			await ViewModel.DisconnectRemoteAsync(ViewModel.SelectedNode);
			UpdateMenuState();
		}
		catch (Exception exception)
		{
			await ShowErrorAsync("Disconnect failed", exception);
		}
	}

	private async void PrintMenuItem_Click(object sender, RoutedEventArgs args)
	{
		if (ViewModel.SelectedNode is not { Hive: not null } node)
			return;

		string filePath = Path.Combine(Path.GetTempPath(), $"RegistryEditor-{Guid.NewGuid():N}.reg");
		try
		{
			await ViewModel.ExportAsync(filePath, node);
			Process? process = Process.Start(new ProcessStartInfo
			{
				FileName = "notepad.exe",
				UseShellExecute = true,
				ArgumentList = { "/p", filePath },
			});

			if (process is null)
				throw new InvalidOperationException("The system print helper could not be started.");

			process.EnableRaisingEvents = true;
			process.Exited += (_, _) => TryDeleteFile(filePath);
		}
		catch (Exception exception)
		{
			TryDeleteFile(filePath);
			await ShowErrorAsync("Print failed", exception);
		}
	}

	private void ExitMenuItem_Click(object sender, RoutedEventArgs args)
		=> App.Window.Close();

	private async void PermissionsMenuItem_Click(object sender, RoutedEventArgs args)
		=> await ShowPermissionsAsync(ViewModel.SelectedNode);

	private async void DeleteMenuItem_Click(object sender, RoutedEventArgs args)
		=> await ExecuteNodeActionAsync("delete", ViewModel.SelectedNode);

	private async void RenameMenuItem_Click(object sender, RoutedEventArgs args)
		=> await ExecuteNodeActionAsync("rename", ViewModel.SelectedNode);

	private void CopyKeyMenuItem_Click(object sender, RoutedEventArgs args)
		=> CopyKeyName(ViewModel.SelectedNode);

	private async void FindMenuItem_Click(object sender, RoutedEventArgs args)
	{
		try
		{
			await FindMenuItemAsync();
		}
		catch (Exception exception)
		{
			await ShowErrorAsync("Find failed", exception);
		}
	}

	private void AddressBarMenuItem_Click(object sender, RoutedEventArgs args)
	{
		NavigationBarHost.Visibility = AddressBarMenuItem.IsChecked ? Visibility.Visible : Visibility.Collapsed;
	}

	private void SplitMenuItem_Click(object sender, RoutedEventArgs args)
	{
		_treePaneIsWide = !_treePaneIsWide;
		RegistryTreeColumn.Width = _treePaneIsWide
			? new GridLength(1, GridUnitType.Star)
			: new GridLength(320);
	}

	private async void DisplayBinaryDataMenuItem_Click(object sender, RoutedEventArgs args)
	{
		await ShowSelectedValueAsync();
	}

	private async Task ShowSelectedValueAsync()
	{
		if (ViewModel.SelectedValue is not { } value)
			return;

		string data = value.RawValue switch
		{
			byte[] bytes => string.Join(" ", bytes.Select(item => item.ToString("x2"))),
			_ => value.Data,
		};
		await ShowMessageAsync($"{value.Name} ({value.Type})", data.Length == 0 ? "(empty)" : data);
	}

	private async void RefreshMenuItem_Click(object sender, RoutedEventArgs args)
	{
		try
		{
			await ViewModel.RefreshAsync();
		}
		catch (Exception exception)
		{
			await ShowErrorAsync("Refresh failed", exception);
		}
	}

	private async void FontMenuItem_Click(object sender, RoutedEventArgs args)
	{
		string? value = await ShowInputDialogAsync("Font size", "Size in pixels", FontSize.ToString());
		if (value is not null && double.TryParse(value, out double fontSize) && fontSize is >= 8 and <= 48)
			FontSize = fontSize;
	}

	private async void AddFavoriteMenuItem_Click(object sender, RoutedEventArgs args)
	{
		if (ViewModel.SelectedNode is not { Hive: not null } node)
			return;

		string path = ViewModel.GetRegistryPath(node);
		if (SaveFavorite(path))
			await ShowMessageAsync("Favorites", $"Added '{path}' to Favorites.");
		else
			await ShowMessageAsync("Favorites", "The favorite could not be saved.");
	}

	private async void RemoveFavoriteMenuItem_Click(object sender, RoutedEventArgs args)
	{
		if (ViewModel.SelectedNode is not { Hive: not null } node)
			return;

		string path = ViewModel.GetRegistryPath(node);
		if (_favorites.Remove(path))
		{
			DeleteFavorite(path);
			await ShowMessageAsync("Favorites", $"Removed '{path}' from Favorites.");
		}
	}

	private async void AboutMenuItem_Click(object sender, RoutedEventArgs args)
		=> await ShowMessageAsync("About Registry Editor", "Registry Editor\nWinUI 3 registry browser");

	private async Task ExecuteNodeActionAsync(string action, RegistryNodeViewModel? node)
	{
		try
		{
			switch (action)
			{
				case "expand" when node is not null:
					node.IsExpanded = !node.IsExpanded;
					if (node.IsExpanded)
						await ViewModel.LoadChildrenAsync(node);
					break;

				case "new-key":
					if (await ShowInputDialogAsync("New key", "Key name", "New Key") is { } keyName)
						await ViewModel.CreateKeyAsync(node, keyName);
					break;

				case "new-string":
					await CreateValueAsync(node, RegistryValueKind.String);
					break;

				case "new-expandable-string":
					await CreateValueAsync(node, RegistryValueKind.ExpandString);
					break;

				case "new-binary":
					await CreateValueAsync(node, RegistryValueKind.Binary);
					break;

				case "new-dword":
					await CreateValueAsync(node, RegistryValueKind.DWord);
					break;

				case "new-qword":
					await CreateValueAsync(node, RegistryValueKind.QWord);
					break;

				case "new-multi-string":
					await CreateValueAsync(node, RegistryValueKind.MultiString);
					break;

				case "find":
					await FindMenuItemAsync(node);
					break;

				case "delete" when node is not null && await ConfirmAsync("Delete key", $"Delete '{node.Name}' and all of its subkeys?"):
					await ViewModel.DeleteKeyAsync(node);
					break;

				case "rename" when node is not null:
					if (await ShowInputDialogAsync("Rename key", "New key name", node.Name) is { } newName)
					{
						RegistryNodeViewModel? renamedNode = await ViewModel.RenameKeyAsync(node, newName);
						if (renamedNode is not null)
							await ViewModel.SelectNodeAsync(renamedNode);
					}
					break;

				case "export":
					await ExportNodeAsync(node);
					break;

				case "permissions":
					await ShowPermissionsAsync(node);
					break;

				case "copy":
					CopyKeyName(node);
					break;
			}

			UpdateMenuState();
		}
		catch (Exception exception)
		{
			await ShowErrorAsync("Registry operation failed", exception);
		}
	}

	private async Task CreateValueAsync(RegistryNodeViewModel? node, RegistryValueKind kind)
	{
		string? name = await ShowInputDialogAsync("New value", "Value name", "New Value");
		if (name is not null)
			await ViewModel.CreateValueAsync(node, name, kind);
	}

	private async Task FindMenuItemAsync(RegistryNodeViewModel? startNode = null)
	{
		string? query = await ShowInputDialogAsync("Find", "Search for a key, value, or data", "");
		if (query is null)
			return;

		RegistryNodeViewModel? result = await ViewModel.FindAsync(query, startNode);
		if (result is null)
		{
			await ShowMessageAsync("Find", $"No match was found for '{query}'.");
			return;
		}

		result.IsExpanded = true;
		await ViewModel.SelectNodeAsync(result);
	}

	private void CopyKeyName(RegistryNodeViewModel? node)
	{
		if (node?.Hive is null)
			return;

		DataPackage package = new();
		package.SetText(ViewModel.GetRegistryPath(node));
		Clipboard.SetContent(package);
	}

	private async Task ShowPermissionsAsync(RegistryNodeViewModel? node)
	{
		if (node?.Hive is null)
			return;

		try
		{
			using RegistryKey key = RegistryFileService.OpenKey(node, writable: true);
			RegistrySecurity security = key.GetAccessControl(AccessControlSections.Access | AccessControlSections.Owner);
			List<RegistryPermissionRule> rules = security
				.GetAccessRules(includeExplicit: true, includeInherited: true, typeof(NTAccount))
				.OfType<RegistryAccessRule>()
				.Select(rule => new RegistryPermissionRule(
					$"{rule.IdentityReference} — {rule.AccessControlType} — {rule.RegistryRights}"))
				.ToList();

			StackPanel content = new() { Spacing = 8 };
			content.Children.Add(new TextBlock
			{
				Text = ViewModel.GetRegistryPath(node),
				TextWrapping = TextWrapping.Wrap,
			});
			content.Children.Add(new TextBlock { Text = "Current rules" });
			content.Children.Add(new ListView
			{
				ItemsSource = rules,
				MaxHeight = 220,
				SelectionMode = ListViewSelectionMode.None,
				DisplayMemberPath = nameof(RegistryPermissionRule.Text),
			});

			TextBox accountBox = new() { PlaceholderText = "Account (for example, DOMAIN\\User)" };
			ComboBox rightsBox = new()
			{
				ItemsSource = new[]
				{
					new RegistryPermissionChoice("Read", RegistryRights.ReadKey),
					new RegistryPermissionChoice("Read and write", RegistryRights.ReadKey | RegistryRights.SetValue | RegistryRights.CreateSubKey),
					new RegistryPermissionChoice("Full control", RegistryRights.FullControl),
				},
				DisplayMemberPath = nameof(RegistryPermissionChoice.Name),
				SelectedIndex = 0,
			};
			ComboBox accessTypeBox = new()
			{
				ItemsSource = new[] { AccessControlType.Allow, AccessControlType.Deny },
				SelectedIndex = 0,
			};
			TextBlock errorText = new() { Foreground = new SolidColorBrush(global::Microsoft.UI.Colors.Red), TextWrapping = TextWrapping.Wrap };
			content.Children.Add(new TextBlock { Text = "Add a rule" });
			content.Children.Add(accountBox);
			content.Children.Add(rightsBox);
			content.Children.Add(accessTypeBox);
			content.Children.Add(errorText);

			ContentDialog dialog = new()
			{
				Title = "Access permissions",
				Content = new ScrollViewer { Content = content, MaxHeight = 520 },
				PrimaryButtonText = "Save",
				CloseButtonText = "Cancel",
				XamlRoot = XamlRoot,
			};
			dialog.PrimaryButtonClick += (_, eventArgs) =>
			{
				string account = accountBox.Text.Trim();
				if (account.Length == 0)
				{
					errorText.Text = "Enter an account name.";
					eventArgs.Cancel = true;
					return;
				}

				try
				{
					RegistryPermissionChoice choice = rightsBox.SelectedItem as RegistryPermissionChoice
						?? throw new InvalidOperationException("Select a permission level.");
					AccessControlType accessType = accessTypeBox.SelectedItem is AccessControlType selectedType
						? selectedType
						: AccessControlType.Allow;
					security.AddAccessRule(new RegistryAccessRule(
						new NTAccount(account),
						choice.Rights,
						InheritanceFlags.ContainerInherit,
						PropagationFlags.None,
						accessType));
					key.SetAccessControl(security);
				}
				catch (Exception exception)
				{
					errorText.Text = exception.Message;
					eventArgs.Cancel = true;
				}
			};

			await dialog.ShowAsync();
		}
		catch (Exception exception)
		{
			await ShowErrorAsync("Unable to read access permissions", exception);
		}
	}

	private async Task<StorageFile?> PickImportFileAsync()
	{
		FileOpenPicker picker = new();
		picker.FileTypeFilter.Add(".reg");
		InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.Window));
		return await picker.PickSingleFileAsync();
	}

	private async Task<StorageFile?> PickHiveFileAsync()
	{
		FileOpenPicker picker = new();
		picker.FileTypeFilter.Add(".hiv");
		picker.FileTypeFilter.Add(".dat");
		picker.FileTypeFilter.Add(".*");
		InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.Window));
		return await picker.PickSingleFileAsync();
	}

	private async Task<StorageFile?> PickExportFileAsync()
	{
		FileSavePicker picker = new()
		{
			SuggestedFileName = "registry.reg",
		};
		picker.FileTypeChoices.Add("Registry file", [".reg"]);
		InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(App.Window));
		return await picker.PickSaveFileAsync();
	}

	private async Task<string?> ShowInputDialogAsync(string title, string placeholder, string initialText)
	{
		TextBox textBox = new()
		{
			PlaceholderText = placeholder,
			Text = initialText,
			MinWidth = 360,
		};
		ContentDialog dialog = new()
		{
			Title = title,
			Content = textBox,
			PrimaryButtonText = "OK",
			CloseButtonText = "Cancel",
			XamlRoot = XamlRoot,
		};
		ContentDialogResult result = await dialog.ShowAsync();
		return result == ContentDialogResult.Primary ? textBox.Text.Trim() : null;
	}

	private async Task<bool> ConfirmAsync(string title, string message)
	{
		ContentDialog dialog = new()
		{
			Title = title,
			Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
			PrimaryButtonText = "Yes",
			CloseButtonText = "No",
			XamlRoot = XamlRoot,
		};
		return await dialog.ShowAsync() == ContentDialogResult.Primary;
	}

	private async Task ShowMessageAsync(string title, string message)
	{
		ContentDialog dialog = new()
		{
			Title = title,
			Content = new ScrollViewer
			{
				Content = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap },
				MaxHeight = 420,
			},
			CloseButtonText = "OK",
			XamlRoot = XamlRoot,
		};
		await dialog.ShowAsync();
	}

	private Task ShowErrorAsync(string title, Exception exception)
		=> ShowMessageAsync(title, exception.Message);

	private void UpdateMenuState()
	{
		if (!IsLoaded)
			return;

		ImportMenuItem.IsEnabled = true;
		ExportMenuItem.IsEnabled = ViewModel.CanExportSelectedNode;
		LoadHiveMenuItem.IsEnabled = ViewModel.CanLoadHive;
		UnloadHiveMenuItem.IsEnabled = ViewModel.CanUnloadHive;
		DisconnectMenuItem.IsEnabled = ViewModel.CanDisconnectRemote;
		PermissionsMenuItem.IsEnabled = ViewModel.CanEditSelectedNode;
		DeleteMenuItem.IsEnabled = ViewModel.CanDeleteSelectedNode;
		RenameMenuItem.IsEnabled = ViewModel.CanRenameSelectedNode;
		CopyKeyMenuItem.IsEnabled = ViewModel.CanCopySelectedKey;
		NewMenuItem.IsEnabled = ViewModel.CanEditSelectedNode;
		AddressBarMenuItem.IsChecked = NavigationBarHost.Visibility == Visibility.Visible;
	}

	private void LoadFavorites()
	{
		try
		{
			using RegistryKey? key = Registry.CurrentUser.OpenSubKey(FavoritesRegistryPath, writable: false);
			if (key is null)
				return;

			foreach (string valueName in key.GetValueNames())
			{
				if (key.GetValue(valueName, defaultValue: null, RegistryValueOptions.DoNotExpandEnvironmentNames) is string path
					&& !string.IsNullOrWhiteSpace(path))
					_favorites.Add(path);
			}
		}
		catch (SecurityException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
		catch (IOException)
		{
		}
	}

	private bool SaveFavorite(string path)
	{
		try
		{
			using RegistryKey key = Registry.CurrentUser.CreateSubKey(FavoritesRegistryPath, writable: true)
				?? throw new UnauthorizedAccessException("The Favorites registry key could not be opened.");
			key.SetValue(path, path, RegistryValueKind.String);
			_favorites.Add(path);
			return true;
		}
		catch (SecurityException)
		{
			return false;
		}
		catch (UnauthorizedAccessException)
		{
			return false;
		}
		catch (ArgumentException)
		{
			return false;
		}
		catch (IOException)
		{
			return false;
		}
	}

	private static void DeleteFavorite(string path)
	{
		try
		{
			using RegistryKey? key = Registry.CurrentUser.OpenSubKey(FavoritesRegistryPath, writable: true);
			key?.DeleteValue(path, throwOnMissingValue: false);
		}
		catch (SecurityException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
		catch (IOException)
		{
		}
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

	private sealed record RegistryPermissionRule(string Text);

	private sealed record RegistryPermissionChoice(string Name, RegistryRights Rights);

}
