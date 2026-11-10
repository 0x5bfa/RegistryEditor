// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

namespace RegistryEditor.Views;

public sealed partial class AboutContentDialog : ContentDialog
{
	public AboutContentDialog()
	{
		InitializeComponent();

		Version version = typeof(App).Assembly.GetName().Version ?? new Version(1, 0, 0);
		VersionTextBlock.Text = $"Version {version.Major}.{version.Minor}.{Math.Max(0, version.Build)}";
	}
}
