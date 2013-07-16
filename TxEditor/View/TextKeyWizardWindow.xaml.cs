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

		private string exactMatchTextKey;
		private string initialText;
		private int keySuggestPhase;

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
			TranslationText.HiddenChars = App.Settings.ShowHiddenChars;
			TranslationText.FontSize = MainWindowViewModel.Instance.FontSize;
			if (App.Settings.MonospaceFont)
			{
				MonospaceFontConverter monospaceFontConverter = new MonospaceFontConverter();
				TranslationText.FontFamily = monospaceFontConverter.Convert(App.Settings.MonospaceFont, null, null, null) as FontFamily;
			}
			TextOptions.SetTextFormattingMode(TranslationText, MainWindowViewModel.Instance.TextFormattingMode);

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
						MessageBox.Show("Error reading the clipboard.\n\n" + ex.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
						Close();
						return;
					}
				}
			}
			if (retryCount < 20)
			{
				MessageBox.Show("Tried reading the clipboard " + (20 - retryCount) + " times!");
			}
			
			Match m = Regex.Match(str, @"^""(.*)""$");
			if (m.Success)
			{
				TranslationText.Text = m.Groups[1].Value
					.Replace("\\n", "\n")
					.Replace("\\r", "")
					.Replace("\\t", "    ")
					.Replace("\\\"", "\"")
					.Replace("\\\\", "\\");
			}
			else
			{
				TranslationText.Text = str;
			}
			initialText = TranslationText.Text;

			if (MainWindowViewModel.Instance.LoadedCultureNames.Count > 0)
			{
				ScanAllTexts(MainWindowViewModel.Instance.RootTextKey);
			}

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
			SetDefaultCheckbox.IsEnabled = SourceCodeCombobox.SelectedItem as string == "XAML";
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
					"Invalid text key entered: " + errorMessage,
					"Input error",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				return;
			}
			if (TranslationText.Text != initialText &&
				MainWindowViewModel.Instance.TextKeys.ContainsKey(textKey))
			{
				TaskDialogResult result = TaskDialog.Show(
					owner: this,
					allowDialogCancellation: true,
					title: "TxEditor",
					mainInstruction: "The text key " + Tx.Q(textKey) + " already exists.",
					content: "If you continue, the existing text (in the primary culture) will be overwritten with the text entered in this dialog.",
					customButtons: new string[] { "&Overwrite", "Cancel" });
				if (result.CustomButtonResult != 0)
				{
					return;
				}
			}

			if (SourceCodeCombobox.SelectedIndex == 0)   // C#
			{
				App.Settings.WizardSourceCode = "C#";

				string keyString = textKey.Replace("\\", "\\\\").Replace("\"", "\\\"");

				string code = "Tx.T(\"" + keyString + "\")";
				Clipboard.SetText(code);
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

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		#endregion Control event handlers

		#region Support functions

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
	}
}
