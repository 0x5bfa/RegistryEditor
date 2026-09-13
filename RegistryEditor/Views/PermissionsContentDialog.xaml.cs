// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.Win32;
using RegistryEditor.ViewModels;
using System.Security.AccessControl;

namespace RegistryEditor.Views;

public sealed partial class PermissionsContentDialog : ContentDialog
{
	public PermissionsContentDialog()
	{
		InitializeComponent();
	}

	public string Account => AccountTextBox.Text.Trim();

	public RegistryPermissionChoice? SelectedPermission
		=> RightsComboBox.SelectedItem as RegistryPermissionChoice;

	public AccessControlType SelectedAccessType
		=> AccessTypeComboBox.SelectedItem is AccessControlType accessType
			? accessType
			: AccessControlType.Allow;

	public void Configure(string keyPath, IReadOnlyList<RegistryPermissionRuleViewModel> rules)
	{
		Title = "Access permissions";
		KeyPathTextBlock.Text = keyPath;
		RulesListView.ItemsSource = rules;
		RightsComboBox.ItemsSource = new[]
		{
			new RegistryPermissionChoice("Read", RegistryRights.ReadKey),
			new RegistryPermissionChoice(
				"Read and write",
				RegistryRights.ReadKey | RegistryRights.SetValue | RegistryRights.CreateSubKey),
			new RegistryPermissionChoice("Full control", RegistryRights.FullControl),
		};
		RightsComboBox.SelectedIndex = 0;
		AccessTypeComboBox.ItemsSource = new[] { AccessControlType.Allow, AccessControlType.Deny };
		AccessTypeComboBox.SelectedIndex = 0;
		ErrorTextBlock.Text = string.Empty;
	}

	public void SetError(string message)
		=> ErrorTextBlock.Text = message;
}
