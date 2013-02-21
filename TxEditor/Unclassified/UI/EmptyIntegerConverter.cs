using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Unclassified.UI
{
	/// <summary>
	/// Provides a value converter that converts an integer value for use in a TextBox control.
	/// The integer value of zero is mapped to an empty string.
	/// </summary>
	public class EmptyIntegerConverter : IValueConverter
	{
		/// <summary>
		/// Converts the integer to a string value.
		/// </summary>
		/// <param name="value">Number value, must be int.</param>
		/// <param name="targetType">Unused.</param>
		/// <param name="parameter">Unused.</param>
		/// <param name="culture">Culture that is used for conversion.</param>
		/// <returns></returns>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is int))
				throw new ArgumentException("Invalid argument type.", "value");
			int val = (int) value;

			if (val == 0)
				return "";
			return val.ToString(culture);
		}

		/// <summary>
		/// Converts the string back to an integer value.
		/// </summary>
		/// <param name="value">Number value, must be string.</param>
		/// <param name="targetType">Unused.</param>
		/// <param name="parameter">Unused.</param>
		/// <param name="culture">Culture that is used for conversion.</param>
		/// <returns></returns>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (!(value is string) && value != null)
				throw new ArgumentException("Invalid argument type.", "value");
			string val = value as string;

			if (string.IsNullOrWhiteSpace(val))
				return 0;
			int i;
			if (int.TryParse(val, NumberStyles.Integer, culture, out i))
			{
				return i;
			}
			// What else could we do now?
			return 0;
			//return Binding.DoNothing;   // This leaves the wrong text and doesn't update the binding, with no error indication!
		}
	}
}
