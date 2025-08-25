using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using RegistryValley.App.Extensions;
using RegistryValley.App.Models;
using RegistryValley.App.Services;
using RegistryValley.App.ViewModels;
using RegistryValley.App.Views.Setups;
using WinRT.Interop;

namespace RegistryValley.App.Views
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
