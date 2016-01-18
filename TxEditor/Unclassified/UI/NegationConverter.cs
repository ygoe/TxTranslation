using System;
using System.Globalization;
using System.Windows.Data;

namespace Unclassified.UI
{
	/// <summary>
	/// Provides a value converter that converts between a value and its negated value.
	/// </summary>
	public class NegationConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value is bool)
				return !((bool)value);

			if (value is short)
				return -((short)value);
			if (value is int)
				return -((int)value);
			if (value is long)
				return -((long)value);
			if (value is float)
				return -((float)value);
			if (value is double)
				return -((double)value);
			if (value is decimal)
				return -((decimal)value);

			throw new NotSupportedException("Value type is not supported.");
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return Convert(value, targetType, parameter, culture);
		}
	}
}
