using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using RegistryValley.App.Models;
using RegistryValley.App.ViewModels.Properties;
using Windows.Foundation.Metadata;
using Windows.Graphics;

namespace RegistryValley.App.Views.Properties
{
    public sealed partial class GeneralPage : Page
    {
        public GeneralPage()
        {
            InitializeComponent();

            var provider = App.Current.Services;
            ViewModel = provider.GetRequiredService<GeneralViewModel>();
        }

        public GeneralViewModel ViewModel { get; }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ViewModel.KeyItem = e.Parameter as KeyItem;
        }
    }
}
