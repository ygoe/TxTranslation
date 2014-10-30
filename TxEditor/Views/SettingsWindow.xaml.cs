using System;
using System.Windows;
using Unclassified.UI;

namespace Unclassified.TxEditor.Views
{
	public partial class SettingsWindow : Window
	{
		public SettingsWindow()
		{
			InitializeComponent();
			this.HideIcon();

			DataContext = App.Settings;
		}
	}
}
