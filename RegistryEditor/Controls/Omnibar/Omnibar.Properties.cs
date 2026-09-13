// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using CommunityToolkit.WinUI;

namespace RegistryEditor.Controls
{
	public partial class Omnibar
	{
		[GeneratedDependencyProperty]
		public partial IList<OmnibarMode>? Modes { get; set; }

		[GeneratedDependencyProperty]
		public partial OmnibarMode? CurrentSelectedMode { get; set; }

		[GeneratedDependencyProperty]
		public partial string? CurrentSelectedModeName { get; set; }

		[GeneratedDependencyProperty]
		public partial Thickness AutoSuggestBoxPadding { get; set; }

		[GeneratedDependencyProperty]
		public partial bool IsFocused { get; set; }

		[GeneratedDependencyProperty]
		public partial string? TextBoxAutomationId { get; set; }

		partial void OnCurrentSelectedModePropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.NewValue is not OmnibarMode newMode)
			{
				CurrentSelectedModeName = null;
				if (_textBoxSuggestionsPopup is not null)
				{
					_textBoxSuggestionsPopup.IsOpen = false;
				}

				return;
			}

			if (_modesHostGrid is not null && (Modes is null || !Modes.Contains(newMode)))
			{
				CurrentSelectedMode = Modes?.FirstOrDefault(x => x.IsDefault) ?? Modes?.FirstOrDefault();

				return;
			}

			ChangeMode(e.OldValue as OmnibarMode, newMode);
			CurrentSelectedModeName = newMode.ModeName ?? newMode.Name;
		}

		partial void OnCurrentSelectedModeNameChanged(string? newValue)
		{
			if (string.IsNullOrEmpty(newValue) || Modes is null)
			{
				return;
			}

			if (string.Equals(CurrentSelectedMode?.ModeName ?? CurrentSelectedMode?.Name, newValue, StringComparison.Ordinal))
			{
				return;
			}

			var newMode = Modes.FirstOrDefault(x => string.Equals(x.ModeName ?? x.Name, newValue, StringComparison.Ordinal));
			if (newMode is null)
			{
				return;
			}

			CurrentSelectedMode = newMode;
		}

		partial void OnIsFocusedChanged(bool newValue)
		{
			if (CurrentSelectedMode is not { } currentMode || _textBox is null)
			{
				return;
			}

			ApplyCurrentModeVisualState(currentMode, true);

			TryToggleIsSuggestionsPopupOpen(newValue);
		}
	}
}

