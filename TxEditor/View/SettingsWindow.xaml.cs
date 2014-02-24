using System;
using System.Windows;
using Unclassified.UI;

namespace Unclassified.TxEditor.View
{
	public partial class SettingsWindow : Window
	{
		public SettingsWindow()
		{
			InitializeComponent();

			DataContext = App.Settings;
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			this.HideIcon();
			base.OnSourceInitialized(e);
		}
	}
}
