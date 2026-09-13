// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;

namespace RegistryEditor.Controls
{
	public partial class Toolbar
	{
		/// <summary>
		/// Gets or sets the size preset used by toolbar items.
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = ToolbarSizes.Medium)]
		public partial ToolbarSizes ToolbarSize { get; set; }

		/// <summary>
		/// Gets or sets the items displayed by the toolbar.
		/// </summary>
		[GeneratedDependencyProperty]
		public partial IList<ToolbarItem>? Items { get; set; }

		/// <summary>
		/// Gets or sets the optional data template used to present materialized toolbar controls.
		/// </summary>
		[GeneratedDependencyProperty]
		public partial DataTemplate? ItemTemplate { get; set; }

		/// <summary>
		/// Gets or sets the accessible label and tooltip for the overflow button.
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = "More")]
		public partial string OverflowButtonLabel { get; set; }

		partial void OnItemsChanged(IList<ToolbarItem>? newValue)
		{
			ItemsChanged(newValue);
		}

		partial void OnItemsPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			ItemsChanged(e.NewValue as IList<ToolbarItem>);
		}

		partial void OnItemTemplateChanged(DataTemplate? newValue)
		{
			ItemTemplateChanged(newValue);
		}

		partial void OnToolbarSizeChanged(ToolbarSizes newValue)
		{
			ToolbarSizeChanged(newValue);
		}

		partial void OnOverflowButtonLabelChanged(string newValue)
		{
			OverflowButtonLabelChanged(newValue);
		}
	}
}
