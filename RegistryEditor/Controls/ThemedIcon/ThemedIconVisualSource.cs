// Copyright (c) Files Community
// SPDX-License-Identifier: MPL-2.0

using System.Collections.ObjectModel;
using System.Numerics;
using System.Security;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.Graphics.Canvas.Svg;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace RegistryEditor.Controls;

[WinRT.GeneratedWinRTExposedType]
internal sealed partial class ThemedIconVisualSource : IAnimatedVisualSource2
{
	private const string AccentBrushKey = "ThemedIconAccentBrush";
	private const string AccentContrastBrushKey = "ThemedIconAccentContrastBrush";
	private const string AltBrushKey = "ThemedIconAltBrush";
	private const string BaseBrushKey = "ThemedIconBaseBrush";
	private const string CautionBackgroundBrushKey = "ThemedIconCautionBackgroundBrush";
	private const string CautionBrushKey = "ThemedIconCautionBrush";
	private const string CriticalBackgroundBrushKey = "ThemedIconCriticalBackgroundBrush";
	private const string CriticalBrushKey = "ThemedIconCriticalBrush";
	private const string DisabledBrushKey = "ThemedIconDisabledBrush";
	private const string DisabledToggleBrushKey = "ThemedIconDisabledToggleBrush";
	private const string NeutralBackgroundBrushKey = "ThemedIconNeutralBackgroundBrush";
	private const string NeutralBrushKey = "ThemedIconNeutralBrush";
	private const string SuccessBackgroundBrushKey = "ThemedIconSuccessBackgroundBrush";
	private const string SuccessBrushKey = "ThemedIconSuccessBrush";

	private static readonly IReadOnlyDictionary<string, double> _markers = new ReadOnlyDictionary<string, double>(new Dictionary<string, double>());

	private readonly List<BrushBinding> _brushBindings = [];
	private readonly Dictionary<ColorRole, Color> _colorOverrides = [];
	private readonly ThemedIconData _data;
	private readonly float _intrinsicSize;
	private readonly LayerDefinition[] _layers;
	private readonly IconVariant _variant;
	private IReadOnlyDictionary<ColorRole, Color> _colors;
	private ThemedIconColorType _iconColorType;
	private bool _isEnabled;
	private bool _isToggled;

	public IReadOnlyDictionary<string, double> Markers => _markers;

	public ThemedIconVisualSource(
		ThemedIconData data, ThemedIconTypes iconType, ThemedIconColorType iconColorType, bool isFilled, bool isToggled, bool isEnabled, bool isHighContrast,
		Brush? foreground, Brush? customColor, ElementTheme theme, bool useThemeResources)
	{
		_data = data;
		_intrinsicSize = (float)(double.IsFinite(data.Size) && data.Size > 0 ? data.Size : 16);
		_variant = GetActiveVariant(data, iconType, isFilled, isToggled, isEnabled, isHighContrast);
		_layers = CreateLayers(data, _variant);
		_iconColorType = iconColorType;
		_isToggled = isToggled;
		_isEnabled = isEnabled;
		_colors = CreatePalette(foreground, customColor, theme, useThemeResources);
	}

	public IAnimatedVisual TryCreateAnimatedVisual(Compositor compositor, out object diagnostics)
	{
		try
		{
			diagnostics = null!;

			return CreateAnimatedVisual(compositor);
		}
		catch (Exception exception)
		{
			diagnostics = exception;

			return null!;
		}
	}

	public void SetColorProperty(string propertyName, Color value)
	{
		if (!Enum.TryParse<ColorRole>(propertyName, true, out var colorRole))
		{
			return;
		}

		_colorOverrides[colorRole] = value;
		UpdateBrushes();
	}

	public bool UpdateAppearance(
		ThemedIconTypes iconType, ThemedIconColorType iconColorType, bool isFilled, bool isToggled, bool isEnabled, bool isHighContrast,
		Brush? foreground, Brush? customColor, ElementTheme theme, bool useThemeResources)
	{
		if (GetActiveVariant(_data, iconType, isFilled, isToggled, isEnabled, isHighContrast) != _variant)
		{
			return false;
		}

		_iconColorType = iconColorType;
		_isToggled = isToggled;
		_isEnabled = isEnabled;
		_colors = CreatePalette(foreground, customColor, theme, useThemeResources);
		UpdateBrushes();

		return true;
	}

	private static Color ApplyOpacity(Color color, double opacity)
	{
		var effectiveOpacity = Math.Clamp(opacity, 0, 1);
		color.A = (byte)Math.Round(color.A * effectiveOpacity);

		return color;
	}

	private static CanvasGeometry CreateCanvasGeometry(string pathData)
	{
		var escapedPathData = SecurityElement.Escape(pathData) ?? string.Empty;
		using var document = CanvasSvgDocument.LoadFromXml(CanvasDevice.GetSharedDevice(), $"<svg xmlns='http://www.w3.org/2000/svg'><path id='icon' d='{escapedPathData}' /></svg>");
		using var path = document.FindElementById("icon");
		using var pathAttribute = (CanvasSvgPathAttribute)path.GetAttribute("d");

		return pathAttribute.CreatePathGeometry(CanvasFilledRegionDetermination.Alternate);
	}

	private static LayerDefinition[] CreateLayers(ThemedIconData data, IconVariant variant)
	{
		return variant switch
		{
			IconVariant.Outline => CreatePrimaryLayers(data.OutlineData, variant),
			IconVariant.Filled => CreatePrimaryLayers(data.FilledData, variant),
			_ => data.Layers
				.Where(static layer => !string.IsNullOrWhiteSpace(layer.PathData))
				.Select(static layer => new LayerDefinition(layer.PathData, IconVariant.Layered, layer.LayerType, layer.Opacity))
				.ToArray(),
		};
	}

	private static IReadOnlyDictionary<ColorRole, Color> CreatePalette(Brush? foreground, Brush? customColor, ElementTheme theme, bool useThemeResources)
	{
		var foregroundColor = GetBrushColor(foreground, global::Microsoft.UI.Colors.Black);

		return new Dictionary<ColorRole, Color>
		{
			[ColorRole.Foreground] = foregroundColor,
			[ColorRole.Base] = GetResourceColor(BaseBrushKey, foregroundColor, theme, useThemeResources),
			[ColorRole.Alt] = GetResourceColor(AltBrushKey, Color.FromArgb(102, foregroundColor.R, foregroundColor.G, foregroundColor.B), theme, useThemeResources),
			[ColorRole.Accent] = GetResourceColor(AccentBrushKey, foregroundColor, theme, useThemeResources),
			[ColorRole.AccentContrast] = GetResourceColor(AccentContrastBrushKey, global::Microsoft.UI.Colors.White, theme, useThemeResources),
			[ColorRole.Disabled] = GetResourceColor(DisabledBrushKey, Color.FromArgb(92, foregroundColor.R, foregroundColor.G, foregroundColor.B), theme, useThemeResources),
			[ColorRole.DisabledToggle] = GetResourceColor(DisabledToggleBrushKey, Color.FromArgb(140, 255, 255, 255), theme, useThemeResources),
			[ColorRole.Critical] = GetResourceColor(CriticalBrushKey, global::Microsoft.UI.Colors.Red, theme, useThemeResources),
			[ColorRole.CriticalBackground] = GetResourceColor(CriticalBackgroundBrushKey, global::Microsoft.UI.Colors.White, theme, useThemeResources),
			[ColorRole.Caution] = GetResourceColor(CautionBrushKey, global::Microsoft.UI.Colors.Orange, theme, useThemeResources),
			[ColorRole.CautionBackground] = GetResourceColor(CautionBackgroundBrushKey, global::Microsoft.UI.Colors.Black, theme, useThemeResources),
			[ColorRole.Success] = GetResourceColor(SuccessBrushKey, global::Microsoft.UI.Colors.Green, theme, useThemeResources),
			[ColorRole.SuccessBackground] = GetResourceColor(SuccessBackgroundBrushKey, global::Microsoft.UI.Colors.White, theme, useThemeResources),
			[ColorRole.Neutral] = GetResourceColor(NeutralBrushKey, foregroundColor, theme, useThemeResources),
			[ColorRole.NeutralBackground] = GetResourceColor(NeutralBackgroundBrushKey, global::Microsoft.UI.Colors.White, theme, useThemeResources),
			[ColorRole.Custom] = GetBrushColor(customColor, foregroundColor),
		};
	}

	private static LayerDefinition[] CreatePrimaryLayers(string? pathData, IconVariant variant)
	{
		return string.IsNullOrWhiteSpace(pathData) ? [] : [new LayerDefinition(pathData, variant, ThemedIconLayerType.Base, 1)];
	}

	private static Color GetBrushColor(Brush? brush, Color fallback)
	{
		return brush is SolidColorBrush solidColorBrush ? ApplyOpacity(solidColorBrush.Color, solidColorBrush.Opacity) : fallback;
	}

	private static ColorRole GetLayerColorRole(ThemedIconLayerType layerType, ThemedIconColorType iconColorType)
	{
		if (layerType is ThemedIconLayerType.Accent)
		{
			return iconColorType switch
			{
				ThemedIconColorType.Critical => ColorRole.Critical,
				ThemedIconColorType.Caution => ColorRole.Caution,
				ThemedIconColorType.Success => ColorRole.Success,
				ThemedIconColorType.Neutral => ColorRole.Neutral,
				ThemedIconColorType.Custom => ColorRole.Custom,
				_ => ColorRole.Accent,
			};
		}

		if (layerType is ThemedIconLayerType.AccentContrast)
		{
			return iconColorType switch
			{
				ThemedIconColorType.Critical => ColorRole.CriticalBackground,
				ThemedIconColorType.Caution => ColorRole.CautionBackground,
				ThemedIconColorType.Success => ColorRole.SuccessBackground,
				ThemedIconColorType.Neutral => ColorRole.NeutralBackground,
				ThemedIconColorType.Custom => ColorRole.Foreground,
				_ => ColorRole.AccentContrast,
			};
		}

		return layerType is ThemedIconLayerType.Alt ? ColorRole.Alt : ColorRole.Base;
	}

	private static ColorRole GetPrimaryColorRole(ThemedIconColorType iconColorType, bool isFilled, bool isToggled, bool isEnabled)
	{
		if (!isEnabled)
		{
			return isFilled && isToggled ? ColorRole.DisabledToggle : ColorRole.Disabled;
		}

		if (isToggled)
		{
			return ColorRole.AccentContrast;
		}

		return iconColorType switch
		{
			ThemedIconColorType.Critical => ColorRole.Critical,
			ThemedIconColorType.Caution => ColorRole.Caution,
			ThemedIconColorType.Success => ColorRole.Success,
			ThemedIconColorType.Neutral => ColorRole.Neutral,
			ThemedIconColorType.Accent => ColorRole.Accent,
			ThemedIconColorType.Custom => ColorRole.Custom,
			_ => isFilled ? ColorRole.Accent : ColorRole.Base,
		};
	}

	private static Color GetResourceColor(string resourceKey, Color fallback, ElementTheme theme, bool useThemeResources)
	{
		if (!useThemeResources)
		{
			return fallback;
		}

		var effectiveResourceKey = theme is ElementTheme.Default ? resourceKey : GetThemeResourceKey(resourceKey);
		if (Application.Current is not null && TryGetResourceValue(Application.Current.Resources, effectiveResourceKey, theme, out var value))
		{
			return value switch
			{
				Color color => color,
				SolidColorBrush brush => brush.Color,
				_ => fallback,
			};
		}

		return fallback;
	}

	private static string GetThemeResourceKey(string resourceKey)
	{
		return resourceKey switch
		{
			AccentBrushKey => "AccentFillColorDefaultBrush",
			AccentContrastBrushKey => "TextOnAccentFillColorPrimaryBrush",
			AltBrushKey => "ThemedIconAltColor",
			BaseBrushKey => "ThemedIconBaseColor",
			CautionBackgroundBrushKey => "SystemFillColorCautionBackgroundBrush",
			CautionBrushKey => "SystemFillColorCaution",
			CriticalBackgroundBrushKey => "SystemFillColorCriticalBackgroundBrush",
			CriticalBrushKey => "SystemFillColorCritical",
			DisabledBrushKey => "TextFillColorDisabledBrush",
			DisabledToggleBrushKey => "TextOnAccentFillColorDisabledBrush",
			NeutralBackgroundBrushKey => "SystemFillColorNeutralBackgroundBrush",
			NeutralBrushKey => "SystemFillColorSolidNeutral",
			SuccessBackgroundBrushKey => "SystemFillColorSuccessBackgroundBrush",
			SuccessBrushKey => "SystemFillColorSuccess",
			_ => resourceKey,
		};
	}

	private static bool TryGetResourceValue(ResourceDictionary dictionary, string resourceKey, ElementTheme theme, out object? value)
	{
		var themeKey = theme switch
		{
			ElementTheme.Dark => "Default",
			ElementTheme.Light => "Light",
			_ => null,
		};
		if (themeKey is not null)
		{
			if (dictionary.ThemeDictionaries.TryGetValue(themeKey, out var themeValue) &&
				themeValue is ResourceDictionary themeDictionary &&
				TryGetResourceValue(themeDictionary, resourceKey, ElementTheme.Default, out value))
			{
				return true;
			}

			for (var index = dictionary.MergedDictionaries.Count - 1; index >= 0; index--)
			{
				if (TryGetResourceValue(dictionary.MergedDictionaries[index], resourceKey, theme, out value))
				{
					return true;
				}
			}

			value = null;

			return false;
		}

		if (dictionary.TryGetValue(resourceKey, out value))
		{
			return true;
		}

		for (var index = dictionary.MergedDictionaries.Count - 1; index >= 0; index--)
		{
			if (TryGetResourceValue(dictionary.MergedDictionaries[index], resourceKey, theme, out value))
			{
				return true;
			}
		}

		value = null;

		return false;
	}

	private static IconVariant GetActiveVariant(ThemedIconData data, ThemedIconTypes iconType, bool isFilled, bool isToggled, bool isEnabled, bool isHighContrast)
	{
		if ((isFilled || isToggled) && !string.IsNullOrWhiteSpace(data.FilledData))
		{
			return IconVariant.Filled;
		}

		if ((isHighContrast || !isEnabled || iconType is ThemedIconTypes.Outline) && !string.IsNullOrWhiteSpace(data.OutlineData))
		{
			return IconVariant.Outline;
		}

		if (iconType is ThemedIconTypes.Layered && data.Layers.Any(static layer => !string.IsNullOrWhiteSpace(layer.PathData)))
		{
			return IconVariant.Layered;
		}

		if (!string.IsNullOrWhiteSpace(data.OutlineData))
		{
			return IconVariant.Outline;
		}

		return !string.IsNullOrWhiteSpace(data.FilledData) ? IconVariant.Filled : IconVariant.Layered;
	}

	private IAnimatedVisual CreateAnimatedVisual(Compositor compositor)
	{
		_brushBindings.Clear();
		var root = compositor.CreateShapeVisual();
		root.Size = new Vector2(_intrinsicSize, _intrinsicSize);
		var geometries = new List<CanvasGeometry>(_layers.Length);
		foreach (var layer in _layers)
		{
			var canvasGeometry = CreateCanvasGeometry(layer.PathData);
			geometries.Add(canvasGeometry);
			var pathGeometry = compositor.CreatePathGeometry(new CompositionPath(canvasGeometry));
			var shape = compositor.CreateSpriteShape(pathGeometry);
			var brush = compositor.CreateColorBrush(GetLayerColor(layer));
			shape.FillBrush = brush;
			root.Shapes.Add(shape);
			_brushBindings.Add(new BrushBinding(brush, layer));
		}

		return new ThemedIconAnimatedVisual(root, new Vector2(_intrinsicSize, _intrinsicSize), geometries);
	}

	private Color GetLayerColor(LayerDefinition layer)
	{
		var colorRole = layer.Variant switch
		{
			IconVariant.Outline => GetPrimaryColorRole(_iconColorType, false, false, _isEnabled),
			IconVariant.Filled => GetPrimaryColorRole(_iconColorType, true, _isToggled, _isEnabled),
			_ => GetLayerColorRole(layer.LayerType, _iconColorType),
		};
		var color = _colorOverrides.TryGetValue(colorRole, out var overrideColor) ? overrideColor : _colors[colorRole];

		return ApplyOpacity(color, layer.Opacity);
	}

	private void UpdateBrushes()
	{
		foreach (var binding in _brushBindings)
		{
			binding.Brush.Color = GetLayerColor(binding.Layer);
		}
	}

	private enum IconVariant
	{
		Outline,
		Filled,
		Layered,
	}

	private enum ColorRole
	{
		Foreground,
		Base,
		Alt,
		Accent,
		AccentContrast,
		Disabled,
		DisabledToggle,
		Critical,
		CriticalBackground,
		Caution,
		CautionBackground,
		Success,
		SuccessBackground,
		Neutral,
		NeutralBackground,
		Custom,
	}

	private sealed record BrushBinding(CompositionColorBrush Brush, LayerDefinition Layer);

	private sealed record LayerDefinition(string PathData, IconVariant Variant, ThemedIconLayerType LayerType, double Opacity);

	[WinRT.GeneratedWinRTExposedType]
	private sealed partial class ThemedIconAnimatedVisual : IAnimatedVisual
	{
		private readonly IReadOnlyList<CanvasGeometry> _geometries;

		public TimeSpan Duration => TimeSpan.FromSeconds(1);

		public Visual RootVisual { get; }

		public Vector2 Size { get; }

		public ThemedIconAnimatedVisual(Visual rootVisual, Vector2 size, IReadOnlyList<CanvasGeometry> geometries)
		{
			RootVisual = rootVisual;
			Size = size;
			_geometries = geometries;
		}

		public void Dispose()
		{
			RootVisual.Dispose();

			foreach (var geometry in _geometries)
			{
				geometry.Dispose();
			}
		}
	}
}
