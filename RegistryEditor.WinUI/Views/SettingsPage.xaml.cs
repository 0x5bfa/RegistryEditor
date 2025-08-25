using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RegistryEditor.WinUI.Dialogs;
using RegistryEditor.WinUI.Models;
using RegistryEditor.WinUI.ViewModels;

namespace RegistryEditor.WinUI.Views
{
	public sealed partial class SettingsPage : Page
	{
		public SettingsPage()
		{
			InitializeComponent();

			var provider = App.Current.Services;
			ViewModel = provider.GetRequiredService<SettingsViewModel>();
		}

		public SettingsViewModel ViewModel { get; }

		private void ResetAppButton_Click(object sender, RoutedEventArgs e)
		{
			App.Window.NavigateFrameTo(typeof(SetupPage));
		}
	}
}
