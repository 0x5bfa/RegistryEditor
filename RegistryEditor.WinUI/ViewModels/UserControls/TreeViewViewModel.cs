using RegistryEditor.WinUI.Services;
using RegistryEditor.WinUI.Models;

namespace RegistryEditor.WinUI.ViewModels.UserControls
{
	public class TreeViewViewModel
	{
		public TreeViewViewModel()
		{
			KeyItems = Data.DefaultKeyItemFactory.DefaultNestedKeyTree;
			FlatKeyItems = Data.DefaultKeyItemFactory.DefaultFlattenedKeyTree;
		}

		#region Fields and Properties
		public ObservableCollection<KeyItem> KeyItems { get; }
		public ObservableCollection<KeyItem> FlatKeyItems { get; }

		public string LastRenamedNewName { get; set; }
		public bool CreatingNewKey { get; set; }
		#endregion

		#region Methods
		public void ExpandChildren(KeyItem item)
		{
			item.IsExpanded = true;
			int index = FlatKeyItems.IndexOf(item);

			var flattenedKeyItemNodeTree = GetFlattenNodes(KeyItems);

			var itemFromFlattenedTreeItem = flattenedKeyItemNodeTree.Where(x => x.PathForPwsh == item.PathForPwsh).FirstOrDefault();
			itemFromFlattenedTreeItem.IsExpanded = true;

			if (item.Depth != 1)
				GetChildItems(itemFromFlattenedTreeItem);

			index++;
			foreach (var child in itemFromFlattenedTreeItem.Children)
			{
				FlatKeyItems.Insert(index, child);
				index++;
			}
		}

		public void CollapseChildren(KeyItem item)
		{
			item.IsExpanded = false;
			int index = FlatKeyItems.IndexOf(item);

			var flattenedKeyItemNodeTree = GetFlattenNodes(KeyItems);

			var itemFromFlattenedTree = flattenedKeyItemNodeTree.Where(x => x.PathForPwsh == item.PathForPwsh).First();
			itemFromFlattenedTree.IsExpanded = false;

			if (item.Depth != 1)
				RemoveChildItems(itemFromFlattenedTree);

			RemoveAll(item.Depth, index);
		}

		private IEnumerable<KeyItem> EnumerateRegistryKeys(HKEY hRootKey, string subRoot, KeyItem parent)
		{
			List<KeyItem> keys = new();
			Win32Error result;

			// Calling Win32API
			result = RegOpenKeyEx(hRootKey, subRoot, 0, REGSAM.KEY_READ, out var phKey);
			if (result.Failed)
			{
				Kernel32.SetLastError((uint)result);
				result.ThrowIfFailed();
				return null;
			}

			// Calling Win32API
			result = RegQueryInfoKey(phKey, null, ref NullRef<uint>(), default, out uint cSubKeys, out uint cbMaxSubKeyLen, out _, out _, out _, out _, out _, out _);
			if (result.Failed)
			{
				Kernel32.SetLastError((uint)result);
				result.ThrowIfFailed();
				return null;
			}

			StringBuilder szName;
			uint cchName;

			for (uint dwIndex = 0; dwIndex < cSubKeys; dwIndex++)
			{
				cchName = cbMaxSubKeyLen + 1;
				szName = new((int)cchName, (int)cchName);

				// Calling Win32API
				result = RegEnumKeyEx(phKey, dwIndex, szName, ref cchName, default, null, ref NullRef<uint>(), out _);
				if (result.Failed)
				{
					Kernel32.SetLastError((uint)result);
					result.ThrowIfFailed();
					return null;
				}

				var subKeyPath = subRoot == "" ? $"{szName}" : $"{subRoot}\\{szName}";

				// Calling Win32API
				result = HasSubKeys(hRootKey, subKeyPath, out var hasChildren);
				if (result.Failed && result != Win32Error.ERROR_ACCESS_DENIED)
				{
					Kernel32.SetLastError((uint)result);
					result.ThrowIfFailed();
					return default;
				}

				keys.Add(new()
				{
					Name = szName.ToString(),
					RootHive = hRootKey,
					BasePath = subRoot,
					IsDeletable = true,
					IsRenamable = true,
					HasChildren = hasChildren,
					Image = "ms-appx:///Assets/Images/Folder.png",
					Depth = parent.Depth + 1,
					Parent = parent,
				});
			}

			return keys;
		}

		private Win32Error HasSubKeys(HKEY hRootKey, string subRoot, out bool hasChildren)
		{
			Win32Error result;
			hasChildren = false;

			// Calling Win32API
			result = RegOpenKeyEx(hRootKey, subRoot, 0, REGSAM.KEY_READ, out var phKey);
			if (result.Failed)
			{
				return result;
			}

			// Calling Win32API
			result = RegQueryInfoKey(phKey, null, ref NullRef<uint>(), default, out uint cSubKeys, out _, out _, out _, out _, out _, out _, out _);
			if (result.Failed)
			{
				return result;
			}

			if (cSubKeys == 0)
				hasChildren = false;
			else
				hasChildren = true;

			return Win32Error.ERROR_SUCCESS;
		}

		private void GetChildItems(KeyItem item)
		{
			if (item.Depth == 1)
				return;

			item.Children.Clear();

			var children = EnumerateRegistryKeys(item.RootHive, item.Path, item);
			if (children != null)
			{
				foreach (var child in children)
					item.Children.Add(child);
			}
			else
			{
				// Error handling
				var result = Kernel32.GetLastError();
			}
		}

		private void RemoveChildItems(KeyItem item)
		{
			item.Children.Clear();
			item.HasChildren = true;
		}

		private void RemoveAll(int depth, int startIndex)
		{
			int lastRemovedItemIndex = 0;
			var list = FlatKeyItems.Where(x => x.Depth > depth && FlatKeyItems.IndexOf(x) > startIndex).ToList();
			lastRemovedItemIndex = FlatKeyItems.IndexOf(list.First());

			foreach (var item in list)
			{
				if (lastRemovedItemIndex == FlatKeyItems.IndexOf(item))
					FlatKeyItems.Remove(item);
				else
					break;
			}
		}

		private IEnumerable<KeyItem> GetFlattenNodes(IEnumerable<KeyItem> masterList)
		{
			foreach (var node in masterList)
			{
				yield return node;

				foreach (var children in GetFlattenNodes(node.Children))
				{
					yield return children;
				}
			}
		}
		#endregion
	}
}
