using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections;

namespace Unclassified
{
	/// <summary>
	/// Simple observable HashSet class. Currently only the Count property is observable.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	class ObservableHashSet<T> : INotifyPropertyChanged, IEnumerable<T>, IEnumerable
	{
		public event PropertyChangedEventHandler PropertyChanged;
		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		private HashSet<T> set;

		public ObservableHashSet()
		{
			set = new HashSet<T>();
		}

		public bool Add(T item)
		{
			bool b = set.Add(item);
			OnPropertyChanged("Count");
			return b;
		}

		public void Clear()
		{
			set.Clear();
			OnPropertyChanged("Count");
		}

		public bool Contains(T item)
		{
			return set.Contains(item);
		}

		public int Count
		{
			get { return set.Count; }
		}

		public void ExceptWith(IEnumerable<T> other)
		{
			set.ExceptWith(other);
			OnPropertyChanged("Count");
		}

		public void IntersectWith(IEnumerable<T> other)
		{
			set.IntersectWith(other);
			OnPropertyChanged("Count");
		}

		public bool IsProperSubsetOf(IEnumerable<T> other)
		{
			return set.IsProperSubsetOf(other);
		}

		public bool IsProperSupersetOf(IEnumerable<T> other)
		{
			return set.IsProperSupersetOf(other);
		}

		public bool IsSubsetOf(IEnumerable<T> other)
		{
			return set.IsSubsetOf(other);
		}

		public bool IsSupersetOf(IEnumerable<T> other)
		{
			return set.IsSupersetOf(other);
		}

		public bool Overlaps(IEnumerable<T> other)
		{
			return set.Overlaps(other);
		}

		public bool Remove(T item)
		{
			bool b = set.Remove(item);
			OnPropertyChanged("Count");
			return b;
		}

		public int RemoveWhere(Predicate<T> match)
		{
			int i = set.RemoveWhere(match);
			OnPropertyChanged("Count");
			return i;
		}

		public bool SetEquals(IEnumerable<T> other)
		{
			return set.SetEquals(other);
		}

		public void SymmetricExceptWith(IEnumerable<T> other)
		{
			set.SymmetricExceptWith(other);
			OnPropertyChanged("Count");
		}

		public void TrimExcess()
		{
			set.TrimExcess();
		}

		public void UnionWith(IEnumerable<T> other)
		{
			set.UnionWith(other);
			OnPropertyChanged("Count");
		}

		public IEnumerator<T> GetEnumerator()
		{
			return set.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return set.GetEnumerator();
		}
	}
}
