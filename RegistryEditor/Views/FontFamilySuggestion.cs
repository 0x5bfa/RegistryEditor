// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;

namespace RegistryEditor.Views;

[Microsoft.UI.Xaml.Data.Bindable]
public sealed class FontFamilySuggestion(string name, FontFamily fontFamily)
{
	public string Name { get; } = name;

	public FontFamily FontFamily { get; } = fontFamily;
}
