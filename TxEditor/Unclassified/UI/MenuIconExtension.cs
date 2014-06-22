using System;
using System.Windows.Markup;
using System.Windows.Media.Imaging;

namespace Unclassified.UI
{
	/// <summary>
	/// Provides a MenuItem Icon image that is correctly sized and turns light and grey when the
	/// MenuItem is disabled.
	/// </summary>
	public class MenuIconExtension : MarkupExtension
	{
		private MenuIconImage img;

		/// <summary>
		/// Initialises a new instance of the MenuIconExtension class.
		/// </summary>
		/// <param name="sourceUri">The source URI of the image to display. Use the absolute path in the project, e. g. "/Images/myicon.png".</param>
		public MenuIconExtension(string sourceUri)
		{
			img = new MenuIconImage();
			BitmapImage bmp = new BitmapImage();
			bmp.BeginInit();
			bmp.UriSource = new Uri("pack://application:,,," + sourceUri, UriKind.Absolute);
			bmp.EndInit();
			img.Source = bmp;
			img.Width = bmp.PixelWidth;
			img.Height = bmp.PixelHeight;
		}

		/// <summary>
		/// Overridden.
		/// </summary>
		/// <param name="serviceProvider">Unused.</param>
		/// <returns>The MenuIconImage instance.</returns>
		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			return img;
		}
	}
}
