// Copyright (c) 0x5BFA. All rights reserved.
// Licensed under the MIT license.

namespace RegistryEditor.Views;

public sealed partial class EditValueContentDialog : ContentDialog
{
	private bool _isIntegerValue;
	private bool _isChangingBase;
	private bool _isHexadecimal;

	public EditValueContentDialog()
	{
		InitializeComponent();
	}

	public string ValueName
	{
		get
		{
			return ValueNameTextBox.Text;
		}
	}

	public string ValueData
	{
		get
		{
			return ValueDataTextBox.Text;
		}
	}

	public bool IsHexadecimal
	{
		get
		{
			return BaseRadioButtons.SelectedIndex == 0;
		}
	}

	public void Configure(string valueName, string valueData)
	{
		_isIntegerValue = false;
		BaseRadioButtons.Visibility = Visibility.Collapsed;
		ValueNameTextBox.Text = valueName;
		ValueDataTextBox.Text = valueData;
		ValueNameTextBox.SelectAll();
	}

	public void ConfigureInteger(string valueName, ulong value)
	{
		_isIntegerValue = true;
		_isHexadecimal = true;
		_isChangingBase = true;
		BaseRadioButtons.Visibility = Visibility.Visible;
		BaseRadioButtons.SelectedIndex = 0;
		_isChangingBase = false;
		ValueNameTextBox.Text = valueName;
		ValueDataTextBox.Text = value.ToString("x", System.Globalization.CultureInfo.InvariantCulture);
		ValueNameTextBox.SelectAll();
	}

	private void BaseRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs args)
	{
		if (!_isIntegerValue
			|| _isChangingBase
			|| BaseRadioButtons.SelectedIndex < 0)
			return;

		bool isHexadecimal = BaseRadioButtons.SelectedIndex == 0;
		if (isHexadecimal == _isHexadecimal)
			return;

		if (TryParseValueData(ValueDataTextBox.Text, _isHexadecimal, out ulong value))
		{
			_isChangingBase = true;
			ValueDataTextBox.Text = value.ToString(
				isHexadecimal ? "x" : "0",
				System.Globalization.CultureInfo.InvariantCulture);
			_isChangingBase = false;
		}

		_isHexadecimal = isHexadecimal;
	}

	private static bool TryParseValueData(string text, bool isHexadecimal, out ulong value)
	{
		text = text.Trim();
		if (isHexadecimal && text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
			text = text[2..];

		return isHexadecimal
			? ulong.TryParse(
				text,
				System.Globalization.NumberStyles.AllowHexSpecifier,
				System.Globalization.CultureInfo.InvariantCulture,
				out value)
			: ulong.TryParse(
				text,
				System.Globalization.NumberStyles.Integer,
				System.Globalization.CultureInfo.InvariantCulture,
				out value);
	}
}
