// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Win32.SafeHandles;

namespace RegistryEditor.Services;

internal sealed class RegistrySecurityKey : IDisposable
{
	internal RegistrySecurityKey(SafeRegistryHandle handle, bool canWriteSecurity)
	{
		Handle = handle;
		CanWriteSecurity = canWriteSecurity;
	}

	internal SafeRegistryHandle Handle { get; }

	internal bool CanWriteSecurity { get; }

	public void Dispose() => Handle.Dispose();
}
