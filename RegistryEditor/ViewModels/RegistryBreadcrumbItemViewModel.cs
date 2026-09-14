// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

namespace RegistryEditor.ViewModels;

public sealed class RegistryBreadcrumbItemViewModel
{
	public RegistryBreadcrumbItemViewModel(RegistryNodeViewModel node, string text, bool isChevronVisible)
	{
		Node = node;
		Text = text;
		IsChevronVisible = isChevronVisible;
	}

	public RegistryNodeViewModel Node { get; }

	public string Text { get; }

	public bool IsChevronVisible { get; }
}
