// Copyright (c) Files Community
// Licensed under the MIT License.

namespace RegistryEditor.Controls
{
	// TemplateParts
	[TemplatePart(Name = ToolbarItemsPanelPartName, Type = typeof(ToolbarItemsPanel))]
	[TemplatePart(Name = OverflowStackPanelPartName, Type = typeof(StackPanel))]
	[TemplatePart(Name = OverflowButtonPartName, Type = typeof(ToolbarFlyoutButton))]
	[TemplatePart(Name = OverflowFlyoutPartName, Type = typeof(MenuFlyout))]

	// VisualStates
	[TemplateVisualState(Name = OverflowOnStateName, GroupName = CommonStatesGroupName)]
	[TemplateVisualState(Name = OverflowOffStateName, GroupName = CommonStatesGroupName)]
	public partial class Toolbar : Control
	{
		// TemplatePart Names
		internal const string ToolbarItemsPanelPartName = "PART_ItemsPanel";
		internal const string OverflowStackPanelPartName = "PART_OverflowStackPanel";
		internal const string OverflowButtonPartName = "PART_OverflowButton";
		internal const string OverflowFlyoutPartName = "PART_OverflowFlyout";

		// VisualState Group Names
		internal const string CommonStatesGroupName = "OverflowStates";

		// VisualState Names
		internal const string OverflowOnStateName = "OverflowOn";
		internal const string OverflowOffStateName = "OverflowOff";
		// ResourceDictionary Keys
		internal const string SmallMinWidthResourceKey = "ToolbarButtonSmallMinWidth";
		internal const string SmallMinHeightResourceKey = "ToolbarButtonSmallMinHeight";

		internal const string MediumMinWidthResourceKey = "ToolbarButtonMediumMinWidth";
		internal const string MediumMinHeightResourceKey = "ToolbarButtonMediumMinHeight";

		internal const string LargeMinWidthResourceKey = "ToolbarButtonLargeMinWidth";
		internal const string LargeMinHeightResourceKey = "ToolbarButtonLargeMinHeight";
	}
}
