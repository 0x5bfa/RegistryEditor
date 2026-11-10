// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RegistryEditor.ViewModels;

public sealed class RegistryIntegerByteViewModel(int highBit, int lowBit, ObservableCollection<RegistryIntegerBitViewModel> bits)
{
	public string RangeLabel => $"{highBit}–{lowBit}";

	public ObservableCollection<RegistryIntegerBitViewModel> Bits { get; } = bits;
}

public sealed partial class RegistryIntegerBitViewModel(int bitPosition, bool isSet, Action toggle) : ObservableObject
{
	public int BitPosition { get; } = bitPosition;

	public bool IsSet
	{
		get => isSet;
		internal set
		{
			if (!SetProperty(ref isSet, value))
			{
				return;
			}

			OnPropertyChanged(nameof(DisplayValue));
			OnPropertyChanged(nameof(AccessibleName));
		}
	}

	public string DisplayValue => IsSet ? "1" : "0";

	public string AccessibleName => $"Bit {BitPosition}";

	public RelayCommand ToggleCommand { get; } = new RelayCommand(toggle);
}
