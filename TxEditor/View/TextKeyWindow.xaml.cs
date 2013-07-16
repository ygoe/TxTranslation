using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using Unclassified;
using System.Text.RegularExpressions;
using TxEditor.ViewModel;

namespace TxEditor.View
{
	public partial class TextKeyWindow : Window
	{
		public TextKeyWindow()
		{
			InitializeComponent();
		}

		public string TextKey
		{
			get { return TextKeyText.Text.Trim(); }
			set { TextKeyText.Text = value != null ? value.Trim() : null; }
		}

		public bool RenameSelectMode { get; set; }

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			//TextKeyText.SelectAll();
			if (!RenameSelectMode)
			{
				Match m = Regex.Match(TextKey, @"^((?:.*?:)?(?:[^.]*\.)*)([^.]*[:.])$");
				if (m.Success)
				{
					TextKeyText.SelectionStart = m.Groups[1].Length;
					TextKeyText.SelectionLength = m.Groups[2].Length;
				}
			}
			else
			{
				Match m = Regex.Match(TextKey, @"^((?:.*?:)?(?:[^.]*\.)*)([^.]*)$");
				if (m.Success)
				{
					TextKeyText.SelectionStart = m.Groups[1].Length;
					TextKeyText.SelectionLength = m.Groups[2].Length;
				}
			}
			TextKeyText.Focus();
		}

		private void OKButton_Click(object sender, RoutedEventArgs e)
		{
			string errorMessage;
			if (!TextKeyViewModel.ValidateName(TextKey, out errorMessage))
			{
				MessageBox.Show(
					"Invalid text key entered: " + errorMessage,
					"Input error",
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				return;
			}
			
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
