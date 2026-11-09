// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using RegistryEditor.Controls;
using RegistryEditor.ViewModels;
using RegistryBreadcrumbBar = RegistryEditor.Controls.BreadcrumbBar;
using RegistryBreadcrumbBarItemClickedEventArgs = RegistryEditor.Controls.BreadcrumbBarItemClickedEventArgs;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using WinUI.TableView;

namespace RegistryEditor.Views;

public sealed partial class RootView : UserControl
{
	public RootViewModel ViewModel { get; }

	public RootView()
	{
		ViewModel = new RootViewModel();
		InitializeComponent();
		RegistryValueTableView.AddHandler(
			UIElement.PointerPressedEvent,
			(PointerEventHandler)RegistryValueTableView_PointerPressed,
			handledEventsToo: true);
		RegistryValueTableView.AddHandler(
			UIElement.TappedEvent,
			(TappedEventHandler)RegistryValueTableView_Tapped,
			handledEventsToo: true);
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
		if (!ReferenceEquals(ViewModel.SelectedNode, selectedNode))
			RegistryTreeView.SelectedItem = ViewModel.SelectedNode;
	}

	private async void NavigationOmnibar_QuerySubmitted(Omnibar sender, OmnibarQuerySubmittedEventArgs args)
	{
		if (ReferenceEquals(args.Mode, PathOmnibarMode))
			await ViewModel.NavigateToPathAsync(args.Text);
	}

	private async void PathBreadcrumbBar_ItemClicked(RegistryBreadcrumbBar sender, RegistryBreadcrumbBarItemClickedEventArgs args)
	{
		await ViewModel.NavigateToBreadcrumbAsync(args.Index, args.IsRootItem);
	}

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

	private async void RegistryValueTableView_SelectionChanged(object sender, SelectionChangedEventArgs args)
	{
		RegistryValueViewModel? selectedValue = args.AddedItems.OfType<RegistryValueViewModel>().FirstOrDefault();
		if (selectedValue is not null && !await ViewModel.TrySelectValueAsync(selectedValue))
			RegistryValueTableView.SelectedItem = ViewModel.SelectedValue;
	}

	private void RegistryValueTableView_PointerPressed(object sender, PointerRoutedEventArgs args)
		=> RegistryValueTableView.Focus(FocusState.Pointer);

	private async void RegistryValueTableView_Tapped(object sender, TappedRoutedEventArgs args)
	{
		if (IsTableContentElement(args.OriginalSource as DependencyObject))
			return;

		if (!await ViewModel.TrySelectValueAsync(null))
		{
			RegistryValueTableView.SelectedItem = ViewModel.SelectedValue;
			return;
		}

		RegistryValueTableView.SelectedItem = null;
	}

	private bool IsTableContentElement(DependencyObject? element)
	{
		while (element is not null)
		{
			if (element is TableViewRow
				or TableViewHeaderRow
				or TableViewColumnHeader
				or TableViewGroupHeaderRow)
				return true;

			if (ReferenceEquals(element, RegistryValueTableView))
				return false;

			element = VisualTreeHelper.GetParent(element);
		}

		return false;
	}

	private async void RegistryValueTableView_RowContextFlyoutOpening(object sender, TableViewRowContextFlyoutEventArgs args)
	{
		if (args.Item is not RegistryValueViewModel value
			|| args.Flyout is not MenuFlyout flyout)
			return;

		if (!await ViewModel.TrySelectValueAsync(value))
		{
			flyout.Hide();
			return;
		}

		foreach (MenuFlyoutItem item in flyout.Items.OfType<MenuFlyoutItem>())
		{
			item.Command = (item.Tag as string) switch
			{
				"Delete" => ViewModel.DeleteValueCommand,
				"Rename" => ViewModel.RenameValueCommand,
				_ => null,
			};
			item.CommandParameter = value;
			item.IsEnabled = (item.Tag as string) switch
			{
				"Modify" => ViewModel.CanEditSelectedValue,
				_ => item.Command?.CanExecute(value) ?? false,
			};
		}
	}

	private void RegistryValueTableView_EditInPaneClicked(object sender, RoutedEventArgs args)
	{
		if (!ViewModel.CanEditSelectedValue)
			return;

		ValueEditorDataTextBox.Focus(FocusState.Programmatic);
		ValueEditorDataTextBox.SelectAll();
	}

	private void RegistryTreeItem_ContextRequested(UIElement sender, ContextRequestedEventArgs args)
	{
		if (sender is not TreeViewItem treeViewItem
			|| treeViewItem.DataContext is not RegistryNodeViewModel node
			|| treeViewItem.ContextFlyout is not MenuFlyout flyout)
			return;

		ConfigureContextMenu(flyout, node);
	}

	private void RegistryTreeContextFlyout_Opening(object sender, object args)
	{
		if (sender is not MenuFlyout { Target: TreeViewItem { DataContext: RegistryNodeViewModel node } } flyout)
			return;

		ConfigureContextMenu(flyout, node);
	}

	private void ConfigureContextMenu(MenuFlyout flyout, RegistryNodeViewModel node)
	{
		foreach (MenuFlyoutItem item in EnumerateMenuItems(flyout.Items))
		{
			item.Command = (item.Tag as string) switch
			{
				"Expand" => ViewModel.ExpandNodeCommand,
				"NewKey" => ViewModel.NewKeyCommand,
				"NewStringValue" => ViewModel.NewStringValueCommand,
				"NewBinaryValue" => ViewModel.NewBinaryValueCommand,
				"NewDWordValue" => ViewModel.NewDWordValueCommand,
				"NewQWordValue" => ViewModel.NewQWordValueCommand,
				"NewMultiStringValue" => ViewModel.NewMultiStringValueCommand,
				"NewExpandableStringValue" => ViewModel.NewExpandableStringValueCommand,
				"Find" => ViewModel.FindCommand,
				"DeleteKey" => ViewModel.DeleteKeyCommand,
				"RenameKey" => ViewModel.RenameKeyCommand,
				"Export" => ViewModel.ExportCommand,
				"Permissions" => ViewModel.PermissionsCommand,
				"CopyKey" => ViewModel.CopyKeyCommand,
				_ => null,
			};
			item.CommandParameter = node;

			if (item.Tag as string == "Expand")
				item.Text = node.ExpandMenuText;

			item.IsEnabled = item.Command?.CanExecute(node) ?? false;
		}
	}

	private static IEnumerable<MenuFlyoutItem> EnumerateMenuItems(IEnumerable<MenuFlyoutItemBase> items)
	{
		foreach (MenuFlyoutItemBase item in items)
		{
			if (item is MenuFlyoutItem menuItem)
			{
				yield return menuItem;
			}
			else if (item is MenuFlyoutSubItem subItem)
			{
				foreach (MenuFlyoutItem child in EnumerateMenuItems(subItem.Items))
					yield return child;
			}
		}
	}

}
