// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;

namespace RegistryEditor.ViewModels;

public sealed class RegistryValueViewModel
{
	public RegistryValueViewModel(
		string name,
		string type,
		string data,
		BitmapImage icon,
		string registryName,
		RegistryValueKind kind,
		object? rawValue)
	{
		Name = name;
		Type = type;
		Data = data;
		Icon = icon;
		RegistryName = registryName;
		Kind = kind;
		RawValue = rawValue;
	}

	public string Name { get; }

	public string Type { get; }

	public string Data { get; }

	public BitmapImage Icon { get; }

	public string RegistryName { get; }

	public RegistryValueKind Kind { get; }

	public object? RawValue { get; }
}
