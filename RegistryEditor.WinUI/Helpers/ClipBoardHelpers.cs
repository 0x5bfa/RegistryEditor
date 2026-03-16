// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Windows.ApplicationModel.DataTransfer;

namespace RegistryEditor.WinUI.Helpers
{
	public static class ClipBoardHelpers
	{
		public static void SetContent(string str)
		{
			var dp = new DataPackage();
			dp.SetText(str);
			Clipboard.SetContent(dp);
		}
	}
}
