using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using RegistryEditor.WinUI.Extensions;
using RegistryEditor.WinUI.Models;
using RegistryEditor.WinUI.Services;
using RegistryEditor.WinUI.ViewModels;
using RegistryEditor.WinUI.Views.Setups;
using WinRT.Interop;

namespace RegistryEditor.WinUI.Views
{
	public sealed partial class SetupPage : Page
	{
		public SetupPage()
		{
			InitializeComponent();
		}

		private void SetupButton_Click(object sender, RoutedEventArgs e)
		{
			Frame.Navigate(typeof(TermsPage), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
		}
	}
}
