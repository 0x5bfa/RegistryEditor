// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using Microsoft.UI.Xaml.Input;

namespace RegistryEditor.Controls
{
	[DebuggerDisplay("{" + nameof(ToString) + "(),nq}")]
	public partial class OmnibarMode : ItemsControl
	{
		// Constants

		private const string TemplatePartName_ModeButton = "PART_ModeButton";

		// Fields

		private WeakReference<Omnibar>? _ownerRef;

		private Button _modeButton = null!;
		private long? _itemsSourceCallbackToken;

		internal double ModeButtonWidth => _modeButton is not null && _modeButton.ActualWidth > 0 ? _modeButton.ActualWidth : _modeButton?.Width > 0 ? _modeButton.Width : ActualWidth;

		// Constructor

		public OmnibarMode()
		{
			DefaultStyleKey = typeof(OmnibarMode);
		}

		// Methods

		protected override void OnApplyTemplate()
		{
			UnhookTemplateParts();
			base.OnApplyTemplate();

			_modeButton = GetTemplateChild(TemplatePartName_ModeButton) as Button
				?? throw new MissingFieldException($"Could not find {TemplatePartName_ModeButton} in the given {nameof(OmnibarMode)}'s style.");

			_itemsSourceCallbackToken = RegisterPropertyChangedCallback(ItemsSourceProperty, (sender, property) =>
			{
				if (_ownerRef is not null && _ownerRef.TryGetTarget(out var owner))
				{
					owner.TryToggleIsSuggestionsPopupOpen(true);
				}
			});

			Loaded += OmnibarMode_Loaded;
			_modeButton.PointerEntered += ModeButton_PointerEntered;
			_modeButton.PointerPressed += ModeButton_PointerPressed;
			_modeButton.PointerReleased += ModeButton_PointerReleased;
			_modeButton.PointerExited += ModeButton_PointerExited;
			_modeButton.Click += ModeButton_Click;
		}

		protected override void OnKeyUp(KeyRoutedEventArgs args)
		{
			if (args.Handled || IsEnabled is false)
			{
				base.OnKeyUp(args);

				return;
			}

			if (args.Key is Windows.System.VirtualKey.Enter)
			{
				if (_ownerRef is null || _ownerRef.TryGetTarget(out var owner) is false || owner.CurrentSelectedMode == this)
				{
					base.OnKeyUp(args);

					return;
				}

				VisualStateManager.GoToState(this, "PointerPressed", true);

				// Change the current mode
				owner.CurrentSelectedMode = this;
				owner.FocusTextBox();

				VisualStateManager.GoToState(this, "PointerNormal", true);
			}

			base.OnKeyUp(args);
		}

		protected override void OnItemsChanged(object e)
		{
			base.OnItemsChanged(e);

			if (_ownerRef is not null && _ownerRef.TryGetTarget(out var owner))
			{
				owner.TryToggleIsSuggestionsPopupOpen(true);
			}
		}

		public void SetOwner(Omnibar owner)
		{
			ArgumentNullException.ThrowIfNull(owner);

			_ownerRef = new(owner);
		}

		public override string ToString()
		{
			return ModeName ?? Name ?? string.Empty;
		}

		internal void ClearOwner(Omnibar owner)
		{
			ArgumentNullException.ThrowIfNull(owner);

			if (_ownerRef is null)
			{
				return;
			}

			if (_ownerRef.TryGetTarget(out var currentOwner) && !ReferenceEquals(currentOwner, owner))
			{
				return;
			}

			_ownerRef = null;
		}

		private void OmnibarMode_Loaded(object sender, RoutedEventArgs e)
		{
			// Set this mode as the current mode if it is the default mode
			if (IsDefault && _ownerRef is not null && _ownerRef.TryGetTarget(out var owner) && owner.CurrentSelectedMode is null)
			{
				DispatcherQueue.TryEnqueue(() => { owner.CurrentSelectedMode = this; });
			}
		}

		private void UnhookTemplateParts()
		{
			if (_itemsSourceCallbackToken is { } itemsSourceCallbackToken)
			{
				UnregisterPropertyChangedCallback(ItemsSourceProperty, itemsSourceCallbackToken);
				_itemsSourceCallbackToken = null;
			}

			Loaded -= OmnibarMode_Loaded;
			if (_modeButton is not null)
			{
				_modeButton.PointerEntered -= ModeButton_PointerEntered;
				_modeButton.PointerPressed -= ModeButton_PointerPressed;
				_modeButton.PointerReleased -= ModeButton_PointerReleased;
				_modeButton.PointerExited -= ModeButton_PointerExited;
				_modeButton.Click -= ModeButton_Click;
			}

			_modeButton = null!;
		}
	}
}

