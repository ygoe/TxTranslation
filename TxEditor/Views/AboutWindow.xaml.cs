using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Media.Effects;
using Unclassified.FieldLog;
using Unclassified.UI;
using Unclassified.Util;

namespace Unclassified.TxEditor.Views
{
	public partial class AboutWindow : Window
	{
		public AboutWindow()
		{
			InitializeComponent();
			this.HideIcon();

			VersionText.Text = FL.AppVersion;
		}

		public new void ShowDialog()
		{
			UIElement root = null;
			BlurEffect blur = null;

			if (Owner != null)
			{
				root = Owner.Content as UIElement;

				blur = new BlurEffect();
				blur.Radius = 0;
				root.Effect = blur;

				root.AnimateEase(UIElement.OpacityProperty, 1, 0.6, TimeSpan.FromSeconds(1));
				blur.AnimateEase(BlurEffect.RadiusProperty, 1, 4, TimeSpan.FromSeconds(0.5));
				// NOTE: Blur radius is internally converted to integer which is bad for slow animations.
				// See https://social.msdn.microsoft.com/Forums/vstudio/en-US/ced0cc07-44fd-43e7-8829-e329be038d82
			}

			base.ShowDialog();

			if (root != null && blur != null)
			{
				root.AnimateEase(UIElement.OpacityProperty, 0.6, 1, TimeSpan.FromSeconds(0.2));
				blur.AnimateEase(BlurEffect.RadiusProperty, 4, 0, TimeSpan.FromSeconds(0.2));

				DelayedCall.Start(() => { root.Effect = null; }, 250);
			}
		}

		private void Hyperlink_Click(object sender, RoutedEventArgs e)
		{
			Process.Start("http://unclassified.software/txtranslation?ref=inapp-txeditor");
		}
	}
}
