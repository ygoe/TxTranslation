using System;
using System.Collections;
using System.Linq;
using System.Windows.Data;
using Unclassified.TxEditor.ViewModels;
using Unclassified.TxLib;

namespace Unclassified.TxEditor.Converters
{
	internal class TreeNodesConverter : IMultiValueConverter
	{
		#region IMultiValueConverter Member

		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			IList selectedItems = (IList) values[0];
			int selectedCount = (int) values[1];
			int itemsCount = (int) values[2];

			if (itemsCount == 0)
			{
				return new DetailsMessageViewModel(Tx.T("msg.nothing available"), Tx.T("msg.nothing available.desc"), "ArrowUp");
			}

			if (selectedItems.Count == 0)
			{
				return new DetailsMessageViewModel(Tx.T("msg.nothing selected"), Tx.T("msg.nothing selected.desc"), "ArrowLeft");
			}

			if (selectedItems.Count == 1)
			{
				TextKeyViewModel tkVM = selectedItems[0] as TextKeyViewModel;
				if (tkVM != null && !tkVM.IsFullKey)
				{
					return new DetailsMessageViewModel(Tx.T("msg.incomplete key selected"), Tx.T("msg.incomplete key selected.desc"), "ArrowLeft");
				}

				return selectedItems[0];
			}

			Type firstType = null;
			foreach (object item in selectedItems)
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
				return new DetailsMessageViewModel(selectedItems.Count + " text key nodes selected");
				//TextKeyViewModel[] nodes = new TextKeyViewModel[items.Count];
				//for (int i = 0; i < items.Count; i++)
				//{
				//    nodes[i] = (TextKeyViewModel) items[i];
				//}
				//return new TextKeyMultiViewModel(nodes);
			}

			return new DetailsMessageViewModel(selectedItems.Count + " " + firstType.Name + " items selected");
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotImplementedException();
		}

		#endregion IMultiValueConverter Member
	}
}
