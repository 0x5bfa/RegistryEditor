using RegistryValley.App.Models;

namespace RegistryValley.App.Data
{
    public class DefaultKeyItemFactory
    {
        public static ObservableCollection<KeyItem> DefaultNestedKeyTree { get; } = new()
        {
            new()
            {
                Name = "Computer",
                RootHive = HKEY.NULL,
                IsExpanded = true,
                BasePath = "",
                IsDeletable = false,
                IsRenamable = false,
                HasChildren = true,
                SelectedRootComputer = true,
                Image = "ms-appx:///Assets/Images/Computer.png",
                Depth = 1,
                Children = new()
                {
                    new()
                    {
                        Name = "HKEY_CLASSES_ROOT",
                        RootHive = HKEY.HKEY_CLASSES_ROOT,
                        BasePath = "",
                        IsDeletable = false,
                        IsRenamable = false,
                        HasChildren = true,
                        Depth = 2,
                        Image = "ms-appx:///Assets/Images/Folder.png"
                    },
                    new()
                    {
                        Name = "HKEY_CURRENT_USER",
                        RootHive = HKEY.HKEY_CURRENT_USER,
                        BasePath = "",
                        IsDeletable = false,
                        IsRenamable = false,
                        HasChildren = true,
                        Depth = 2,
                        Image = "ms-appx:///Assets/Images/Folder.png"
                    },
                    new()
                    {
                        Name = "HKEY_LOCAL_MACHINE",
                        RootHive = HKEY.HKEY_LOCAL_MACHINE,
                        BasePath = "",
                        IsDeletable = false,
                        IsRenamable = false,
                        HasChildren = true,
                        Depth = 2,
                        Image = "ms-appx:///Assets/Images/Folder.png"
                    },
                    new()
                    {
                        Name = "HKEY_USERS",
                        RootHive = HKEY.HKEY_USERS,
                        BasePath = "",
                        IsDeletable = false,
                        IsRenamable = false,
                        HasChildren = true,
                        Depth = 2,
                        Image = "ms-appx:///Assets/Images/Folder.png"
                    },
                    new()
                    {
                        Name = "HKEY_CURRENT_CONFIG",
                        RootHive = HKEY.HKEY_CURRENT_CONFIG,
                        BasePath = "",
                        IsDeletable = false,
                        IsRenamable = false,
                        HasChildren = true,
                        Depth = 2,
                        Image = "ms-appx:///Assets/Images/Folder.png"
                    }
                },
            }
        };

        public static ObservableCollection<KeyItem> DefaultFlattenedKeyTree { get; } = new()
        {
            new()
            {
                Name = "Computer",
                RootHive = HKEY.NULL,
                IsExpanded = true,
                BasePath = "",
                IsDeletable = false,
                IsRenamable = false,
                HasChildren = true,
                SelectedRootComputer = true,
                Depth = 1,
                Image = "ms-appx:///Assets/Images/Computer.png",
            },

            new()
            {
                Name = "HKEY_CLASSES_ROOT",
                RootHive = HKEY.HKEY_CLASSES_ROOT,
                BasePath = "",
                IsDeletable = false,
                IsRenamable = false,
                HasChildren = true,
                Depth = 2,
                Image = "ms-appx:///Assets/Images/Folder.png"
            },
            new()
            {
                Name = "HKEY_CURRENT_USER",
                RootHive = HKEY.HKEY_CURRENT_USER,
                BasePath = "",
                IsDeletable = false,
                IsRenamable = false,
                HasChildren = true,
                Depth = 2,
                Image = "ms-appx:///Assets/Images/Folder.png"
            },
            new()
            {
                Name = "HKEY_LOCAL_MACHINE",
                RootHive = HKEY.HKEY_LOCAL_MACHINE,
                BasePath = "",
                IsDeletable = false,
                IsRenamable = false,
                HasChildren = true,
                Depth = 2,
                Image = "ms-appx:///Assets/Images/Folder.png"
            },
            new()
            {
                Name = "HKEY_USERS",
                RootHive = HKEY.HKEY_USERS,
                BasePath = "",
                IsDeletable = false,
                IsRenamable = false,
                HasChildren = true,
                Depth = 2,
                Image = "ms-appx:///Assets/Images/Folder.png"
            },
            new()
            {
                Name = "HKEY_CURRENT_CONFIG",
                RootHive = HKEY.HKEY_CURRENT_CONFIG,
                BasePath = "",
                IsDeletable = false,
                IsRenamable = false,
                HasChildren = true,
                Depth = 2,
                Image = "ms-appx:///Assets/Images/Folder.png"
            },
        };
    }
}
