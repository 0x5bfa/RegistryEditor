// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

namespace RegistryEditor.Controls;

/// <summary>
/// Describes the geometry and intrinsic size of a <see cref="ThemedIcon"/>.
/// </summary>
public sealed class ThemedIconData
{
	internal static ThemedIconData Default { get; } = new()
	{
		Size = 16,
		OutlineData = "M8 2C4.68629 2 2 4.68629 2 8C2 11.3137 4.68629 14 8 14C11.3137 14 14 11.3137 14 8C14 4.68629 11.3137 2 8 2Z" +
			"M1 8C1 4.13401 4.13401 1 8 1C11.866 1 15 4.13401 15 8C15 11.866 11.866 15 8 15C4.13401 15 1 11.866 1 8Z",
		FilledData = "M8 1C4.13401 1 1 4.13401 1 8C1 11.866 4.13401 15 8 15C11.866 15 15 11.866 15 8C15 4.13401 11.866 1 8 1Z",
	};

	/// <summary>Gets or sets the intrinsic coordinate size of the icon data.</summary>
	public double Size { get; set; } = 16;

	/// <summary>Gets or sets the outline path data.</summary>
	public string? OutlineData { get; set; }

	/// <summary>Gets or sets the filled path data.</summary>
	public string? FilledData { get; set; }

	/// <summary>Gets the paths used by the layered variant.</summary>
	public IList<ThemedIconLayer> Layers { get; } = [];
}
