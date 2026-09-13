// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Windows.Foundation;

namespace RegistryEditor.Controls
{
	public partial class Toolbar : Control
	{
		private const string CaptionTextBlockStyleResourceKey = "CaptionTextBlockStyle";
		private const string LabelButtonPaddingResourceKey = "ToolbarLabelButtonPadding";
		private const double DefaultItemSpacing = 4d;
		private const double DefaultOverflowButtonWidth = 40d;

		private readonly List<MaterializedToolbarItem> _materializedItems = [];
		private readonly List<ToolbarItem> _subscribedItems = [];
		private readonly Dictionary<ToolbarItem, MaterializedToolbarItem> _itemOwners = [];
		private readonly Dictionary<FrameworkElement, ToggleEventHandlers> _toggleEventHandlers = [];
		private readonly Dictionary<FrameworkElement, MenuFlyout> _generatedFlyouts = [];
		private List<ToolbarItem> _overflowItems = [];

		private ToolbarItemsPanel? _itemsPanel;
		private StackPanel? _overflowStackPanel;
		private ToolbarFlyoutButton? _overflowButton;
		private MenuFlyout? _overflowFlyout;
		private INotifyCollectionChanged? _itemsCollection;
		private IList<ToolbarItem>? _observedItems;
		private bool _overflowMenuDirty = true;
		private bool _isUpdatingLayoutPartition;
		private bool _templateApplied;

		private double _smallMinWidth = 32;
		private double _mediumMinWidth = 32;
		private double _largeMinWidth = 40;
		private double _smallMinHeight = 32;
		private double _mediumMinHeight = 40;
		private double _largeMinHeight = 40;
		private double _currentMinWidth = 32;
		private double _currentMinHeight = 40;

		/// <summary>
		/// Initializes a new toolbar.
		/// </summary>
		public Toolbar()
		{
			DefaultStyleKey = typeof(Toolbar);
			Items = [];
			Loaded += OnLoaded;
			Unloaded += OnUnloaded;
			UpdateMinSizesFromResources();
		}

		protected override void OnApplyTemplate()
		{
			if (_overflowFlyout is not null)
			{
				_overflowFlyout.Opening -= OnOverflowFlyoutOpening;
				_overflowFlyout.Hide();
				_overflowFlyout.Items.Clear();
			}

			if (_itemsPanel is not null)
			{
				_itemsPanel.Children.Clear();
				_itemsPanel.SetVisibleChildren(Array.Empty<UIElement>());
			}

			ReleaseMaterializedItems();
			_materializedItems.Clear();
			_generatedFlyouts.Clear();

			_templateApplied = false;
			base.OnApplyTemplate();

			_itemsPanel = GetTemplateChild(ToolbarItemsPanelPartName) as ToolbarItemsPanel;
			_overflowStackPanel = GetTemplateChild(OverflowStackPanelPartName) as StackPanel;
			_overflowButton = GetTemplateChild(OverflowButtonPartName) as ToolbarFlyoutButton;
			_overflowFlyout = GetTemplateChild(OverflowFlyoutPartName) as MenuFlyout ?? _overflowButton?.Flyout as MenuFlyout;

			if (_overflowFlyout is not null)
			{
				_overflowFlyout.Opening += OnOverflowFlyoutOpening;
			}

			if (_overflowButton is not null)
			{
				_overflowButton.AllowFocusOnInteraction = AllowFocusOnInteraction;
				_overflowButton.Label = OverflowButtonLabel;
			}

			_templateApplied = true;
			RebuildMaterializedItems();
			InvalidateMeasure();
		}

		protected override Size MeasureOverride(Size availableSize)
		{
			if (!_templateApplied || _itemsPanel is null || _isUpdatingLayoutPartition)
			{
				return base.MeasureOverride(availableSize);
			}

			_isUpdatingLayoutPartition = true;
			try
			{
				UpdateLayoutPartition(GetAvailableContentWidth(availableSize.Width));
			}
			finally
			{
				_isUpdatingLayoutPartition = false;
			}

			return base.MeasureOverride(availableSize);
		}

		private void OnLoaded(object sender, RoutedEventArgs args)
		{
			ItemsChanged(Items);
			InvalidateMeasure();
		}

		private void OnUnloaded(object sender, RoutedEventArgs args)
		{
			_overflowFlyout?.Hide();
		}

		private void ItemsChanged(IList<ToolbarItem>? newItems)
		{
			if (ReferenceEquals(_observedItems, newItems))
			{
				return;
			}

			ClearItemSubscriptions();
			if (_itemsCollection is not null)
			{
				_itemsCollection.CollectionChanged -= OnItemsCollectionChanged;
			}

			_observedItems = newItems;
			_itemsCollection = newItems as INotifyCollectionChanged;
			if (_itemsCollection is not null)
			{
				_itemsCollection.CollectionChanged += OnItemsCollectionChanged;
			}

			RebuildMaterializedItems();
			InvalidateMeasure();
		}

		private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
		{
			RebuildMaterializedItems();
			InvalidateMeasure();
		}

		private void RebuildMaterializedItems()
		{
			if (_itemsPanel is not null)
			{
				_itemsPanel.Children.Clear();
				_itemsPanel.SetVisibleChildren(Array.Empty<UIElement>());
			}

			ReleaseMaterializedItems();
			ClearItemSubscriptions();
			_materializedItems.Clear();
			_overflowMenuDirty = true;

			if (_templateApplied && _itemsPanel is not null && Items is not null)
			{
				foreach (var item in Items)
				{
					if (item is null)
					{
						continue;
					}

					var element = CreateToolbarItem(item);
					var host = new ContentPresenter { Content = element, ContentTemplate = ItemTemplate };
					var materializedItem = new MaterializedToolbarItem(item, element, host);
					_materializedItems.Add(materializedItem);
					_itemsPanel.Children.Add(host);
					SubscribeToItemTree(item, materializedItem);
				}
			}

			_overflowItems = [];
		}

		private void ReleaseMaterializedItems()
		{
			foreach (var materializedItem in _materializedItems)
			{
				if (_toggleEventHandlers.Remove(materializedItem.Element, out var handlers))
				{
					if (materializedItem.Element is ToolbarToggleButton toggleButton)
					{
						toggleButton.Checked -= handlers.Checked;
						if (handlers.Unchecked is not null)
						{
							toggleButton.Unchecked -= handlers.Unchecked;
						}
					}
					else if (materializedItem.Element is ToolbarRadioButton radioButton)
					{
						radioButton.Checked -= handlers.Checked;
					}
				}

				if (_generatedFlyouts.Remove(materializedItem.Element, out var generatedFlyout))
				{
					generatedFlyout.Hide();
					generatedFlyout.Items.Clear();
				}

				if (materializedItem.Element is ToolbarButton toolbarButton)
				{
					toolbarButton.Flyout?.Hide();
					toolbarButton.Flyout = null;
				}
				else if (materializedItem.Element is ToolbarSplitButton splitButton)
				{
					splitButton.Flyout?.Hide();
					splitButton.Flyout = null;
				}

				if (materializedItem.Element is ContentControl contentControl)
				{
					contentControl.Content = null;
				}

				materializedItem.Host.Content = null;
			}
		}

		private void SubscribeToItemTree(ToolbarItem item, MaterializedToolbarItem owner)
		{
			item.PropertyChanged += OnToolbarItemPropertyChanged;
			_subscribedItems.Add(item);
			_itemOwners[item] = owner;

			if (item.SubItems is null)
			{
				return;
			}

			foreach (var subItem in item.SubItems)
			{
				if (subItem is not null)
				{
					SubscribeToItemTree(subItem, owner);
				}
			}
		}

		private void ClearItemSubscriptions()
		{
			foreach (var item in _subscribedItems)
			{
				item.PropertyChanged -= OnToolbarItemPropertyChanged;
			}

			_subscribedItems.Clear();
			_itemOwners.Clear();
		}

		private void OnToolbarItemPropertyChanged(object? sender, PropertyChangedEventArgs args)
		{
			if (sender is not ToolbarItem item)
			{
				return;
			}

			_overflowMenuDirty = true;
			if (!_itemOwners.TryGetValue(item, out var materializedItem))
			{
				return;
			}

			if (ReferenceEquals(item, materializedItem.Item) && args.PropertyName == nameof(ToolbarItem.ItemType))
			{
				RebuildMaterializedItems();

				InvalidateMeasure();

				return;
			}

			if (args.PropertyName == nameof(ToolbarItem.SubItems))
			{
				RefreshItemSubscriptions();
			}

			if (ReferenceEquals(item, materializedItem.Item))
			{
				ApplyToolbarItemProperty(materializedItem.Element, materializedItem.Item, args.PropertyName);
			}
			else
			{
				ApplyToolbarItemFlyout(materializedItem.Element, materializedItem.Item);
			}

			InvalidateMeasure();
		}

		private void RefreshItemSubscriptions()
		{
			ClearItemSubscriptions();
			foreach (var materializedItem in _materializedItems)
			{
				SubscribeToItemTree(materializedItem.Item, materializedItem);
			}
		}

		private FrameworkElement CreateToolbarItem(ToolbarItem item)
		{
			FrameworkElement element = item.ItemType switch
			{
				ToolbarItemTypes.Button => new ToolbarButton(),
				ToolbarItemTypes.ToggleButton => new ToolbarToggleButton(),
				ToolbarItemTypes.FlyoutButton => new ToolbarFlyoutButton(),
				ToolbarItemTypes.RadioButton => new ToolbarRadioButton(),
				ToolbarItemTypes.SplitButton => new ToolbarSplitButton(),
				ToolbarItemTypes.Separator => new ToolbarSeparator(),
				_ => new ToolbarContentHost(),
			};

			ApplyToolbarItem(element, item);
			if (element is ToolbarToggleButton toggleButton)
			{
				RoutedEventHandler checkedHandler = (_, _) => item.IsChecked = true;
				RoutedEventHandler uncheckedHandler = (_, _) => item.IsChecked = false;
				toggleButton.Checked += checkedHandler;
				toggleButton.Unchecked += uncheckedHandler;
				_toggleEventHandlers[element] = new ToggleEventHandlers(checkedHandler, uncheckedHandler);
			}
			else if (element is ToolbarRadioButton radioButton)
			{
				RoutedEventHandler checkedHandler = (_, _) => item.IsChecked = true;
				radioButton.Checked += checkedHandler;
				_toggleEventHandlers[element] = new ToggleEventHandlers(checkedHandler, null);
			}

			return element;
		}

		private void ApplyToolbarItem(FrameworkElement element, ToolbarItem item)
		{
			SetCommonItemProperties(element, item);

			switch (element)
			{
				case ToolbarFlyoutButton flyoutButton:
					flyoutButton.Content = CreateToolbarContent(item);
					if (item.Flyout is not null)
					{
						RemoveGeneratedFlyout(element);
					}

					flyoutButton.Flyout = item.Flyout ?? GetGeneratedFlyout(element, item);
					flyoutButton.Command = item.Command;
					flyoutButton.CommandParameter = item.CommandParameter;
					break;
				case ToolbarButton button:
					button.Label = item.Label;
					button.ThemedIcon = item.ThemedIcon;
					button.IconSize = item.IconSize;
					button.Content = CreateToolbarButtonContent(item);
					button.Command = item.Command;
					button.CommandParameter = item.CommandParameter;
					break;
				case ToolbarToggleButton toggleButton:
					toggleButton.Label = item.Label;
					toggleButton.ThemedIcon = item.ThemedIcon;
					toggleButton.IconSize = item.IconSize;
					toggleButton.Content = CreateToolbarButtonContent(item);
					toggleButton.Command = item.Command;
					toggleButton.CommandParameter = item.CommandParameter;
					toggleButton.IsChecked = item.IsChecked;
					break;
				case ToolbarRadioButton radioButton:
					radioButton.Content = CreateToolbarContent(item);
					radioButton.GroupName = item.GroupName;
					radioButton.IsChecked = item.IsChecked;
					radioButton.Command = item.Command;
					radioButton.CommandParameter = item.CommandParameter;
					break;
				case ToolbarSplitButton splitButton:
					splitButton.Content = CreateToolbarContent(item);
					if (item.Flyout is not null)
					{
						RemoveGeneratedFlyout(element);
					}

					splitButton.Flyout = item.Flyout ?? GetGeneratedFlyout(element, item);
					splitButton.Command = item.Command;
					splitButton.CommandParameter = item.CommandParameter;
					break;
				case ToolbarContentHost contentHost:
					contentHost.Content = item.Content;
					break;
			}
		}

		private void ApplyToolbarItemProperty(FrameworkElement element, ToolbarItem item, string? propertyName)
		{
			switch (propertyName)
			{
				case nameof(ToolbarItem.OverflowBehavior):
				case nameof(ToolbarItem.KeyboardAcceleratorTextOverride):
					break;
				case nameof(ToolbarItem.Label):
					ApplyToolbarItemLabel(element, item);
					break;
				case nameof(ToolbarItem.IsLabelVisible):
					ApplyToolbarItemContent(element, item);
					break;
				case nameof(ToolbarItem.Content):
				case nameof(ToolbarItem.ThemedIcon):
				case nameof(ToolbarItem.IconSize):
					ApplyToolbarItemContent(element, item);
					break;
				case nameof(ToolbarItem.IsEnabled):
					if (element is Control control)
					{
						control.IsEnabled = item.IsEnabled;
					}
					break;
				case nameof(ToolbarItem.IsChecked):
					if (element is ToolbarToggleButton toggleButton)
					{
						toggleButton.IsChecked = item.IsChecked;
					}
					else if (element is ToolbarRadioButton radioButton)
					{
						radioButton.IsChecked = item.IsChecked;
					}
					break;
				case nameof(ToolbarItem.GroupName):
					if (element is ToolbarRadioButton groupRadioButton)
					{
						groupRadioButton.GroupName = item.GroupName;
					}
					break;
				case nameof(ToolbarItem.Command):
				case nameof(ToolbarItem.CommandParameter):
					ApplyToolbarItemCommand(element, item);
					break;
				case nameof(ToolbarItem.IsVisible):
					element.Visibility = item.IsVisible ? Visibility.Visible : Visibility.Collapsed;
					break;
				case nameof(ToolbarItem.Flyout):
				case nameof(ToolbarItem.SubItems):
					ApplyToolbarItemFlyout(element, item);
					break;
				case nameof(ToolbarItem.KeyboardAccelerators):
					ApplyKeyboardAccelerators(element, item);
					break;
				default:
					ApplyToolbarItem(element, item);
					break;
			}
		}

		private static void ApplyToolbarItemLabel(FrameworkElement element, ToolbarItem item)
		{
			ToolTipService.SetToolTip(element, string.IsNullOrWhiteSpace(item.Label) ? null : item.Label);
			if (element is Control control)
			{
				AutomationProperties.SetName(control, item.Label);
			}

			if (element is ToolbarButton button)
			{
				button.Label = item.Label;
			}
			else if (element is ToolbarToggleButton toggleButton)
			{
				toggleButton.Label = item.Label;
			}

			if (item.IsLabelVisible)
			{
				ApplyToolbarItemContent(element, item);
			}
		}

		private static void ApplyToolbarItemContent(FrameworkElement element, ToolbarItem item)
		{
			ApplyToolbarItemPadding(element, item);
			switch (element)
			{
				case ToolbarFlyoutButton flyoutButton:
					flyoutButton.Content = CreateToolbarContent(item);
					break;
				case ToolbarButton button:
					button.ThemedIcon = item.ThemedIcon;
					button.IconSize = item.IconSize;
					button.Content = CreateToolbarButtonContent(item);
					break;
				case ToolbarToggleButton toggleButton:
					toggleButton.ThemedIcon = item.ThemedIcon;
					toggleButton.IconSize = item.IconSize;
					toggleButton.Content = CreateToolbarButtonContent(item);
					break;
				case ToolbarRadioButton radioButton:
					radioButton.Content = CreateToolbarContent(item);
					break;
				case ToolbarSplitButton splitButton:
					splitButton.Content = CreateToolbarContent(item);
					break;
				case ToolbarContentHost contentHost:
					contentHost.Content = item.Content;
					break;
			}
		}

		private static void ApplyToolbarItemCommand(FrameworkElement element, ToolbarItem item)
		{
			switch (element)
			{
				case ToolbarSplitButton splitButton:
					splitButton.Command = item.Command;
					splitButton.CommandParameter = item.CommandParameter;
					break;
				case ButtonBase button:
					button.Command = item.Command;
					button.CommandParameter = item.CommandParameter;
					break;
			}
		}

		private void ApplyToolbarItemFlyout(FrameworkElement element, ToolbarItem item)
		{
			if (element is not ToolbarFlyoutButton && element is not ToolbarSplitButton)
			{
				RemoveGeneratedFlyout(element);

				return;
			}

			if (item.Flyout is not null)
			{
				RemoveGeneratedFlyout(element);
			}

			var flyout = item.Flyout ?? GetGeneratedFlyout(element, item);
			if (element is ToolbarFlyoutButton flyoutButton)
			{
				flyoutButton.Flyout = flyout;
			}
			else if (element is ToolbarSplitButton splitButton)
			{
				splitButton.Flyout = flyout;
			}
		}

		private static void ApplyKeyboardAccelerators(FrameworkElement element, ToolbarItem item)
		{
			if (element is not Control control)
			{
				return;
			}

			control.KeyboardAccelerators.Clear();
			if (item.KeyboardAccelerators is null)
			{
				return;
			}

			foreach (var keyboardAccelerator in item.KeyboardAccelerators)
			{
				control.KeyboardAccelerators.Add(CloneKeyboardAccelerator(keyboardAccelerator));
			}
		}

		private void SetCommonItemProperties(FrameworkElement element, ToolbarItem item)
		{
			ApplyToolbarItemSize(element);
			element.AllowFocusOnInteraction = AllowFocusOnInteraction;
			element.Visibility = item.IsVisible ? Visibility.Visible : Visibility.Collapsed;
			element.HorizontalAlignment = HorizontalAlignment.Center;
			element.VerticalAlignment = VerticalAlignment.Center;
			ToolTipService.SetToolTip(element, string.IsNullOrWhiteSpace(item.Label) ? null : item.Label);

			if (element is Control control)
			{
				control.IsEnabled = item.IsEnabled;
				control.HorizontalContentAlignment = HorizontalAlignment.Center;
				control.VerticalContentAlignment = VerticalAlignment.Center;
				AutomationProperties.SetName(control, item.Label);
			}

			ApplyToolbarItemPadding(element, item);
			ApplyKeyboardAccelerators(element, item);
		}

		private static void ApplyToolbarItemPadding(FrameworkElement element, ToolbarItem item)
		{
			if (element is not Control control)
			{
				return;
			}

			if (item.IsLabelVisible && item.Content is null)
			{
				control.Padding = GetResourceThickness(LabelButtonPaddingResourceKey, new Thickness(8, 4, 8, 4));
			}
			else
			{
				control.ClearValue(Control.PaddingProperty);
			}
		}

		private void ApplyToolbarItemSize(FrameworkElement element)
		{
			element.MinWidth = _currentMinWidth;
			element.MinHeight = _currentMinHeight;
			if (element is ToolbarSeparator separator)
			{
				separator.MinWidth = 1;
			}
		}

		private static KeyboardAccelerator CloneKeyboardAccelerator(KeyboardAccelerator source)
		{
			ArgumentNullException.ThrowIfNull(source);

			return new KeyboardAccelerator
			{
				Key = source.Key,
				Modifiers = source.Modifiers,
			};
		}

		private static object? CreateToolbarContent(ToolbarItem item)
		{
			if (item.Content is not null)
			{
				return item.Content;
			}

			return item.IsLabelVisible ? CreateLabelContent(item) : CreateIcon(item);
		}

		private static object? CreateToolbarButtonContent(ToolbarItem item)
		{
			return item.Content ?? (item.IsLabelVisible ? CreateLabelContent(item) : null);
		}

		private static FrameworkElement CreateLabelContent(ToolbarItem item)
		{
			var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
			if (CreateIcon(item) is { } icon)
			{
				panel.Children.Add(icon);
			}

			var label = new TextBlock { Text = item.Label, VerticalAlignment = VerticalAlignment.Center };
			if (Application.Current?.Resources.TryGetValue(CaptionTextBlockStyleResourceKey, out var style) is true && style is Style captionStyle)
			{
				label.Style = captionStyle;
			}

			panel.Children.Add(label);

			return panel;
		}

		private static ThemedIcon? CreateIcon(ToolbarItem item)
		{
			if (item.ThemedIcon is null)
			{
				return null;
			}

			var iconSize = double.IsFinite(item.IconSize) && item.IconSize > 0 ? item.IconSize : 16;

			return new ThemedIcon { Data = item.ThemedIcon, IconSize = iconSize, Width = iconSize, Height = iconSize };
		}

		private MenuFlyout? GetGeneratedFlyout(FrameworkElement element, ToolbarItem item)
		{
			if (item.SubItems is not { Count: > 0 })
			{
				RemoveGeneratedFlyout(element);

				return null;
			}

			if (!_generatedFlyouts.TryGetValue(element, out var flyout))
			{
				flyout = CreateFlyout(item.SubItems);
				_generatedFlyouts[element] = flyout;

				return flyout;
			}

			flyout.Hide();
			flyout.Items.Clear();
			AddMenuItems(item.SubItems, flyout.Items);

			return flyout;
		}

		private void RemoveGeneratedFlyout(FrameworkElement element)
		{
			if (_generatedFlyouts.Remove(element, out var flyout))
			{
				flyout.Hide();
				flyout.Items.Clear();
			}
		}

		private static MenuFlyout CreateFlyout(IList<ToolbarItem>? items)
		{
			var flyout = new MenuFlyout();
			if (items is not null)
			{
				AddMenuItems(items, flyout.Items);
			}

			return flyout;
		}

		private static void AddMenuItems(IEnumerable<ToolbarItem> items, IList<MenuFlyoutItemBase> destination)
		{
			var separatorPending = false;
			foreach (var item in items)
			{
				if (item is null || !item.IsVisible)
				{
					continue;
				}

				if (item.ItemType == ToolbarItemTypes.Separator)
				{
					separatorPending = destination.Count > 0 && destination[^1] is not MenuFlyoutSeparator;

					continue;
				}

				if (separatorPending)
				{
					destination.Add(new MenuFlyoutSeparator());
					separatorPending = false;
				}

				destination.Add(CreateMenuItem(item));
			}
		}

		private static MenuFlyoutItemBase CreateMenuItem(ToolbarItem item)
		{
			if (item.ItemType == ToolbarItemTypes.Separator)
			{
				return new MenuFlyoutSeparator();
			}

			if (item.ItemType == ToolbarItemTypes.RadioButton)
			{
				var radioItem = new RadioMenuFlyoutItem
				{
					Text = item.Label,
					GroupName = item.GroupName,
					IsChecked = item.IsChecked,
					IsEnabled = item.IsEnabled,
					Command = item.Command,
					CommandParameter = item.CommandParameter,
					KeyboardAcceleratorTextOverride = item.KeyboardAcceleratorTextOverride,
					Icon = CreateIcon(item),
				};
				radioItem.Click += (_, _) => item.IsChecked = true;

				return radioItem;
			}

			if (item.ItemType == ToolbarItemTypes.ToggleButton)
			{
				var toggleItem = new ToggleMenuFlyoutItem
				{
					Text = item.Label,
					IsChecked = item.IsChecked,
					IsEnabled = item.IsEnabled,
					Command = item.Command,
					CommandParameter = item.CommandParameter,
					KeyboardAcceleratorTextOverride = item.KeyboardAcceleratorTextOverride,
					Icon = CreateIcon(item),
				};
				toggleItem.Click += (_, _) => item.IsChecked = toggleItem.IsChecked;

				return toggleItem;
			}

			if (item.ItemType is ToolbarItemTypes.FlyoutButton or ToolbarItemTypes.SplitButton || item.SubItems?.Count > 0)
			{
				var subItem = new MenuFlyoutSubItem { Text = item.Label, Icon = CreateIcon(item), IsEnabled = item.IsEnabled };
				if (item.ItemType == ToolbarItemTypes.SplitButton && item.Command is not null)
				{
					subItem.Items.Add(CreateCommandMenuItem(item));
				}

				if (item.SubItems is not null)
				{
					AddMenuItems(item.SubItems, subItem.Items);
				}

				return subItem;
			}

			return CreateCommandMenuItem(item);
		}

		private static MenuFlyoutItem CreateCommandMenuItem(ToolbarItem item)
		{
			return new MenuFlyoutItem
			{
				Text = item.Label,
				IsEnabled = item.IsEnabled,
				Command = item.Command,
				CommandParameter = item.CommandParameter,
				KeyboardAcceleratorTextOverride = item.KeyboardAcceleratorTextOverride,
				Icon = CreateIcon(item),
			};
		}

		private void ItemTemplateChanged(DataTemplate? newDataTemplate)
		{
			foreach (var materializedItem in _materializedItems)
			{
				materializedItem.Host.ContentTemplate = newDataTemplate;
			}

			InvalidateMeasure();
		}

		private void ToolbarSizeChanged(ToolbarSizes newToolbarSize)
		{
			UpdateMinSizesFromResources();
			foreach (var materializedItem in _materializedItems)
			{
				ApplyToolbarItemSize(materializedItem.Element);
			}

			InvalidateMeasure();
		}

		private void OverflowButtonLabelChanged(string newOverflowButtonLabel)
		{
			if (_overflowButton is not null)
			{
				_overflowButton.Label = newOverflowButtonLabel;
			}
		}

		private void UpdateMinSizesFromResources()
		{
			_smallMinWidth = GetResourceDouble(SmallMinWidthResourceKey, _smallMinWidth);
			_smallMinHeight = GetResourceDouble(SmallMinHeightResourceKey, _smallMinHeight);
			_mediumMinWidth = GetResourceDouble(MediumMinWidthResourceKey, _mediumMinWidth);
			_mediumMinHeight = GetResourceDouble(MediumMinHeightResourceKey, _mediumMinHeight);
			_largeMinWidth = GetResourceDouble(LargeMinWidthResourceKey, _largeMinWidth);
			_largeMinHeight = GetResourceDouble(LargeMinHeightResourceKey, _largeMinHeight);

			(_currentMinWidth, _currentMinHeight) = ToolbarSize switch
			{
				ToolbarSizes.Small => (_smallMinWidth, _smallMinHeight),
				ToolbarSizes.Large => (_largeMinWidth, _largeMinHeight),
				_ => (_mediumMinWidth, _mediumMinHeight),
			};
		}

		private static double GetResourceDouble(string key, double fallback)
		{
			var resources = Application.Current?.Resources;
			if (resources is not null && resources.TryGetValue(key, out var value) && value is double resourceValue && double.IsFinite(resourceValue))
			{
				return resourceValue;
			}

			return fallback;
		}

		private static Thickness GetResourceThickness(string key, Thickness fallback)
		{
			var resources = Application.Current?.Resources;
			if (resources is not null && resources.TryGetValue(key, out var value) && value is Thickness resourceValue)
			{
				return resourceValue;
			}

			return fallback;
		}

		private void UpdateLayoutPartition(double availableWidth)
		{
			var spacing = GetItemSpacing();
			var infiniteSize = new Size(double.PositiveInfinity, double.PositiveInfinity);
			foreach (var materializedItem in _materializedItems)
			{
				materializedItem.Host.Measure(infiniteSize);
				materializedItem.Width = Math.Max(materializedItem.Host.DesiredSize.Width, materializedItem.Element.MinWidth);
			}

			var participatingItems = NormalizeMaterializedItems(_materializedItems.Where(static item => item.Item.IsVisible));
			var overflowWidth = GetOverflowWidth(infiniteSize, spacing);
			var hasAlwaysItems = participatingItems.Any(static item => item.Item.OverflowBehavior == OverflowBehaviors.Always);
			var allNonAlwaysWidth = GetMaterializedWidth(participatingItems, static item => item.Item.OverflowBehavior != OverflowBehaviors.Always, spacing);
			var overflowItems = new List<ToolbarItem>();
			var visibleItems = new HashSet<ToolbarItem>();

			if (!hasAlwaysItems && allNonAlwaysWidth <= availableWidth)
			{
				foreach (var materializedItem in participatingItems)
				{
					if (materializedItem.Item.OverflowBehavior != OverflowBehaviors.Always)
					{
						visibleItems.Add(materializedItem.Item);
					}
				}
			}
			else
			{
				var availableForItems = Math.Max(0, availableWidth - overflowWidth - spacing);
				var requiredWidth = GetMaterializedWidth(participatingItems, static item => item.Item.OverflowBehavior == OverflowBehaviors.Never, spacing);
				var optionalWidth = Math.Max(0, availableForItems - requiredWidth);
				var optionalCount = 0;

				foreach (var materializedItem in participatingItems)
				{
					if (materializedItem.Item.OverflowBehavior == OverflowBehaviors.Never)
					{
						visibleItems.Add(materializedItem.Item);
						continue;
					}

					if (materializedItem.Item.OverflowBehavior == OverflowBehaviors.Always)
					{
						overflowItems.Add(materializedItem.Item);
						continue;
					}

					var candidateWidth = materializedItem.Width + ((requiredWidth > 0 || optionalCount > 0) ? spacing : 0);
					if (candidateWidth <= optionalWidth)
					{
						optionalWidth -= candidateWidth;
						optionalCount++;
						visibleItems.Add(materializedItem.Item);
					}
					else
					{
						overflowItems.Add(materializedItem.Item);
					}
				}
			}

			visibleItems = NormalizeToolbarItems(participatingItems.Where(item => visibleItems.Contains(item.Item)).Select(static item => item.Item)).ToHashSet();
			overflowItems = NormalizeToolbarItems(overflowItems);
			_itemsPanel?.SetVisibleChildren(_materializedItems.Where(item => visibleItems.Contains(item.Item)).Select(item => item.Host));
			foreach (var materializedItem in _materializedItems)
			{
				var isVisible = visibleItems.Contains(materializedItem.Item);
				materializedItem.Host.Opacity = isVisible ? 1 : 0;
				materializedItem.Host.IsHitTestVisible = isVisible;
				if (materializedItem.Element is Control control)
				{
					control.IsTabStop = isVisible && materializedItem.Element is not ToolbarSeparator;
				}
			}

			var overflowChanged = !_overflowItems.SequenceEqual(overflowItems);
			_overflowItems = overflowItems;
			_overflowMenuDirty |= overflowChanged;
			if (_overflowItems.Count > 0)
			{
				if (_overflowStackPanel is not null)
				{
					_overflowStackPanel.Visibility = Visibility.Visible;
				}
			}
			else
			{
				if (_overflowStackPanel is not null)
				{
					_overflowStackPanel.Visibility = Visibility.Collapsed;
				}
			}

			if (_overflowMenuDirty)
			{
				PopulateOverflowMenu(_overflowItems);
			}
		}

		private void OnOverflowFlyoutOpening(object? sender, object args)
		{
			if (_overflowMenuDirty)
			{
				PopulateOverflowMenu(_overflowItems);
			}
		}

		private void PopulateOverflowMenu(IReadOnlyList<ToolbarItem> items)
		{
			if (_overflowFlyout is null)
			{
				return;
			}

			_overflowFlyout.Items.Clear();
			AddMenuItems(items, _overflowFlyout.Items);
			_overflowMenuDirty = false;
		}

		private static List<MaterializedToolbarItem> NormalizeMaterializedItems(IEnumerable<MaterializedToolbarItem> items)
		{
			var normalized = new List<MaterializedToolbarItem>();
			MaterializedToolbarItem? pendingSeparator = null;
			foreach (var item in items)
			{
				if (item.Item.ItemType == ToolbarItemTypes.Separator)
				{
					pendingSeparator = normalized.Count > 0 ? item : null;

					continue;
				}

				if (pendingSeparator is not null)
				{
					normalized.Add(pendingSeparator);
					pendingSeparator = null;
				}

				normalized.Add(item);
			}

			return normalized;
		}

		private static List<ToolbarItem> NormalizeToolbarItems(IEnumerable<ToolbarItem> items)
		{
			var normalized = new List<ToolbarItem>();
			ToolbarItem? pendingSeparator = null;
			foreach (var item in items)
			{
				if (item.ItemType == ToolbarItemTypes.Separator)
				{
					pendingSeparator = normalized.Count > 0 ? item : null;

					continue;
				}

				if (pendingSeparator is not null)
				{
					normalized.Add(pendingSeparator);
					pendingSeparator = null;
				}

				normalized.Add(item);
			}

			return normalized;
		}

		private static double GetMaterializedWidth(IEnumerable<MaterializedToolbarItem> items, Func<MaterializedToolbarItem, bool> predicate, double spacing)
		{
			var width = 0d;
			var count = 0;
			foreach (var materializedItem in items)
			{
				if (predicate(materializedItem))
				{
					width += materializedItem.Width;
					count++;
				}
			}

			return count == 0 ? 0 : width + ((count - 1) * spacing);
		}

		private double GetOverflowWidth(Size infiniteSize, double spacing)
		{
			if (_overflowStackPanel is not null)
			{
				var width = 0d;
				var childCount = 0;
				foreach (var child in _overflowStackPanel.Children)
				{
					if (child is FrameworkElement element && element.Visibility == Visibility.Visible)
					{
						element.Measure(infiniteSize);
						width += element.DesiredSize.Width;
						childCount++;
					}
				}

				if (childCount > 0)
				{
					return width + ((childCount - 1) * spacing);
				}
			}

			return Math.Max(_overflowButton?.MinWidth ?? 0, DefaultOverflowButtonWidth);
		}

		private double GetAvailableContentWidth(double availableWidth)
		{
			if (!double.IsFinite(availableWidth))
			{
				return double.PositiveInfinity;
			}

			var horizontalChrome = Padding.Left + Padding.Right + BorderThickness.Left + BorderThickness.Right;

			return Math.Max(0, availableWidth - horizontalChrome);
		}

		private static double GetItemSpacing()
		{
			var resources = Application.Current?.Resources;
			if (resources is not null && resources.TryGetValue("ToolbarItemSpacing", out var value) && value is double spacing && double.IsFinite(spacing) && spacing >= 0)
			{
				return spacing;
			}

			return DefaultItemSpacing;
		}

		private sealed class MaterializedToolbarItem
		{
			public ToolbarItem Item { get; }

			public FrameworkElement Element { get; }

			public ContentPresenter Host { get; }

			public double Width { get; set; }

			public MaterializedToolbarItem(ToolbarItem item, FrameworkElement element, ContentPresenter host)
			{
				Item = item;
				Element = element;
				Host = host;
			}
		}

		private sealed class ToggleEventHandlers
		{
			public RoutedEventHandler Checked { get; }

			public RoutedEventHandler? Unchecked { get; }

			public ToggleEventHandlers(RoutedEventHandler checkedHandler, RoutedEventHandler? uncheckedHandler)
			{
				Checked = checkedHandler;
				Unchecked = uncheckedHandler;
			}
		}

		private sealed partial class ToolbarContentHost : ContentControl, IToolbarItemSet
		{
			public ToolbarContentHost()
			{
				HorizontalContentAlignment = HorizontalAlignment.Center;
				VerticalContentAlignment = VerticalAlignment.Center;
			}
		}
	}
}
