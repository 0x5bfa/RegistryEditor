using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using RegistryEditor.WinUI.Extensions;
using RegistryEditor.WinUI.Helpers;
using RegistryEditor.WinUI.Models;
using RegistryEditor.WinUI.Services;
using RegistryEditor.WinUI.ViewModels;
using RegistryEditor.WinUI.ViewModels.UserControls;

namespace RegistryEditor.WinUI.UserControls
{
	public sealed partial class TreeView : UserControl
	{
		public TreeView()
		{
			InitializeComponent();

			var provider = App.Current.Services;
			ViewModel = provider.GetRequiredService<TreeViewViewModel>();
			ValuesViewerViewModel = provider.GetRequiredService<ValuesViewerViewModel>();
			UserSettingsServices = App.Current.Services.GetRequiredService<UserSettingsServices>();
		}

		#region Fields and Properties
		public TreeViewViewModel ViewModel { get; }
		public ValuesViewerViewModel ValuesViewerViewModel { get; }
		private UserSettingsServices UserSettingsServices { get; }

		public event SelectionChangedEventHandler BaseSelectionChanged;
		public event RoutedEventHandler KeyDeleting;
		public event RoutedEventHandler KeyExporting;
		public event RoutedEventHandler KeyRenaming;
		public event RoutedEventHandler KeyPropertyWindowOpening;
		#endregion

		#region TreeView event methods
		private void CustomMainTreeView_SelectionChanged(object sender, SelectionChangedEventArgs e)
			=> BaseSelectionChanged?.Invoke(sender, e);

		private void ExpandCollapseButton_Click(object sender, RoutedEventArgs e)
		{
			var item = (KeyItem)((Button)sender).DataContext;

			if (!item.IsExpanded)
				ViewModel.ExpandChildren(item);
			else
				ViewModel.CollapseChildren(item);
		}
		#endregion

		#region MenuFlyout event methods
		private void KeyTreeViewItemMenuFlyout_Opening(object sender, object e)
		{
			var flyout = (MenuFlyout)sender;
			var target = (Grid)flyout.Target;
			var item = (KeyItem)target.DataContext;

			if (item.SelectedRootComputer && flyout.Items.Count > 5)
			{
				// TODO: More reliable way
				flyout.Items.RemoveAt(1);
				flyout.Items.RemoveAt(1);
				flyout.Items.RemoveAt(1);
				flyout.Items.RemoveAt(1);
				flyout.Items.RemoveAt(1);
				flyout.Items.RemoveAt(3);
				flyout.Items.RemoveAt(3);
				flyout.Items.RemoveAt(3);
				return;
			}
		}

		private void KeyTreeViewItemMenuFlyout_Opened(object sender, object e)
		{
			var flyout = (MenuFlyout)sender;
			var target = (Grid)flyout.Target;
			var item = (KeyItem)target.DataContext;

			SelectItem(item);
		}

		private void KeyTreeViewItemMenuFlyoutExpand_Click(object sender, RoutedEventArgs e)
		{
			var item = (KeyItem)((MenuFlyoutItem)sender).DataContext;
			ViewModel.ExpandChildren(item);
		}

		private void KeyTreeViewItemMenuFlyoutCollapse_Click(object sender, RoutedEventArgs e)
		{
			var item = (KeyItem)((MenuFlyoutItem)sender).DataContext;
			ViewModel.CollapseChildren(item);
		}

		private void KeyTreeViewItemMenuFlyoutNew_Click(object sender, RoutedEventArgs e)
		{
			var item = (KeyItem)CustomMainTreeView.SelectedItem;
			var itemIndex = CustomMainTreeView.SelectedIndex + 1;

			if (!item.IsExpanded && item.HasChildren)
				ViewModel.ExpandChildren(item);

			item.HasChildren = true;

			// TODO: Should check if this name is already exist.
			string keyName = "New Key #1";

			var defaultNewKeyItem = new KeyItem()
			{
				Name = keyName,
				RootHive = item.RootHive,
				BasePath = item.Path,
				IsDeletable = true,
				IsRenamable = true,
				IsRenaming = true,
				HasChildren = false,
				Image = "ms-appx:///Assets/Images/Folder.png",
				Depth = item.Depth + 1,
				Parent = item,
			};

			ViewModel.CreatingNewKey = true;
			ViewModel.FlatKeyItems.Insert(itemIndex, defaultNewKeyItem);
			CustomMainTreeView.SelectedIndex++;
		}

		private void KeyTreeViewItemMenuFlyoutDelete_Click(object sender, RoutedEventArgs e)
			=> KeyDeleting?.Invoke(sender, e);

		private void KeyTreeViewItemMenuFlyoutRename_Click(object sender, RoutedEventArgs e)
			=> ((KeyItem)CustomMainTreeView.SelectedItem).IsRenaming = true;

		private void KeyTreeViewItemMenuFlyoutExport_Click(object sender, RoutedEventArgs e)
			=> KeyExporting?.Invoke(sender, e);

		private void KeyTreeViewItemMenuFlyoutPermissions_Click(object sender, RoutedEventArgs e)
			=> KeyPropertyWindowOpening?.Invoke(sender, e);

		private void KeyTreeViewItemMenuFlyoutCopyKeyName_Click(object sender, RoutedEventArgs e)
		{
			var item = (KeyItem)((MenuFlyoutItem)sender).DataContext;
			ClipBoardHelpers.SetContent(item.PathForPwsh);
		}
		#endregion

		#region TextBox event for renaming
		private void KeyItemNameRenamingTextBox_Loaded(object sender, RoutedEventArgs e)
		{
			var textBox = (TextBox)sender;
			textBox.Focus(FocusState.Programmatic);
			textBox.SelectAll();
		}

		private void KeyItemNameRenamingTextBox_LostFocus(object sender, RoutedEventArgs e)
		{
			var textBox = (TextBox)sender;
			var item = (KeyItem)CustomMainTreeView.SelectedItem;

			if (item.Name != textBox.Text || ViewModel.CreatingNewKey)
			{
				ViewModel.CreatingNewKey = false;
				ViewModel.LastRenamedNewName = textBox.Text;

				KeyRenaming?.Invoke(textBox.Text, e);
			}

			item.IsRenaming = false;
		}

		private void KeyItemNameRenamingTextBox_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
		{
			if (e.Key == Windows.System.VirtualKey.Enter)
			{
				var textBox = (TextBox)sender;
				var item = (KeyItem)CustomMainTreeView.SelectedItem;

				if (item.Name != textBox.Text)
				{
					ViewModel.CreatingNewKey = false;
					ViewModel.LastRenamedNewName = textBox.Text;

					KeyRenaming?.Invoke(sender, e);
				}

				item.IsRenaming = false;
			}
		}
		#endregion

		#region Public methods for MainPage
		public void UnselectItem()
		{
			CustomMainTreeView.SelectedIndex = -1;
		}

		public void SelectItem(KeyItem item)
		{
			int index = ((ObservableCollection<KeyItem>)CustomMainTreeView.ItemsSource).IndexOf(item);
			CustomMainTreeView.SelectedIndex = index;
		}

		public KeyItem GetSelectedItem()
		{
			return (KeyItem)CustomMainTreeView.SelectedItem;
		}

		public void RemoveItem(KeyItem item)
		{
			ViewModel.FlatKeyItems.Remove(item);
		}

		public void RemoveItemRecursively(KeyItem item)
		{
			int startIndex = ViewModel.FlatKeyItems.IndexOf(item);
			int depth = item.Depth;

			var list = ViewModel.FlatKeyItems.Where(x => x.Depth > depth && ViewModel.FlatKeyItems.IndexOf(x) > startIndex).ToList();

			if (list.Count != 0)
			{
				var lastRemovedItemIndex = ViewModel.FlatKeyItems.IndexOf(list.First());

				// Remove children recursively
				foreach (var listItem in list)
				{
					if (lastRemovedItemIndex == ViewModel.FlatKeyItems.IndexOf(listItem))
						ViewModel.FlatKeyItems.Remove(listItem);
					else
						break;
				}
			}

			// Remove itself
			RemoveItem(item);
		}
		#endregion
	}
}
