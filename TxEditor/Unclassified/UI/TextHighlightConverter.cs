using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Unclassified.UI
{
	class TextHighlightConverter : IValueConverter, IMultiValueConverter
	{
		#region IValueConverter Member

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			string text = value as string;
			string search = parameter as string;
			int start = 0;
			int next;
			Run run;

			var textblock = new TextBlock();

			while (text != null && search != null && search.Length > 0)
			{
				next = text.IndexOf(search, start, StringComparison.CurrentCultureIgnoreCase);
				if (next < 0) break;

				if (next > start)
				{
					run = new Run(text.Substring(start, next - start));
					textblock.Inlines.Add(run);
				}

				run = new Run(text.Substring(next, search.Length));
				run.Background = new SolidColorBrush(Color.FromArgb(128, 255, 255, 0));
				textblock.Inlines.Add(run);

				start = next + search.Length;
			}

			if (text != null && start < text.Length)
			{
				run = new Run(text.Substring(start));
				textblock.Inlines.Add(run);
			}

			return textblock;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion IValueConverter Member

		#region IMultiValueConverter Member

		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Convert(values[0], targetType, values[1], culture);
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion IMultiValueConverter Member
	}
}
