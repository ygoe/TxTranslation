using System;
using System.Globalization;
using System.Windows.Data;

namespace Unclassified.UI
{
	/// <summary>
	/// Provides a comparer converter that compares multiple values and returns true if all are
	/// equal.
	/// </summary>
	internal class ComparerConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			// Nothing is not equal
			if (values == null) return false;
			if (values.Length == 0) return false;
			// One thing is always equal
			if (values.Length == 1) return true;
			// Compare every item with the first one
			IComparable cmp0 = values[0] as IComparable;
			for (int i = 1; i < values.Length; i++)
			{
				IComparable cmpi = values[i] as IComparable;
				if (cmp0 != null && cmpi != null)
				{
					// Both are comparable, then do compare
					if (cmp0.CompareTo(cmpi) != 0) return false;
				}
				else
				{
					// Either one is not comparable, so they are not equal
					return false;
				}
			}
			// All tests passed, all are equal
			return true;
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
