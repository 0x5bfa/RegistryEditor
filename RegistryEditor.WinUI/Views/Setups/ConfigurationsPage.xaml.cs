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
    public sealed partial class ConfigurationsPage : Page
    {
        public string SourceCodeLicenseStatement = Constants.Terms.SourceCodeLicense;

        public ConfigurationsPage()
        {
            InitializeComponent();
        }

        private void BackOnBottomButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(TermsPage), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft });
        }

        private void FinishButton_Click(object sender, RoutedEventArgs e)
        {
            UserSettingsServices userSettingsServices = App.Current.Services.GetRequiredService<UserSettingsServices>();
            userSettingsServices.SetupCompleted = true;

            Frame.Navigate(typeof(MainPage));
        }

        private void NavigateBackButton_Click(object sender, RoutedEventArgs e)
        {
            Frame.Navigate(typeof(TermsPage), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft });
        }
    }
}
