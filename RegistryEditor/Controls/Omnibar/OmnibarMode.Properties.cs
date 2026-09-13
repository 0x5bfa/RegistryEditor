// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using CommunityToolkit.WinUI;

namespace RegistryEditor.Controls
{
	public partial class OmnibarMode
	{
		[GeneratedDependencyProperty]
		public partial string? Text { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsDefault { get; set; }

		[GeneratedDependencyProperty]
		public partial string? PlaceholderText { get; set; }

		[GeneratedDependencyProperty]
		public partial string? ModeName { get; set; }

		[GeneratedDependencyProperty]
		public partial FrameworkElement? ContentOnInactive { get; set; }

		[GeneratedDependencyProperty]
		public partial IconElement? IconOnActive { get; set; }

		[GeneratedDependencyProperty]
		public partial IconElement? IconOnInactive { get; set; }

		[GeneratedDependencyProperty]
		/// <remark>
		/// Implement <see cref="IOmnibarTextMemberPathProvider"/> in <see cref="ItemsSource"/> to get the text member path from the suggestion item correctly.
		/// </remark>
		public partial string? TextMemberPath { get; set; }

		[GeneratedDependencyProperty(DefaultValue = true)]
		public partial bool UpdateTextOnSelect { get; set; }

		[GeneratedDependencyProperty(DefaultValue = true)]
		public partial bool UpdateTextOnArrowKeys { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsAutoFocusEnabled { get; set; }

		partial void OnTextChanged(string? newValue)
		{
			if (_ownerRef is null || _ownerRef.TryGetTarget(out var owner) is false)
			{
				return;
			}

			var text = newValue ?? string.Empty;
			if (ReferenceEquals(owner.CurrentSelectedMode, this))
			{
				owner.ChangeTextBoxTextFromMode(text);

				return;
			}

			owner.UpdateUserInputFromMode(this, text);
		}

		partial void OnPlaceholderTextChanged(string? newValue)
		{
			if (_ownerRef is null || _ownerRef.TryGetTarget(out var owner) is false)
			{
				return;
			}

			owner.UpdatePlaceholderTextFromMode(this, newValue ?? string.Empty);
		}

		partial void OnContentOnInactiveChanged(FrameworkElement? newValue)
		{
			if (_ownerRef is null || _ownerRef.TryGetTarget(out var owner) is false)
			{
				return;
			}

			owner.UpdateModeVisualStateFromMode(this);
		}
	}
}

