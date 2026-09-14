// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Win32;
using RegistryEditor.ViewModels;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace RegistryEditor.Services;

[GeneratedComInterface]
[Guid("965FC360-16FF-11D0-91CB-00AA00BBB723")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
internal partial interface IRegistrySecurityInformation
{
	[PreserveSig]
	int GetObjectInformation(ref SecurityObjectInfo objectInfo);

	[PreserveSig]
	int GetSecurity(uint requestedInformation, out nint securityDescriptor, int defaultSecurity);

	[PreserveSig]
	int SetSecurity(uint securityInformation, nint securityDescriptor);

	[PreserveSig]
	int GetAccessRights(nint objectType, uint flags, out nint access, out uint accessCount, out uint defaultIndex);

	[PreserveSig]
	int MapGeneric(nint objectType, ref byte aceFlags, ref uint accessMask);

	[PreserveSig]
	int GetInheritTypes(out nint inheritTypes, out uint inheritTypeCount);

	[PreserveSig]
	int PropertySheetPageCallback(nint hwnd, uint message, uint page);
}

[StructLayout(LayoutKind.Sequential)]
internal struct SecurityObjectInfo
{
	internal uint Flags;
	internal nint Instance;
	internal nint ServerName;
	internal nint ObjectName;
	internal nint PageTitle;
	internal Guid ObjectType;
}

[StructLayout(LayoutKind.Sequential)]
internal struct SecurityAccess
{
	internal nint ObjectType;
	internal uint Mask;
	internal nint Name;
	internal uint Flags;
}

[StructLayout(LayoutKind.Sequential)]
internal struct SecurityInheritType
{
	internal nint ObjectType;
	internal uint Flags;
	internal nint Name;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RegistryGenericMapping
{
	internal uint GenericRead;
	internal uint GenericWrite;
	internal uint GenericExecute;
	internal uint GenericAll;
}

internal sealed class RegistrySecurityKey : IDisposable
{
	private nint _handle;

	internal RegistrySecurityKey(nint handle, bool canWriteSecurity)
	{
		_handle = handle;
		CanWriteSecurity = canWriteSecurity;
	}

	internal nint Handle
	{
		get
		{
			return _handle;
		}
	}

	internal bool CanWriteSecurity { get; }

	public void Dispose()
	{
		if (_handle == nint.Zero)
			return;

		RegistryNative.RegCloseKey(_handle);
		_handle = nint.Zero;
	}
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
				if (RegistryNative.EditSecurity(owner, (nint)securityInformation) == 0)
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
internal sealed partial class RegistrySecurityInformation : IRegistrySecurityInformation, IDisposable
{
	private const int HResultSuccess = 0;
	private const int HResultNotImplemented = unchecked((int)0x80004001);
	private const int HResultInvalidArgument = unchecked((int)0x80070057);
	private const int HResultAccessDenied = unchecked((int)0x80070005);
	private const int HResultOutOfMemory = unchecked((int)0x8007000E);

	private const uint SiEditOwner = 0x00000001;
	private const uint SiContainer = 0x00000004;
	private const uint SiReadOnly = 0x00000008;
	private const uint SiAdvanced = 0x00000010;
	private const uint SiNoTreeApply = 0x00000400;

	private const uint SiAccessSpecific = 0x00010000;
	private const uint SiAccessGeneral = 0x00020000;
	private const uint SiAccessContainer = 0x00040000;

	private const uint RegistryObjectType = 4;
	private const uint OwnerSecurityInformation = 0x00000001;
	private const uint GroupSecurityInformation = 0x00000002;
	private const uint DaclSecurityInformation = 0x00000004;
	private const uint SaclSecurityInformation = 0x00000008;
	private const uint ProtectedDaclSecurityInformation = 0x80000000;
	private const uint ProtectedSaclSecurityInformation = 0x40000000;
	private const uint UnprotectedDaclSecurityInformation = 0x20000000;
	private const uint UnprotectedSaclSecurityInformation = 0x10000000;
	private const uint SecurityInformationMask =
		OwnerSecurityInformation
		| GroupSecurityInformation
		| DaclSecurityInformation
		| SaclSecurityInformation
		| ProtectedDaclSecurityInformation
		| ProtectedSaclSecurityInformation
		| UnprotectedDaclSecurityInformation
		| UnprotectedSaclSecurityInformation;

	private const ushort DaclProtected = 0x1000;
	private const ushort SaclProtected = 0x2000;
	private const uint GenericRead = 0x80000000;
	private const uint GenericWrite = 0x40000000;
	private const uint GenericExecute = 0x20000000;
	private const uint GenericAll = 0x10000000;
	private const uint KeyQueryValue = 0x00000001;
	private const uint KeySetValue = 0x00000002;
	private const uint KeyCreateSubKey = 0x00000004;
	private const uint KeyEnumerateSubKeys = 0x00000008;
	private const uint KeyNotify = 0x00000010;
	private const uint KeyCreateLink = 0x00000020;
	private const uint Delete = 0x00010000;
	private const uint ReadControl = 0x00020000;
	private const uint WriteDac = 0x00040000;

	private static readonly SecurityAccessDefinition[] AccessDefinitions =
	[
		new("Query value", KeyQueryValue, SiAccessSpecific),
		new("Set value", KeySetValue, SiAccessSpecific),
		new("Create subkey", KeyCreateSubKey, SiAccessSpecific),
		new("Enumerate subkeys", KeyEnumerateSubKeys, SiAccessSpecific),
		new("Notify", KeyNotify, SiAccessSpecific),
		new("Create link", KeyCreateLink, SiAccessSpecific),
		new("Delete", Delete, SiAccessSpecific),
		new("Read permissions", ReadControl, SiAccessSpecific),
		new("Change permissions", WriteDac, SiAccessSpecific),
		new("Read", GenericRead, SiAccessGeneral | SiAccessContainer),
		new("Write", GenericWrite, SiAccessGeneral | SiAccessContainer),
		new("Full control", GenericAll, SiAccessGeneral | SiAccessContainer),
	];

	private static readonly SecurityInheritDefinition[] InheritDefinitions =
	[
		new("This key and subkeys", 0x00000003),
		new("Subkeys only", 0x00000002),
		new("This key only", 0x00000000),
	];

	private static readonly RegistryGenericMapping GenericMapping = new()
	{
		GenericRead = 0x00020019,
		GenericWrite = 0x00020006,
		GenericExecute = 0x00020019,
		GenericAll = 0x000F003F,
	};

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

	public int GetObjectInformation(ref SecurityObjectInfo objectInfo)
	{
		if (_key is null)
			return HResultInvalidArgument;

		objectInfo = new SecurityObjectInfo
		{
			Flags = SiEditOwner | SiContainer | SiAdvanced | SiNoTreeApply,
			Instance = nint.Zero,
			ServerName = _serverName,
			ObjectName = _objectName,
			PageTitle = nint.Zero,
			ObjectType = Guid.Empty,
		};
		if (!_key.CanWriteSecurity)
			objectInfo.Flags |= SiReadOnly;

		return HResultSuccess;
	}

	public int GetSecurity(uint requestedInformation, out nint securityDescriptor, int defaultSecurity)
	{
		securityDescriptor = nint.Zero;
		if (_key is null)
			return HResultInvalidArgument;
		if (defaultSecurity != 0)
			return HResultNotImplemented;

		uint result = RegistryNative.GetSecurityInfo(
			_key.Handle,
			RegistryObjectType,
			requestedInformation,
			out nint owner,
			out nint group,
			out nint dacl,
			out nint sacl,
			out securityDescriptor);
		if (result == 0)
			return HResultSuccess;

		if (securityDescriptor != nint.Zero)
		{
			RegistryNative.LocalFree(securityDescriptor);
			securityDescriptor = nint.Zero;
		}

		return ToHResult(result);
	}

	public int SetSecurity(uint securityInformation, nint securityDescriptor)
	{
		if (_key is null || securityDescriptor == nint.Zero)
			return HResultInvalidArgument;
		if (!_key.CanWriteSecurity)
			return HResultAccessDenied;

		int status = RegistryNative.GetSecurityDescriptorControl(
			securityDescriptor,
			out ushort control,
			out uint revision);
		if (status == 0)
			return LastErrorAsHResult();

		status = RegistryNative.GetSecurityDescriptorOwner(
			securityDescriptor,
			out nint owner,
			out int ownerDefaulted);
		if (status == 0)
			return LastErrorAsHResult();

		status = RegistryNative.GetSecurityDescriptorGroup(
			securityDescriptor,
			out nint group,
			out int groupDefaulted);
		if (status == 0)
			return LastErrorAsHResult();

		status = RegistryNative.GetSecurityDescriptorDacl(
			securityDescriptor,
			out int daclPresent,
			out nint dacl,
			out int daclDefaulted);
		if (status == 0)
			return LastErrorAsHResult();

		status = RegistryNative.GetSecurityDescriptorSacl(
			securityDescriptor,
			out int saclPresent,
			out nint sacl,
			out int saclDefaulted);
		if (status == 0)
			return LastErrorAsHResult();

		uint effectiveInformation = securityInformation & SecurityInformationMask;
		if ((effectiveInformation & DaclSecurityInformation) != 0)
		{
			effectiveInformation &= ~(ProtectedDaclSecurityInformation | UnprotectedDaclSecurityInformation);
			if ((control & DaclProtected) != 0)
				effectiveInformation |= ProtectedDaclSecurityInformation;
			else
				effectiveInformation |= UnprotectedDaclSecurityInformation;
		}
		if ((effectiveInformation & SaclSecurityInformation) != 0)
		{
			effectiveInformation &= ~(ProtectedSaclSecurityInformation | UnprotectedSaclSecurityInformation);
			if ((control & SaclProtected) != 0)
				effectiveInformation |= ProtectedSaclSecurityInformation;
			else
				effectiveInformation |= UnprotectedSaclSecurityInformation;
		}

		uint result = RegistryNative.SetSecurityInfo(
			_key.Handle,
			RegistryObjectType,
			effectiveInformation,
			owner,
			group,
			dacl,
			sacl);
		return ToHResult(result);
	}

	public int GetAccessRights(nint objectType, uint flags, out nint access, out uint accessCount, out uint defaultIndex)
	{
		access = _accessRights;
		accessCount = (uint)AccessDefinitions.Length;
		defaultIndex = 9;
		return _accessRights == nint.Zero ? HResultOutOfMemory : HResultSuccess;
	}

	public int MapGeneric(nint objectType, ref byte aceFlags, ref uint accessMask)
	{
		aceFlags &= 0xFE;
		RegistryNative.MapGenericMask(ref accessMask, in GenericMapping);
		return HResultSuccess;
	}

	public int GetInheritTypes(out nint inheritTypes, out uint inheritTypeCount)
	{
		inheritTypes = _inheritTypes;
		inheritTypeCount = (uint)InheritDefinitions.Length;
		return _inheritTypes == nint.Zero ? HResultOutOfMemory : HResultSuccess;
	}

	public int PropertySheetPageCallback(nint hwnd, uint message, uint page)
	{
		return HResultSuccess;
	}

	public void Dispose()
	{
		if (_accessRights != nint.Zero)
		{
			Marshal.FreeHGlobal(_accessRights);
			_accessRights = nint.Zero;
		}
		if (_inheritTypes != nint.Zero)
		{
			Marshal.FreeHGlobal(_inheritTypes);
			_inheritTypes = nint.Zero;
		}

		foreach (nint allocatedString in _allocatedStrings)
			Marshal.FreeCoTaskMem(allocatedString);
		_allocatedStrings.Clear();

		_key?.Dispose();
		_key = null;
		GC.SuppressFinalize(this);
	}

	private nint AllocateString(string value)
	{
		nint pointer = Marshal.StringToCoTaskMemUni(value);
		if (pointer == nint.Zero)
			throw new OutOfMemoryException();

		_allocatedStrings.Add(pointer);
		return pointer;
	}

	private nint AllocateAccessRights()
	{
		int itemSize = Marshal.SizeOf<SecurityAccess>();
		nint buffer = Marshal.AllocHGlobal(checked(itemSize * AccessDefinitions.Length));
		try
		{
			for (int index = 0; index < AccessDefinitions.Length; index++)
			{
				SecurityAccessDefinition definition = AccessDefinitions[index];
				SecurityAccess access = new()
				{
					ObjectType = nint.Zero,
					Mask = definition.Mask,
					Name = AllocateString(definition.Name),
					Flags = definition.Flags,
				};
				Marshal.StructureToPtr(access, IntPtr.Add(buffer, index * itemSize), fDeleteOld: false);
			}

			return buffer;
		}
		catch
		{
			Marshal.FreeHGlobal(buffer);
			throw;
		}
	}

	private nint AllocateInheritTypes()
	{
		int itemSize = Marshal.SizeOf<SecurityInheritType>();
		nint buffer = Marshal.AllocHGlobal(checked(itemSize * InheritDefinitions.Length));
		try
		{
			for (int index = 0; index < InheritDefinitions.Length; index++)
			{
				SecurityInheritDefinition definition = InheritDefinitions[index];
				SecurityInheritType inheritType = new()
				{
					ObjectType = nint.Zero,
					Flags = definition.Flags,
					Name = AllocateString(definition.Name),
				};
				Marshal.StructureToPtr(inheritType, IntPtr.Add(buffer, index * itemSize), fDeleteOld: false);
			}

			return buffer;
		}
		catch
		{
			Marshal.FreeHGlobal(buffer);
			throw;
		}
	}

	private static int ToHResult(uint error)
	{
		if (error == 0)
			return HResultSuccess;

		return unchecked((int)(0x80070000u | error));
	}

	private static int LastErrorAsHResult()
	{
		int error = Marshal.GetLastWin32Error();
		if (error == 0)
			error = 1;

		return ToHResult((uint)error);
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
		internal SecurityInheritDefinition(string name, uint flags)
		{
			Name = name;
			Flags = flags;
		}

		internal string Name { get; }
		internal uint Flags { get; }
	}
}

internal static partial class RegistryNative
{
	[LibraryImport("aclui.dll", EntryPoint = "EditSecurity", SetLastError = true)]
	internal static partial int EditSecurity(nint owner, nint securityInformation);

	[LibraryImport("advapi32.dll", EntryPoint = "GetSecurityInfo")]
	internal static partial uint GetSecurityInfo(
		nint handle,
		uint objectType,
		uint securityInformation,
		out nint owner,
		out nint group,
		out nint dacl,
		out nint sacl,
		out nint securityDescriptor);

	[LibraryImport("advapi32.dll", EntryPoint = "SetSecurityInfo")]
	internal static partial uint SetSecurityInfo(
		nint handle,
		uint objectType,
		uint securityInformation,
		nint owner,
		nint group,
		nint dacl,
		nint sacl);

	[LibraryImport("advapi32.dll", EntryPoint = "GetSecurityDescriptorControl", SetLastError = true)]
	internal static partial int GetSecurityDescriptorControl(
		nint securityDescriptor,
		out ushort control,
		out uint revision);

	[LibraryImport("advapi32.dll", EntryPoint = "GetSecurityDescriptorOwner", SetLastError = true)]
	internal static partial int GetSecurityDescriptorOwner(
		nint securityDescriptor,
		out nint owner,
		out int ownerDefaulted);

	[LibraryImport("advapi32.dll", EntryPoint = "GetSecurityDescriptorGroup", SetLastError = true)]
	internal static partial int GetSecurityDescriptorGroup(
		nint securityDescriptor,
		out nint group,
		out int groupDefaulted);

	[LibraryImport("advapi32.dll", EntryPoint = "GetSecurityDescriptorDacl", SetLastError = true)]
	internal static partial int GetSecurityDescriptorDacl(
		nint securityDescriptor,
		out int daclPresent,
		out nint dacl,
		out int daclDefaulted);

	[LibraryImport("advapi32.dll", EntryPoint = "GetSecurityDescriptorSacl", SetLastError = true)]
	internal static partial int GetSecurityDescriptorSacl(
		nint securityDescriptor,
		out int saclPresent,
		out nint sacl,
		out int saclDefaulted);

	[LibraryImport("advapi32.dll", EntryPoint = "MapGenericMask")]
	internal static partial void MapGenericMask(ref uint accessMask, in RegistryGenericMapping mapping);

	[LibraryImport("kernel32.dll", EntryPoint = "LocalFree")]
	internal static partial nint LocalFree(nint memory);
}
