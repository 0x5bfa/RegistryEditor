// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace RegistryEditor.Controls;

/// <summary>
/// Provides a shareable themed icon source that creates <see cref="ThemedIcon"/> instances.
/// </summary>
public sealed partial class ThemedIconSource : AnimatedIconSource
{
	private ThemedIconVisualSource _visualSource;

	/// <summary>Initializes a themed icon source.</summary>
	public ThemedIconSource()
	{
		_visualSource = new ThemedIconVisualSource(ThemedIconData.Default, ThemedIconTypes.Layered, ThemedIconColorType.None, false, false, true, false, null, null, ElementTheme.Default, false);
		Source = _visualSource;
		_ = RegisterPropertyChangedCallback(ForegroundProperty, OnForegroundPropertyChanged);
	}

	/// <inheritdoc />
	protected override IconElement CreateIconElementCore()
	{
		return new ThemedIcon()
		{
			Data = IconData,
			IconType = IconType,
			IconColorType = IconColorType,
			Color = Color,
			IsFilled = IsFilled,
			IsToggled = IsToggled,
			IconSize = IconSize,
			ToggleBehavior = ToggleBehavior,
			IsHighContrast = IsHighContrast,
		};
	}

	/// <inheritdoc />
	protected override DependencyProperty GetIconElementPropertyCore(DependencyProperty iconSourceProperty)
	{
		if (iconSourceProperty == IconDataProperty)
		{
			return ThemedIcon.DataProperty;
		}

		if (iconSourceProperty == IconTypeProperty)
		{
			return ThemedIcon.IconTypeProperty;
		}

		if (iconSourceProperty == IconColorTypeProperty)
		{
			return ThemedIcon.IconColorTypeProperty;
		}

		if (iconSourceProperty == ColorProperty)
		{
			return ThemedIcon.ColorProperty;
		}

		if (iconSourceProperty == IsFilledProperty)
		{
			return ThemedIcon.IsFilledProperty;
		}

		if (iconSourceProperty == IsToggledProperty)
		{
			return ThemedIcon.IsToggledProperty;
		}

		if (iconSourceProperty == IconSizeProperty)
		{
			return ThemedIcon.IconSizeProperty;
		}

		if (iconSourceProperty == ToggleBehaviorProperty)
		{
			return ThemedIcon.ToggleBehaviorProperty;
		}

		if (iconSourceProperty == IsHighContrastProperty)
		{
			return ThemedIcon.IsHighContrastProperty;
		}

		return base.GetIconElementPropertyCore(iconSourceProperty);
	}

	private void OnForegroundPropertyChanged(DependencyObject sender, DependencyProperty property)
	{
		UpdateAppearance();
	}

	private void UpdateAppearance()
	{
		var isToggled = ToggleBehavior is ToggleBehaviors.On || (ToggleBehavior is ToggleBehaviors.Auto && IsToggled);
		if (!_visualSource.UpdateAppearance(IconType, IconColorType, IsFilled, isToggled, true, IsHighContrast, Foreground, Color, ElementTheme.Default, false))
		{
			_visualSource = new ThemedIconVisualSource(IconData ?? ThemedIconData.Default, IconType, IconColorType, IsFilled, isToggled, true, IsHighContrast, Foreground, Color, ElementTheme.Default, false);
			Source = _visualSource;
		}
	}

	private void UpdateDataSource()
	{
		var isToggled = ToggleBehavior is ToggleBehaviors.On || (ToggleBehavior is ToggleBehaviors.Auto && IsToggled);
		_visualSource = new ThemedIconVisualSource(IconData ?? ThemedIconData.Default, IconType, IconColorType, IsFilled, isToggled, true, IsHighContrast, Foreground, Color, ElementTheme.Default, false);
		Source = _visualSource;
	}
}
