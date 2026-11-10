// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;

namespace RegistryEditor.ViewModels;

public sealed class RegistryValueViewModel : ObservableObject
{
	private FontFamily _dataFontFamily;

	public RegistryValueViewModel(
		string name,
		string type,
		string data,
		BitmapImage icon,
		string registryName,
		RegistryValueKind kind,
		object? rawValue,
		FontFamily dataFontFamily)
	{
		Name = name;
		Type = type;
		Data = data;
		Icon = icon;
		RegistryName = registryName;
		Kind = kind;
		RawValue = rawValue;
		_dataFontFamily = dataFontFamily;
	}

	public string Name { get; }

	public string Type { get; }

	public string Data { get; }

	public BitmapImage Icon { get; }

	public string RegistryName { get; }

	public RegistryValueKind Kind { get; }

	public object? RawValue { get; }

	public FontFamily DataFontFamily
	{
		get => _dataFontFamily;
		private set => SetProperty(ref _dataFontFamily, value);
	}

	public void SetDataFont(FontFamily fontFamily)
		=> DataFontFamily = fontFamily;
}
