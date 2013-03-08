using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows;

namespace Unclassified.UI
{
	class ColorMath : MarkupExtension
	{
		public ColorMath()
		{
		}

		public object Key { get; set; }
		public Color Color { get; set; }
		public Brush Brush { get; set; }

		public double Darken { get; set; }
		public double Lighten { get; set; }

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			bool brushMode = false;
			Color c = Color;

			if (Key != null)
			{
				object res = Application.Current.FindResource(Key);
				if (res is Color)
				{
					c = (Color) res;
				}
				else if (res is SolidColorBrush)
				{
					c = (res as SolidColorBrush).Color;
					brushMode = true;
				}
				else
				{
					throw new NotSupportedException("Unsupported resource type.");
				}
			}
			else if (Brush != null)
			{
				if (Brush is SolidColorBrush)
				{
					c = (Brush as SolidColorBrush).Color;
					brushMode = true;
				}
				else
				{
					throw new NotSupportedException("Unsupported Brush type.");
				}
			}
			else
			{
				c = Color;
			}

			if (Darken != 0)
			{
				byte r = (byte) Math.Round(c.R * (1 - Darken));
				byte g = (byte) Math.Round(c.G * (1 - Darken));
				byte b = (byte) Math.Round(c.B * (1 - Darken));
				c = Color.FromArgb(c.A, r, g, b);
			}
			if (Lighten != 0)
			{
				byte r = (byte) Math.Round(c.R * (1 - Lighten) + 255 * Lighten);
				byte g = (byte) Math.Round(c.G * (1 - Lighten) + 255 * Lighten);
				byte b = (byte) Math.Round(c.B * (1 - Lighten) + 255 * Lighten);
				c = Color.FromArgb(c.A, r, g, b);
			}

			if (brushMode)
			{
				return new SolidColorBrush(c);
			}
			else
			{
				return c;
			}
		}
	}
}
