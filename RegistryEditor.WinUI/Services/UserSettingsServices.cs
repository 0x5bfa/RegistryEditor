// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

namespace RegistryEditor.WinUI.Services
{
	internal sealed partial class UserSettingsServices : BaseSettingsServices
	{
		public bool RunAsAdminOnStartup
		{
			get => Get(false);
			set => Set(value);
		}

		public string SelectedAppTheme
		{
			get => Get("Default");
			set => Set(value);
		}

		public bool SetupCompleted
		{
			get => Get(false);
			set => Set(value);
		}

		public string AppThemeBackgroundColor
		{
			get => Get("#00000000");
			set => Set(value);
		}

		public bool UseCompactLayout
		{
			get => Get(false);
			set => Set(value);
		}
	}
}
