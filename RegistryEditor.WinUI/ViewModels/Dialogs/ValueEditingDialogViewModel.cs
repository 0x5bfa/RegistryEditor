// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using RegistryEditor.WinUI.Models;

namespace RegistryEditor.WinUI.ViewModels.Dialogs;

public partial class ValueEditingDialogViewModel : ObservableObject
{
    [ObservableProperty]
    public partial ValueItem ValueItem { get; set; }

    public ValueEditingDialogViewModel()
    {
    }
}
