// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

namespace RegistryEditor.Views;

public sealed partial class ConfirmationContentDialog : ContentDialog
{
	public ConfirmationContentDialog()
	{
		InitializeComponent();
	}

	public void Configure(string title, string message)
	{
		Title = title;
		MessageTextBlock.Text = message;
	}
}
