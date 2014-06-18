using System;
using System.Linq;
using System.Windows.Data;
using Unclassified.Util;

namespace Unclassified.TxEditor.Converters
{
	internal class UnicodeInfoConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			int codePoint;
			if (value is int)
			{
				codePoint = (int) value;
			}
			else if (value is char)
			{
				codePoint = (char) value;
			}
			else if (value is string)
			{
				codePoint = ((string) value)[0];
			}
			else
			{
				//throw new ArgumentException("Unsupported value type.");
				return null;
			}

			var info = UnicodeInfo.GetChar(codePoint);
			return "U+" + codePoint.ToString("X4") + "  " + info.Name;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
