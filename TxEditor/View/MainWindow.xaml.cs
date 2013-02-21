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

namespace TxEditor.View
{
	public partial class MainWindow : Window
	{
		public MainWindow()
		{
			InitializeComponent();

			WindowStartupLocation = WindowStartupLocation.Manual;
			Left = App.Settings.GetInt("window.left", (int) SystemParameters.WorkArea.Left + 20);
			Top = App.Settings.GetInt("window.top", (int) SystemParameters.WorkArea.Top + 20);
			Width = App.Settings.GetInt("window.width", 950);
			Height = App.Settings.GetInt("window.height", 600);
			WindowState = (WindowState) App.Settings.GetInt("window.state", (int) WindowState.Normal);
		}

		private void Window_LocationChanged(object sender, EventArgs e)
		{
			if (App.Settings != null)
			{
				App.Settings.Set("window.left", (int) RestoreBounds.Left);
				App.Settings.Set("window.top", (int) RestoreBounds.Top);
				App.Settings.Set("window.width", (int) RestoreBounds.Width);
				App.Settings.Set("window.height", (int) RestoreBounds.Height);
				App.Settings.Set("window.state", (int) WindowState);
			}
		}

		private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
		{
			Window_LocationChanged(this, EventArgs.Empty);
		}

	}
}
