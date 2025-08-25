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

namespace RegistryEditor.WinUI.Views.Setups
{
	public sealed partial class TermsPage : Page
	{
		public string SourceCodeLicenseStatement = RegistryEditor.WinUI.Constants.Terms.SourceCodeLicense;

		public TermsPage()
		{
			InitializeComponent();
		}

		private void DisagreeButton_Click(object sender, RoutedEventArgs e)
		{
			Frame.Navigate(typeof(SetupPage), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft });
		}

		private void AgreeButton_Click(object sender, RoutedEventArgs e)
		{
			Frame.Navigate(typeof(ConfigurationsPage), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight });
		}

		private void NavigateBackButton_Click(object sender, RoutedEventArgs e)
		{
			Frame.Navigate(typeof(SetupPage), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft });
		}
	}
}
