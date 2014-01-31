using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Unclassified
{
	/// <summary>
	/// Provides extension methods for sequences, lists and collections.
	/// </summary>
	public static class ListExtensions
	{
		#region List iteration

		/// <summary>
		/// Invokes a method for each element in the sequence.
		/// </summary>
		/// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">The source sequence.</param>
		/// <param name="action">Method to invoke for each sequence element.</param>
		/// <returns>Returns the source sequence to support chaining.</returns>
		public static IEnumerable<T> ForEach<T>(this IEnumerable<T> source, Action<T> action)
		{
			foreach (var element in source)
			{
				action(element);
			}
			return source;
		}

		/// <summary>
		/// Invokes a method for each element in the sequence. The sequence is copied in advance so
		/// that the method may change the source sequence while it is being enumerated.
		/// </summary>
		/// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">The source sequence.</param>
		/// <param name="action">Method to invoke for each sequence element.</param>
		/// <returns>Returns the source sequence to support chaining.</returns>
		public static IEnumerable<T> ForEachSafe<T>(this IEnumerable<T> source, Action<T> action)
		{
			foreach (var element in new List<T>(source))
			{
				action(element);
			}
			return source;
		}

		/// <summary>
		/// Removes all element from a list that do not match the specified condition.
		/// </summary>
		/// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">The list to filter.</param>
		/// <param name="predicate">Function that determines whether an element matches the condition.</param>
		public static void Filter<T>(this IList<T> source, Predicate<T> predicate)
		{
			for (int index = 0; index < source.Count; index++)
			{
				if (!predicate(source[index]))
				{
					source.RemoveAt(index);
					index--;
				}
			}
		}

		/// <summary>
		/// Returns a value indicating whether the sequence is empty.
		/// </summary>
		/// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">The source sequence.</param>
		/// <returns>true if the sequence is empty; otherwise, false.</returns>
		public static bool IsEmpty<T>(this IEnumerable<T> source)
		{
			foreach (T element in source)
			{
				return false;
			}
			return true;
		}

		#endregion List iteration

		#region Default list

		/// <summary>
		/// Returns a default sequence if the source sequence is empty. This is a sequence-keeping
		/// variant of the framework's DefaultIfEmpty method which only returns a single value for
		/// an empty sequence - which cannot be aggregated like non-empty sequences.
		/// </summary>
		/// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">The source sequence.</param>
		/// <param name="defaultSequence">The sequence to return if <paramref name="source"/> is empty.</param>
		/// <returns></returns>
		public static IEnumerable<T> DefaultIfEmpty<T>(this IEnumerable<T> source, IEnumerable<T> defaultSequence)
		{
			if (source.IsEmpty())
			{
				return defaultSequence;
			}
			else
			{
				return source;
			}
		}

		#endregion Default list

		#region SingleOrDefault replacement

		/// <summary>
		/// Returns the single element of a sequence, or the default value if there are no or
		/// multiple elements in the sequence. The multiple-value case distinguishes this method
		/// from the framework's SingleOrDefault method which throws an exception in this case.
		/// </summary>
		/// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">The source sequence.</param>
		/// <returns></returns>
		public static T SingleOrDefault2<T>(this IEnumerable<T> source)
		{
			T singleValue = default(T);
			int count = 0;
			foreach (T element in source)
			{
				if (count++ == 0)
				{
					singleValue = element;
				}
				else
				{
					return default(T);
				}
			}
			return singleValue;
		}

		#endregion SingleOrDefault replacement

		#region Sorted collections

		/// <summary>
		/// Inserts a ViewModel instance to a ViewModel collection, sorted by the specified
		/// comparison delegate.
		/// </summary>
		/// <typeparam name="T">Type of the collection items.</typeparam>
		/// <param name="collection">Collection to insert the new item to.</param>
		/// <param name="vm">New item to insert into the collection.</param>
		public static void InsertSorted<T>(this ObservableCollection<T> collection, T vm, Comparison<T> comparison)
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
				int cmp = comparison(collection[index], vm);

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
		/// <typeparam name="T">Type of the collection items.</typeparam>
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
		/// <typeparam name="T">Type of the collection items.</typeparam>
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
	}
}
