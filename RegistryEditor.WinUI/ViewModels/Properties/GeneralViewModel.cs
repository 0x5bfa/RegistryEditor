using Microsoft.UI.Xaml;
using RegistryEditor.WinUI.Models;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace RegistryEditor.WinUI.ViewModels.Properties
{
	public class GeneralViewModel : ObservableObject
	{
		public GeneralViewModel()
		{
		}

		#region Fields and Properties
		private KeyItem _keyItem;
		public KeyItem KeyItem { get => _keyItem; set => SetProperty(ref _keyItem, value); }
		#endregion
	}
}
