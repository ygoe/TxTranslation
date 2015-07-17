using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Unclassified.Util
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
		/// Removes all elements from a list that do not match the specified condition.
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

		/// <summary>
		/// Returns an accumulated string of sequence items with the specified separator.
		/// </summary>
		/// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">The source sequence.</param>
		/// <param name="separator">The separator that is inserted between all elements of the sequence.</param>
		/// <param name="lastSeparator">If not null, the separator that is inserted between the last and second-last elements of the sequence.</param>
		/// <returns>The final accumulated string.</returns>
		public static string Aggregate<T>(this IEnumerable<T> source, string separator, string lastSeparator = null)
		{
			List<string> items = source.Select(i => i.ToString()).ToList();
			if (lastSeparator != null && items.Count > 1)
			{
				string lastItem = items[items.Count - 1];
				items.RemoveAt(items.Count - 1);
				return items.Aggregate((a, b) => a + separator + b) + lastSeparator + lastItem;
			}
			return items.Aggregate((a, b) => a + separator + b);
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

		/// <summary>
		/// Returns an empty sequence if the source is null.
		/// </summary>
		/// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">The source sequence.</param>
		/// <returns></returns>
		public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> source)
		{
			if (source == null)
			{
				return new T[0];
			}
			else
			{
				return source;
			}
		}

		/// <summary>
		/// Determines whether any element of a sequence satisfies a condition. If the sequence is
		/// empty, this method returns true (in contrast to the framework's Any method which
		/// returns false in that case).
		/// </summary>
		/// <typeparam name="TSource">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">The sequence whose elements to apply the predicate to.</param>
		/// <param name="predicate">A function to test each element for a condition.</param>
		/// <returns>true if any elements in the source sequence pass the test in the specified predicate or if the
		/// source sequence is empty; otherwise, false.</returns>
		public static bool AnyOrTrue<TSource>(this IEnumerable<TSource> source, Func<TSource, bool> predicate)
		{
			if (!source.Any()) return true;
			return source.Any(predicate);
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

		#region Append

		/// <summary>
		/// Appends a single element at the end of the sequence.
		/// </summary>
		/// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">The source sequence.</param>
		/// <param name="element">The element to append at the end of the sequence.</param>
		/// <returns>A sequence of <paramref name="source"/> appended by <paramref name="element"/>.</returns>
		public static IEnumerable<T> Append<T>(this IEnumerable<T> source, T element)
		{
			foreach (T item in source)
			{
				yield return item;
			}
			yield return element;
		}

		#endregion Append

		#region Sorted collections

		/// <summary>
		/// Returns all elements of a sequence that occur before the search item.
		/// </summary>
		/// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">The source sequence.</param>
		/// <param name="search">The element to search.</param>
		/// <returns></returns>
		public static IEnumerable<T> Before<T>(this IEnumerable<T> source, T search)
		{
			foreach (T item in source)
			{
				if (item.Equals(search))
				{
					yield break;
				}
				yield return item;
			}
		}

		/// <summary>
		/// Returns all elements of a sequence that occur after the search item.
		/// </summary>
		/// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">The source sequence.</param>
		/// <param name="search">The element to search.</param>
		/// <returns></returns>
		public static IEnumerable<T> After<T>(this IEnumerable<T> source, T search)
		{
			bool found = false;
			foreach (T item in source)
			{
				if (found)
				{
					yield return item;
				}
				if (item.Equals(search))
				{
					found = true;
				}
			}
		}

		/// <summary>
		/// Inserts an item to a list, sorted by the specified comparison delegate.
		/// </summary>
		/// <typeparam name="T">Type of the list items.</typeparam>
		/// <param name="list">The list to insert the new item to.</param>
		/// <param name="newItem">New item to insert into the list.</param>
		/// <param name="comparison">The comparison for sorting the items.</param>
		/// <returns>The index of the inserted item in the list.</returns>
		public static int InsertSorted<T>(this IList<T> list, T newItem, Comparison<T> comparison)
		{
			if (list.Count == 0)
			{
				// Easy...
				list.Add(newItem);
				return list.Count - 1;
			}

			// Do a binary search in the collection to find the best match position
			// (an exact match will likely not exist yet)
			int lower = 0;
			int upper = list.Count - 1;
			int index = (lower + upper) / 2;
			while (lower <= upper)
			{
				// As long as lower <= upper, index is valid and can be used for comparison
				int cmp = comparison(list[index], newItem);

				if (cmp == 0)
				{
					// Direct hit, insert after this existing (undefined behaviour for multiple equal items...)
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

			list.Insert(index, newItem);
			return index;
		}

		/// <summary>
		/// Updates the position of an item in a list, sorted by the specified comparison delegate.
		/// </summary>
		/// <typeparam name="T">Type of the list items.</typeparam>
		/// <param name="list">The list in which to move the item.</param>
		/// <param name="item">The item to move to the new position.</param>
		/// <param name="comparison">The comparison for sorting the items.</param>
		/// <returns>The new index of the item in the list. -1 if the item is not in the list.</returns>
		public static int UpdateSorted<T>(this IList<T> list, T item, Comparison<T> comparison)
		{
			if (list.Remove(item))
			{
				return list.InsertSorted(item, comparison);
			}
			return -1;
		}

		/// <summary>
		/// Sorts the items in a list by the specified key.
		/// </summary>
		/// <typeparam name="T">Type of the list items.</typeparam>
		/// <typeparam name="TKey">Type of the key to order by.</typeparam>
		/// <param name="list">The list to sort.</param>
		/// <param name="keySelector">Function to extract the key from an element.</param>
		public static void Sort<T, TKey>(this IList<T> list, Func<T, TKey> keySelector)
		{
			var array = list.OrderBy(keySelector).ToArray();
			list.Clear();
			foreach (var item in array)
			{
				list.Add(item);
			}
		}

		/// <summary>
		/// Replaces an item in a list by another item at the same index.
		/// </summary>
		/// <typeparam name="T">Type of the list items.</typeparam>
		/// <param name="list">The list to replace the item in.</param>
		/// <param name="item">Item to find and replace.</param>
		/// <param name="replacement">New item to be set in the list.</param>
		/// <returns>true if the item was replaced, false if it did not exist.</returns>
		public static bool Replace<T>(this IList<T> list, T item, T replacement)
		{
			int index = list.IndexOf(item);
			if (index >= 0)
			{
				list[index] = replacement;
				return true;
			}
			return false;
		}

		#endregion Sorted collections

		#region Shuffling

		// Source: http://stackoverflow.com/a/1262619/143684
		/// <summary>
		/// Shuffles a list.
		/// </summary>
		/// <typeparam name="T">The type of the elements of <paramref name="list"/>.</typeparam>
		/// <param name="list">The list to shuffle.</param>
		/// <remarks>
		/// This method is based on the Fisher-Yates shuffle.
		/// </remarks>
		public static void Shuffle<T>(this IList<T> list)
		{
			int n = list.Count;
			while (n > 1)
			{
				n--;
				int k = ThreadSafeRandom.Next(n + 1);
				T value = list[k];
				list[k] = list[n];
				list[n] = value;
			}
		}

		/// <summary>
		/// Shuffles a copy of a list.
		/// </summary>
		/// <typeparam name="T">The type of the elements of <paramref name="list"/>.</typeparam>
		/// <param name="list">The list to shuffle.</param>
		/// <returns>A new List instance with the shuffled items from <paramref name="list"/>.</returns>
		public static List<T> ShuffleCopy<T>(this IEnumerable<T> list)
		{
			List<T> copy = new List<T>(list);
			copy.Shuffle();
			return copy;
		}

		#endregion Shuffling

		#region INotifyCollectionChanged helpers

		/// <summary>
		/// Invokes an action for every element that is added to or removed from the
		/// ObservableCollection. <paramref name="newAction"/> is also invoked immediately for
		/// every element that is currently in the collection.
		/// </summary>
		/// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">The source sequence.</param>
		/// <param name="newAction">Method to invoke for each added sequence element.</param>
		/// <param name="oldAction">Method to invoke for each removed sequence element.</param>
		public static void ForAddedRemoved<T>(this ObservableCollection<T> source, Action<T> newAction, Action<T> oldAction)
		{
			foreach (T item in source)
			{
				newAction(item);
			}

			source.CollectionChanged += (s, e) =>
			{
				if (e.NewItems != null)
				{
					foreach (T item in e.NewItems)
					{
						newAction(item);
					}
				}
				if (e.OldItems != null)
				{
					foreach (T item in e.OldItems)
					{
						oldAction(item);
					}
				}
			};
		}

		#endregion INotifyCollectionChanged helpers

		#region Repetition

		/// <summary>
		/// Invokes the action a number of times.
		/// </summary>
		/// <param name="count">The number of invocations.</param>
		/// <param name="action">The action to invoke.</param>
		public static void Times(this int count, Action action)
		{
			for (int i = 0; i < count; i++)
			{
				action();
			}
		}

		/// <summary>
		/// Invokes the action a number of times.
		/// </summary>
		/// <param name="count">The number of invocations.</param>
		/// <param name="action">The action to invoke. The first parameter is the zero-based iteration.</param>
		public static void Times(this int count, Action<int> action)
		{
			for (int i = 0; i < count; i++)
			{
				action(i);
			}
		}

		#endregion Repetition

		#region Searching

		/// <summary>
		/// Returns the index of the first element that matches the predicate, or -1.
		/// </summary>
		/// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">The sequence whose elements to apply the predicate to.</param>
		/// <param name="predicate">A function to test each element for a condition.</param>
		/// <returns></returns>
		public static int IndexOf<T>(this IEnumerable<T> source, Predicate<T> predicate)
		{
			int index = 0;
			foreach (T item in source)
			{
				if (predicate(item)) return index;
				index++;
			}
			return -1;
		}

		#endregion Searching

		#region List conversion

		/// <summary>
		/// Creates an <see cref="ObservableCollection{T}"/> from an <see cref="IEnumerable{T}"/>.
		/// </summary>
		/// <typeparam name="T">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">The <see cref="IEnumerable{T}"/> to create an <see cref="ObservableCollection{T}"/> from.</param>
		/// <returns>An <see cref="ObservableCollection{T}"/> that contains elements from the input sequence.</returns>
		public static ObservableCollection<T> ToObservableCollection<T>(this IEnumerable<T> source)
		{
			return new ObservableCollection<T>(source);
		}

		#endregion List conversion
	}
}
