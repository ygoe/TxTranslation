using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
using System.Xml;
using TxLib;

namespace WpfDemo
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			// Setup logging
			Tx.LogFileName = "tx.log";
			//Tx.LogFileName = "";
			//Environment.SetEnvironmentVariable("TX_LOG_UNUSED", "1", EnvironmentVariableTarget.User);
			//Environment.SetEnvironmentVariable("TX_LOG_UNUSED", null, EnvironmentVariableTarget.User);

			// Setup translation data
			try
			{
				// Set the XML file's build action to "Embedded Resource" and "Never copy" for this to work.
				Tx.LoadFromEmbeddedResource("WpfDemo.lang.languages.txd");
			}
			catch (ArgumentException)
			{
				// The file was not embedded, try reading the file. This enables file change notifications.
				Tx.UseFileSystemWatcher = true;
				Tx.LoadFromXmlFile(@"lang\languages.txd");
			}

			// Simulate web environment
			//Tx.SetWebCulture("de-de;q=1,de-at;q=0.8,de;q=0.7,en-us;q=0.5,en;q=0.3");
			//Tx.SetWebCulture("en-us;q=1,en-gb;q=0.8,en;q=0.7,de-de;q=0.5,de;q=0.3");

			InitializeComponent();

			//Info2Text.Text = Tx.QT("errors and warnings", "err", "5", "warn", "1");

			//Info2Text.Text = Tx.Time(DateTime.Now, TxTime.fr);

			//Info2Text.Text = Tx.RelativeTime(DateTime.Now.AddMinutes(-4).AddSeconds(-10));
			//Info2Text.Text = Tx.TimeSpan(DateTime.Now.AddMinutes(-1).AddSeconds(-10));
			//Info2Text.Text = Tx.TimeSpan(TimeSpan.FromDays(3.4));

			//Info2Text.Text = Tx.DataSize(123456);
			Info2Text.Text = Tx.T("months");
		}

		private void ChangeLanguageButton_Click(object sender, RoutedEventArgs e)
		{
			Tx.SetCulture("en-us");
		}
	}
}
