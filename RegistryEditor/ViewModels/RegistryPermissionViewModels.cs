// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Win32;
using System.Security.AccessControl;

namespace RegistryEditor.ViewModels;

public sealed record RegistryPermissionRuleViewModel(string Text);

public sealed record RegistryPermissionChoice(string Name, RegistryRights Rights);
