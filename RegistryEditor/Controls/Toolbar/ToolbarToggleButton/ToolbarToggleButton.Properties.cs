// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;

namespace RegistryEditor.Controls
{
	public partial class ToolbarToggleButton
	{
		/// <summary>
		/// Gets or sets the text displayed by the button.
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = "")]
		public partial string Label { get; set; }

		/// <summary>
		/// Gets or sets the themed icon displayed by the button.
		/// </summary>
		[GeneratedDependencyProperty]
		public partial ThemedIconData? ThemedIcon { get; set; }

		/// <summary>
		/// Gets or sets the rendered icon size.
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = 16d)]
		public partial double IconSize { get; set; }
	}
}
