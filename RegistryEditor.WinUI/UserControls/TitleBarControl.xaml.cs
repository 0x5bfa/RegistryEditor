// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;

namespace RegistryEditor.WinUI.UserControls
{
    public sealed partial class TitleBarControl : UserControl
    {
        [GeneratedDependencyProperty]
        public partial string? Title { get; set; }

        public TitleBarControl()
        {
            InitializeComponent();
        }
    }
}
