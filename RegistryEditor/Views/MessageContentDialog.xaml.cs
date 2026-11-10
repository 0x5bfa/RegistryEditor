// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Media;

namespace RegistryEditor.Views;

public sealed partial class MessageContentDialog : ContentDialog
{
	public MessageContentDialog()
	{
		InitializeComponent();
	}

	public void Configure(
		string title,
		string message,
		FontFamily? messageFontFamily = null)
	{
		Title = title;
		MessageTextBlock.Text = message;
		if (messageFontFamily is not null)
			MessageTextBlock.FontFamily = messageFontFamily;
	}
}
