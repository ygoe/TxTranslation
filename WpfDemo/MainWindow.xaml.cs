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
using TxLib;
using System.Xml;
using System.Diagnostics;

namespace Demo
{
	/// <summary>
	/// Interaktionslogik für MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			//Tx.LogFileName = "tx.log";
			//Tx.LogFileName = "";
			//Environment.SetEnvironmentVariable("TX_LOG_UNUSED", "1", EnvironmentVariableTarget.User);

			//Tx.UseFileSystemWatcher = true;

			//Tx.LoadFromXmlFile("wsz.de.xml");
			//Tx.LoadFromXmlFile("wsz.en.xml");
			//Tx.LoadFromXmlFile("combined.xml");
			Tx.LoadDirectory(".", "wsz");
			Tx.PrimaryCulture = "de";

			Tx.AddText("de", Tx.SystemKeys.NumberNegative, "\u2212");
			Tx.AddText("de", Tx.SystemKeys.NumberGroupSeparator, "\u202f");
			Tx.AddText("de", Tx.SystemKeys.NumberGroupSeparatorThreshold, "10000");
			Tx.AddText("de", Tx.SystemKeys.NumberUnitSeparator, "\u202f");

			//Tx.SetCulture("en-us");

			InitializeComponent();

			Info2Text.Text = Tx.QT("errors and warnings", "err", "5", "warn", "1");

			//Stopwatch sw = new Stopwatch();
			//sw.Start();
			//for (int i = 0; i < 1000000; i++)
			//{
			//    string s = Tx.T("monate");
			//}
			//sw.Stop();
			//Info2Text.Text = Tx.NumberUnit(Tx.Number(sw.ElapsedMilliseconds), "ms");
		}

		private void ChangeLanguageButton_Click(object sender, RoutedEventArgs e)
		{
			Tx.SetCulture("en-us");
		}
	}
}
