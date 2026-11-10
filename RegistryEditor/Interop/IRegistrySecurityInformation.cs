// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
using Microsoft.Extensions.Logging;
using RegistryEditor.ViewModels;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;
using System.Security.AccessControl;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Security;
using Windows.Win32.Security.Authorization;
using Windows.Win32.Security.Authorization.UI;
using Windows.Win32.System.Registry;

namespace RegistryEditor.Interop;

[GeneratedComInterface]
[Guid("965FC360-16FF-11D0-91CB-00AA00BBB723")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal unsafe partial interface IRegistrySecurityInformation
{
	[PreserveSig]
	int GetObjectInformation(out SI_OBJECT_INFO objectInfo);

	[PreserveSig]
	int GetSecurity(OBJECT_SECURITY_INFORMATION requestedInformation, out PSECURITY_DESCRIPTOR securityDescriptor, BOOL defaultSecurity);

	[PreserveSig]
	int SetSecurity(OBJECT_SECURITY_INFORMATION securityInformation, PSECURITY_DESCRIPTOR securityDescriptor);

	// pguidObjectType can be NULL in both callbacks, so keep it as an opaque pointer.
	[PreserveSig]
	int GetAccessRights(nint objectType, SECURITY_INFO_PAGE_FLAGS flags, out nint access, out uint accessCount, out uint defaultIndex);

	[PreserveSig]
	int MapGeneric(nint objectType, in byte aceFlags, in uint accessMask);

	[PreserveSig]
	int GetInheritTypes(out nint inheritTypes, out uint inheritTypeCount);

	[PreserveSig]
	int PropertySheetPageCallback(HWND hwnd, uint message, SI_PAGE_TYPE page);
}
