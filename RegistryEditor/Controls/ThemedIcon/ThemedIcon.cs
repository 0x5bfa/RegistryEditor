// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace RegistryEditor.Controls
{
	/// <summary>
	/// Displays a state-aware, color-aware icon that can be used in <see cref="IconElement"/> properties.
	/// </summary>
	public partial class ThemedIcon : AnimatedIcon
	{
		private Control? _ownerControl;
		private ToggleButton? _ownerToggleButton;
		private ThemedIconVisualSource? _visualSource;

		/// <summary>Initializes a themed icon.</summary>
		public ThemedIcon()
		{
			Loaded += OnLoaded;
			Unloaded += OnUnloaded;
			ActualThemeChanged += OnActualThemeChanged;
			_ = RegisterPropertyChangedCallback(ForegroundProperty, OnForegroundPropertyChanged);
		}

		private void OnLoaded(object sender, RoutedEventArgs args)
		{
			AttachOwner();
			UpdateDataSource();
		}

		private void OnUnloaded(object sender, RoutedEventArgs args)
		{
			DetachOwner();
		}

		private void OnActualThemeChanged(FrameworkElement sender, object args)
		{
			UpdateAppearance();
		}

		private void OnForegroundPropertyChanged(DependencyObject sender, DependencyProperty property)
		{
			UpdateAppearance();
		}

		private void AttachOwner()
		{
			var ownerControl = this.FindAscendant<Control>();
			var ownerToggleButton = this.FindAscendant<ToggleButton>();
			if (ReferenceEquals(_ownerControl, ownerControl) && ReferenceEquals(_ownerToggleButton, ownerToggleButton))
			{
				return;
			}

			DetachOwner();
			_ownerControl = ownerControl;
			_ownerToggleButton = ownerToggleButton;
			_ownerControl?.IsEnabledChanged += OnOwnerEnabledChanged;
			_ownerToggleButton?.Checked += OnOwnerToggleChanged;
			_ownerToggleButton?.Unchecked += OnOwnerToggleChanged;
		}

		private void DetachOwner()
		{
			_ownerControl?.IsEnabledChanged -= OnOwnerEnabledChanged;
			_ownerControl = null;
			_ownerToggleButton?.Checked -= OnOwnerToggleChanged;
			_ownerToggleButton?.Unchecked -= OnOwnerToggleChanged;
			_ownerToggleButton = null;
		}

		private void OnOwnerEnabledChanged(object sender, DependencyPropertyChangedEventArgs args)
		{
			UpdateAppearance();
		}

		private void OnOwnerToggleChanged(object sender, RoutedEventArgs args)
		{
			UpdateAppearance();
		}

		private void UpdateAppearance()
		{
			var isToggled = ToggleBehavior is ToggleBehaviors.On || (ToggleBehavior is ToggleBehaviors.Auto && (IsToggled || _ownerToggleButton?.IsChecked is true));
			var isEnabled = IsEnabled && _ownerControl?.IsEnabled is not false;
			var effectiveSize = GetEffectiveSize();
			var isSystemHighContrast = GetHighContrastResource();
			var isHighContrast = IsHighContrast || isSystemHighContrast;
			var data = Data ?? ThemedIconData.Default;
			var resourceTheme = isSystemHighContrast ? ElementTheme.Default : ActualTheme;
			if (_visualSource is null || !_visualSource.UpdateAppearance(IconType, IconColorType, IsFilled, isToggled, isEnabled, isHighContrast, Foreground, Color, resourceTheme, true))
			{
				_visualSource = new ThemedIconVisualSource(data, IconType, IconColorType, IsFilled, isToggled, isEnabled, isHighContrast, Foreground, Color, resourceTheme, true);
				Source = _visualSource;
			}

			Width = effectiveSize;
			Height = effectiveSize;
		}

		private void UpdateDataSource()
		{
			_visualSource = null;
			UpdateAppearance();
		}

		private double GetEffectiveSize()
		{
			if (!double.IsNaN(IconSize))
			{
				return double.IsFinite(IconSize) && IconSize > 0 ? IconSize : 16;
			}

			var dataSize = Data?.Size ?? ThemedIconData.Default.Size;

			return double.IsFinite(dataSize) && dataSize > 0 ? dataSize : 16;
		}

		private static bool GetHighContrastResource()
		{
			return Application.Current?.Resources.TryGetValue("ThemedIconHighContrast", out var value) is true && value is true;
		}
	}
}
