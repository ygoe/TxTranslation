using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using TxEditor.ViewModel;

namespace TxEditor
{
	/// <summary>
	/// Provides extension methods for the application.
	/// </summary>
	static class Extensions
	{
		#region List iteration

		// TODO: Translate to English

		/// <summary>
		/// Führt eine Methode für jedes Element der Auflistung aus.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="items"></param>
		/// <param name="action"></param>
		/// <returns></returns>
		public static IEnumerable<T> ForEach<T>(this IEnumerable<T> items, Action<T> action)
		{
			foreach (var item in items)
			{
				action(item);
			}
			return items;
		}

		/// <summary>
		/// Führt eine Methode für jedes Element der Auflistung aus. Die Auflistung wird vorher in
		/// eine Liste kopiert, damit die Methode die Auflistung ggf. verändern kann.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="items"></param>
		/// <param name="action"></param>
		/// <returns></returns>
		public static IEnumerable<T> ForEachSafe<T>(this IEnumerable<T> items, Action<T> action)
		{
			foreach (var item in new List<T>(items))
			{
				action(item);
			}
			return items;
		}

		/// <summary>
		/// Removes all items from a list that do not match the specified condition.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="items"></param>
		/// <param name="predicate"></param>
		public static void Filter<T>(this IList<T> items, Predicate<T> predicate)
		{
			for (int i = 0; i < items.Count; i++)
			{
				if (!predicate(items[i]))
				{
					items.RemoveAt(i);
					i--;
				}
			}
		}

		#endregion List iteration

		#region String conversions

		/// <summary>
		/// Konvertiert eine Zeichenkette zur sicheren Verwendung in Dateinamen. Unzulässige
		/// Zeichen werden durch jeweils einen Unterstrich ersetzt.
		/// </summary>
		/// <param name="str">Zu konvertierende Zeichenkette.</param>
		/// <returns></returns>
		public static string ToFileName(this string str)
		{
			return str.Replace('"', '_').Replace('*', '_').Replace('/', '_').Replace(':', '_').Replace('<', '_').Replace('>', '_').Replace('?', '_').Replace('\\', '_');
		}

		#endregion String conversions

		#region Sorted collections

		/// <summary>
		/// Inserts a ViewModel instance to a ViewModel collection, sorted by their DisplayName
		/// values.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="collection">Collection to insert the new item to.</param>
		/// <param name="vm">New item to insert into the collection.</param>
		public static void InsertSorted<T>(this ObservableCollection<T> collection, T vm) where T : ViewModelBase
		{
			InsertSorted(collection, vm, null);
		}

		/// <summary>
		/// Inserts a ViewModel instance to a ViewModel collection, sorted by the specified
		/// comparison delegate.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="collection">Collection to insert the new item to.</param>
		/// <param name="vm">New item to insert into the collection.</param>
		public static void InsertSorted<T>(this ObservableCollection<T> collection, T vm, Comparison<T> comparison) where T : ViewModelBase
		{
			if (collection.Count == 0)
			{
				// Easy...
				collection.Add((T) vm);
				return;
			}

			// Do a binary search in the collection to find the best match position
			// (an exact match will likely not exist yet)
			int lower = 0;
			int upper = collection.Count - 1;
			int index = (lower + upper) / 2;
			while (lower <= upper)
			{
				// As long as lower <= upper, index is valid and can be used for comparison
				int cmp;
				if (comparison != null)
				{
					cmp = comparison(collection[index], vm);
				}
				else
				{
					cmp = string.Compare(collection[index].DisplayName, vm.DisplayName, StringComparison.InvariantCultureIgnoreCase);
				}
				
				if (cmp == 0)
				{
					// Direct hit, insert after this existing (undefined behaviour for multiple equal items...)
					//index++;
					break;
				}
				else if (cmp < 0)
				{
					// Item at index is less than item to insert, move on to right side
					lower = index + 1;
					index = (lower + upper) / 2;
				}
				else
				{
					// Item at index is greater than item to insert, move on to left side
					upper = index - 1;
					index = (lower + upper) / 2;
				}
			}

			// The resulting index is not equal to the new name because it doesn't exist yet.
			// Because the index was always rounded to the smaller integer, the item at index is
			// always less than the item to insert, if it exists at all (in case index == -1).
			// Use next index to insert new item.
			// Except if upper < 0, because then index was rounded to the higher integer (towards
			// zero).
			if (upper >= 0)
			{
				index++;
			}

			collection.Insert(index, (T) vm);
		}

		/// <summary>
		/// Sorts the items in an ObservableCollection by the specified key.
		/// </summary>
		/// <typeparam name="T">Type of the ObservableCollection items.</typeparam>
		/// <typeparam name="TKey">Type of the key to order by.</typeparam>
		/// <param name="collection">Collection to sort.</param>
		/// <param name="keySelector">Function to extract the key from an element.</param>
		public static void Sort<T, TKey>(this ObservableCollection<T> collection, Func<T, TKey> keySelector)
		{
			var array = collection.OrderBy(keySelector).ToArray();
			collection.Clear();
			foreach (var item in array)
			{
				collection.Add(item);
			}
		}

		/// <summary>
		/// Replaces an item in an ObservableCollection by another item at the same index.
		/// </summary>
		/// <typeparam name="T">Type of the ObservableCollection items.</typeparam>
		/// <param name="collection">Collection to replace the item in.</param>
		/// <param name="item">Item to find and replace.</param>
		/// <param name="replacement">New item to be set in the collection.</param>
		/// <returns>true if the item was replaced, false if it did not exist.</returns>
		public static bool Replace<T>(this ObservableCollection<T> collection, T item, T replacement)
		{
			int index = collection.IndexOf(item);
			if (index >= 0)
			{
				collection[index] = replacement;
				return true;
			}
			return false;
		}

		#endregion Sorted collections

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

		public static string ReplaceStart(this string str, string search, string replacement)
		{
			if (str.StartsWith(search))
			{
				return replacement + str.Substring(search.Length);
			}
			return str;
		}
	}
}
