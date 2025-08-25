// Copyright (c) 2025 0x5BFA.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using RegistryEditor.WinUI.Dialogs;
using RegistryEditor.WinUI.Models;
using RegistryEditor.WinUI.ViewModels;

namespace RegistryEditor.WinUI.Views
{
	public sealed partial class ValuesViewerPage : Page
	{
		public ValuesViewerViewModel ViewModel = App.Current.Services.GetRequiredService<ValuesViewerViewModel>();

		public ValuesViewerPage()
		{
			InitializeComponent();
		}

		private async void ValueListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			if ((ValueItem)ValueListView.SelectedItem is not { } item)
				return;

			var dialog = new ValueEditingDialog
			{
				ViewModel = new() { ValueItem = item },
				XamlRoot = Content.XamlRoot,
			};

			_ = await dialog.ShowAsync();
		}

		private void ValueListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ViewModel.SelectedValueItem = (ValueItem)ValueListView.SelectedItem;
		}

		private void OnKeyPermissionsButtonClick(object sender, RoutedEventArgs e)
		{
			var item = ViewModel.SelectedKeyItem;

			Helpers.PropertyWindowHelpers.CreatePropertyWindow(item);
		}
	}
}
