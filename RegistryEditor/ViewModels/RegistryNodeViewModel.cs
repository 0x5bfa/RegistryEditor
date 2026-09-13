// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Win32;
using System.Runtime.CompilerServices;

namespace RegistryEditor.ViewModels;

public sealed class RegistryNodeViewModel : INotifyPropertyChanged
{
	private readonly string _collapsedIconUri;
	private readonly string? _expandedIconUri;
	private BitmapImage _icon;
	private bool _hasUnrealizedChildren;
	private bool _isChildrenLoading;
	private bool _isExpanded;

	public RegistryNodeViewModel(
		string name,
		RegistryHive? hive,
		string subKeyPath,
		string iconUri,
		bool hasUnrealizedChildren,
		string? computerName = null,
		RegistryNodeViewModel? parent = null)
	{
		Name = name;
		Hive = hive;
		SubKeyPath = subKeyPath;
		ComputerName = computerName;
		Parent = parent;
		_collapsedIconUri = iconUri;
		_expandedIconUri = hive is null ? null : "ms-appx:///Assets/Images/FolderOpened.png";
		_icon = new(new Uri(iconUri));
		_hasUnrealizedChildren = hasUnrealizedChildren;
	}

	public string Name { get; }

	public RegistryHive? Hive { get; }

	public string SubKeyPath { get; }

	/// <summary>
	/// The remote computer that owns this node. <see langword="null"/> means the local computer.
	/// </summary>
	public string? ComputerName { get; }

	public RegistryNodeViewModel? Parent { get; }

	public bool IsComputerNode => Hive is null;

	public bool IsHiveRoot => Hive is not null && string.IsNullOrEmpty(SubKeyPath);

	public bool IsRemote => !string.IsNullOrEmpty(ComputerName);

	public ObservableCollection<RegistryNodeViewModel> Children { get; } = [];

	public BitmapImage Icon
	{
		get => _icon;
		private set
		{
			if (ReferenceEquals(_icon, value))
				return;

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

	public bool IsExpanded
	{
		get => _isExpanded;
		set
		{
			if (!SetProperty(ref _isExpanded, value) || _expandedIconUri is null)
				return;

			Icon = new(new Uri(value ? _expandedIconUri : _collapsedIconUri));
		}
	}

	public event PropertyChangedEventHandler? PropertyChanged;

	private bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(storage, value))
			return false;

		storage = value;
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		return true;
	}

	private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
		=> PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
