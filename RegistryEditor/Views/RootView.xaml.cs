using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using RegistryEditor.Controls;
using RegistryEditor.Controls.Primitives;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RegistryEditor.Views;

public sealed partial class RootView : UserControl
{
    private const string ComputerImageUri = "ms-appx:///Assets/Images/Computer.png";
    private const string FolderImageUri = "ms-appx:///Assets/Images/Folder.png";
    private const string FolderOpenedImageUri = "ms-appx:///Assets/Images/FolderOpened.png";

    public RootView()
    {
        InitializeComponent();
        InitializeToolbar();
        InitializeRegistryTree();
    }

    private void InitializeToolbar()
    {
        NavigationToolbar.Items =
        [
            CreateNavigationItem("Back", "\uE72B"),
            CreateNavigationItem("Forward", "\uE72A"),
            CreateNavigationItem("Refresh", "\uE72C"),
        ];
    }

    private static ToolbarItem CreateNavigationItem(string label, string glyph)
    {
        return new ToolbarItem
        {
            ItemType = ToolbarItemTypes.Button,
            Label = label,
            OverflowBehavior = OverflowBehaviors.Never,
            Content = new FontIcon
            {
                Glyph = glyph,
                FontSize = 16,
            },
        };
    }

    private void InitializeRegistryTree()
    {
        TreeViewNode computerNode = CreateNode("Computer", null, string.Empty, ComputerImageUri, hasChildren: false);
        computerNode.IsExpanded = true;

        computerNode.Children.Add(CreateNode("HKEY_CLASSES_ROOT", RegistryHive.ClassesRoot, string.Empty, FolderImageUri, hasChildren: true));
        computerNode.Children.Add(CreateNode("HKEY_CURRENT_USER", RegistryHive.CurrentUser, string.Empty, FolderImageUri, hasChildren: true));
        computerNode.Children.Add(CreateNode("HKEY_LOCAL_MACHINE", RegistryHive.LocalMachine, string.Empty, FolderImageUri, hasChildren: true));
        computerNode.Children.Add(CreateNode("HKEY_USERS", RegistryHive.Users, string.Empty, FolderImageUri, hasChildren: true));
        computerNode.Children.Add(CreateNode("HKEY_CURRENT_CONFIG", RegistryHive.CurrentConfig, string.Empty, FolderImageUri, hasChildren: true));

        RegistryTreeView.RootNodes.Add(computerNode);
        RegistryTreeView.SelectedNode = computerNode;
        UpdateBreadcrumb(computerNode);
    }

    private TreeViewNode CreateNode(string name, RegistryHive? hive, string subKeyPath, string imageUri, bool hasChildren)
    {
        return new TreeViewNode
        {
            Content = new RegistryNodeInfo(name, hive, subKeyPath, CreateBitmapImage(imageUri)),
            HasUnrealizedChildren = hasChildren,
        };
    }

    private void RegistryTreeView_Expanding(TreeView sender, TreeViewExpandingEventArgs args)
    {
        if (args.Node.Content is not RegistryNodeInfo info)
            return;

        if (info.Hive is not null)
            info.Icon = CreateBitmapImage(FolderOpenedImageUri);

        if (!args.Node.HasUnrealizedChildren || info.Hive is null)
            return;

        PopulateChildren(args.Node, info);
    }

    private void RegistryTreeView_Collapsed(TreeView sender, TreeViewCollapsedEventArgs args)
    {
        if (args.Node.Content is RegistryNodeInfo info && info.Hive is not null)
            info.Icon = CreateBitmapImage(FolderImageUri);
    }

    private void PopulateChildren(TreeViewNode node, RegistryNodeInfo info)
    {
        node.HasUnrealizedChildren = false;

        try
        {
            using RegistryKey baseKey = RegistryKey.OpenBaseKey(info.Hive!.Value, RegistryView.Default);
            using RegistryKey? key = string.IsNullOrEmpty(info.SubKeyPath)
                ? baseKey
                : baseKey.OpenSubKey(info.SubKeyPath, writable: false);

            if (key is null)
                return;

            foreach (string subKeyName in key.GetSubKeyNames().OrderBy(name => name, StringComparer.OrdinalIgnoreCase))
            {
                string path = string.IsNullOrEmpty(info.SubKeyPath)
                    ? subKeyName
                    : $"{info.SubKeyPath}\\{subKeyName}";

                bool hasChildren = true;
                try
                {
                    using RegistryKey? childKey = key.OpenSubKey(subKeyName, writable: false);
                    hasChildren = childKey?.SubKeyCount > 0;
                }
                catch (UnauthorizedAccessException)
                {
                    // Keep the expansion affordance when access is restricted.
                }
                catch (System.Security.SecurityException)
                {
                    // Keep the expansion affordance when access is restricted.
                }

                node.Children.Add(CreateNode(subKeyName, info.Hive, path, FolderImageUri, hasChildren));
            }
        }
        catch (UnauthorizedAccessException)
        {
        }
        catch (System.Security.SecurityException)
        {
        }
        catch (SystemIO.IOException)
        {
        }
    }

    private void RegistryTreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
        if (RegistryTreeView.SelectedNode is { } selectedNode)
            UpdateBreadcrumb(selectedNode);
    }

    private void UpdateBreadcrumb(TreeViewNode node)
    {
        if (node.Content is not RegistryNodeInfo info)
            return;

        if (info.Hive is null)
        {
            PathBreadcrumbBar.RootItem = CreateBreadcrumbRootItem("Computer");
            PathBreadcrumbBar.ItemsSource = Array.Empty<RegistryEditor.Controls.BreadcrumbBarItem>();
            return;
        }

        PathBreadcrumbBar.RootItem = CreateBreadcrumbRootItem(GetHiveName(info.Hive.Value));

        string[] segments = string.IsNullOrEmpty(info.SubKeyPath)
            ? []
            : info.SubKeyPath.Split('\\');

        PathBreadcrumbBar.ItemsSource = segments
            .Select((segment, index) => new RegistryEditor.Controls.BreadcrumbBarItem
            {
                Content = segment,
                ItemToolTip = segment,
                IsChevronVisible = index != segments.Length - 1,
            })
            .ToArray();
    }

    private static FrameworkElement CreateBreadcrumbRootItem(string text)
        => new TextBlock
        {
            Text = text,
            VerticalAlignment = VerticalAlignment.Center,
        };

    private static string GetHiveName(RegistryHive hive) => hive switch
    {
        RegistryHive.ClassesRoot => "HKEY_CLASSES_ROOT",
        RegistryHive.CurrentUser => "HKEY_CURRENT_USER",
        RegistryHive.LocalMachine => "HKEY_LOCAL_MACHINE",
        RegistryHive.Users => "HKEY_USERS",
        RegistryHive.CurrentConfig => "HKEY_CURRENT_CONFIG",
        _ => hive.ToString(),
    };

    private static BitmapImage CreateBitmapImage(string uri) => new(new Uri(uri));
}

public sealed class RegistryNodeInfo : INotifyPropertyChanged
{
    private BitmapImage _icon;

    public RegistryNodeInfo(string name, RegistryHive? hive, string subKeyPath, BitmapImage icon)
    {
        Name = name;
        Hive = hive;
        SubKeyPath = subKeyPath;
        _icon = icon;
    }

    public string Name { get; }

    public RegistryHive? Hive { get; }

    public string SubKeyPath { get; }

    public BitmapImage Icon
    {
        get => _icon;
        set
        {
            if (ReferenceEquals(_icon, value))
                return;

            _icon = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
