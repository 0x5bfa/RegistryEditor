// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using RegistryEditor.Controls;
using RegistryEditor.Services;
using RegistryEditor.ViewModels;
using RegistryBreadcrumbBar = RegistryEditor.Controls.BreadcrumbBar;
using RegistryBreadcrumbBarItemClickedEventArgs = RegistryEditor.Controls.BreadcrumbBarItemClickedEventArgs;
using Microsoft.UI.Xaml.Input;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace RegistryEditor.Views;

public sealed partial class RootView : UserControl, IRegistryEditorInteraction
{
	public RootViewModel ViewModel { get; }

	public RootView()
	{
		ViewModel = new RootViewModel();
		InitializeComponent();
		ViewModel.Interaction = this;
	}

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
	}

	private async void NavigationOmnibar_QuerySubmitted(Omnibar sender, OmnibarQuerySubmittedEventArgs args)
	{
		if (ReferenceEquals(args.Mode, PathOmnibarMode))
			await ViewModel.NavigateToPathAsync(args.Text);
	}

	private async void PathBreadcrumbBar_ItemClicked(RegistryBreadcrumbBar sender, RegistryBreadcrumbBarItemClickedEventArgs args)
		=> await ViewModel.NavigateToBreadcrumbAsync(args.Index, args.IsRootItem);

	private async void PathBreadcrumbBar_ItemDropDownFlyoutOpening(object? sender, BreadcrumbBarItemDropDownFlyoutEventArgs args)
	{
		args.Flyout.Items.Clear();
		args.Flyout.Items.Add(new MenuFlyoutItem
		{
			IsEnabled = false,
			Text = "Loading...",
		});

		try
		{
			IReadOnlyList<RegistryNodeViewModel> children = await ViewModel.GetBreadcrumbChildrenAsync(args.Index, args.IsRootItem);
			args.Flyout.Items.Clear();

			if (children.Count == 0)
			{
				args.Flyout.Items.Add(new MenuFlyoutItem
				{
					IsEnabled = false,
					Text = "No subkeys",
				});
				return;
			}

			foreach (RegistryNodeViewModel child in children)
			{
				args.Flyout.Items.Add(new MenuFlyoutItem
				{
					Command = ViewModel.NavigateToNodeCommand,
					CommandParameter = child,
					Text = child.Name,
				});
			}
		}
		catch (Exception exception)
		{
			args.Flyout.Items.Clear();
			args.Flyout.Items.Add(new MenuFlyoutItem
			{
				IsEnabled = false,
				Text = exception.Message,
			});
		}
	}

	private void RegistryValueListView_SelectionChanged(object sender, SelectionChangedEventArgs args)
	{
		ViewModel.SelectedValue = args.AddedItems.OfType<RegistryValueViewModel>().FirstOrDefault();
	}

	private async void RegistryValueListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs args)
		=> await ViewModel.DisplaySelectedValueAsync();

	private void RegistryTreeItem_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		if (sender is TreeViewItem { DataContext: RegistryNodeViewModel node })
			ViewModel.SelectedNode = node;
	}

	public async Task<string?> RequestTextAsync(string title, string placeholder, string initialText)
	{
		InputContentDialog dialog = new()
		{
			XamlRoot = XamlRoot,
		};
		dialog.Configure(title, placeholder, initialText);
		ContentDialogResult result = await dialog.ShowAsync();
		return result == ContentDialogResult.Primary ? dialog.Text.Trim() : null;
	}

	public async Task<bool> ConfirmAsync(string title, string message)
	{
		ConfirmationContentDialog dialog = new()
		{
			XamlRoot = XamlRoot,
		};
		dialog.Configure(title, message);
		return await dialog.ShowAsync() == ContentDialogResult.Primary;
	}

	public async Task ShowMessageAsync(string title, string message)
	{
		MessageContentDialog dialog = new()
		{
			XamlRoot = XamlRoot,
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
				XamlRoot = XamlRoot,
			};
			dialog.Configure(ViewModel.GetRegistryPath(node), ViewModel.GetPermissionRules(node));
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
					ViewModel.AddPermission(node, dialog.Account, permission, dialog.SelectedAccessType);
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

	public void CopyText(string text)
	{
		DataPackage package = new();
		package.SetText(text);
		Clipboard.SetContent(package);
	}

	public void Close()
		=> App.Window.Close();

	public void SetAddressBarVisibility(bool isVisible)
		=> NavigationBarHost.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;

	public void SetTreePaneWidth(bool isWide)
		=> RegistryTreeColumn.Width = isWide
			? new GridLength(1, GridUnitType.Star)
			: new GridLength(320);
}
