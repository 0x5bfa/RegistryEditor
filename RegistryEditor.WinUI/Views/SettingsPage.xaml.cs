// Copyright (c) 2025 0x5BFA.
// Licensed under the MIT License.

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
