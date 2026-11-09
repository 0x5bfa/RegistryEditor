// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Win32;
using Microsoft.Win32.SafeHandles;
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

namespace RegistryEditor.Services;

[GeneratedComInterface]
[Guid("965FC360-16FF-11D0-91CB-00AA00BBB723")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IRegistrySecurityInformation
{
	[PreserveSig]
	HRESULT GetObjectInformation(ref SI_OBJECT_INFO objectInfo);

	[PreserveSig]
	HRESULT GetSecurity(OBJECT_SECURITY_INFORMATION requestedInformation, out nint securityDescriptor, int defaultSecurity);

	[PreserveSig]
	HRESULT SetSecurity(OBJECT_SECURITY_INFORMATION securityInformation, nint securityDescriptor);

	[PreserveSig]
	HRESULT GetAccessRights(nint objectType, uint flags, out nint access, out uint accessCount, out uint defaultIndex);

	[PreserveSig]
	HRESULT MapGeneric(nint objectType, ref byte aceFlags, ref uint accessMask);

	[PreserveSig]
	HRESULT GetInheritTypes(out nint inheritTypes, out uint inheritTypeCount);

	[PreserveSig]
	HRESULT PropertySheetPageCallback(nint hwnd, uint message, uint page);
}

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

internal static class RegistrySecurityEditor
{
	internal static void Show(nint owner, RegistryNodeViewModel node)
	{
		using RegistrySecurityInformation information = new(node);
		unsafe
		{
			void* securityInformation = ComInterfaceMarshaller<IRegistrySecurityInformation>.ConvertToUnmanaged(information);
			try
			{
				if (!PInvoke.EditSecurity(new HWND(owner), (ISecurityInformation*)securityInformation))
				{
					int error = Marshal.GetLastWin32Error();
					if (error == 0)
						error = 1;

					throw new Win32Exception(error);
				}
			}
			finally
			{
				ComInterfaceMarshaller<IRegistrySecurityInformation>.Free(securityInformation);
			}
		}
	}
}

[GeneratedComClass]
internal sealed unsafe partial class RegistrySecurityInformation : IRegistrySecurityInformation, IDisposable
{
	private static readonly SecurityAccessDefinition[] AccessDefinitions =
	[
		new("Query value", (uint)REG_SAM_FLAGS.KEY_QUERY_VALUE, 0x00010000 /* SI_ACCESS_SPECIFIC */),
		new("Set value", (uint)REG_SAM_FLAGS.KEY_SET_VALUE, 0x00010000 /* SI_ACCESS_SPECIFIC */),
		new("Create subkey", (uint)REG_SAM_FLAGS.KEY_CREATE_SUB_KEY, 0x00010000 /* SI_ACCESS_SPECIFIC */),
		new("Enumerate subkeys", (uint)REG_SAM_FLAGS.KEY_ENUMERATE_SUB_KEYS, 0x00010000 /* SI_ACCESS_SPECIFIC */),
		new("Notify", (uint)REG_SAM_FLAGS.KEY_NOTIFY, 0x00010000 /* SI_ACCESS_SPECIFIC */),
		new("Create link", (uint)REG_SAM_FLAGS.KEY_CREATE_LINK, 0x00010000 /* SI_ACCESS_SPECIFIC */),
		new("Delete", (uint)TOKEN_ACCESS_MASK.TOKEN_DELETE, 0x00010000 /* SI_ACCESS_SPECIFIC */),
		new("Read permissions", (uint)TOKEN_ACCESS_MASK.TOKEN_READ_CONTROL, 0x00010000 /* SI_ACCESS_SPECIFIC */),
		new("Change permissions", (uint)TOKEN_ACCESS_MASK.TOKEN_WRITE_DAC, 0x00010000 /* SI_ACCESS_SPECIFIC */),
		new("Read", (uint)GENERIC_ACCESS_RIGHTS.GENERIC_READ, 0x00020000 /* SI_ACCESS_GENERAL */ | 0x00040000 /* SI_ACCESS_CONTAINER */),
		new("Write", (uint)GENERIC_ACCESS_RIGHTS.GENERIC_WRITE, 0x00020000 /* SI_ACCESS_GENERAL */ | 0x00040000 /* SI_ACCESS_CONTAINER */),
		new("Full control", (uint)GENERIC_ACCESS_RIGHTS.GENERIC_ALL, 0x00020000 /* SI_ACCESS_GENERAL */ | 0x00040000 /* SI_ACCESS_CONTAINER */),
	];

	private static readonly SecurityInheritDefinition[] InheritDefinitions =
	[
		new("This key and subkeys", ACE_FLAGS.SUB_CONTAINERS_AND_OBJECTS_INHERIT),
		new("Subkeys only", ACE_FLAGS.SUB_CONTAINERS_ONLY_INHERIT),
		new("This key only", ACE_FLAGS.NO_INHERITANCE),
	];

	private static readonly GENERIC_MAPPING GenericMapping = new()
	{
		GenericRead = 0x00020019,
		GenericWrite = 0x00020006,
		GenericExecute = 0x00020019,
		GenericAll = 0x000F003F,
	};
	private const OBJECT_SECURITY_INFORMATION SecurityInformationMask =
		OBJECT_SECURITY_INFORMATION.OWNER_SECURITY_INFORMATION
		| OBJECT_SECURITY_INFORMATION.GROUP_SECURITY_INFORMATION
		| OBJECT_SECURITY_INFORMATION.DACL_SECURITY_INFORMATION
		| OBJECT_SECURITY_INFORMATION.SACL_SECURITY_INFORMATION
		| OBJECT_SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION
		| OBJECT_SECURITY_INFORMATION.PROTECTED_SACL_SECURITY_INFORMATION
		| OBJECT_SECURITY_INFORMATION.UNPROTECTED_DACL_SECURITY_INFORMATION
		| OBJECT_SECURITY_INFORMATION.UNPROTECTED_SACL_SECURITY_INFORMATION;

	private readonly List<nint> _allocatedStrings = [];
	private RegistrySecurityKey? _key;
	private nint _serverName;
	private nint _objectName;
	private nint _accessRights;
	private nint _inheritTypes;

	internal RegistrySecurityInformation(RegistryNodeViewModel node)
	{
		ArgumentNullException.ThrowIfNull(node);

		try
		{
			_key = RegistryFileService.OpenSecurityKey(node);
			string serverName = node.IsRemote && !string.IsNullOrEmpty(node.ComputerName)
				? node.ComputerName
				: Environment.MachineName;
			_serverName = AllocateString(serverName);
			_objectName = AllocateString(GetRegistryPath(node));
			_accessRights = AllocateAccessRights();
			_inheritTypes = AllocateInheritTypes();
		}
		catch
		{
			Dispose();
			throw;
		}
	}

	public HRESULT GetObjectInformation(ref SI_OBJECT_INFO objectInfo)
	{
		if (_key is null)
			return HRESULT.E_INVALIDARG;

		objectInfo = new SI_OBJECT_INFO
		{
			dwFlags = (SI_OBJECT_INFO_FLAGS)(
				0x00000001 /* SI_EDIT_OWNER */
				| 0x00000004 /* SI_CONTAINER */
				| 0x00000010 /* SI_ADVANCED */
				| 0x00000400 /* SI_NO_TREE_APPLY */),
			pszServerName = new PWSTR(_serverName),
			pszObjectName = new PWSTR(_objectName),
			guidObjectType = Guid.Empty,
		};
		if (!_key.CanWriteSecurity)
			objectInfo.dwFlags |= (SI_OBJECT_INFO_FLAGS)0x00000008 /* SI_READONLY */;

		return HRESULT.S_OK;
	}

	public unsafe HRESULT GetSecurity(OBJECT_SECURITY_INFORMATION requestedInformation, out nint securityDescriptor, int defaultSecurity)
	{
		securityDescriptor = nint.Zero;
		if (_key is null)
			return HRESULT.E_INVALIDARG;
		if (defaultSecurity != 0)
			return HRESULT.E_NOTIMPL;

		WIN32_ERROR result = PInvoke.GetSecurityInfo(
			_key.Handle,
			SE_OBJECT_TYPE.SE_REGISTRY_KEY,
			requestedInformation,
			out _,
			out _,
			out _,
			out _,
			out PSECURITY_DESCRIPTOR descriptor);
		securityDescriptor = (nint)descriptor.Value;
		if (result == WIN32_ERROR.NO_ERROR)
			return HRESULT.S_OK;

		if (descriptor.Value is not null)
		{
			PInvoke.LocalFree((HLOCAL)(IntPtr)descriptor);
			securityDescriptor = nint.Zero;
		}

		return HRESULT.FromWin32Error(result);
	}

	public unsafe HRESULT SetSecurity(OBJECT_SECURITY_INFORMATION securityInformation, nint securityDescriptor)
	{
		if (_key is null || securityDescriptor == nint.Zero)
			return HRESULT.E_INVALIDARG;
		if (!_key.CanWriteSecurity)
			return HRESULT.E_ACCESSDENIED;

		PSECURITY_DESCRIPTOR descriptor = (PSECURITY_DESCRIPTOR)securityDescriptor;
		if (!PInvoke.GetSecurityDescriptorControl(descriptor, out ushort controlValue, out _))
			return LastErrorAsHResult();
		if (!PInvoke.GetSecurityDescriptorOwner(descriptor, out PSID owner, out _))
			return LastErrorAsHResult();
		if (!PInvoke.GetSecurityDescriptorGroup(descriptor, out PSID group, out _))
			return LastErrorAsHResult();
		if (!PInvoke.GetSecurityDescriptorDacl(descriptor, out _, out ACL* dacl, out _))
			return LastErrorAsHResult();
		if (!PInvoke.GetSecurityDescriptorSacl(descriptor, out _, out ACL* sacl, out _))
			return LastErrorAsHResult();

		ControlFlags control = (ControlFlags)controlValue;
		OBJECT_SECURITY_INFORMATION effectiveInformation = securityInformation & SecurityInformationMask;
		if ((effectiveInformation & OBJECT_SECURITY_INFORMATION.DACL_SECURITY_INFORMATION) != 0)
		{
			effectiveInformation &= ~(OBJECT_SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION
				| OBJECT_SECURITY_INFORMATION.UNPROTECTED_DACL_SECURITY_INFORMATION);
			effectiveInformation |= (control & ControlFlags.DiscretionaryAclProtected) != 0
				? OBJECT_SECURITY_INFORMATION.PROTECTED_DACL_SECURITY_INFORMATION
				: OBJECT_SECURITY_INFORMATION.UNPROTECTED_DACL_SECURITY_INFORMATION;
		}
		if ((effectiveInformation & OBJECT_SECURITY_INFORMATION.SACL_SECURITY_INFORMATION) != 0)
		{
			effectiveInformation &= ~(OBJECT_SECURITY_INFORMATION.PROTECTED_SACL_SECURITY_INFORMATION
				| OBJECT_SECURITY_INFORMATION.UNPROTECTED_SACL_SECURITY_INFORMATION);
			effectiveInformation |= (control & ControlFlags.SystemAclProtected) != 0
				? OBJECT_SECURITY_INFORMATION.PROTECTED_SACL_SECURITY_INFORMATION
				: OBJECT_SECURITY_INFORMATION.UNPROTECTED_SACL_SECURITY_INFORMATION;
		}

		WIN32_ERROR result = PInvoke.SetSecurityInfo(
			_key.Handle,
			SE_OBJECT_TYPE.SE_REGISTRY_KEY,
			effectiveInformation,
			owner,
			group,
			dacl is null ? null : *dacl,
			sacl is null ? null : *sacl);
		return HRESULT.FromWin32Error(result);
	}

	public HRESULT GetAccessRights(nint objectType, uint flags, out nint access, out uint accessCount, out uint defaultIndex)
	{
		access = _accessRights;
		accessCount = (uint)AccessDefinitions.Length;
		defaultIndex = 9;
		return _accessRights == nint.Zero ? HRESULT.E_OUTOFMEMORY : HRESULT.S_OK;
	}

	public HRESULT MapGeneric(nint objectType, ref byte aceFlags, ref uint accessMask)
	{
		aceFlags &= 0xFE;
		PInvoke.MapGenericMask(ref accessMask, in GenericMapping);
		return HRESULT.S_OK;
	}

	public HRESULT GetInheritTypes(out nint inheritTypes, out uint inheritTypeCount)
	{
		inheritTypes = _inheritTypes;
		inheritTypeCount = (uint)InheritDefinitions.Length;
		return _inheritTypes == nint.Zero ? HRESULT.E_OUTOFMEMORY : HRESULT.S_OK;
	}

	public HRESULT PropertySheetPageCallback(nint hwnd, uint message, uint page) => HRESULT.S_OK;

	public void Dispose()
	{
		if (_accessRights != nint.Zero)
		{
			NativeMemory.Free((void*)_accessRights);
			_accessRights = nint.Zero;
		}
		if (_inheritTypes != nint.Zero)
		{
			NativeMemory.Free((void*)_inheritTypes);
			_inheritTypes = nint.Zero;
		}

		foreach (nint allocatedString in _allocatedStrings)
			NativeMemory.Free((void*)allocatedString);
		_allocatedStrings.Clear();

		_key?.Dispose();
		_key = null;
		GC.SuppressFinalize(this);
	}

	private unsafe nint AllocateString(string value)
	{
		char* buffer = (char*)NativeMemory.Alloc((nuint)(value.Length + 1), (nuint)sizeof(char));
		if (buffer is null)
			throw new OutOfMemoryException();

		value.AsSpan().CopyTo(new Span<char>(buffer, value.Length));
		buffer[value.Length] = '\0';
		nint pointer = (nint)buffer;
		_allocatedStrings.Add(pointer);
		return pointer;
	}

	private unsafe nint AllocateAccessRights()
	{
		SI_ACCESS* buffer = (SI_ACCESS*)NativeMemory.Alloc((nuint)AccessDefinitions.Length, (nuint)sizeof(SI_ACCESS));
		if (buffer is null)
			throw new OutOfMemoryException();

		try
		{
			for (int index = 0; index < AccessDefinitions.Length; index++)
			{
				SecurityAccessDefinition definition = AccessDefinitions[index];
				buffer[index] = new SI_ACCESS
				{
					pguid = null,
					mask = definition.Mask,
					pszName = (PCWSTR)(char*)AllocateString(definition.Name),
					dwFlags = (uint)definition.Flags,
				};
			}

			return (nint)buffer;
		}
		catch
		{
			NativeMemory.Free(buffer);
			throw;
		}
	}

	private unsafe nint AllocateInheritTypes()
	{
		SI_INHERIT_TYPE* buffer = (SI_INHERIT_TYPE*)NativeMemory.Alloc((nuint)InheritDefinitions.Length, (nuint)sizeof(SI_INHERIT_TYPE));
		if (buffer is null)
			throw new OutOfMemoryException();

		try
		{
			for (int index = 0; index < InheritDefinitions.Length; index++)
			{
				SecurityInheritDefinition definition = InheritDefinitions[index];
				buffer[index] = new SI_INHERIT_TYPE
				{
					pguid = null,
					dwFlags = (ACE_FLAGS)definition.Flags,
					pszName = (PCWSTR)(char*)AllocateString(definition.Name),
				};
			}

			return (nint)buffer;
		}
		catch
		{
			NativeMemory.Free(buffer);
			throw;
		}
	}

	private static HRESULT LastErrorAsHResult()
	{
		int error = Marshal.GetLastWin32Error();
		if (error == 0)
			error = 1;

		return HRESULT.FromWin32Error((WIN32_ERROR)error);
	}

	private static string GetRegistryPath(RegistryNodeViewModel node)
	{
		string hiveName = node.Hive switch
		{
			RegistryHive.ClassesRoot => "HKEY_CLASSES_ROOT",
			RegistryHive.CurrentUser => "HKEY_CURRENT_USER",
			RegistryHive.LocalMachine => "HKEY_LOCAL_MACHINE",
			RegistryHive.Users => "HKEY_USERS",
			RegistryHive.CurrentConfig => "HKEY_CURRENT_CONFIG",
			_ => node.Hive?.ToString() ?? string.Empty,
		};
		string path = string.IsNullOrEmpty(node.SubKeyPath)
			? hiveName
			: $"{hiveName}\\{node.SubKeyPath}";

		return node.IsRemote ? $"\\\\{node.ComputerName}\\{path}" : path;
	}

	private readonly struct SecurityAccessDefinition
	{
		internal SecurityAccessDefinition(string name, uint mask, uint flags)
		{
			Name = name;
			Mask = mask;
			Flags = flags;
		}

		internal string Name { get; }
		internal uint Mask { get; }
		internal uint Flags { get; }
	}

	private readonly struct SecurityInheritDefinition
	{
		internal SecurityInheritDefinition(string name, ACE_FLAGS flags)
		{
			Name = name;
			Flags = (uint)flags;
		}

		internal string Name { get; }
		internal uint Flags { get; }
	}
}
