using System;
using System.Collections;
using System.Collections.Generic;

namespace Unclassified.Util
{
	/// <summary>
	/// Walks through an <see cref="IEnumerable{T}"/> and allows retrieving additional items.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class EnumerableWalker<T> : IEnumerable<T>
		where T : class
	{
		private IEnumerable<T> array;
		private IEnumerator<T> enumerator;

		/// <summary>
		/// Initialises a new instance of the <see cref="EnumerableWalker{T}"/> class.
		/// </summary>
		/// <param name="array">The array to walk though.</param>
		public EnumerableWalker(IEnumerable<T> array)
		{
			if (array == null) throw new ArgumentNullException("array");
			this.array = array;
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			enumerator = array.GetEnumerator();
			return enumerator;
		}

		/// <summary>
		/// Gets the enumerator.
		/// </summary>
		/// <returns></returns>
		public IEnumerator GetEnumerator()
		{
			enumerator = array.GetEnumerator();
			return enumerator;
		}

		/// <summary>
		/// Gets the next item.
		/// </summary>
		/// <returns></returns>
		public T GetNext()
		{
			if (enumerator.MoveNext())
			{
				return enumerator.Current;
			}
			else
			{
				return null;
			}
		}
	}
}
