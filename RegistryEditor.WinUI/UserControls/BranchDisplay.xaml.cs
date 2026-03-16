// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace RegistryEditor.WinUI.UserControls
{
    public sealed partial class BranchDisplay : UserControl
    {
        [GeneratedDependencyProperty]
        public partial int NumberOfBranch { get; set; }

        [GeneratedDependencyProperty]
        public partial bool HasChildren { get; set; }

        private readonly ObservableCollection<bool> _branches;
        public ReadOnlyObservableCollection<bool> Branches { get; }
        
        public BranchDisplay()
        {
            InitializeComponent();

            _branches = [];
            Branches = new(_branches);
        }

        partial void OnNumberOfBranchPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            _branches.Clear();
            for (int i = 0; i < NumberOfBranch - 1; i++)
                _branches.Add(true);
        }
    }
}
