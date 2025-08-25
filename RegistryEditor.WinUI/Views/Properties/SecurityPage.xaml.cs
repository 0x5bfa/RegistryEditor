using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using RegistryValley.App.Models;
using RegistryValley.App.ViewModels.Properties;
using System.IO;
using Windows.Foundation.Metadata;
using Windows.Graphics;

namespace RegistryValley.App.Views.Properties
{
    public sealed partial class SecurityPage : Page
    {
        public SecurityPage()
        {
            InitializeComponent();

            var provider = App.Current.Services;
            ViewModel = provider.GetRequiredService<SecurityViewModel>();
        }

        public SecurityViewModel ViewModel { get; }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            ViewModel.KeyItem = e.Parameter as KeyItem;
            ViewModel.GetKeyAccessControlList();
        }

        private void ViewAdvancedSecurityButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
            {
                var frame = new Frame()
                {
                    RequestedTheme = Services.ThemeModeServices.RootTheme,
                };

                frame.Navigate(typeof(SecurityAdvancedPage), ViewModel.KeyItem, new SuppressNavigationTransitionInfo());

                // Initialize window
                var propertiesWindow = new WinUIEx.WindowEx()
                {
                    IsMinimizable = false,
                    IsMaximizable = false,
                    Content = frame,
                    MinWidth = 850,
                    MinHeight = 550,
                    Backdrop = new WinUIEx.MicaSystemBackdrop(),
                };

                var appWindow = propertiesWindow.AppWindow;

                if (frame.Content is SecurityAdvancedPage properties)
                    properties.AppWindow = appWindow;

                appWindow.SetIcon(Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, Constants.AssetPaths.Logo));
                appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
                appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

                appWindow.Title = string.Format("Advanced Permissions");
                appWindow.Resize(new SizeInt32(850, 550));
                //appWindow.Destroying += AppWindow_Destroying;
                appWindow.Show();
            }
            else
            {
                // Unsupported
            }
        }

        private void MergedPermissionPrincipalsListView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var listView = (ListView)sender;

            if (listView.ItemsSource != null && ViewModel.Principals.Count != 0)
            {
                // Select first item
                listView.SelectedIndex = 0;
            }
        }
    }
}
