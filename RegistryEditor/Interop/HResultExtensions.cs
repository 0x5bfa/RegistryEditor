// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

namespace Windows.Win32.Foundation;

internal readonly partial struct HRESULT
{
	/// <summary>Converts a Win32 error code to an HRESULT.</summary>
	public static HRESULT FromWin32Error(WIN32_ERROR error)
	{
		int value = unchecked((int)error);
		return value <= 0
			? (HRESULT)value
			: (HRESULT)unchecked((int)(0x80070000u | ((uint)error & 0xFFFFu)));
	}
}
