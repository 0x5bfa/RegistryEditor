// Copyright (c) 2025 0x5BFA.
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RegistryEditor.WinUI.Helpers;
using RegistryEditor.WinUI.Services;
using RegistryEditor.WinUI.ViewModels;

namespace RegistryEditor.WinUI.Views
{
	public sealed partial class MainPage : Page
	{
		public MainViewModel ViewModel = App.Current.Services.GetRequiredService<MainViewModel>();
		public ValuesViewerViewModel ValuesViewerViewModel = App.Current.Services.GetRequiredService<ValuesViewerViewModel>();
		private UserSettingsServices UserSettingsServices = App.Current.Services.GetRequiredService<UserSettingsServices>();

		public MainPage()
		{
			InitializeComponent();

			LoadAppResources();
			ContentFrame.Navigate(typeof(ValuesViewerPage));
		}

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
	}
}
