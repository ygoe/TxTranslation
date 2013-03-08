using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using TxEditor.ViewModel;

namespace TxEditor.Converters
{
	class TreeNodesConverter : IMultiValueConverter
	{
		#region IMultiValueConverter Member

		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			IList items = (IList) values[0];

			if (items.Count == 0)
			{
				return new DetailsMessageViewModel("Nothing selected", "Select a text key from the list to display and edit it.", "Arrow");
			}

			if (items.Count == 1)
			{
				TextKeyViewModel tkVM = items[0] as TextKeyViewModel;
				if (tkVM != null && !tkVM.IsFullKey)
				{
					return new DetailsMessageViewModel("Incomplete text key selected", "This is only a segment of a text key. Select a complete text key’s node to display and edit it.", "Arrow");
				}

				return items[0];
			}

			Type firstType = null;
			foreach (object item in items)
			{
				if (firstType == null)
				{
					firstType = item.GetType();
				}
				else if (item.GetType() != firstType)
				{
					return new DetailsMessageViewModel("Inconsistent selection", "Multiple items of different types are selected. Only elements of the same type can be displayed and edited concurrently.", "Flash");
				}
			}

			if (firstType == typeof(TextKeyViewModel))
			{
				return new DetailsMessageViewModel(items.Count + " text key nodes selected");
				//TextKeyViewModel[] nodes = new TextKeyViewModel[items.Count];
				//for (int i = 0; i < items.Count; i++)
				//{
				//    nodes[i] = (TextKeyViewModel) items[i];
				//}
				//return new TextKeyMultiViewModel(nodes);
			}

			return new DetailsMessageViewModel(items.Count + " " + firstType.Name + " items selected");
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
