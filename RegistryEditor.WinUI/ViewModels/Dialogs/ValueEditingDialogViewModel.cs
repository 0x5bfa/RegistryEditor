// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using RegistryEditor.WinUI.Models;

namespace RegistryEditor.WinUI.ViewModels.Dialogs
{
	public partial class ValueEditingDialogViewModel : ObservableObject
	{
		public ValueEditingDialogViewModel()
		{
		}

		private ValueItem _valueItem;
		public ValueItem ValueItem { get => _valueItem; set => SetProperty(ref _valueItem, value); }
	}
}
