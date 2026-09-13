// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

namespace RegistryEditor.Views;

public sealed partial class InputContentDialog : ContentDialog
{
	public InputContentDialog()
	{
		InitializeComponent();
	}

	public string Text => InputTextBox.Text;

	public void Configure(string title, string placeholder, string initialText)
	{
		Title = title;
		PromptTextBlock.Text = placeholder;
		InputTextBox.PlaceholderText = placeholder;
		InputTextBox.Text = initialText;
		InputTextBox.SelectAll();
	}
}
