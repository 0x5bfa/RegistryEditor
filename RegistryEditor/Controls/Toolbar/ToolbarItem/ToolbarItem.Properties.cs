// Copyright (c) Files Community
// Licensed under the MIT License.

using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Input;
using System.Windows.Input;

namespace RegistryEditor.Controls
{
	public partial class ToolbarItem
	{
		/// <summary>
		/// Gets or sets the control type used to present this item.
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = ToolbarItemTypes.Content)]
		public partial ToolbarItemTypes ItemType { get; set; }

		/// <summary>
		/// Gets or sets when this item is moved to the overflow menu.
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = OverflowBehaviors.Auto)]
		public partial OverflowBehaviors OverflowBehavior { get; set; }

		/// <summary>
		/// Gets or sets the label and tooltip text for this item.
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = "")]
		public partial string Label { get; set; }

		/// <summary>
		/// Gets or sets whether generated button content displays <see cref="Label"/> next to the icon.
		/// </summary>
		[GeneratedDependencyProperty]
		public partial bool IsLabelVisible { get; set; }

		/// <summary>
		/// Gets or sets the child items displayed by a flyout or split button.
		/// </summary>
		[GeneratedDependencyProperty]
		public partial IList<ToolbarItem>? SubItems { get; set; }

		/// <summary>
		/// Gets or sets custom content for this item.
		/// </summary>
		[GeneratedDependencyProperty]
		public partial object? Content { get; set; }

		/// <summary>
		/// Gets or sets the themed icon data displayed by this item.
		/// </summary>
		[GeneratedDependencyProperty]
		public partial ThemedIconData? ThemedIcon { get; set; }

		/// <summary>
		/// Gets or sets the rendered icon size.
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = 16d)]
		public partial double IconSize { get; set; }

		/// <summary>
		/// Gets or sets whether this item is checked.
		/// </summary>
		[GeneratedDependencyProperty]
		public partial bool IsChecked { get; set; }

		/// <summary>
		/// Gets or sets the keyboard accelerator text shown in the overflow menu.
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = "")]
		public partial string KeyboardAcceleratorTextOverride { get; set; }

		/// <summary>
		/// Gets or sets the radio group name for this item.
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = "")]
		public partial string GroupName { get; set; }

		/// <summary>
		/// Gets or sets the command invoked by this item.
		/// </summary>
		[GeneratedDependencyProperty]
		public partial ICommand? Command { get; set; }

		/// <summary>
		/// Gets or sets the parameter passed to <see cref="Command"/>.
		/// </summary>
		[GeneratedDependencyProperty]
		public partial object? CommandParameter { get; set; }

		/// <summary>
		/// Gets or sets whether the item participates in toolbar layout and overflow presentation.
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = true)]
		public partial bool IsVisible { get; set; }

		/// <summary>
		/// Gets or sets whether the rendered item is enabled.
		/// </summary>
		[GeneratedDependencyProperty(DefaultValue = true)]
		public partial bool IsEnabled { get; set; }

		/// <summary>
		/// Gets or sets the custom flyout displayed by a flyout or split button.
		/// </summary>
		[GeneratedDependencyProperty]
		public partial FlyoutBase? Flyout { get; set; }

		/// <summary>
		/// Gets or sets the keyboard accelerators associated with this item.
		/// </summary>
		[GeneratedDependencyProperty]
		public partial IList<KeyboardAccelerator>? KeyboardAccelerators { get; set; }

		partial void OnSubItemsPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			ObserveSubItems(e.NewValue as IList<ToolbarItem>);
			RaisePropertyChanged(nameof(SubItems));
		}

		partial void OnKeyboardAcceleratorsPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			ObserveKeyboardAccelerators(e.NewValue as IList<KeyboardAccelerator>);
			RaisePropertyChanged(nameof(KeyboardAccelerators));
		}

		partial void OnIsVisibleChanged(bool newValue)
		{
			RaisePropertyChanged(nameof(IsVisible));
		}

		partial void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
		{
			if (e.Property != SubItemsProperty)
			{
				string? propertyName = null;

				if (e.Property == ItemTypeProperty)
				{
					propertyName = nameof(ItemType);
				}
				else if (e.Property == OverflowBehaviorProperty)
				{
					propertyName = nameof(OverflowBehavior);
				}
				else if (e.Property == LabelProperty)
				{
					propertyName = nameof(Label);
				}
				else if (e.Property == IsLabelVisibleProperty)
				{
					propertyName = nameof(IsLabelVisible);
				}
				else if (e.Property == ContentProperty)
				{
					propertyName = nameof(Content);
				}
				else if (e.Property == ThemedIconProperty)
				{
					propertyName = nameof(ThemedIcon);
				}
				else if (e.Property == IconSizeProperty)
				{
					propertyName = nameof(IconSize);
				}
				else if (e.Property == IsCheckedProperty)
				{
					propertyName = nameof(IsChecked);
				}
				else if (e.Property == KeyboardAcceleratorTextOverrideProperty)
				{
					propertyName = nameof(KeyboardAcceleratorTextOverride);
				}
				else if (e.Property == GroupNameProperty)
				{
					propertyName = nameof(GroupName);
				}
				else if (e.Property == CommandProperty)
				{
					propertyName = nameof(Command);
				}
				else if (e.Property == CommandParameterProperty)
				{
					propertyName = nameof(CommandParameter);
				}
				else if (e.Property == IsEnabledProperty)
				{
					propertyName = nameof(IsEnabled);
				}
				else if (e.Property == FlyoutProperty)
				{
					propertyName = nameof(Flyout);
				}
				else if (e.Property == KeyboardAcceleratorsProperty)
				{
					propertyName = nameof(KeyboardAccelerators);
				}

				if (propertyName is not null)
				{
					RaisePropertyChanged(propertyName);
				}
			}
		}
	}
}
