using System;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace Unclassified.UI
{
	internal class TextHighlightConverter : IValueConverter, IMultiValueConverter
	{
		#region IValueConverter Member

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			string text = value as string;
			string search = parameter as string;
			int start = 0;
			Run run;

			// Create new composited TextBlock element
			var textblock = new TextBlock();

			if (text != null && search != null && search.Length > 0)
			{
				// Split search string into chunks to highlight any of them
				string[] searches = search.Split('.', ':').Where(s => s.Length > 0).OrderByDescending(s => s.Length).ToArray();
				// TODO: Configure split characters through property or constructor parameter or something

				// Scan text for matches
				while (true)
				{
					string nextString = null;
					int nextIndex = int.MaxValue;
					foreach (var s in searches)
					{
						int next = text.IndexOf(s, start, StringComparison.CurrentCultureIgnoreCase);
						if (next != -1 && next < nextIndex)
						{
							nextIndex = next;
							nextString = s;
						}
					}
					if (nextIndex == int.MaxValue) break;   // Nothing more found

					if (nextIndex > start)
					{
						// Add unmatched text until the next match
						run = new Run(text.Substring(start, nextIndex - start));
						textblock.Inlines.Add(run);
					}

					// Add next match with hightlighted style
					run = new Run(text.Substring(nextIndex, nextString.Length));
					run.Background = new SolidColorBrush(Color.FromArgb(128, 255, 255, 0));
					textblock.Inlines.Add(run);

					start = nextIndex + nextString.Length;
				}
			}

			// Add remaining unmatched text
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
			if (values.Length > 2 && values[2] is bool && (bool)values[2] == false)
			{
				// Highlighting disabled for this item
				return values[0];
			}

			return Convert(values[0], targetType, values[1], culture);
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion IMultiValueConverter Member
	}
}
