// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Media.Imaging;

namespace RegistryEditor.ViewModels;

public sealed class RegistryValueViewModel
{
	public RegistryValueViewModel(string name, string type, string data, BitmapImage icon)
	{
		Name = name;
		Type = type;
		Data = data;
		Icon = icon;
	}

	public string Name { get; }

	public string Type { get; }

	public string Data { get; }

	public BitmapImage Icon { get; }
}
