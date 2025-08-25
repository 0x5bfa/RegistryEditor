using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RegistryValley.App.Extensions;
using RegistryValley.App.Helpers;
using RegistryValley.App.Models;
using RegistryValley.App.Services;
using RegistryValley.App.ViewModels;

namespace RegistryValley.App.Views
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();

            var provider = App.Current.Services;
            ViewModel = provider.GetRequiredService<MainViewModel>();
            ValuesViewerViewModel = provider.GetRequiredService<ValuesViewerViewModel>();
            UserSettingsServices = App.Current.Services.GetRequiredService<UserSettingsServices>();

            LoadAppResources();

            ContentFrame.Navigate(typeof(ValuesViewerPage));
        }

        #region Fields and Properties
        public MainViewModel ViewModel { get; }
        public ValuesViewerViewModel ValuesViewerViewModel { get; }
        private UserSettingsServices UserSettingsServices { get; }
        #endregion

        private void LoadAppResources()
        {
            var appThemeBackgroundColor = ColorHelper.ToColor(UserSettingsServices.AppThemeBackgroundColor);
            AppThemeResourcesHelpers.SetAppThemeBackgroundColor(appThemeBackgroundColor);
            var useCompactSpacing = UserSettingsServices.UseCompactLayout;
            AppThemeResourcesHelpers.SetCompactSpacing(useCompactSpacing);
            AppThemeResourcesHelpers.ApplyResources();

            ThemeModeServices.Initialize();
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            CustomMainTreeView.UnselectItem();

            SettingsButtonClickedIndicator.Visibility = Visibility.Visible;
            SettingsButtonClickedBackground.Visibility = Visibility.Visible;

            ContentFrame.Navigate(typeof(SettingsPage));
        }

        private void EnsureCurrentPageIsValuesViewer()
        {
            var currentSourcePageType = ContentFrame.CurrentSourcePageType;

            if (currentSourcePageType == typeof(SettingsPage))
            {
                SettingsButtonClickedIndicator.Visibility = Visibility.Collapsed;
                SettingsButtonClickedBackground.Visibility = Visibility.Collapsed;

                ContentFrame.Navigate(typeof(ValuesViewerPage));
            }
        }

        #region CustomMainTreeView Events
        private void CustomMainTreeView_BaseSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            EnsureCurrentPageIsValuesViewer();

            var selectedItem = CustomMainTreeView.GetSelectedItem();

            // Load selected key's values
            if (selectedItem != null && ValuesViewerViewModel.SelectedKeyItem != selectedItem)
            {
                ValuesViewerViewModel.SelectedKeyItem = selectedItem;
            }
        }

        private async void CustomMainTreeView_KeyDeleting(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new()
            {
                Title = "Confirm key deletion",
                Content = "Are you sure you want permanently\ndelete this key and all of its subkeys?",
                PrimaryButtonText = "Yes",
                SecondaryButtonText = "No",
                XamlRoot = Content.XamlRoot,
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Secondary)
                return;

            var item = CustomMainTreeView.GetSelectedItem();

            ViewModel.DeleteSelectedKey(item);
            CustomMainTreeView.RemoveItemRecursively(item);
        }

        private void CustomMainTreeView_KeyRenaming(object sender, RoutedEventArgs e)
        {
            ViewModel.RenameSelectedKey(CustomMainTreeView.GetSelectedItem(), ((TextBox)sender).Text);
        }

        private async void CustomMainTreeView_KeyExporting(object sender, RoutedEventArgs e)
        {
            await ViewModel.ExportSelectedKeyTree(CustomMainTreeView.GetSelectedItem());
        }

        private void CustomMainTreeView_KeyPropertyWindowOpening(object sender, RoutedEventArgs e)
        {
            PropertyWindowHelpers.CreatePropertyWindow(CustomMainTreeView.GetSelectedItem());
        }
        #endregion
    }
}
