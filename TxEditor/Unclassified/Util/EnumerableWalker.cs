using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Unclassified.Util
{
	internal class EnumerableWalker<T> : IEnumerable<T>
		where T : class
	{
		private IEnumerable<T> array;
		private IEnumerator<T> enumerator;

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

		public IEnumerator GetEnumerator()
		{
			enumerator = array.GetEnumerator();
			return enumerator;
		}

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
