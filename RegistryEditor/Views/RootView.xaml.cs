// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using RegistryEditor.ViewModels;
using RegistryEditor.Controls;
using RegistryBreadcrumbBar = RegistryEditor.Controls.BreadcrumbBar;
using RegistryBreadcrumbBarItemClickedEventArgs = RegistryEditor.Controls.BreadcrumbBarItemClickedEventArgs;

namespace RegistryEditor.Views;

public sealed partial class RootView : UserControl
{
	private readonly RootViewModel _viewModel;

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

}
