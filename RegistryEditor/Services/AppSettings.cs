// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

namespace RegistryEditor.Services;

internal sealed record AppSettings
{
	public string ValueDataFontFamily { get; init; } = "Cascadia Mono";

	public string AppFontFamily { get; init; } = "Segoe UI Variable";

	public int ThemeIndex { get; init; }

	public bool ShowAddressBar { get; init; } = true;
}
