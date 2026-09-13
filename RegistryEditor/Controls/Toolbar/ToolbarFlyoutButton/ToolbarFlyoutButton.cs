// Copyright (c) Files Community
// Licensed under the MIT License.

namespace RegistryEditor.Controls
{
	public partial class ToolbarFlyoutButton : ToolbarButton, IToolbarItemSet
	{
		public ToolbarFlyoutButton()
		{
			this.DefaultStyleKey = typeof(ToolbarFlyoutButton);
		}

		protected override void OnApplyTemplate()
		{
			base.OnApplyTemplate();
		}
	}
}
