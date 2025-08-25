using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using RegistryValley.App.Models;
using RegistryValley.App.ViewModels.Properties;
using Windows.Foundation.Metadata;
using Windows.UI.WindowManagement;

namespace RegistryValley.App.Views.Properties
{
	public sealed partial class SecurityAdvancedPage : Page
	{
		public SecurityAdvancedPage()
		{
			InitializeComponent();

			var provider = App.Current.Services;
			ViewModel = provider.GetRequiredService<SecurityAdvancedViewModel>();
		}

		public SecurityAdvancedViewModel ViewModel { get; }

		public Microsoft.UI.Windowing.AppWindow AppWindow;

		protected override void OnNavigatedTo(NavigationEventArgs e)
		{
			ViewModel.KeyItem = e.Parameter as KeyItem;
			ViewModel.LoadKeySecurityOwner();
			ViewModel.GetKeyAccessControlList();
		}

		private void AdvancedPermissionListView_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
		{
		}

		private void OKButton_Click(object sender, RoutedEventArgs e)
		{
			// TODO: Save and sync

			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				AppWindow.Destroy();
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			if (ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 8))
				AppWindow.Destroy();
		}
	}
}
