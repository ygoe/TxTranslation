using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace TxEditor.Converters
{
	class MonospaceFontConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			bool monospaceFont = (bool) value;
			
			if (monospaceFont)
			{
				string monospaceFontFamily;
				if (Fonts.SystemFontFamilies.Any(f => f.Source == "Consolas"))
				{
					monospaceFontFamily = "Consolas";
				}
				else if (Fonts.SystemFontFamilies.Any(f => f.Source == "Andale Mono"))
				{
					monospaceFontFamily = "Andale Mono";
				}
				else
				{
					monospaceFontFamily = "Courier New";
				}
				return new FontFamily(monospaceFontFamily);
			}
			return DependencyProperty.UnsetValue;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
