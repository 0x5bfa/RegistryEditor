// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Media;

namespace RegistryEditor.Controls
{
	public partial class ThemedIcon
	{
		/// <summary>Gets or sets the geometry displayed by the icon.</summary>
		[GeneratedDependencyProperty]
		public partial ThemedIconData? Data { get; set; }

		/// <summary>Gets or sets the brush used when <see cref="IconColorType"/> is <see cref="ThemedIconColorType.Custom"/>.</summary>
		[GeneratedDependencyProperty]
		public partial Brush? Color { get; set; }

		/// <summary>Gets or sets the preferred icon variant.</summary>
		[GeneratedDependencyProperty(DefaultValue = ThemedIconTypes.Layered)]
		public partial ThemedIconTypes IconType { get; set; }

		/// <summary>Gets or sets the semantic color applied to accent layers.</summary>
		[GeneratedDependencyProperty(DefaultValue = ThemedIconColorType.None)]
		public partial ThemedIconColorType IconColorType { get; set; }

		/// <summary>Gets or sets the rendered size, or <see cref="double.NaN"/> to use <see cref="ThemedIconData.Size"/>.</summary>
		[GeneratedDependencyProperty(DefaultValue = double.NaN)]
		public partial double IconSize { get; set; }

		/// <summary>Gets or sets whether the icon is in its toggled state.</summary>
		[GeneratedDependencyProperty]
		public partial bool IsToggled { get; set; }

		/// <summary>Gets or sets whether the filled geometry is displayed.</summary>
		[GeneratedDependencyProperty]
		public partial bool IsFilled { get; set; }

		/// <summary>Gets or sets whether the outline geometry is preferred for high contrast.</summary>
		[GeneratedDependencyProperty]
		public partial bool IsHighContrast { get; set; }

		/// <summary>Gets or sets whether this icon is rendered in its enabled state.</summary>
		[GeneratedDependencyProperty(DefaultValue = true)]
		public partial bool IsEnabled { get; set; }

		/// <summary>Gets or sets how an owning toggle control affects the icon variant.</summary>
		[GeneratedDependencyProperty(DefaultValue = ToggleBehaviors.Auto)]
		public partial ToggleBehaviors ToggleBehavior { get; set; }

		partial void OnDataChanged(ThemedIconData? newValue)
		{
			UpdateDataSource();
		}

		partial void OnColorChanged(Brush? newValue)
		{
			UpdateAppearance();
		}

		partial void OnIconTypeChanged(ThemedIconTypes newValue)
		{
			UpdateAppearance();
		}

		partial void OnIconColorTypeChanged(ThemedIconColorType newValue)
		{
			UpdateAppearance();
		}

		partial void OnIconSizeChanged(double newValue)
		{
			UpdateAppearance();
		}

		partial void OnIsToggledChanged(bool newValue)
		{
			UpdateAppearance();
		}

		partial void OnIsFilledChanged(bool newValue)
		{
			UpdateAppearance();
		}

		partial void OnIsHighContrastChanged(bool newValue)
		{
			UpdateAppearance();
		}

		partial void OnIsEnabledChanged(bool newValue)
		{
			UpdateAppearance();
		}

		partial void OnToggleBehaviorChanged(ToggleBehaviors newValue)
		{
			UpdateAppearance();
		}
	}
}
