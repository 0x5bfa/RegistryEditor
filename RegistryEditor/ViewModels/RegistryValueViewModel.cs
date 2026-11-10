// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;

namespace RegistryEditor.ViewModels;

public sealed partial class RegistryValueViewModel(
	string name, string type, string data, BitmapImage icon, string registryName, RegistryValueKind kind, object? rawValue, FontFamily dataFontFamily)
	: ObservableObject
{
	private FontFamily _dataFontFamily = dataFontFamily;

	public string Name { get; } = name;

	public string Type { get; } = type;

	public string Data { get; } = data;

	public BitmapImage Icon { get; } = icon;

	public string RegistryName { get; } = registryName;

	public RegistryValueKind Kind { get; } = kind;

	public object? RawValue { get; } = rawValue;

	public FontFamily DataFontFamily
	{
		get => _dataFontFamily;
		private set => SetProperty(ref _dataFontFamily, value);
	}

	public void SetDataFont(FontFamily fontFamily)
	{
		DataFontFamily = fontFamily;
	}
}
