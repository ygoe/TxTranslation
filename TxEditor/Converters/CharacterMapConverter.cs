using System;
using System.Linq;
using System.Windows.Data;

namespace Unclassified.TxEditor.Converters
{
	internal class CharacterMapConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			string charMapString = (string)value;

			return charMapString.ToCharArray().Select(c => c.ToString()).ToArray();
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
