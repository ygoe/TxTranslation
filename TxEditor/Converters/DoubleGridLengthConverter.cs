using System;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace Unclassified.TxEditor.Converters
{
	// Source: http://stackoverflow.com/a/5260065/143684
	public class DoubleGridLengthConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return new GridLength((double) value);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			GridLength gridLength = (GridLength) value;
			return gridLength.Value;
		}
	}
}
