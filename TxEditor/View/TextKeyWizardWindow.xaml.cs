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
using TxEditor.Converters;
using TxEditor.ViewModel;

namespace TxEditor.View
{
	public partial class TextKeyWizardWindow : Window
	{
		private string exactMatchTextKey;

		public TextKeyWizardWindow()
		{
			InitializeComponent();

			WindowStartupLocation = WindowStartupLocation.Manual;
		}

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

			string str = Clipboard.GetText();
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

			if (MainWindowViewModel.Instance.LoadedCultureNames.Count > 0)
			{
				ScanAllTexts(MainWindowViewModel.Instance.RootTextKey);
			}

			if (exactMatchTextKey != null)
			{
				TextKeyText.Text = exactMatchTextKey;
			}

			UpdateLayout();
			
			Left = App.Settings.GetInt("wizard-window.left", (int) (SystemParameters.WorkArea.Right - 40 - ActualWidth));
			Top = App.Settings.GetInt("wizard-window.top", (int) (SystemParameters.WorkArea.Bottom - 40 - ActualHeight));

			TextKeyText.Focus();
			TextKeyText.SelectAll();
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

		private void Window_LocationChanged(object sender, EventArgs e)
		{
			if (App.Settings != null)
			{
				//App.Settings.Set("wizard-window.left", (int) RestoreBounds.Left);
				//App.Settings.Set("wizard-window.top", (int) RestoreBounds.Top);
			}
		}

		private void DecoratedTextBox_ValidateKey(object sender, ValidateKeyEventArgs e)
		{
			e.IsValid = MainWindowViewModel.Instance.TextKeys.Contains(e.TextKey);
		}

		private void OKButton_Click(object sender, RoutedEventArgs e)
		{
			string errorMessage;
			if (!TextKeyViewModel.ValidateName(TextKeyText.Text, out errorMessage))
			{
				MessageBox.Show(
					"Invalid text key entered: " + errorMessage,
					"Input error",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				return;
			}

			Clipboard.SetText("Tx.T(\"" + TextKeyText.Text + "\")");

			DialogResult = true;
			Close();
		}

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}
	}
}
