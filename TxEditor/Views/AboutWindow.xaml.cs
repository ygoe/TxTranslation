using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using Unclassified.FieldLog;

namespace Unclassified.TxEditor.Views
{
	public partial class AboutWindow : Window
	{
		public AboutWindow()
		{
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			VersionLabel.Text = "Version " + FL.AppVersion;
			CopyrightLabel.Text = FL.AppCopyright;
		}

		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("http://dev.unclassified.de/source/txtranslation");
		}

		private void CloseButton_Click(object sender, RoutedEventArgs e)
		{
			Close();
		}

		private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Escape)
			{
				Close();
			}
		}
	}
}
