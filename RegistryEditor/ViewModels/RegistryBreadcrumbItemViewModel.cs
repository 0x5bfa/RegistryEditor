// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

namespace RegistryEditor.ViewModels;

public sealed class RegistryBreadcrumbItemViewModel(RegistryNodeViewModel node, string text, bool isChevronVisible)
{
	public RegistryNodeViewModel Node { get; } = node;

	public string Text { get; } = text;

	public bool IsChevronVisible { get; } = isChevronVisible;
}
