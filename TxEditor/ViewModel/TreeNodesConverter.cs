using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace TxEditor.ViewModel
{
	class TreeNodesConverter : IMultiValueConverter
	{
		#region IMultiValueConverter Member

		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			IList items = (IList) values[0];

			if (items.Count == 0)
			{
				//return new DataBrowserMessageViewModel("Keine Auswahl", "Es ist kein Element in der Liste ausgewählt.", "Arrow");
				return "No text key selected.";
			}

			if (items.Count == 1)
			{
				TextKeyViewModel tkVM = items[0] as TextKeyViewModel;
				if (tkVM != null && !tkVM.IsLeafNode)
				{
					return "Not a leaf node.";
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
					//return new DataBrowserMessageViewModel("Uneinheitliche Auswahltypen", "Es können nur Elemente des gleichen Typs gemeinsam angezeigt und bearbeitet werden.", "Flash");
					return "Inconsistent item types selected.";
				}
			}

			//if (firstType == typeof(NodeViewModel))
			//{
			//    NodeViewModel[] nodes = new NodeViewModel[items.Count];
			//    for (int i = 0; i < items.Count; i++)
			//    {
			//        nodes[i] = (NodeViewModel) items[i];
			//    }
			//    return new NodeMultiViewModel(nodes, null, nodes[0].ProjectVM);
			//}

			//return new DataBrowserMessageViewModel("Auflistung mit " + items.Count + " Elementen vom Typ " + firstType.Name);
			return items.Count + " " + firstType.Name + " items selected.";
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
