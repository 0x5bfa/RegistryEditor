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

namespace RegistryValley.App.Views.Setups
{
    public sealed partial class TermsPage : Page
    {
        public string SourceCodeLicenseStatement = RegistryValley.App.Constants.Terms.SourceCodeLicense;

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
