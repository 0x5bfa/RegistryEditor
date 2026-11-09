// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RegistryEditor.ViewModels;

public sealed class RegistryIntegerByteViewModel(
	int highBit,
	int lowBit,
	ObservableCollection<RegistryIntegerBitViewModel> bits)
{
	public string RangeLabel => $"{highBit}–{lowBit}";

	public ObservableCollection<RegistryIntegerBitViewModel> Bits { get; } = bits;
}

public sealed class RegistryIntegerBitViewModel : ObservableObject
{
	private bool _isSet;

	public RegistryIntegerBitViewModel(int bitPosition, bool isSet, Action toggle)
	{
		BitPosition = bitPosition;
		_isSet = isSet;
		ToggleCommand = new RelayCommand(toggle);
	}

	public int BitPosition { get; }

	public bool IsSet
	{
		get => _isSet;
		internal set
		{
			if (!SetProperty(ref _isSet, value))
				return;

			OnPropertyChanged(nameof(DisplayValue));
			OnPropertyChanged(nameof(AccessibleName));
		}
	}

	public string DisplayValue => IsSet ? "1" : "0";

	public string AccessibleName => $"Bit {BitPosition}";

	public RelayCommand ToggleCommand { get; }
}
