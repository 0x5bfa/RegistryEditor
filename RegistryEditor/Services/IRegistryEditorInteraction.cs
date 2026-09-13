// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using RegistryEditor.ViewModels;

namespace RegistryEditor.Services;

public interface IRegistryEditorInteraction
{
	Task<string?> RequestTextAsync(string title, string placeholder, string initialText);

	Task<bool> ConfirmAsync(string title, string message);

	Task ShowMessageAsync(string title, string message);

	Task<string?> PickImportFileAsync();

	Task<string?> PickHiveFileAsync();

	Task<string?> PickExportFileAsync();

	Task EditPermissionsAsync(RegistryNodeViewModel node);

	Task PrintFileAsync(string filePath);

	Task ShowValueAsync(RegistryValueViewModel value);

	void CopyText(string text);

	void Close();

	void SetAddressBarVisibility(bool isVisible);

	void SetTreePaneWidth(bool isWide);
}
