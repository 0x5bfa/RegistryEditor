using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RegistryEditor.WinUI.ViewModels;
using RegistryEditor.WinUI.ViewModels.Dialogs;

namespace RegistryEditor.WinUI.Dialogs
{
	public sealed partial class ValueEditingDialog : ContentDialog
	{
		#region propdp
		public static readonly DependencyProperty ViewModelProperty =
			DependencyProperty.Register(
				nameof(ViewModel),
				typeof(ValueEditingDialogViewModel),
				typeof(ValueEditingDialog),
				new PropertyMetadata(null));

		public ValueEditingDialogViewModel ViewModel
		{
			get => (ValueEditingDialogViewModel)GetValue(ViewModelProperty);
			set => SetValue(ViewModelProperty, value);
		}
		#endregion

		public ValueEditingDialog()
			=> InitializeComponent();

		private void OnValueEditorTextBoxTextChanged(object sender, TextChangedEventArgs e)
		{
		}
	}
}
