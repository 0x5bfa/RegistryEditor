using Microsoft.Extensions.DependencyInjection;
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
		public ValuesViewerPage()
		{
			InitializeComponent();

			var provider = App.Current.Services;
			ViewModel = provider.GetRequiredService<ValuesViewerViewModel>();
		}

		public ValuesViewerViewModel ViewModel { get; }

		#region ListView events
		private async void ValueListView_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
		{
			var item = (ValueItem)ValueListView.SelectedItem;
			if (item == null)
				return;

			var dialog = new ValueEditingDialog
			{
				ViewModel = new() { ValueItem = item },
				// WinUI3: https://github.com/microsoft/microsoft-ui-xaml/issues/2504
				XamlRoot = Content.XamlRoot,
			};

			_ = await dialog.ShowAsync();
		}

		private void ValueListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ViewModel.SelectedValueItem = (ValueItem)ValueListView.SelectedItem;
		}
		#endregion

		#region Custom AppBarCommandButtons event
		private void OnKeyPermissionsButtonClick(object sender, RoutedEventArgs e)
		{
			var item = ViewModel.SelectedKeyItem;

			Helpers.PropertyWindowHelpers.CreatePropertyWindow(item);
		}
		#endregion
	}
}
