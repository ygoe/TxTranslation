using System;
using System.Collections.ObjectModel;
using System.Windows.Media;
using Unclassified.TxEditor.ViewModel;
using Unclassified;

namespace Unclassified.TxEditor
{
	/// <summary>
	/// Provides extension methods for the application.
	/// </summary>
	internal static class Extensions
	{
		#region Sorted ViewModel collections

		/// <summary>
		/// Inserts a ViewModel instance to a ViewModel collection, sorted by their DisplayName
		/// values.
		/// </summary>
		/// <typeparam name="T">Type of the collection items. Must be derived from ViewModelBase.</typeparam>
		/// <param name="collection">Collection to insert the new item to.</param>
		/// <param name="vm">New item to insert into the collection.</param>
		public static void InsertSorted<T>(this ObservableCollection<T> collection, T vm)
			where T : ViewModelBase
		{
			collection.InsertSorted(
				vm,
				(a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.InvariantCultureIgnoreCase));
		}

		#endregion Sorted ViewModel collections

		#region Color maths

		// TODO: Compare with ColorMath class, add comments

		public static Color BlendWith(this Color c1, Color c2, float ratio = 0.5f)
		{
			return Color.Add(Color.Multiply(c1, ratio), Color.Multiply(c2, 1 - ratio));
		}

		public static SolidColorBrush BlendWith(this SolidColorBrush b1, SolidColorBrush b2, float ratio = 0.5f)
		{
			return new SolidColorBrush(Color.Add(Color.Multiply(b1.Color, ratio), Color.Multiply(b2.Color, 1 - ratio)));
		}

		public static SolidColorBrush BlendWith(this SolidColorBrush b1, Color c2, float ratio = 0.5f)
		{
			return new SolidColorBrush(Color.Add(Color.Multiply(b1.Color, ratio), Color.Multiply(c2, 1 - ratio)));
		}

		#endregion Color maths
	}
}
