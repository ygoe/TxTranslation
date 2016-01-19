using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TaskDialogInterop;
using Unclassified.TxEditor.Controls;
using Unclassified.TxEditor.Converters;
using Unclassified.TxEditor.ViewModels;
using Unclassified.TxLib;
using Unclassified.Util;

namespace Unclassified.TxEditor.Views
{
	public partial class TextKeyWizardWindow : Window
	{
		#region Private static data

		private static string prevTextKey;

		#endregion Private static data

		#region Private data

		private string initialClipboardText;
		private string parsedText;
		private bool isPartialString;
		private List<PlaceholderData> placeholders = new List<PlaceholderData>();
		private List<SuggestionViewModel> suggestions = new List<SuggestionViewModel>();

		#endregion Private data

		#region Constructors

		public TextKeyWizardWindow()
		{
			InitializeComponent();

			WindowStartupLocation = WindowStartupLocation.Manual;
			switch (App.Settings.Wizard.SourceCode)
			{
				case "C#":
				default:
					SourceCSharpButton.IsChecked = true;
					break;
				case "XAML":
					SourceXamlButton.IsChecked = true;
					break;
				case "aspx":
					SourceAspxButton.IsChecked = true;
					break;
			}
		}

		#endregion Constructors

		#region Properties

		public IDataObject ClipboardBackup { get; set; }

		#endregion Properties

		#region Window event handlers

		private void Window_Loaded(object sender, RoutedEventArgs args)
		{
			// Require a primary culture for the wizard
			if (MainViewModel.Instance.LoadedCultureNames.Count == 0)
			{
				App.WarningMessage(Tx.T("window.wizard.no culture added"));
				Close();
				return;
			}

			// Text input UI setup
			TranslationText.HiddenChars = App.Settings.View.ShowHiddenChars;
			TranslationText.FontSize = MainViewModel.Instance.FontSize;
			if (App.Settings.View.MonospaceFont)
			{
				MonospaceFontConverter monospaceFontConverter = new MonospaceFontConverter();
				TranslationText.FontFamily = monospaceFontConverter.Convert(App.Settings.View.MonospaceFont, null, null, null) as FontFamily;
			}
			TextOptions.SetTextFormattingMode(TranslationText, MainViewModel.Instance.TextFormattingMode);

			// Read the source text from the clipboard
			initialClipboardText = ReadClipboard();
			if (initialClipboardText == null)
			{
				// Nothing to read, close the dialog again. There's nothing we can do.
				Close();
				return;
			}

			// Restore clipboard
			if (ClipboardBackup != null)
			{
				Clipboard.SetDataObject(ClipboardBackup, true);
				ClipboardBackup = null;
			}

			// Parse the source text and perform other keys lookup
			Reset(true);

			UpdateLayout();

			if (App.Settings != null &&
				App.Settings.Wizard.RememberLocation &&
				App.Settings.Wizard.WindowLeft != int.MinValue &&
				App.Settings.Wizard.WindowTop != int.MinValue)
			{
				Left = App.Settings.Wizard.WindowLeft;
				Top = App.Settings.Wizard.WindowTop;
			}
			else
			{
				Left = SystemParameters.WorkArea.Right - 40 - ActualWidth;
				Top = SystemParameters.WorkArea.Bottom - 40 - ActualHeight;
			}

			Unclassified.Util.WinApi.SetForegroundWindow(new Unclassified.UI.Wpf32Window(this).Handle);

			TextKeyText.Focus();
		}

		private void Window_LocationChanged(object sender, EventArgs args)
		{
			if (App.Settings != null && App.Settings.Wizard.RememberLocation)
			{
				App.Settings.Wizard.WindowLeft = (int)RestoreBounds.Left;
				App.Settings.Wizard.WindowTop = (int)RestoreBounds.Top;
			}
		}

		private void Window_SizeChanged(object sender, SizeChangedEventArgs args)
		{
			if (App.Settings == null || !App.Settings.Wizard.RememberLocation)
			{
				Left = SystemParameters.WorkArea.Right - 40 - ActualWidth;
				Top = SystemParameters.WorkArea.Bottom - 40 - ActualHeight;
			}
		}

		#endregion Window event handlers

		#region Control event handlers

		private void SourceCSharpButton_Checked(object sender, RoutedEventArgs args)
		{
			// Uncheck all other source code buttons
			SourceXamlButton.IsChecked = false;
			SourceAspxButton.IsChecked = false;

			SetDefaultCheckbox.Visibility = Visibility.Collapsed;

			// Re-evaluate the format and parameters
			Reset(false);
		}

		private void SourceXamlButton_Checked(object sender, RoutedEventArgs args)
		{
			// Uncheck all other source code buttons
			SourceCSharpButton.IsChecked = false;
			SourceAspxButton.IsChecked = false;

			SetDefaultCheckbox.Visibility = Visibility.Visible;

			// Re-evaluate the format and parameters
			Reset(false);
		}

		private void SourceAspxButton_Checked(object sender, RoutedEventArgs args)
		{
			// Uncheck all other source code buttons
			SourceCSharpButton.IsChecked = false;
			SourceXamlButton.IsChecked = false;

			SetDefaultCheckbox.Visibility = Visibility.Collapsed;

			// Re-evaluate the format and parameters
			Reset(false);
		}

		private void OtherKeysList_MouseDoubleClick(object sender, MouseButtonEventArgs args)
		{
			if (args.ChangedButton == MouseButton.Left)
			{
				SuggestionViewModel suggestion = OtherKeysList.SelectedItem as SuggestionViewModel;
				if (suggestion != null)
				{
					TextKeyText.Text = suggestion.TextKey;
					AutoSelectKeyText();
					TextKeyText.Focus();
					// TODO: Also update the translated text from suggestion.BaseText? Consider different parameter names!
				}
			}
		}

		[Obfuscation(Exclude = true, Feature = "renaming")]
		private void DecoratedTextBox_ValidateKey(object sender, ValidateKeyEventArgs args)
		{
			args.IsValid = MainViewModel.Instance.TextKeys.ContainsKey(args.TextKey);
		}

		private void OKButton_Click(object sender, RoutedEventArgs args)
		{
			// Unfocus parameter name TextBox to have its changes updated
			OKButton.Focus();

			string textKey = TextKeyText.Text != null ? TextKeyText.Text.Trim() : "";

			// Validate the entered text key
			string errorMessage;
			if (!TextKeyViewModel.ValidateName(textKey, out errorMessage))
			{
				App.WarningMessage(Tx.T("msg.invalid text key entered", "msg", errorMessage));
				TextKeyText.Focus();
				return;
			}

			string colonSuffix = null;
			string translationString = TranslationText.Text;
			if (SourceCSharpButton.IsChecked == true && AddColonCheckbox.IsChecked == true)
			{
				var match = Regex.Match(parsedText, @"^(.+?)\s*:(\s*)$");
				if (match.Success)
				{
					translationString = match.Groups[1].Value;
					colonSuffix = match.Groups[2].Value;
				}
			}

			// Check if the text key already exists but the translated text is different
			TextKeyViewModel existingTextKeyVM;
			if (MainViewModel.Instance.TextKeys.TryGetValue(textKey, out existingTextKeyVM))
			{
				if (translationString != existingTextKeyVM.CultureTextVMs[0].Text)
				{
					TaskDialogResult result = TaskDialog.Show(
						owner: this,
						allowDialogCancellation: true,
						title: "TxEditor",
						mainInstruction: Tx.T("window.wizard.text key exists", "key", Tx.Q(textKey)),
						content: Tx.T("window.wizard.text key exists.content"),
						customButtons: new string[] { Tx.T("task dialog.button.overwrite"), Tx.T("task dialog.button.cancel") });
					if (result.CustomButtonResult != 0)
					{
						TextKeyText.Focus();
						return;
					}
				}
			}

			// Backup current clipboard content
			ClipboardBackup = ClipboardHelper.GetDataObject();

			// Build the text to paste back into the source document, depending on the selected code
			// language
			if (SourceCSharpButton.IsChecked == true)
			{
				App.Settings.Wizard.SourceCode = "C#";

				string keyString = textKey.Replace("\\", "\\\\").Replace("\"", "\\\"");

				StringBuilder codeSb = new StringBuilder();
				if (isPartialString)
				{
					codeSb.Append("\" + ");
				}
				codeSb.Append("Tx.T");
				if (AddColonCheckbox.IsChecked == true)
				{
					codeSb.Append("C");
				}
				codeSb.Append("(\"" + keyString + "\"");
				var countPlaceholder = placeholders.FirstOrDefault(p => p.Name == "#");
				if (countPlaceholder != null)
				{
					codeSb.Append(", ");
					codeSb.Append(countPlaceholder.Code);
				}
				foreach (var pd in placeholders)
				{
					if (pd == countPlaceholder) continue;   // Already processed
					if (string.IsNullOrWhiteSpace(pd.Name)) continue;   // Not used anymore
					codeSb.Append(", \"" + pd.Name.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"");
					codeSb.Append(", ");
					if (pd.IsQuoted)
					{
						codeSb.Append("Tx.Q(");
					}
					codeSb.Append(pd.Code);
					if (pd.IsQuoted)
					{
						codeSb.Append(")");
					}
				}
				codeSb.Append(")");
				if (isPartialString)
				{
					codeSb.Append(" + \"");
					if (!string.IsNullOrEmpty(colonSuffix))
					{
						codeSb.Append(colonSuffix);
					}
				}
				else if (!string.IsNullOrEmpty(colonSuffix))
				{
					codeSb.Append(" + \"");
					codeSb.Append(colonSuffix);
					codeSb.Append("\"");
				}
				Clipboard.SetText(codeSb.ToString());
			}
			if (SourceXamlButton.IsChecked == true)
			{
				App.Settings.Wizard.SourceCode = "XAML";

				string keyString = textKey.Replace("\\", "\\\\").Replace("'", "\\'");
				string defaultString = translationString.Replace("\\", "\\\\").Replace("'", "\\'");

				string code = "{Tx:T '" + keyString + "'";
				if (SetDefaultCheckbox.IsChecked == true)
				{
					code += ", Default='" + defaultString + "'";
				}
				code += "}";
				Clipboard.SetText(code);
			}
			if (SourceAspxButton.IsChecked == true)
			{
				App.Settings.Wizard.SourceCode = "aspx";

				string keyString = textKey.Replace("\\", "\\\\").Replace("\"", "\\\"");

				StringBuilder codeSb = new StringBuilder();
				codeSb.Append("<%= Tx.T(\"" + keyString + "\") %>");
				Clipboard.SetText(codeSb.ToString());
			}

			// Remember the text key for next time and close the wizard dialog window
			prevTextKey = textKey;
			TranslationText.Text = translationString;
			DialogResult = true;
			Close();
		}

		private void ResetButton_Click(object sender, RoutedEventArgs args)
		{
			Reset(false);
		}

		private void CancelButton_Click(object sender, RoutedEventArgs args)
		{
			DialogResult = false;
			Close();
		}

		#endregion Control event handlers

		#region Support functions

		private string ReadClipboard()
		{
			string str = null;
			int retryCount = 20;
			while (true)
			{
				try
				{
					str = Clipboard.GetText();
					break;
				}
				catch (COMException ex)
				{
					retryCount--;
					if (retryCount > 0)
					{
						System.Threading.Thread.Sleep(50);
						continue;
					}
					else
					{
						App.ErrorMessage(Tx.T("window.wizard.error reading clipboard"), ex, "Reading from clipboard");
						return null;
					}
				}
			}
#if DEBUG
			if (retryCount < 20)
			{
				App.WarningMessage("Tried reading the clipboard " + (20 - retryCount) + " times!");
			}
#endif
			return str;
		}

		private void UpdatePlaceholderName(PlaceholderData pd, string newName)
		{
			if (newName != pd.Name)
			{
				TranslationText.Text = TranslationText.Text.Replace("{" + pd.Name + "}", "{" + newName + "}");
				pd.Name = newName;
			}
		}

		private enum LangState
		{
			None,
			String,
			VerbatimString,
			InterpolString,
			Char,
			Parenthesis,
			Bracket,
			Brace
		}

		private void AddParsedPlaceholder(StringBuilder textContent, StringBuilder placeholderContent)
		{
			var pd = new PlaceholderData(placeholders.Count + 1, placeholderContent.ToString());
			if (pd.Code != "")
			{
				placeholders.Add(pd);
				textContent.Append("{").Append(pd.Name).Append("}");
			}
			placeholderContent.Clear();
		}

		private void Reset(bool setTextKey)
		{
			AddColonCheckbox.IsChecked = false;
			AddColonCheckbox.Visibility = Visibility.Collapsed;

			// Detect parameters in the code
			placeholders.Clear();
			parsedText = "";
			isPartialString = false;
			if (initialClipboardText != null)
			{
				if (SourceCSharpButton.IsChecked == true)
				{
					isPartialString = true;
					if ((initialClipboardText.StartsWith("\"") || initialClipboardText.StartsWith("@\"") || initialClipboardText.StartsWith("$\"")) &&
						initialClipboardText.EndsWith("\""))
					{
						isPartialString = false;
					}

					StringBuilder textContent = new StringBuilder();
					StringBuilder placeholderContent = new StringBuilder();
					Stack<LangState> stateStack = new Stack<LangState>();
					if (isPartialString)
					{
						// Assume a simple string
						stateStack.Push(LangState.String);
					}

					for (int pos = 0; pos < initialClipboardText.Length; pos++)
					{
						char ch = initialClipboardText[pos];
						char nextChar = pos + 1 < initialClipboardText.Length ? initialClipboardText[pos + 1] : '\0';
						var allStates = stateStack.ToArray();   // Stack top = Array beginning
						var state = allStates.Length >= 1 ? allStates[0] : LangState.None;
						var prevState = allStates.Length >= 2 ? allStates[1] : LangState.None;

						if (ch == '@' && nextChar == '"' &&
							!(state == LangState.Char || state == LangState.InterpolString || state == LangState.String || state == LangState.VerbatimString))
						{
							// Start verbatim string literal
							stateStack.Push(LangState.VerbatimString);
							if (stateStack.Count == 1)
							{
								AddParsedPlaceholder(textContent, placeholderContent);
							}
							else
							{
								placeholderContent.Append(ch).Append(nextChar);
							}
							// Handled 2 characters
							pos++;
						}
						else if (ch == '$' && nextChar == '"' &&
							!(state == LangState.Char || state == LangState.InterpolString || state == LangState.String || state == LangState.VerbatimString))
						{
							// Start interpolated string literal
							stateStack.Push(LangState.InterpolString);
							if (stateStack.Count == 1)
							{
								AddParsedPlaceholder(textContent, placeholderContent);
							}
							else
							{
								placeholderContent.Append(ch).Append(nextChar);
							}
							// Handled 2 characters
							pos++;
						}
						else if (ch == '"' &&
							!(state == LangState.Char || state == LangState.InterpolString || state == LangState.String || state == LangState.VerbatimString))
						{
							// Start simple string literal
							stateStack.Push(LangState.String);
							if (stateStack.Count == 1)
							{
								AddParsedPlaceholder(textContent, placeholderContent);
							}
							else
							{
								placeholderContent.Append(ch);
							}
						}
						else if (ch == '\\' &&
							(state == LangState.InterpolString || state == LangState.String))
						{
							// String escape sequence, evaluate further characters
							if (stateStack.Count == 1)
							{
								switch (nextChar)
								{
									case '\'':
									case '"':
									case '\\':
										textContent.Append(nextChar);
										break;
									case '0': textContent.Append('\0'); break;
									case 'a': textContent.Append('\a'); break;
									case 'b': textContent.Append('\b'); break;
									case 'f': textContent.Append('\f'); break;
									case 'n': textContent.Append('\n'); break;
									case 'r': textContent.Append('\r'); break;
									case 't': textContent.Append('\t'); break;
									case 'U':
										long value = long.Parse(initialClipboardText.Substring(pos + 2, 8), System.Globalization.NumberStyles.HexNumber);
										//textContent.Append();   // TODO: What does that value mean?
										pos += 8;
										break;
									case 'u':
										int codepoint = int.Parse(initialClipboardText.Substring(pos + 2, 4), System.Globalization.NumberStyles.HexNumber);
										textContent.Append((char)codepoint);
										pos += 4;
										break;
									case 'v': textContent.Append('\v'); break;
									case 'x':
										// TODO: variable length hex value!
										break;
									default:
										// Ignore invalid escape sequences altogether
										break;
								}
							}
							else
							{
								placeholderContent.Append(ch).Append(nextChar);
							}
							// Handled 2 characters (or more)
							pos++;
						}
						else if (ch == '"' && nextChar == '"' &&
							state == LangState.VerbatimString)
						{
							// Verbatim string escape sequence, skip next character
							if (stateStack.Count == 1)
							{
								textContent.Append(nextChar);
							}
							else
							{
								placeholderContent.Append(ch).Append(nextChar);
							}
							pos++;
						}
						else if (ch == '{' && nextChar == '{' &&
							state == LangState.InterpolString)
						{
							// Interpolated string escape sequence, skip next character
							if (stateStack.Count == 1)
							{
								textContent.Append(nextChar);
							}
							else
							{
								placeholderContent.Append(ch).Append(nextChar);
							}
							pos++;
						}
						else if (ch == '}' && nextChar == '}' &&
							state == LangState.InterpolString)
						{
							// Interpolated string escape sequence, skip next character
							if (stateStack.Count == 1)
							{
								textContent.Append(nextChar);
							}
							else
							{
								placeholderContent.Append(ch).Append(nextChar);
							}
							pos++;
						}
						else if (ch == '{' &&
							state == LangState.InterpolString)
						{
							// Start interpolated string code
							stateStack.Push(LangState.None);
							if (stateStack.Count > 2)
							{
								placeholderContent.Append(ch);
							}
						}
						else if (ch == '}' &&
							state == LangState.None &&
							prevState == LangState.InterpolString)
						{
							// End interpolated string code
							stateStack.Pop();
							if (stateStack.Count == 1)
							{
								AddParsedPlaceholder(textContent, placeholderContent);
							}
							else
							{
								placeholderContent.Append(ch);
							}
						}
						else if (ch == '"' &&
							(state == LangState.InterpolString || state == LangState.String || state == LangState.VerbatimString))
						{
							// End string literal
							stateStack.Pop();
							if (stateStack.Count > 1)
							{
								placeholderContent.Append(ch);
							}
						}
						else if (stateStack.Count == 1 &&
							(state == LangState.InterpolString || state == LangState.String || state == LangState.VerbatimString))
						{
							// String content (only on first level)
							textContent.Append(ch);
						}

						else if (ch == '\'' &&
							!(state == LangState.Char || state == LangState.InterpolString || state == LangState.String || state == LangState.VerbatimString))
						{
							// Start char literal
							stateStack.Push(LangState.Char);
							placeholderContent.Append(ch);
						}
						else if (ch == '\\' &&
							state == LangState.Char)
						{
							// Char escape sequence, skip next character
							// (Don't parse char literals, they're always in placeholders. If it's
							// a longer escape sequence like '\u0000' we just take the first two
							// characters now and happily append more characters. We don't care
							// about invalid char literals like 'abc' and just pass them through.)
							placeholderContent.Append(ch).Append(nextChar);
							// Handled 2 characters
							pos++;
						}
						else if (ch == '\'' &&
							state == LangState.Char)
						{
							// End char literal
							placeholderContent.Append(ch);
							stateStack.Pop();
						}

						else if (ch == '(' &&
							!(state == LangState.Char || state == LangState.InterpolString || state == LangState.String || state == LangState.VerbatimString))
						{
							// Open parenthesis
							stateStack.Push(LangState.Parenthesis);
							placeholderContent.Append(ch);
						}
						else if (ch == ')' &&
							state == LangState.Parenthesis)
						{
							// Close parenthesis
							stateStack.Pop();
							placeholderContent.Append(ch);
						}
						else if (ch == '[' &&
							!(state == LangState.Char || state == LangState.InterpolString || state == LangState.String || state == LangState.VerbatimString))
						{
							// Open bracket
							stateStack.Push(LangState.Bracket);
							placeholderContent.Append(ch);
						}
						else if (ch == ']' &&
							state == LangState.Bracket)
						{
							// Close bracket
							stateStack.Pop();
							placeholderContent.Append(ch);
						}
						else if (ch == '{' &&
							!(state == LangState.Char || state == LangState.InterpolString || state == LangState.String || state == LangState.VerbatimString))
						{
							// Open brace
							stateStack.Push(LangState.Brace);
							placeholderContent.Append(ch);
						}
						else if (ch == '}' &&
							state == LangState.Brace)
						{
							// Close brace
							stateStack.Pop();
							placeholderContent.Append(ch);
						}
						else
						{
							// Other code
							placeholderContent.Append(ch);
						}
					}
					if (placeholderContent.Length > 0)
					{
						// Add last placeholder
						AddParsedPlaceholder(textContent, placeholderContent);
					}
					parsedText = textContent.ToString();

					// Detect trailing colon to offer the option to cut off and generate it
					var match = Regex.Match(parsedText, @"^(.+)\s?:(\s*)$");
					if (match.Success)
					{
						AddColonCheckbox.IsChecked = true;
						AddColonCheckbox.Visibility = Visibility.Visible;
					}
				}
				if (SourceXamlButton.IsChecked == true)
				{
					parsedText = initialClipboardText;
					// TODO: Any further processing required?
				}
				if (SourceAspxButton.IsChecked == true)
				{
					// Decode HTML entities
					parsedText = initialClipboardText
						.Replace("&lt;", "<")
						.Replace("&gt;", ">")
						.Replace("&quot;", "\"")
						.Replace("&amp;", "&");
				}
			}

			TranslationText.Text = parsedText;

			ParametersLabel.Visibility = Visibility.Collapsed;
			ParametersGrid.Visibility = Visibility.Collapsed;
			ParametersGrid.Children.Clear();
			if (placeholders.Count > 0)
			{
				ParametersLabel.Visibility = Visibility.Visible;
				ParametersGrid.Visibility = Visibility.Visible;
				int row = 0;
				foreach (var pd in placeholders)
				{
					var localPd = pd;

					ParametersGrid.RowDefinitions.Add(new RowDefinition());

					TextBox nameText = new TextBox();
					nameText.Text = pd.Name;
					nameText.SelectAll();
					nameText.Margin = new Thickness(0, row > 0 ? 4 : 0, 0, 0);
					nameText.LostFocus += (s, e2) => { UpdatePlaceholderName(localPd, nameText.Text); };
					ParametersGrid.Children.Add(nameText);
					Grid.SetRow(nameText, row);
					Grid.SetColumn(nameText, 0);

					TextBox codeText = new TextBox();
					codeText.Text = pd.Code;
					codeText.Margin = new Thickness(4, row > 0 ? 4 : 0, 0, 0);
					codeText.TextChanged += (s, e2) => { localPd.Code = codeText.Text; };
					ParametersGrid.Children.Add(codeText);
					Grid.SetRow(codeText, row);
					Grid.SetColumn(codeText, 1);

					CheckBox quotedCheck = new CheckBox();
					quotedCheck.Content = "Q";
					quotedCheck.IsChecked = pd.IsQuoted;
					quotedCheck.Margin = new Thickness(4, row > 0 ? 4 : 0, 0, 0);
					quotedCheck.VerticalAlignment = VerticalAlignment.Center;
					quotedCheck.Checked += (s, e2) => { localPd.IsQuoted = true; };
					quotedCheck.Unchecked += (s, e2) => { localPd.IsQuoted = false; };
					ParametersGrid.Children.Add(quotedCheck);
					Grid.SetRow(quotedCheck, row);
					Grid.SetColumn(quotedCheck, 2);

					row++;
				}
			}

			suggestions.Clear();
			if (!String.IsNullOrEmpty(TranslationText.Text))
			{
				ScanAllTexts(MainViewModel.Instance.RootTextKey);
			}
			bool havePreviousTextKey = !String.IsNullOrEmpty(prevTextKey);
			bool haveOtherKeys = suggestions.Count > 0;

			// Order suggestions by relevance (descending), then by text key
			suggestions.Sort((a, b) => a.ScoreNum != b.ScoreNum ? -a.ScoreNum.CompareTo(b.ScoreNum) : a.TextKey.CompareTo(b.TextKey));

			string nearestMatch = suggestions.Count > 0 ? suggestions[0].BaseText : null;
			bool haveExactMatch = nearestMatch == TranslationText.Text;

			if (havePreviousTextKey)
			{
				// Store the previous text key with the IsDummy flag at the first position
				suggestions.Insert(0, new SuggestionViewModel(null) { TextKey = prevTextKey, BaseText = "(previous text key)", IsDummy = true });
			}

			// Show the suggestions list if there is at least one other text key and at least two
			// list items to select from (or the nearest text is not an exact match)
			//if (haveOtherKeys &&
			//    (suggestions.Count > 1 || !haveExactMatch))
			//{
				OtherKeysLabel.Visibility = Visibility.Visible;
				OtherKeysList.Visibility = Visibility.Visible;
			//}
			//else
			//{
			//    OtherKeysLabel.Visibility = Visibility.Collapsed;
			//    OtherKeysList.Visibility = Visibility.Collapsed;
			//}
			OtherKeysList.Items.Clear();
			foreach (var suggestion in suggestions)
			{
				OtherKeysList.Items.Add(suggestion);
			}

			// Preset the text key input field if requested and possible
			if (setTextKey)
			{
				if (haveExactMatch)
				{
					if (havePreviousTextKey)
					{
						// There's the previous key and more suggestions (with an exact match), take
						// the first suggestion
						TextKeyText.Text = suggestions[1].TextKey;
						OtherKeysList.SelectedIndex = 1;
					}
					else
					{
						// There's only other suggestions (with an exact match), take the first one
						TextKeyText.Text = suggestions[0].TextKey;
						OtherKeysList.SelectedIndex = 0;
					}
					AutoSelectKeyText();
				}
				else if (havePreviousTextKey)
				{
					// There's only the previous key, take that
					TextKeyText.Text = prevTextKey;
					OtherKeysList.SelectedIndex = 0;
					AutoSelectKeyText();
				}
				// else: We have no exact match to suggest at the moment, leave the text key empty
			}
		}

		private void ScanAllTexts(TextKeyViewModel tk)
		{
			int maxDistance = (int)Math.Round((float)TranslationText.Text.Length / 2, MidpointRounding.AwayFromZero);
			if (tk.IsFullKey && tk.CultureTextVMs[0].Text == TranslationText.Text)
			{
				suggestions.Add(new SuggestionViewModel(null) { TextKey = tk.TextKey, BaseText = tk.CultureTextVMs[0].Text, ScoreNum = 1000, IsExactMatch = true });
			}
			else if (tk.IsFullKey && !String.IsNullOrEmpty(tk.CultureTextVMs[0].Text))
			{
				// TODO: Maybe we should split both strings in words and compare them separately, only accepting if at least one word has a small distance
				int distance = ComputeEditDistance(TranslationText.Text, tk.CultureTextVMs[0].Text);
				if (distance <= maxDistance)
				{
					float score;
					if (distance == 0)
					{
						score = 1000;
					}
					else
					{
						score = 100f / distance;
					}
					suggestions.Add(new SuggestionViewModel(null) { TextKey = tk.TextKey, BaseText = tk.CultureTextVMs[0].Text, ScoreNum = score });
				}
			}
			foreach (TextKeyViewModel child in tk.Children)
			{
				ScanAllTexts(child);
			}
		}

		private void AutoSelectKeyText()
		{
			Match m = Regex.Match(TextKeyText.Text, @"^((?:.*?:)?(?:[^.]*\.)*)([^.]*)$");
			if (m.Success)
			{
				TextKeyText.SelectionStart = m.Groups[1].Length;
				TextKeyText.SelectionLength = m.Groups[2].Length;
			}
		}

		// Based on: https://gist.github.com/449595/cb33c2d0369551d1aa5b6ff5e6a802e21ba4ad5c
		// (c) 2012 Matt Enright, MIT/X11 licence
		// Modified to use float cost instead of int and different cost depending on the type of
		// char difference.

		/// <summary>
		/// Computes the Damerau-Levenshtein distance between two strings.
		/// </summary>
		/// <param name="original"></param>
		/// <param name="modified"></param>
		/// <returns></returns>
		public static int ComputeEditDistance(string original, string modified)
		{
			int len_orig = original.Length;
			int len_diff = modified.Length;

			var matrix = new float[len_orig + 1, len_diff + 1];
			for (int i = 0; i <= len_orig; i++)
			{
				matrix[i, 0] = i;
			}
			for (int j = 0; j <= len_diff; j++)
			{
				matrix[0, j] = j;
			}
			for (int i = 1; i <= len_orig; i++)
			{
				for (int j = 1; j <= len_diff; j++)
				{
					char c1 = modified[j - 1];
					char c2 = original[i - 1];
					float cost;
					if (c1 == c2)
					{
						cost = 0;
					}
					else if (Char.ToLowerInvariant(c1) == Char.ToLowerInvariant(c2))
					{
						// Case-only differences have a smaller distance
						cost = 0.5f;
					}
					else if (Char.IsLetter(c1) != Char.IsLetter(c2))
					{
						// Letter/non-letter differences have a greater distance
						cost = 3;
					}
					else
					{
						cost = 1;
					}
					var vals = new float[]
					{
						matrix[i - 1, j] + 1,
						matrix[i, j - 1] + 1,
						matrix[i - 1, j - 1] + cost
					};
					matrix[i, j] = vals.Min();
					// The Damerau (adjacent transposition) extension:
					if (i > 1 && j > 1 && original[i - 1] == modified[j - 2] && original[i - 2] == modified[j - 1])
					{
						matrix[i, j] = Math.Min(matrix[i, j], matrix[i - 2, j - 2] + cost);
					}
				}
			}
			return (int)Math.Round(matrix[len_orig, len_diff], MidpointRounding.AwayFromZero);
		}

		#endregion Support functions

		#region Placeholder structure

		private class PlaceholderData
		{
			public string Name;
			public string Code;
			public bool IsQuoted;   // TODO: Add detection/handling for this option

			public PlaceholderData(int index, string code)
			{
				// Strip the string glue (the + operators)
				code = code.Trim();
				if (code.StartsWith("+"))
					code = code.Substring(1);
				if (code.EndsWith("+"))
					code = code.Substring(0, code.Length - 1);
				code = code.Trim();

				Name = "param" + index;
				Code = code;
			}
		}

		#endregion Placeholder structure
	}
}
