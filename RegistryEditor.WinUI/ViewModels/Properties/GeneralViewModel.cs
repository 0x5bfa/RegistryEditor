// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using RegistryEditor.WinUI.Models;
using System.Runtime.InteropServices;
using System.Security.AccessControl;

namespace RegistryEditor.WinUI.ViewModels.Properties
{
	public partial class GeneralViewModel : ObservableObject
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
