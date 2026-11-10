// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace RegistryEditor.Services;

internal static unsafe class InstalledFontFamilies
{
	private static readonly Lazy<Task<IReadOnlyList<string>>> FontFamilies = new(() =>
		Task.Run<IReadOnlyList<string>>(Enumerate));

	public static Task<IReadOnlyList<string>> GetAsync()
		=> FontFamilies.Value;

	private static IReadOnlyList<string> Enumerate()
	{
		HashSet<string> fontFamilies = new(StringComparer.OrdinalIgnoreCase);
		HDC deviceContext = PInvoke.GetDC(HWND.Null);
		if (deviceContext.IsNull)
			throw new InvalidOperationException("Unable to get a device context for font enumeration.");

		GCHandle fontFamiliesHandle = GCHandle.Alloc(fontFamilies);
		try
		{
			LOGFONTW logFont = default;
			logFont.lfCharSet = FONT_CHARSET.DEFAULT_CHARSET;
			PInvoke.EnumFontFamiliesEx(
				deviceContext,
				in logFont,
				&EnumFontFamilyCallback,
				GCHandle.ToIntPtr(fontFamiliesHandle),
				0);
		}
		finally
		{
			fontFamiliesHandle.Free();
			PInvoke.ReleaseDC(HWND.Null, deviceContext);
		}

		string[] installedFonts = fontFamilies
			.Where(fontFamily => !fontFamily.StartsWith('@'))
			.OrderBy(fontFamily => fontFamily, StringComparer.CurrentCultureIgnoreCase)
			.ToArray();

		if (installedFonts.Length == 0)
			throw new InvalidOperationException("No installed fonts could be enumerated.");

		return installedFonts;
	}

	[UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvStdcall) })]
	private static int EnumFontFamilyCallback(LOGFONTW* logFont, TEXTMETRICW* textMetric, uint fontType, LPARAM lParam)
	{
		if (logFont is null || lParam.Value == nint.Zero)
			return 1;

		try
		{
			if (GCHandle.FromIntPtr((nint)lParam).Target is not HashSet<string> fontFamilies)
				return 1;

			ReadOnlySpan<char> faceName = logFont->lfFaceName.AsReadOnlySpan();
			int nullTerminator = faceName.IndexOf('\0');
			if (nullTerminator >= 0)
				faceName = faceName[..nullTerminator];

			string fontFamily = faceName.ToString();
			if (!string.IsNullOrWhiteSpace(fontFamily) && !fontFamily.StartsWith('@'))
				fontFamilies.Add(fontFamily);
		}
		catch
		{
			return 0;
		}

		return 1;
	}
}
