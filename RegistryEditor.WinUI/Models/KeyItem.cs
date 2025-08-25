namespace RegistryEditor.WinUI.Models
{
	public class KeyItem : ObservableObject
	{
		private string _name;
		public string Name { get => _name; set => SetProperty(ref _name, value); }

		private string _basePath;
		public string BasePath { get => _basePath; set => SetProperty(ref _basePath, value); }

		public string Path
			=> string.IsNullOrEmpty(BasePath) && Name.StartsWith("HKEY") ? "" : (string.IsNullOrEmpty(BasePath) ? Name : $"{BasePath}\\{Name}");

		public string PathForPwsh
		{
			get
			{
				string displayPath;

				if (string.IsNullOrEmpty(Path))
				{
					if (RootHive == HKEY.HKEY_CLASSES_ROOT)
						displayPath = $"HKEY_CLASSES_ROOT";
					else if (RootHive == HKEY.HKEY_CURRENT_CONFIG)
						displayPath = $"HKEY_CURRENT_CONFIG";
					else if (RootHive == HKEY.HKEY_CURRENT_USER)
						displayPath = $"HKEY_CURRENT_USER";
					else if (RootHive == HKEY.HKEY_LOCAL_MACHINE)
						displayPath = $"HKEY_LOCAL_MACHINE";
					else if (RootHive == HKEY.HKEY_USERS)
						displayPath = $"HKEY_USERS";
					else
						displayPath = $"No path";
				}
				else
				{
					var path = Path.TrimEnd('\\');

					if (RootHive == HKEY.HKEY_CLASSES_ROOT)
						displayPath = $"HKCR:\\{path}";
					else if (RootHive == HKEY.HKEY_CURRENT_CONFIG)
						displayPath = $"HKCC:\\{path}";
					else if (RootHive == HKEY.HKEY_CURRENT_USER)
						displayPath = $"HKCU:\\{path}";
					else if (RootHive == HKEY.HKEY_LOCAL_MACHINE)
						displayPath = $"HKLM:\\{path}";
					else if (RootHive == HKEY.HKEY_USERS)
						displayPath = $"HKU:\\{path}";
					else
						displayPath = $"{path}";
				}

				return displayPath;
			}
		}

		public string PathForCmd
		{
			get
			{
				string displayPath;

				// TODO: Fix following redundant code

				if (string.IsNullOrEmpty(Path))
				{
					if (RootHive == HKEY.HKEY_CLASSES_ROOT)
						displayPath = $"HKCR";
					else if (RootHive == HKEY.HKEY_CURRENT_CONFIG)
						displayPath = $"HKCC";
					else if (RootHive == HKEY.HKEY_CURRENT_USER)
						displayPath = $"HKCU";
					else if (RootHive == HKEY.HKEY_LOCAL_MACHINE)
						displayPath = $"HKLM";
					else if (RootHive == HKEY.HKEY_USERS)
						displayPath = $"HKU";
					else
						displayPath = $"";
				}
				else
				{
					var path = Path.TrimEnd('\\');

					if (RootHive == HKEY.HKEY_CLASSES_ROOT)
						displayPath = $"HKCR\\{path}";
					else if (RootHive == HKEY.HKEY_CURRENT_CONFIG)
						displayPath = $"HKCC\\{path}";
					else if (RootHive == HKEY.HKEY_CURRENT_USER)
						displayPath = $"HKCU\\{path}";
					else if (RootHive == HKEY.HKEY_LOCAL_MACHINE)
						displayPath = $"HKLM\\{path}";
					else if (RootHive == HKEY.HKEY_USERS)
						displayPath = $"HKU\\{path}";
					else
						displayPath = $"{path}";
				}

				return displayPath;
			}
		}

		private bool _isExpanded;
		public bool IsExpanded { get => _isExpanded; set => SetProperty(ref _isExpanded, value); }

		private HKEY _rootHive;
		public HKEY RootHive { get => _rootHive; set => SetProperty(ref _rootHive, value); }

		private string _image;
		public string Image { get => _image; set => SetProperty(ref _image, value); }

		private bool _hasChildren;
		public bool HasChildren { get => _hasChildren; set => SetProperty(ref _hasChildren, value); }

		private bool _isDeletable;
		public bool IsDeletable { get => _isDeletable; set => SetProperty(ref _isDeletable, value); }

		private bool _isRenamable;
		public bool IsRenamable { get => _isRenamable; set => SetProperty(ref _isRenamable, value); }

		private bool _isRenaming;
		public bool IsRenaming { get => _isRenaming; set => SetProperty(ref _isRenaming, value); }

		private bool _selectedRootComputer;
		public bool SelectedRootComputer { get => _selectedRootComputer; set => SetProperty(ref _selectedRootComputer, value); }

		private KeyItem _parent;
		public KeyItem Parent { get => _parent; set => SetProperty(ref _parent, value); }

		public ObservableCollection<KeyItem> Children { get; set; } = new();

		private int _subKeysCount;
		public int SubKeysCount { get => _subKeysCount; set => SetProperty(ref _subKeysCount, value); }

		private int _valuesCount;
		public int ValuesCount { get => _valuesCount; set => SetProperty(ref _valuesCount, value); }

		private DateTime _createdAt;
		public DateTime CreatedAt { get => _createdAt; set => SetProperty(ref _createdAt, value); }

		private int _depth;
		public int Depth { get => _depth; set => SetProperty(ref _depth, value); }

		public override string ToString()
			=> Name;
	}
}
