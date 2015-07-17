// Copyright (c) 2014, Yves Goergen, http://unclassified.software/source/collectiondictionary
//
// Copying and distribution of this file, with or without modification, are permitted provided the
// copyright notice and this notice are preserved. This file is offered as-is, without any warranty.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Unclassified.Util
{
	/// <summary>
	/// Implements a dictionary that can store multiple values for each key.
	/// </summary>
	/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
	/// <typeparam name="TValue">The type of the values in the dictionary.</typeparam>
	public class CollectionDictionary<TKey, TValue> : Dictionary<TKey, ICollection<TValue>>
	{
		#region Private data

		/// <summary>
		/// The empty collection to return in <see cref="GetValuesOrEmpty"/>.
		/// </summary>
		private TValue[] empty = new TValue[0];

		#endregion Private data

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="CollectionDictionary{TKey, TValue}"/> class
		/// that is empty, has the default initial capacity, and uses the default equality comparer
		/// for the key type.
		/// </summary>
		public CollectionDictionary()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CollectionDictionary{TKey, TValue}"/> class
		/// that contains elements copied from the specified <see cref="IDictionary{TKey, TValue}"/>
		/// and uses the default equality comparer for the key type.
		/// </summary>
		/// <param name="dictionary">The dictionary whose elements are copied to the new
		///   <see cref="CollectionDictionary{TKey, TValue}"/>.</param>
		public CollectionDictionary(IDictionary<TKey, TValue> dictionary)
		{
			LoadFromDictionary(dictionary);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CollectionDictionary{TKey, TValue}"/> class
		/// that is empty, has the default initial capacity, and uses the specified
		/// <see cref="IEqualityComparer{T}"/>.
		/// </summary>
		/// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when
		///   comparing keys, or null to use the default <see cref="EqualityComparer{T}"/> for the
		///   type of the key.</param>
		public CollectionDictionary(IEqualityComparer<TKey> comparer)
			: base(comparer)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CollectionDictionary{TKey, TValue}"/> class
		/// that is empty, has the specified initial capacity, and uses the default equality
		/// comparer for the key type.
		/// </summary>
		/// <param name="capacity">The initial number of elements that the
		///   <see cref="CollectionDictionary{TKey, TValue}"/> can contain.</param>
		public CollectionDictionary(int capacity)
			: base(capacity)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CollectionDictionary{TKey, TValue}"/> class
		/// that contains elements copied from the specified <see cref="IDictionary{TKey, TValue}"/>
		/// and uses the specified <see cref="IEqualityComparer{T}"/>.
		/// </summary>
		/// <param name="dictionary">The dictionary whose elements are copied to the new
		///   <see cref="CollectionDictionary{TKey, TValue}"/>.</param>
		/// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when
		///   comparing keys, or null to use the default <see cref="EqualityComparer{T}"/> for the
		///   type of the key.</param>
		public CollectionDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
			: base(comparer)
		{
			LoadFromDictionary(dictionary);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CollectionDictionary{TKey, TValue}"/> class
		/// that is empty, has the specified initial capacity, and uses the specified
		/// <see cref="IEqualityComparer{T}"/>.
		/// </summary>
		/// <param name="capacity">The initial number of elements that the
		///   <see cref="CollectionDictionary{TKey, TValue}"/> can contain.</param>
		/// <param name="comparer">The <see cref="IEqualityComparer{T}"/> implementation to use when
		///   comparing keys, or null to use the default <see cref="EqualityComparer{T}"/> for the
		///   type of the key.</param>
		public CollectionDictionary(int capacity, IEqualityComparer<TKey> comparer)
			: base(capacity, comparer)
		{
		}

		/// <summary>
		/// Initializes the <see cref="CollectionDictionary{TKey, TValue}"/> contents from a
		/// <see cref="Dictionary{TKey, TValue}"/> instance.
		/// </summary>
		/// <param name="dictionary">The dictionary to copy elements from.</param>
		private void LoadFromDictionary(IDictionary<TKey, TValue> dictionary)
		{
			foreach (var kvp in dictionary)
			{
				var list = new List<TValue>();
				list.Add(kvp.Value);
				Add(kvp.Key, list);
			}
		}

		#endregion Constructors

		#region Data access

		/// <summary>
		/// Gets the count of all values for all keys.
		/// </summary>
		public int ValueCount
		{
			get
			{
				int count = 0;
				foreach (var kvp in this)
				{
					count += kvp.Value.Count;
				}
				return count;
			}
		}

		/// <summary>
		/// Adds the specified value to a key in the dictionary.
		/// </summary>
		/// <param name="key">The key of the element to add the value to.</param>
		/// <param name="value">The value to add to the key. The value can be null for reference types.</param>
		public void Add(TKey key, TValue value)
		{
			ICollection<TValue> collection;
			if (!TryGetValue(key, out collection))
			{
				collection = new List<TValue>();
				base.Add(key, collection);
			}
			collection.Add(value);
		}

		/// <summary>
		/// Adds multiple values to a key in the dictionary.
		/// </summary>
		/// <param name="key">The key of the element to add the values to.</param>
		/// <param name="values">The values to add to the key.</param>
		public void AddRange(TKey key, IEnumerable<TValue> values)
		{
			ICollection<TValue> collection;
			if (!TryGetValue(key, out collection))
			{
				collection = new List<TValue>();
				base.Add(key, collection);
			}
			foreach (TValue value in values)
			{
				collection.Add(value);
			}
		}

		/// <summary>
		/// Removes the value from the specified key in the dictionary.
		/// </summary>
		/// <param name="key">The key of the element to remove the value from.</param>
		/// <param name="value">The value to remove from the key. The value can be null for reference types.</param>
		public void RemoveValue(TKey key, TValue value)
		{
			ICollection<TValue> collection;
			if (TryGetValue(key, out collection))
			{
				collection.Remove(value);
				if (collection.Count == 0)
				{
					Remove(key);
				}
			}
		}

		/// <summary>
		/// Determines whether the dictionary contains a specific value in any key.
		/// </summary>
		/// <param name="value">The value to locate in the dictionary. The value can be null for reference types.</param>
		/// <returns>true if the dictionary contains the specified value; otherwise, false.</returns>
		public bool ContainsValue(TValue value)
		{
			foreach (var kvp in this)
			{
				if (kvp.Value.Contains(value)) return true;
			}
			return false;
		}

		/// <summary>
		/// Returns a collection of values for the specified key. If the key does not exist in the
		/// dictionary, an empty collection is returned.
		/// </summary>
		/// <param name="key">The key of the element to return the values from.</param>
		/// <returns></returns>
		public ICollection<TValue> GetValuesOrEmpty(TKey key)
		{
			ICollection<TValue> values;
			if (TryGetValue(key, out values))
			{
				return values;
			}
			return empty;
		}

		#endregion Data access
	}
}
