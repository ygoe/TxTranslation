using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TaskDialogInterop;
using TxEditor.Converters;
using TxEditor.ViewModel;
using TxLib;
using System.Runtime.InteropServices;

namespace TxEditor.View
{
	public partial class TextKeyWizardWindow : Window
	{
		#region Private static data

		private static string prevTextKey;

		#endregion Private static data

		#region Private data

		private string exactMatchTextKey;   // TODO: remove
		private string initialClipboardText;
		private string parsedText;
		private List<PlaceholderData> placeholders = new List<PlaceholderData>();
		private int keySuggestPhase;   // TODO: remove?
		private List<SuggestionViewModel> suggestions = new List<SuggestionViewModel>();

		#endregion Private data

		#region Constructors

		public TextKeyWizardWindow()
		{
			InitializeComponent();

			WindowStartupLocation = WindowStartupLocation.Manual;

			SourceCodeCombobox.Items.Add("C#");
			SourceCodeCombobox.Items.Add("XAML");
			SourceCodeCombobox.SelectedItem = App.Settings.WizardSourceCode;
		}

		#endregion Constructors

		#region Window event handlers

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			if (MainWindowViewModel.Instance.LoadedCultureNames.Count == 0)
			{
				MessageBox.Show(
					Tx.T("window.wizard.no culture added"),
					Tx.T("msg.caption.error"),
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				Close();
				return;
			}

			TranslationText.HiddenChars = App.Settings.ShowHiddenChars;
			TranslationText.FontSize = MainWindowViewModel.Instance.FontSize;
			if (App.Settings.MonospaceFont)
			{
				MonospaceFontConverter monospaceFontConverter = new MonospaceFontConverter();
				TranslationText.FontFamily = monospaceFontConverter.Convert(App.Settings.MonospaceFont, null, null, null) as FontFamily;
			}
			TextOptions.SetTextFormattingMode(TranslationText, MainWindowViewModel.Instance.TextFormattingMode);

			initialClipboardText = ReadClipboard();
			if (initialClipboardText == null)
			{
				Close();
				return;
			}

			ResetButton_Click(null, null);

			suggestions.Clear();
			ScanAllTexts(MainWindowViewModel.Instance.RootTextKey);
	
			if (exactMatchTextKey != null)
			{
				TextKeyText.Text = exactMatchTextKey;
				keySuggestPhase = 2;
				AutoSelectKeyText();
			}
			else if (!string.IsNullOrEmpty(prevTextKey))
			{
				TextKeyText.Text = prevTextKey;
				keySuggestPhase = 1;
				AutoSelectKeyText();
			}
			else
			{
				keySuggestPhase = 0;
			}

			// TODO: Find similar texts and suggest them with their text keys for selection in the ListBox
			OtherKeysLabel.Visibility = Visibility.Collapsed;
			OtherKeysList.Visibility = Visibility.Collapsed;

			UpdateLayout();

			if (App.Settings != null && App.Settings.WizardRememberLocation)
			{
				Left = App.Settings.GetInt("wizard.window.left", (int) (SystemParameters.WorkArea.Right - 40 - ActualWidth));
				Top = App.Settings.GetInt("wizard.window.top", (int) (SystemParameters.WorkArea.Bottom - 40 - ActualHeight));
			}
			else
			{
				Left = SystemParameters.WorkArea.Right - 40 - ActualWidth;
				Top = SystemParameters.WorkArea.Bottom - 40 - ActualHeight;
			}

			Unclassified.WinApi.SetForegroundWindow(new Unclassified.UI.Wpf32Window(this).Handle);

			TextKeyText.Focus();
		}

		private void Window_LocationChanged(object sender, EventArgs e)
		{
			if (App.Settings != null && App.Settings.WizardRememberLocation)
			{
				App.Settings.Set("wizard.window.left", (int) RestoreBounds.Left);
				App.Settings.Set("wizard.window.top", (int) RestoreBounds.Top);
			}
		}

		private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			if (App.Settings == null || !App.Settings.WizardRememberLocation)
			{
				Left = SystemParameters.WorkArea.Right - 40 - ActualWidth;
				Top = SystemParameters.WorkArea.Bottom - 40 - ActualHeight;
			}
		}

		#endregion Window event handlers

		#region Control event handlers

		private void SourceCodeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			SetDefaultCheckbox.Visibility =
				SourceCodeCombobox.SelectedItem as string == "XAML" ? Visibility.Visible : Visibility.Collapsed;
			
			// Re-evaluate the format and parameters
			ResetButton_Click(null, null);
		}

		private void TextKeyText_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (TextKeyText.Text == "" && keySuggestPhase == 2)
			{
				// Other matching key was first suggested and now entirely deleted.
				// Suggest the previously entered key, if it's different.
				if (prevTextKey != exactMatchTextKey)
				{
					TextKeyText.Text = prevTextKey;
					keySuggestPhase = 1;
					AutoSelectKeyText();
				}
				else
				{
					// No further suggestions available.
					keySuggestPhase = 0;
				}
			}
		}

		private void DecoratedTextBox_ValidateKey(object sender, ValidateKeyEventArgs e)
		{
			e.IsValid = MainWindowViewModel.Instance.TextKeys.ContainsKey(e.TextKey);
		}

		private void OKButton_Click(object sender, RoutedEventArgs e)
		{
			string textKey = TextKeyText.Text != null ? TextKeyText.Text.Trim() : "";
			
			string errorMessage;
			if (!TextKeyViewModel.ValidateName(textKey, out errorMessage))
			{
				MessageBox.Show(
					Tx.T("msg.invalid text key entered", "msg", errorMessage),
					Tx.T("msg.caption.error"),
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				return;
			}
			// TODO: Find another source than parsedText, use the actual text key content instead
			if (TranslationText.Text != parsedText &&
				MainWindowViewModel.Instance.TextKeys.ContainsKey(textKey))
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
					return;
				}
			}

			if (SourceCodeCombobox.SelectedIndex == 0)   // C#
			{
				App.Settings.WizardSourceCode = "C#";

				string keyString = textKey.Replace("\\", "\\\\").Replace("\"", "\\\"");

				StringBuilder codeSb = new StringBuilder();
				codeSb.Append("Tx.T(\"" + keyString + "\"");
				foreach (var pd in placeholders)
				{
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
				Clipboard.SetText(codeSb.ToString());
			}
			if (SourceCodeCombobox.SelectedIndex == 1)   // XAML
			{
				App.Settings.WizardSourceCode = "XAML";

				string keyString = textKey.Replace("\\", "\\\\").Replace("'", "\\'");
				string defaultString = TranslationText.Text.Replace("\\", "\\\\").Replace("'", "\\'");

				string code = "{Tx:T '" + keyString + "'";
				if (SetDefaultCheckbox.IsChecked == true)
				{
					code += ", Default='" + defaultString + "'";
				}
				code += "}";
				Clipboard.SetText(code);
			}

			prevTextKey = textKey;
			DialogResult = true;
			Close();
		}

		private void ResetButton_Click(object sender, RoutedEventArgs e)
		{
			// Detect parameters in the code
			placeholders.Clear();
			parsedText = "";
			if (initialClipboardText != null)
			{
				if (SourceCodeCombobox.SelectedIndex == 0)   // C#
				{
					bool inStringLiteral = false;
					bool isVerbatimString = false;
					bool inCharLiteral = false;
					int parensLevel = 0;
					int bracketsLevel = 0;
					int bracesLevel = 0;
					int lastStringEnd = 0;
					StringBuilder stringContent = new StringBuilder();

					for (int pos = 0; pos < initialClipboardText.Length; pos++)
					{
						char ch = initialClipboardText[pos];
						char nextChar = pos + 1 < initialClipboardText.Length ? initialClipboardText[pos + 1] : '\0';

						if (!inStringLiteral && !inCharLiteral && ch == '\'')
						{
							// Start char literal
							inCharLiteral = true;
						}
						else if (inCharLiteral && ch == '\\')
						{
							// Escape sequence, skip next character
							pos++;
						}
						else if (inCharLiteral && ch == '\'')
						{
							// End char literal
							inCharLiteral = false;
						}

						else if (!inStringLiteral && !inCharLiteral && ch == '@' && nextChar == '"')
						{
							// Start verbatim string literal
							inStringLiteral = true;
							isVerbatimString = true;

							if (parensLevel == 0 && bracketsLevel == 0 && bracesLevel == 0 && pos > 0)
							{
								string code = initialClipboardText.Substring(lastStringEnd, pos - lastStringEnd);
								var pd = new PlaceholderData(placeholders.Count + 1, code);
								placeholders.Add(pd);
								parsedText += "{" + pd.Name + "}";
							}

							// Handled 2 characters
							pos++;
						}
						else if (!inStringLiteral && !inCharLiteral && ch == '"')
						{
							// Start string literal
							inStringLiteral = true;

							if (parensLevel == 0 && bracketsLevel == 0 && bracesLevel == 0 && pos > 0)
							{
								string code = initialClipboardText.Substring(lastStringEnd, pos - lastStringEnd);
								var pd = new PlaceholderData(placeholders.Count + 1, code);
								placeholders.Add(pd);
								parsedText += "{" + pd.Name + "}";
							}
						}
						else if (inStringLiteral && !isVerbatimString && ch == '\\')
						{
							// Escape sequence, skip next character
							pos++;

							switch (nextChar)
							{
								case '\'':
								case '"':
								case '\\':
									stringContent.Append(nextChar);
									break;
								case '0': stringContent.Append('\0'); break;
								case 'a': stringContent.Append('\a'); break;
								case 'b': stringContent.Append('\b'); break;
								case 'f': stringContent.Append('\f'); break;
								case 'n': stringContent.Append('\n'); break;
								case 'r': stringContent.Append('\r'); break;
								case 't': stringContent.Append('\t'); break;
								case 'U':
									long value = long.Parse(initialClipboardText.Substring(pos + 1, 8), System.Globalization.NumberStyles.HexNumber);
									//stringContent.Append();   // TODO: What does that value mean?
									pos += 8;
									break;
								case 'u':
									int codepoint = int.Parse(initialClipboardText.Substring(pos + 1, 4), System.Globalization.NumberStyles.HexNumber);
									stringContent.Append((char) codepoint);
									pos += 4;
									break;
								case 'v': stringContent.Append('\v'); break;
								case 'x':
									// TODO: variable length hex value!
									break;
							}
						}
						else if (inStringLiteral && isVerbatimString && ch == '"' && nextChar == '"')
						{
							// Escape sequence, skip next character
							pos++;
							stringContent.Append(nextChar);
						}
						else if (inStringLiteral && ch == '"')
						{
							// End string literal
							inStringLiteral = false;
							isVerbatimString = false;
							lastStringEnd = pos + 1;

							if (parensLevel == 0 && bracketsLevel == 0 && bracesLevel == 0)
							{
								parsedText += stringContent.ToString();
							}
							stringContent.Clear();
						}
						else if (inStringLiteral)
						{
							// Append character to text
							stringContent.Append(ch);
						}

						else if (!inStringLiteral && !inCharLiteral && ch == '(')
						{
							parensLevel++;
						}
						else if (!inStringLiteral && !inCharLiteral && ch == ')')
						{
							if (parensLevel > 0)
								parensLevel--;
						}
						else if (!inStringLiteral && !inCharLiteral && ch == '[')
						{
							bracketsLevel++;
						}
						else if (!inStringLiteral && !inCharLiteral && ch == ']')
						{
							if (bracketsLevel > 0)
								bracketsLevel--;
						}
						else if (!inStringLiteral && !inCharLiteral && ch == '{')
						{
							bracesLevel++;
						}
						else if (!inStringLiteral && !inCharLiteral && ch == '}')
						{
							if (bracesLevel > 0)
								bracesLevel--;
						}
					}
					if (lastStringEnd < initialClipboardText.Length)
					{
						// Some non-string content is still left (parameter at the end)
						string code = initialClipboardText.Substring(lastStringEnd);
						var pd = new PlaceholderData(placeholders.Count + 1, code);
						placeholders.Add(pd);
						parsedText += "{" + pd.Name + "}";
					}
				}
				if (SourceCodeCombobox.SelectedIndex == 1)   // XAML
				{
					parsedText = initialClipboardText;
					// TODO: Any further processing required?
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
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
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
						// TODO: Log exception
						MessageBox.Show(
							Tx.T("window.wizard.error reading clipboard"),
							Tx.T("msg.caption.error"),
							MessageBoxButton.OK,
							MessageBoxImage.Error);
						return null;
					}
				}
			}
#if DEBUG
			if (retryCount < 20)
			{
				MessageBox.Show(
					"Tried reading the clipboard " + (20 - retryCount) + " times!",
					Tx.T("msg.caption.warning"),
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
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

		private void ScanAllTexts(TextKeyViewModel tk)
		{
			if (tk.IsFullKey && tk.CultureTextVMs[0].Text == TranslationText.Text)
			{
				exactMatchTextKey = tk.TextKey;
			}

			foreach (TextKeyViewModel child in tk.Children)
			{
				ScanAllTexts(child);
			}
		}

		private void AutoSelectKeyText()
		{
			if (keySuggestPhase == 2)
			{
				TextKeyText.SelectAll();
			}
			else if (keySuggestPhase == 1)
			{
				Match m = Regex.Match(TextKeyText.Text, @"^((?:.*?:)?(?:[^.]*\.)*)([^.]*)$");
				if (m.Success)
				{
					TextKeyText.SelectionStart = m.Groups[1].Length;
					TextKeyText.SelectionLength = m.Groups[2].Length;
				}
			}
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
