// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using RegistryEditor.Collections;

namespace RegistryEditor.ViewModels;

public sealed partial class RegistryNodeViewModel(
	string name, RegistryHive? hive, string subKeyPath, string iconUri, bool hasUnrealizedChildren, string? computerName = null, RegistryNodeViewModel? parent = null) : ObservableObject
{
	private readonly string _collapsedIconUri = iconUri;
	private readonly string? _expandedIconUri = hive is null ? null : "ms-appx:///Assets/Images/FolderOpened.png";
	private BitmapImage _icon = new(new Uri(iconUri));
	private bool _hasUnrealizedChildren = hasUnrealizedChildren;
	private bool _isChildrenLoading;
	private bool _isExpanded;

	public string Name { get; } = name;

	public RegistryHive? Hive { get; } = hive;

	public string SubKeyPath { get; } = subKeyPath;

	/// <summary>
	/// The remote computer that owns this node. <see langword="null"/> means the local computer.
	/// </summary>
	public string? ComputerName { get; } = computerName;

	public RegistryNodeViewModel? Parent { get; } = parent;

	public bool IsComputerNode => Hive is null;

	public bool IsHiveRoot => Hive is not null && string.IsNullOrEmpty(SubKeyPath);

	public bool IsRemote => !string.IsNullOrEmpty(ComputerName);

	public BulkObservableCollection<RegistryNodeViewModel> Children { get; } = [];

	public BitmapImage Icon
	{
		get => _icon;
		private set
		{
			if (ReferenceEquals(_icon, value))
			{
				return;
			}

			_icon = value;
			OnPropertyChanged();
		}
	}

	public bool HasUnrealizedChildren
	{
		get => _hasUnrealizedChildren;
		set => SetProperty(ref _hasUnrealizedChildren, value);
	}

	public bool IsChildrenLoading
	{
		get => _isChildrenLoading;
		set => SetProperty(ref _isChildrenLoading, value);
	}

	public bool AreChildrenLoaded { get; internal set; }

	internal Task? ChildrenLoadingTask { get; set; }

	public bool IsExpanded
	{
		get => _isExpanded;
		set
		{
			if (!SetProperty(ref _isExpanded, value))
			{
				return;
			}

			OnPropertyChanged(nameof(ExpandMenuText));
			if (_expandedIconUri is not null)
			{
				Icon = new(new Uri(value ? _expandedIconUri : _collapsedIconUri));
			}
		}
	}

	public string ExpandMenuText => IsExpanded ? "Collapse" : "Expand";

}
