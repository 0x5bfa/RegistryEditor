// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Controls;
using RegistryEditor.WinUI.ViewModels;

namespace RegistryEditor.WinUI.Views
{
	public sealed partial class SettingsPage : Page
	{
		public SettingsViewModel ViewModel = App.Current.Services.GetRequiredService<SettingsViewModel>();

		public SettingsPage()
		{
			InitializeComponent();
		}
	}
}
