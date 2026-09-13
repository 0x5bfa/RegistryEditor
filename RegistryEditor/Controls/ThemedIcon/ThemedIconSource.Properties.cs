// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Media;

namespace RegistryEditor.Controls;

public sealed partial class ThemedIconSource
{
	/// <summary>Gets or sets the data displayed by created icons.</summary>
	[GeneratedDependencyProperty]
	public partial ThemedIconData? IconData { get; set; }

	/// <summary>Gets or sets the preferred icon variant.</summary>
	[GeneratedDependencyProperty(DefaultValue = ThemedIconTypes.Layered)]
	public partial ThemedIconTypes IconType { get; set; }

	/// <summary>Gets or sets the semantic color applied to accent layers.</summary>
	[GeneratedDependencyProperty(DefaultValue = ThemedIconColorType.None)]
	public partial ThemedIconColorType IconColorType { get; set; }

	/// <summary>Gets or sets the brush used when <see cref="IconColorType"/> is <see cref="ThemedIconColorType.Custom"/>.</summary>
	[GeneratedDependencyProperty]
	public partial Brush? Color { get; set; }

	/// <summary>Gets or sets whether the filled geometry is displayed.</summary>
	[GeneratedDependencyProperty]
	public partial bool IsFilled { get; set; }

	/// <summary>Gets or sets whether the icon is in its toggled state.</summary>
	[GeneratedDependencyProperty]
	public partial bool IsToggled { get; set; }

	/// <summary>Gets or sets the rendered size, or <see cref="double.NaN"/> to use <see cref="ThemedIconData.Size"/>.</summary>
	[GeneratedDependencyProperty(DefaultValue = double.NaN)]
	public partial double IconSize { get; set; }

	/// <summary>Gets or sets how an owning toggle control affects the icon variant.</summary>
	[GeneratedDependencyProperty(DefaultValue = ToggleBehaviors.Auto)]
	public partial ToggleBehaviors ToggleBehavior { get; set; }

	/// <summary>Gets or sets whether the outline geometry is preferred for high contrast.</summary>
	[GeneratedDependencyProperty]
	public partial bool IsHighContrast { get; set; }

	partial void OnIconDataChanged(ThemedIconData? newValue)
	{
		UpdateDataSource();
	}

	partial void OnIconTypeChanged(ThemedIconTypes newValue)
	{
		UpdateAppearance();
	}

	partial void OnIconColorTypeChanged(ThemedIconColorType newValue)
	{
		UpdateAppearance();
	}

	partial void OnColorChanged(Brush? newValue)
	{
		UpdateAppearance();
	}

	partial void OnIsFilledChanged(bool newValue)
	{
		UpdateAppearance();
	}

	partial void OnIsToggledChanged(bool newValue)
	{
		UpdateAppearance();
	}

	partial void OnIconSizeChanged(double newValue)
	{
		if (!double.IsNaN(newValue) && (!double.IsFinite(newValue) || newValue <= 0))
		{
			throw new ArgumentOutOfRangeException(nameof(newValue));
		}
	}

	partial void OnToggleBehaviorChanged(ToggleBehaviors newValue)
	{
		UpdateAppearance();
	}

	partial void OnIsHighContrastChanged(bool newValue)
	{
		UpdateAppearance();
	}
}
