// Copyright (c) Files Community
// Licensed under the MIT License.

namespace RegistryEditor.Controls;

/// <summary>
/// Describes one path in a layered <see cref="ThemedIconData"/>.
/// </summary>
public sealed class ThemedIconLayer
{
	/// <summary>Gets or sets the semantic color role of this path.</summary>
	public ThemedIconLayerType LayerType { get; set; }

	/// <summary>Gets or sets the SVG path data.</summary>
	public string PathData { get; set; } = string.Empty;

	/// <summary>Gets or sets the opacity of this path.</summary>
	public double Opacity { get; set; } = 1;
}
