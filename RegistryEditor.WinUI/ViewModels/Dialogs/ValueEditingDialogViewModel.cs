using RegistryEditor.WinUI.Models;

namespace RegistryEditor.WinUI.ViewModels.Dialogs
{
	public class ValueEditingDialogViewModel : ObservableObject
	{
		public ValueEditingDialogViewModel()
		{
		}

		private ValueItem _valueItem;
		public ValueItem ValueItem { get => _valueItem; set => SetProperty(ref _valueItem, value); }
	}
}
