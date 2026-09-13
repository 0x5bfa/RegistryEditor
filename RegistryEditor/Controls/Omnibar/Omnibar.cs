// Copyright (c) Files Community
// Licensed under the MIT License.

using System.Collections.Specialized;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.Foundation;

namespace RegistryEditor.Controls
{
	// Content
	[ContentProperty(Name = nameof(Modes))]
	public partial class Omnibar : Control
	{
		// Constants

		private const string TemplatePartName_AutoSuggestBox = "PART_TextBox";
		private const string TemplatePartName_ModesHostGrid = "PART_ModesHostGrid";
		private const string TemplatePartName_AutoSuggestBoxSuggestionsPopup = "PART_SuggestionsPopup";
		private const string TemplatePartName_AutoSuggestBoxSuggestionsContainerBorder = "PART_SuggestionsContainerBorder";
		private const string TemplatePartName_SuggestionsListView = "PART_SuggestionsListView";

		// Fields

		private TextBox _textBox = null!;
		private Grid _modesHostGrid = null!;
		private Popup _textBoxSuggestionsPopup = null!;
		private Border _textBoxSuggestionsContainerBorder = null!;
		private ListView _textBoxSuggestionsListView = null!;

		private readonly Dictionary<OmnibarMode, string> _userInputs = [];
		private INotifyCollectionChanged? _observedModes;

		private OmnibarTextChangeReason _textChangeReason = OmnibarTextChangeReason.None;

		private WeakReference<UIElement?> _previouslyFocusedElement = new(null);
		private int _allowProgrammaticFocusLoss;

		// Events

		public event TypedEventHandler<Omnibar, OmnibarQuerySubmittedEventArgs>? QuerySubmitted;
		public event TypedEventHandler<Omnibar, OmnibarSuggestionChosenEventArgs>? SuggestionChosen;
		public event TypedEventHandler<Omnibar, OmnibarTextChangedEventArgs>? TextChanged;
		public event TypedEventHandler<Omnibar, OmnibarModeChangedEventArgs>? ModeChanged;
		public event TypedEventHandler<Omnibar, OmnibarIsFocusedChangedEventArgs>? IsFocusedChanged;

		// Constructor

		public Omnibar()
		{
			DefaultStyleKey = typeof(Omnibar);

			Modes = [];
			ObserveModes(Modes);
			RegisterPropertyChangedCallback(ModesProperty, (sender, property) =>
			{
				if (sender is Omnibar omnibar)
				{
					omnibar.ObserveModes(omnibar.Modes);
					omnibar.PopulateModes();
				}
			});
			AutoSuggestBoxPadding = new(0, 0, 0, 0);
		}

		// Methods

		protected override void OnApplyTemplate()
		{
			UnhookTemplateParts();
			base.OnApplyTemplate();

			_textBox = GetTemplateChild(TemplatePartName_AutoSuggestBox) as TextBox
				?? throw new MissingFieldException($"Could not find {TemplatePartName_AutoSuggestBox} in the given {nameof(Omnibar)}'s style.");
			_modesHostGrid = GetTemplateChild(TemplatePartName_ModesHostGrid) as Grid
				?? throw new MissingFieldException($"Could not find {TemplatePartName_ModesHostGrid} in the given {nameof(Omnibar)}'s style.");
			_textBoxSuggestionsPopup = GetTemplateChild(TemplatePartName_AutoSuggestBoxSuggestionsPopup) as Popup
				?? throw new MissingFieldException($"Could not find {TemplatePartName_AutoSuggestBoxSuggestionsPopup} in the given {nameof(Omnibar)}'s style.");
			_textBoxSuggestionsContainerBorder = GetTemplateChild(TemplatePartName_AutoSuggestBoxSuggestionsContainerBorder) as Border
				?? throw new MissingFieldException($"Could not find {TemplatePartName_AutoSuggestBoxSuggestionsContainerBorder} in the given {nameof(Omnibar)}'s style.");
			_textBoxSuggestionsListView = GetTemplateChild(TemplatePartName_SuggestionsListView) as ListView
				?? throw new MissingFieldException($"Could not find {TemplatePartName_SuggestionsListView} in the given {nameof(Omnibar)}'s style.");

			PopulateModes();

			SizeChanged += Omnibar_SizeChanged;
			_textBox.GettingFocus += AutoSuggestBox_GettingFocus;
			_textBox.GotFocus += AutoSuggestBox_GotFocus;
			_textBox.LosingFocus += AutoSuggestBox_LosingFocus;
			_textBox.LostFocus += AutoSuggestBox_LostFocus;
			_textBox.KeyDown += AutoSuggestBox_KeyDown;
			_textBox.TextChanged += AutoSuggestBox_TextChanged;
			_textBoxSuggestionsPopup.GettingFocus += AutoSuggestBoxSuggestionsPopup_GettingFocus;
			_textBoxSuggestionsPopup.Opened += AutoSuggestBoxSuggestionsPopup_Opened;
			_textBoxSuggestionsListView.ItemClick += AutoSuggestBoxSuggestionsListView_ItemClick;
			_textBoxSuggestionsListView.SelectionChanged += AutoSuggestBoxSuggestionsListView_SelectionChanged;

			// Set the default width
			_textBoxSuggestionsContainerBorder.Width = ActualWidth;
		}

		public void PopulateModes()
		{
			var modes = Modes;

			if (_modesHostGrid is not null)
			{
				foreach (var mode in _modesHostGrid.Children.OfType<OmnibarMode>())
				{
					if (modes is null || !modes.Contains(mode))
					{
						mode.ClearOwner(this);
					}
				}
			}

			if (modes is null)
			{
				_userInputs.Clear();
				CurrentSelectedMode = null;
				if (_modesHostGrid is not null)
				{
					_modesHostGrid.Children.Clear();
					_modesHostGrid.ColumnDefinitions.Clear();
				}

				return;
			}

			foreach (var mode in _userInputs.Keys.ToArray())
			{
				if (!modes.Contains(mode))
				{
					_userInputs.Remove(mode);
				}
			}

			if (CurrentSelectedMode is null && !string.IsNullOrEmpty(CurrentSelectedModeName))
			{
				CurrentSelectedMode = modes.FirstOrDefault(x => string.Equals(x.ModeName ?? x.Name, CurrentSelectedModeName, StringComparison.Ordinal));
			}

			if (CurrentSelectedMode is null)
			{
				CurrentSelectedMode = modes.FirstOrDefault(x => x.IsDefault) ?? modes.FirstOrDefault();
			}

			if (CurrentSelectedMode is not null && !modes.Contains(CurrentSelectedMode))
			{
				CurrentSelectedMode = modes.FirstOrDefault(x => x.IsDefault) ?? modes.FirstOrDefault();
			}

			if (_modesHostGrid is null)
			{
				return;
			}

			_modesHostGrid.Children.Clear();
			_modesHostGrid.ColumnDefinitions.Clear();

			foreach (var mode in modes)
			{
				// Insert a divider
				if (_modesHostGrid.Children.Count is not 0)
				{
					var divider = new OmnibarModeSeparator();

					_modesHostGrid.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
					Grid.SetColumn(divider, _modesHostGrid.Children.Count);
					_modesHostGrid.Children.Add(divider);
				}

				// Insert the mode
				_modesHostGrid.ColumnDefinitions.Add(new() { Width = GridLength.Auto });
				Grid.SetColumn(mode, _modesHostGrid.Children.Count);
				_modesHostGrid.Children.Add(mode);
				mode.SetOwner(this);
			}

			if (_textBox is not null)
			{
				RestoreCurrentModeTemplateState();
			}
		}

		protected void ChangeMode(OmnibarMode? oldMode, OmnibarMode newMode)
		{
			if (_modesHostGrid is null || Modes is null || CurrentSelectedMode is null)
			{
				return;
			}

			var modesHostGrid = _modesHostGrid;
			var index = modesHostGrid.Children.IndexOf(newMode);
			if (index is -1 || index >= modesHostGrid.ColumnDefinitions.Count)
			{
				return;
			}

			var repositionTransitions = new List<(OmnibarMode Mode, RepositionThemeTransition Transition)>();
			foreach (var mode in Modes)
			{
				var transition = new RepositionThemeTransition();
				mode.Transitions.Add(transition);
				repositionTransitions.Add((mode, transition));
				mode.UpdateLayout();
				mode.IsTabStop = false;
			}

			if (oldMode is not null)
			{
				VisualStateManager.GoToState(oldMode, "Unfocused", true);
			}

			if (!DispatcherQueue.TryEnqueue(() =>
			{
				try
				{
					if (!ReferenceEquals(_modesHostGrid, modesHostGrid))
					{
						return;
					}

					var currentIndex = modesHostGrid.Children.IndexOf(newMode);
					if (currentIndex is -1 || currentIndex >= modesHostGrid.ColumnDefinitions.Count)
					{
						return;
					}

					foreach (var column in modesHostGrid.ColumnDefinitions)
					{
						column.Width = GridLength.Auto;
					}

					modesHostGrid.ColumnDefinitions[currentIndex].Width = new(1, GridUnitType.Star);
				}
				finally
				{
					foreach (var (mode, transition) in repositionTransitions)
					{
						mode.Transitions.Remove(transition);
					}
				}
			}))
			{
				foreach (var (mode, transition) in repositionTransitions)
				{
					mode.Transitions.Remove(transition);
				}
			}

			UpdateAutoSuggestBoxPadding(newMode);

			var newModeText = _userInputs.TryGetValue(newMode, out var savedInput) ? savedInput : newMode.Text ?? string.Empty;
			if (!_userInputs.ContainsKey(newMode))
			{
				_userInputs[newMode] = newModeText;
			}

			if (string.Equals(_textBox.Text, newModeText, StringComparison.Ordinal))
			{
				_textChangeReason = OmnibarTextChangeReason.None;
			}
			else
			{
				_textChangeReason = OmnibarTextChangeReason.ProgrammaticChange;
				ChangeTextBoxText(newModeText);
			}

			VisualStateManager.GoToState(newMode, "Focused", true);
			newMode.IsTabStop = false;

			ModeChanged?.Invoke(this, new(oldMode, newMode!));

			_textBox.PlaceholderText = newMode.PlaceholderText ?? string.Empty;
			_textBoxSuggestionsListView.ItemTemplate = newMode.ItemTemplate;
			_textBoxSuggestionsListView.ItemsSource = newMode.ItemsSource;

			if (newMode.IsAutoFocusEnabled)
			{
				_textBox.Focus(FocusState.Pointer);
			}
			else
			{
				if (IsFocused)
				{
					VisualStateManager.GoToState(newMode, "Focused", true);
					VisualStateManager.GoToState(_textBox, "InputAreaVisible", true);
				}
				else if (newMode?.ContentOnInactive is not null)
				{
					VisualStateManager.GoToState(newMode, "CurrentUnfocused", true);
					VisualStateManager.GoToState(_textBox, "InputAreaCollapsed", true);
				}
				else
				{
					VisualStateManager.GoToState(newMode, "Unfocused", true);
					VisualStateManager.GoToState(_textBox, "InputAreaVisible", true);
				}
			}

			TryToggleIsSuggestionsPopupOpen(true);

		}

		/// <summary>Moves keyboard focus to the text input.</summary>
		public void FocusTextBox()
		{
			if (_textBox is null)
			{
				return;
			}

			_textBox.Focus(FocusState.Keyboard);
		}

		internal void PrepareForShutdown()
		{
			Volatile.Write(ref _allowProgrammaticFocusLoss, 1);
		}

		internal protected bool TryToggleIsSuggestionsPopupOpen(bool wantToOpen)
		{
			if (_textBoxSuggestionsPopup is null)
			{
				return false;
			}

			if (_textBoxSuggestionsListView is null)
			{
				return false;
			}

			if (CurrentSelectedMode is not null)
			{
				_textBoxSuggestionsListView.ItemTemplate = CurrentSelectedMode.ItemTemplate;
				_textBoxSuggestionsListView.ItemsSource = CurrentSelectedMode.ItemsSource;
			}

			if (wantToOpen && (!IsFocused || CurrentSelectedMode is null || _textBoxSuggestionsListView.Items.Count is 0))
			{
				_textBoxSuggestionsPopup.IsOpen = false;

				return false;
			}

			_textBoxSuggestionsPopup.IsOpen = wantToOpen;

			return _textBoxSuggestionsPopup.IsOpen == wantToOpen;
		}

		public void ChooseSuggestionItem(object obj, bool isOriginatedFromArrowKey = false)
		{
			if (CurrentSelectedMode is null || _textBox is null)
			{
				return;
			}

			var shouldUpdateText = isOriginatedFromArrowKey ? CurrentSelectedMode.UpdateTextOnArrowKeys : CurrentSelectedMode.UpdateTextOnSelect;
			if (shouldUpdateText)
			{
				_textChangeReason = OmnibarTextChangeReason.SuggestionChosen;
				ChangeTextBoxText(GetObjectText(obj));
			}

			SuggestionChosen?.Invoke(this, new(CurrentSelectedMode, obj));
		}

		internal protected void ChangeTextBoxText(string text)
		{
			if (_textBox is null)
			{
				return;
			}

			_textBox.Text = text;

			// Move the cursor to the end of the TextBox
			if (_textChangeReason == OmnibarTextChangeReason.SuggestionChosen)
			{
				_textBox?.Select(_textBox.Text.Length, 0);
			}
		}

		internal void ChangeTextBoxTextFromMode(string text)
		{
			if (_textBox is null)
			{
				if (CurrentSelectedMode is { } activeMode)
				{
					_userInputs[activeMode] = text;
				}

				return;
			}

			if (string.Equals(_textBox.Text, text, StringComparison.Ordinal))
			{
				return;
			}

			_textChangeReason = OmnibarTextChangeReason.ProgrammaticChange;
			if (CurrentSelectedMode is { } currentMode)
			{
				_userInputs[currentMode] = text;
			}

			ChangeTextBoxText(text);
		}

		internal void UpdateUserInputFromMode(OmnibarMode mode, string text)
		{
			_userInputs[mode] = text;
		}

		internal void UpdatePlaceholderTextFromMode(OmnibarMode mode, string text)
		{
			if (_textBox is null || !ReferenceEquals(CurrentSelectedMode, mode))
			{
				return;
			}

			_textBox.PlaceholderText = text;
		}

		internal void UpdateModeVisualStateFromMode(OmnibarMode mode)
		{
			if (_textBox is null || !ReferenceEquals(CurrentSelectedMode, mode))
			{
				return;
			}

			ApplyCurrentModeVisualState(mode, false);
		}

		private void SubmitQuery(object? item)
		{
			if (CurrentSelectedMode is null)
			{
				return;
			}

			QuerySubmitted?.Invoke(this, new OmnibarQuerySubmittedEventArgs(CurrentSelectedMode, item, _textBox.Text));

			_textBoxSuggestionsPopup.IsOpen = false;
		}

		private string GetObjectText(object obj)
		{
			if (CurrentSelectedMode is null)
			{
				return string.Empty;
			}

			// Get the text to put into the text box from the chosen suggestion item

			return obj is string text
				? text
				: obj is IOmnibarTextMemberPathProvider textMemberPathProvider
					? textMemberPathProvider.GetTextMemberPath(CurrentSelectedMode.TextMemberPath ?? string.Empty)
					: obj.ToString() ?? string.Empty;
		}

		private void RevertTextToUserInput()
		{
			if (CurrentSelectedMode is not { } currentMode || _textBox is null || _textBoxSuggestionsListView is null)
			{
				return;
			}

			_textBoxSuggestionsListView.SelectedIndex = -1;
			var userInput = _userInputs.TryGetValue(currentMode, out var savedInput) ? savedInput : currentMode.Text ?? string.Empty;
			if (string.Equals(_textBox.Text, userInput, StringComparison.Ordinal))
			{
				_textChangeReason = OmnibarTextChangeReason.None;
			}
			else
			{
				_textChangeReason = OmnibarTextChangeReason.ProgrammaticChange;
				ChangeTextBoxText(userInput);
			}
		}

		private void ObserveModes(IList<OmnibarMode>? modes)
		{
			if (ReferenceEquals(_observedModes, modes))
			{
				return;
			}

			if (_observedModes is not null)
			{
				_observedModes.CollectionChanged -= Modes_CollectionChanged;
			}

			_observedModes = modes as INotifyCollectionChanged;
			if (_observedModes is not null)
			{
				_observedModes.CollectionChanged += Modes_CollectionChanged;
			}
		}

		private void Modes_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
		{
			PopulateModes();
		}

		private void RestoreCurrentModeTemplateState()
		{
			if (CurrentSelectedMode is not { } currentMode || _textBox is null || _textBoxSuggestionsListView is null)
			{
				return;
			}

			var currentModeText = _userInputs.TryGetValue(currentMode, out var savedInput) ? savedInput : currentMode.Text ?? string.Empty;
			if (string.Equals(_textBox.Text, currentModeText, StringComparison.Ordinal))
			{
				_textChangeReason = OmnibarTextChangeReason.None;
			}
			else
			{
				_textChangeReason = OmnibarTextChangeReason.ProgrammaticChange;
				_textBox.Text = currentModeText;
			}

			currentMode.Text = currentModeText;
			_userInputs.TryAdd(currentMode, currentModeText);
			_textBox.PlaceholderText = currentMode.PlaceholderText ?? string.Empty;
			_textBoxSuggestionsListView.ItemTemplate = currentMode.ItemTemplate;
			_textBoxSuggestionsListView.ItemsSource = currentMode.ItemsSource;
			currentMode.IsTabStop = false;

			var currentModeIndex = _modesHostGrid.Children.IndexOf(currentMode);
			if (currentModeIndex is not -1 && currentModeIndex < _modesHostGrid.ColumnDefinitions.Count)
			{
				_modesHostGrid.ColumnDefinitions[currentModeIndex].Width = new(1, GridUnitType.Star);
			}

			UpdateAutoSuggestBoxPadding(currentMode);

			ApplyCurrentModeVisualState(currentMode, false);

			var modesHostGrid = _modesHostGrid;
			DispatcherQueue.TryEnqueue(() =>
			{
				if (ReferenceEquals(_modesHostGrid, modesHostGrid) && ReferenceEquals(CurrentSelectedMode, currentMode))
				{
					ApplyCurrentModeVisualState(currentMode, false);
					UpdateAutoSuggestBoxPadding(currentMode);
				}
			});

			_textChangeReason = OmnibarTextChangeReason.None;
		}

		private void UpdateAutoSuggestBoxPadding(OmnibarMode selectedMode)
		{
			if (Modes is not { Count: > 0 } modes || _modesHostGrid is null)
			{
				AutoSuggestBoxPadding = new(0, 0, 0, 0);

				return;
			}

			var itemIndex = modes.IndexOf(selectedMode);
			if (itemIndex is -1)
			{
				return;
			}

			var separator = _modesHostGrid.Children.OfType<OmnibarModeSeparator>().FirstOrDefault();
			var separatorWidth = separator?.ActualWidth ?? 0;
			if (separatorWidth is 0 && separator is not null)
			{
				separatorWidth = separator.DesiredSize.Width;
			}

			var modeWidths = modes.Select(static mode => mode.ModeButtonWidth).ToArray();
			var leftPadding = modeWidths.Take(itemIndex + 1).Sum() + separatorWidth * itemIndex;
			var rightPadding = modeWidths.Skip(itemIndex + 1).Sum() + separatorWidth * (modes.Count - itemIndex - 1) + 8;

			AutoSuggestBoxPadding = new(leftPadding, 0, rightPadding, 0);
		}

		private void ApplyCurrentModeVisualState(OmnibarMode currentMode, bool useTransitions)
		{
			if (_textBox is null)
			{
				return;
			}

			if (IsFocused)
			{
				VisualStateManager.GoToState(currentMode, "Focused", useTransitions);
				VisualStateManager.GoToState(_textBox, "InputAreaVisible", useTransitions);
			}
			else if (currentMode.ContentOnInactive is not null)
			{
				VisualStateManager.GoToState(currentMode, "CurrentUnfocused", useTransitions);
				VisualStateManager.GoToState(_textBox, "InputAreaCollapsed", useTransitions);
			}
			else
			{
				VisualStateManager.GoToState(currentMode, "Unfocused", useTransitions);
				VisualStateManager.GoToState(_textBox, "InputAreaVisible", useTransitions);
			}
		}

		private void UnhookTemplateParts()
		{
			SizeChanged -= Omnibar_SizeChanged;
			if (_textBox is not null)
			{
				_textBox.GettingFocus -= AutoSuggestBox_GettingFocus;
				_textBox.GotFocus -= AutoSuggestBox_GotFocus;
				_textBox.LosingFocus -= AutoSuggestBox_LosingFocus;
				_textBox.LostFocus -= AutoSuggestBox_LostFocus;
				_textBox.KeyDown -= AutoSuggestBox_KeyDown;
				_textBox.TextChanged -= AutoSuggestBox_TextChanged;
			}

			if (_textBoxSuggestionsPopup is not null)
			{
				_textBoxSuggestionsPopup.IsOpen = false;
				_textBoxSuggestionsPopup.GettingFocus -= AutoSuggestBoxSuggestionsPopup_GettingFocus;
				_textBoxSuggestionsPopup.Opened -= AutoSuggestBoxSuggestionsPopup_Opened;
			}

			if (_textBoxSuggestionsListView is not null)
			{
				_textBoxSuggestionsListView.ItemClick -= AutoSuggestBoxSuggestionsListView_ItemClick;
				_textBoxSuggestionsListView.SelectionChanged -= AutoSuggestBoxSuggestionsListView_SelectionChanged;
			}

			if (_modesHostGrid is not null)
			{
				_modesHostGrid.Children.Clear();
				_modesHostGrid.ColumnDefinitions.Clear();
			}

			_textBox = null!;
			_modesHostGrid = null!;
			_textBoxSuggestionsPopup = null!;
			_textBoxSuggestionsContainerBorder = null!;
			_textBoxSuggestionsListView = null!;
		}
	}
}

