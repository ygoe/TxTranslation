using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Unclassified.UI
{
	/// <summary>
	/// Implements an Image class that turns its image to grey when disabled.
	/// </summary>
	public class MenuIconImage : Image
	{
		// Source: http://weblogs.asp.net/thomaslebrun/archive/2009/03/03/wpf-how-to-gray-the-icon-of-a-menuitem.aspx

		static MenuIconImage()
		{
			IsEnabledProperty.OverrideMetadata(
				typeof(MenuIconImage),
				new FrameworkPropertyMetadata(true, new PropertyChangedCallback(OnIsEnabledPropertyChanged)));
		}

		private static void OnIsEnabledPropertyChanged(DependencyObject source, DependencyPropertyChangedEventArgs args)
		{
			MenuIconImage menuIconImage = source as MenuIconImage;
			if (menuIconImage != null)
			{
				bool isEnabled = Convert.ToBoolean(args.NewValue);
				if (!isEnabled)
				{
					// Get the source bitmap
					BitmapImage bitmapImage = new BitmapImage(new Uri(menuIconImage.Source.ToString()));
					// Convert it to grey
					menuIconImage.Source = new FormatConvertedBitmap(bitmapImage, PixelFormats.Gray32Float, null, 0);
					// Create opacity mask for greyscale image as FormatConvertedBitmap does not keep transparency info
					menuIconImage.OpacityMask = new ImageBrush(bitmapImage);
				}
				else
				{
					// Set the Source property to the original value
					menuIconImage.Source = ((FormatConvertedBitmap) menuIconImage.Source).Source;
					// Reset the opacity mask
					menuIconImage.OpacityMask = null;
				}
			}
		}

		public MenuIconImage()
		{
			Style imgStyle = new Style(typeof(Image));
			Trigger trg = new Trigger();
			trg.Property = Image.IsEnabledProperty;
			trg.Value = false;
			Setter setter = new Setter(Image.OpacityProperty, 0.5);
			trg.Setters.Add(setter);
			imgStyle.Triggers.Add(trg);
			this.Style = imgStyle;
		}
	}
}
