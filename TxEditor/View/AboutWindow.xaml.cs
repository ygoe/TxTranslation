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

namespace TxEditor.View
{
	public partial class AboutWindow : Window
	{
		public AboutWindow()
		{
			InitializeComponent();
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			VersionLabel.Text = "Version " + MyEnvironment.AssemblyInformationalVersion;
			CopyrightLabel.Text = MyEnvironment.AssemblyCopyright;
		}

		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("http://dev.unclassified.de/source/txlib");
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
