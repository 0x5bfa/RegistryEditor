// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

using Microsoft.UI.Xaml.Media;
using RegistryEditor.Services;

namespace RegistryEditor.Views;

public sealed partial class CustomizeFontContentDialog : ContentDialog
{
	private IReadOnlyList<FontFamilySuggestion> _availableFontFamilies = [];
	private string _selectedValueDataFontFamilyName = "Cascadia Mono";
	private string _selectedAppFontFamilyName = "Segoe UI Variable";

	public CustomizeFontContentDialog()
	{
		InitializeComponent();
		ValueDataFontFamilyAutoSuggestBox.TextChanged += FontFamilyAutoSuggestBox_TextChanged;
		ValueDataFontFamilyAutoSuggestBox.SuggestionChosen += FontFamilyAutoSuggestBox_SuggestionChosen;
		ValueDataFontFamilyAutoSuggestBox.QuerySubmitted += FontFamilyAutoSuggestBox_QuerySubmitted;
		AppFontFamilyAutoSuggestBox.TextChanged += FontFamilyAutoSuggestBox_TextChanged;
		AppFontFamilyAutoSuggestBox.SuggestionChosen += FontFamilyAutoSuggestBox_SuggestionChosen;
		AppFontFamilyAutoSuggestBox.QuerySubmitted += FontFamilyAutoSuggestBox_QuerySubmitted;
	}

	public string SelectedValueDataFontFamilyName
		=> FindInstalledFontFamily(ValueDataFontFamilyAutoSuggestBox.Text)!;

	public string SelectedAppFontFamilyName
		=> FindInstalledFontFamily(AppFontFamilyAutoSuggestBox.Text)!;

	public async Task LoadAvailableFontFamiliesAsync()
	{
		IReadOnlyList<string> fontFamilyNames = await InstalledFontFamilies.GetAsync();
		_availableFontFamilies = fontFamilyNames
			.Select(name => new FontFamilySuggestion(name, new FontFamily(name)))
			.ToArray();
	}

	public void Configure(string valueDataFontFamilyName, string appFontFamilyName)
	{
		_selectedValueDataFontFamilyName = GetAvailableFontFamily(valueDataFontFamilyName, "Cascadia Mono", "Consolas", "Courier New");
		_selectedAppFontFamilyName = GetAvailableFontFamily(appFontFamilyName, "Segoe UI Variable", "Segoe UI", "Arial");

		ConfigureFontFamilyBox(ValueDataFontFamilyAutoSuggestBox, _selectedValueDataFontFamilyName);
		ConfigureFontFamilyBox(AppFontFamilyAutoSuggestBox, _selectedAppFontFamilyName);
	}

	private void ConfigureFontFamilyBox(AutoSuggestBox sender, string fontFamilyName)
	{
		sender.Text = fontFamilyName;
		sender.FontFamily = new FontFamily(fontFamilyName);
		sender.ItemsSource = Array.Empty<FontFamilySuggestion>();
		sender.IsSuggestionListOpen = false;
	}

	private void FontFamilyAutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
	{
		if (args.Reason != AutoSuggestionBoxTextChangeReason.UserInput)
			return;

		IReadOnlyList<FontFamilySuggestion> suggestions = GetMatchingFontFamilies(sender.Text);
		sender.ItemsSource = suggestions;
		sender.IsSuggestionListOpen = suggestions.Count > 0;
		GetErrorTextBlock(sender).Visibility = Visibility.Collapsed;

		if (FindInstalledFontFamily(sender.Text) is { } exactMatch)
		{
			SetSelectedFontFamilyName(sender, exactMatch);
			sender.FontFamily = new FontFamily(exactMatch);
		}
		else
		{
			sender.FontFamily = new FontFamily(GetSelectedFontFamilyName(sender));
		}
	}

	private void FontFamilyAutoSuggestBox_SuggestionChosen(
		AutoSuggestBox sender,
		AutoSuggestBoxSuggestionChosenEventArgs args)
	{
		if (args.SelectedItem is not FontFamilySuggestion suggestion)
			return;

		SelectFontFamily(sender, suggestion.Name);
	}

	private void FontFamilyAutoSuggestBox_QuerySubmitted(
		AutoSuggestBox sender,
		AutoSuggestBoxQuerySubmittedEventArgs args)
	{
		string? fontFamilyName = args.ChosenSuggestion is FontFamilySuggestion suggestion
			? suggestion.Name
			: FindInstalledFontFamily(args.QueryText);

		if (fontFamilyName is null)
		{
			GetErrorTextBlock(sender).Visibility = Visibility.Visible;
			return;
		}

		SelectFontFamily(sender, fontFamilyName);
	}

	private void SelectFontFamily(AutoSuggestBox sender, string fontFamilyName)
	{
		SetSelectedFontFamilyName(sender, fontFamilyName);
		sender.Text = fontFamilyName;
		sender.FontFamily = new FontFamily(fontFamilyName);
		sender.IsSuggestionListOpen = false;
		GetErrorTextBlock(sender).Visibility = Visibility.Collapsed;
	}

	private void CustomizeFontContentDialog_PrimaryButtonClick(
		ContentDialog sender,
		ContentDialogButtonClickEventArgs args)
	{
		if (FindInstalledFontFamily(ValueDataFontFamilyAutoSuggestBox.Text) is null)
		{
			args.Cancel = true;
			ShowInvalidFontFamily(ValueDataFontFamilyAutoSuggestBox, ValueDataFontErrorTextBlock);
			return;
		}

		if (FindInstalledFontFamily(AppFontFamilyAutoSuggestBox.Text) is null)
		{
			args.Cancel = true;
			ShowInvalidFontFamily(AppFontFamilyAutoSuggestBox, AppFontErrorTextBlock);
		}
	}

	private static void ShowInvalidFontFamily(AutoSuggestBox sender, TextBlock errorTextBlock)
	{
		errorTextBlock.Visibility = Visibility.Visible;
		sender.Focus(FocusState.Programmatic);
	}

	private IReadOnlyList<FontFamilySuggestion> GetMatchingFontFamilies(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
			return _availableFontFamilies;

		return _availableFontFamilies
			.Where(fontFamily => fontFamily.Name.Contains(text.Trim(), StringComparison.OrdinalIgnoreCase))
			.ToArray();
	}

	private string GetAvailableFontFamily(string fontFamilyName, params string[] fallbackNames)
	{
		string? availableFontFamily = FindInstalledFontFamily(fontFamilyName);
		if (availableFontFamily is not null)
			return availableFontFamily;

		foreach (string fallbackName in fallbackNames)
		{
			availableFontFamily = FindInstalledFontFamily(fallbackName);
			if (availableFontFamily is not null)
				return availableFontFamily;
		}

		return _availableFontFamilies.FirstOrDefault()?.Name ?? fontFamilyName;
	}

	private string? FindInstalledFontFamily(string fontFamilyName)
		=> _availableFontFamilies.FirstOrDefault(fontFamily =>
			string.Equals(fontFamily.Name, fontFamilyName.Trim(), StringComparison.OrdinalIgnoreCase))?.Name;

	private TextBlock GetErrorTextBlock(AutoSuggestBox sender)
		=> ReferenceEquals(sender, ValueDataFontFamilyAutoSuggestBox)
			? ValueDataFontErrorTextBlock
			: AppFontErrorTextBlock;

	private string GetSelectedFontFamilyName(AutoSuggestBox sender)
		=> ReferenceEquals(sender, ValueDataFontFamilyAutoSuggestBox)
			? _selectedValueDataFontFamilyName
			: _selectedAppFontFamilyName;

	private void SetSelectedFontFamilyName(AutoSuggestBox sender, string fontFamilyName)
	{
		if (ReferenceEquals(sender, ValueDataFontFamilyAutoSuggestBox))
			_selectedValueDataFontFamilyName = fontFamilyName;
		else
			_selectedAppFontFamilyName = fontFamilyName;
	}
}
