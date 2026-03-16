// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace RegistryEditor.WinUI.UserControls
{
	public sealed partial class TitleBarControl : UserControl
	{
		#region propdp
		public static readonly DependencyProperty TitleProperty =
			DependencyProperty.Register(
				nameof(Title),
				typeof(string),
				typeof(BranchDisplay),
				new PropertyMetadata(null)
				);

		public string Title
		{
			get => (string)GetValue(TitleProperty);
			set => SetValue(TitleProperty, value);
		}
		#endregion

		public TitleBarControl()
			=> InitializeComponent();
	}
}
