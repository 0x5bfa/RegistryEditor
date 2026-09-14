// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using RegistryEditor.ViewModels;

namespace RegistryEditor.Views;

public sealed partial class SettingsContentDialog : ContentDialog
{
	public SettingsContentDialog(RootViewModel viewModel)
	{
		ViewModel = viewModel;
		InitializeComponent();
	}

	public RootViewModel ViewModel { get; }

	private void AddressBarSettingsToggle_Toggled(object sender, RoutedEventArgs args)
	{
		if (sender is ToggleSwitch toggle)
			ViewModel.SetAddressBarVisibility(toggle.IsOn);
	}
}
