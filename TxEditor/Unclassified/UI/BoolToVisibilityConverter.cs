using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace Unclassified.UI
{
	/// <summary>
	/// Provides a value converter that converts a boolean value into a Visibility value. The
	/// converter parameter can be used to inverse the effect. It can be configured whether the
	/// value Collapsed or Hidden is used, if not visible.
	/// </summary>
	public class BoolToVisibilityConverter : IValueConverter
	{
		/// <summary>
		/// Gets or sets a value indicating whether the value Visibility.Hidden is returned instead
		/// of Visibility.Collapsed.
		/// </summary>
		public bool UseHiddenValue { get; set; }

		/// <summary>
		/// Converts the condition value to a Visibility value.
		/// </summary>
		/// <param name="value">Condition value, must be bool.</param>
		/// <param name="targetType">Unused.</param>
		/// <param name="parameter">Comparison parameter, must be "true" or "false". Defaults to "true" if unset or empty.</param>
		/// <param name="culture">Unused.</param>
		/// <returns>Visible, if value is equal to parameter, Collapsed or Hidden otherwise.</returns>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool param = true;
			string paramStr = parameter as string;
			if (!string.IsNullOrEmpty(paramStr))
			{
				if (!bool.TryParse(paramStr, out param))
					return Visibility.Visible;
			}

			if (!(value is bool))
				return Visibility.Visible;

			bool val = (bool)value;

			return val == param ? Visibility.Visible : (UseHiddenValue ? Visibility.Hidden : Visibility.Collapsed);
		}

		/// <summary>
		/// Not implemented.
		/// </summary>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
