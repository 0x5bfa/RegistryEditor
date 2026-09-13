// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

namespace RegistryEditor.ViewModels;

public sealed class RegistryBreadcrumbItemViewModel
{
	public RegistryBreadcrumbItemViewModel(string text, bool isChevronVisible)
	{
		Text = text;
		IsChevronVisible = isChevronVisible;
	}

	public string Text { get; }

	public bool IsChevronVisible { get; }
}
