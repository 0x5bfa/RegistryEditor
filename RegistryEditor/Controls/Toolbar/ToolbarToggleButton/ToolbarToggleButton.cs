// Copyright (c) Files Community
// Licensed under the MIT License.

namespace RegistryEditor.Controls
{
	public partial class ToolbarToggleButton : ToggleButton, IToolbarItemSet
	{
		private bool _hasContent;
		private long _contentChangedCallbackToken;

		public ToolbarToggleButton()
		{
			DefaultStyleKey = typeof(ToolbarToggleButton);
		}

		/// <inheritdoc/>
		protected override void OnApplyTemplate()
		{
			if (_contentChangedCallbackToken != 0)
			{
				UnregisterPropertyChangedCallback(ContentProperty, _contentChangedCallbackToken);
			}

			base.OnApplyTemplate();
			_contentChangedCallbackToken = RegisterPropertyChangedCallback(ContentProperty, OnContentPropertyChanged);
			UpdateContentState();
		}

		private void OnContentPropertyChanged(DependencyObject sender, DependencyProperty property)
		{
			UpdateContentState();
		}

		private void UpdateContentState()
		{
			_hasContent = Content is not null;
			VisualStateManager.GoToState(this, _hasContent ? HasContentStateName : HasNoContentStateName, true);
		}
	}
}
