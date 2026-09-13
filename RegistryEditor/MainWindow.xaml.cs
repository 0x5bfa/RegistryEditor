// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml;
using RegistryEditor.Views;

namespace RegistryEditor;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();

        AppWindow.Title = "Registry Valley";
        AppWindow.SetIcon(SystemIO.Path.Combine(
            Windows.ApplicationModel.Package.Current.InstalledLocation.Path,
            "Assets/Branding/AppLogo.ico"));

        Content = new RootView();
    }
}
