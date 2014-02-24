using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace Unclassified.TxEditor.ViewModel
{
	internal class TreeViewItemViewModel : ViewModelBase
	{
		#region Private data

		private static readonly TreeViewItemViewModel DummyChild = new TreeViewItemViewModel();

		private ObservableCollection<TreeViewItemViewModel> children;
		private ListCollectionView visibleChildren;
		private TreeViewItemViewModel parent;

		private bool isExpanded;
		private bool isSelected;
		private bool isVisible = true;
		private bool isEnabled = true;
		private int sortIndex;

		#endregion Private data

		#region Constructors

		protected TreeViewItemViewModel(TreeViewItemViewModel parent, bool lazyLoadChildren)
		{
			this.parent = parent;

			children = new ObservableCollection<TreeViewItemViewModel>();

			visibleChildren = new ListCollectionView(children);
			visibleChildren.Filter = item => (item is TreeViewItemViewModel) && (item as TreeViewItemViewModel).IsVisible;

			if (lazyLoadChildren)
				children.Add(DummyChild);
		}

		/// <summary>
		/// Empty constructor for creating static DummyChild instance.
		/// </summary>
		private TreeViewItemViewModel()
		{
			DisplayName = "Dummy";
		}

		#endregion Constructors

		#region Presentation Members

		/// <summary>
		/// Gets the logical child items of this object.
		/// </summary>
		public ObservableCollection<TreeViewItemViewModel> Children
		{
			get { return children; }
			set
			{
				children = value;
				OnPropertyChanged("Children");

				visibleChildren = new ListCollectionView(children);
				OnPropertyChanged("VisibleChildren");
			}
		}

		/// <summary>
		/// Gets the visible child items of this object.
		/// </summary>
		public CollectionView VisibleChildren
		{
			get { return visibleChildren; }
		}

		/// <summary>
		/// Gets a value indicating whether this object's Children have not yet been populated.
		/// </summary>
		public bool HasDummyChild
		{
			get { return Children.Count == 1 && Children[0] == DummyChild; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the TreeViewItem associated with this object is
		/// expanded.
		/// </summary>
		public bool IsExpanded
		{
			get { return isExpanded; }
			set
			{
				if (value != isExpanded)
				{
					isExpanded = value;
					OnPropertyChanged("IsExpanded");
				}

				if (isExpanded)
				{
					// Expand all the way up to the root.
					if (parent != null)
					{
						parent.IsExpanded = true;
					}
					// Lazy load the child items, if necessary.
					if (HasDummyChild)
					{
						Children.Remove(DummyChild);
						LoadChildren(true);
					}
				}
			}
		}

		public void EnsureChildren(bool allowAsync)
		{
			if (!Application.Current.Dispatcher.CheckAccess())
			{
				Application.Current.Dispatcher.Invoke((Action<bool>) EnsureChildren, allowAsync);
			}
			else
			{
				// Lazy load the child items, if necessary.
				if (HasDummyChild)
				{
					Children.Remove(DummyChild);
					LoadChildren(allowAsync);
				}
			}
		}

		public void ExpandNoParent(bool allowAsync)
		{
			if (!isExpanded)
			{
				if (!Application.Current.Dispatcher.CheckAccess())
				{
					Application.Current.Dispatcher.Invoke((Action<bool>) ExpandNoParent, allowAsync);
				}
				else
				{
					isExpanded = true;
					OnPropertyChanged("IsExpanded");

					// Lazy load the child items, if necessary.
					if (isExpanded && HasDummyChild)
					{
						Children.Remove(DummyChild);
						LoadChildren(allowAsync);
					}
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the TreeViewItem associated with this object is
		/// selected.
		/// </summary>
		public bool IsSelected
		{
			get { return isSelected; }
			set
			{
				if (value != isSelected)
				{
					isSelected = value;
					OnIsSelectedChanged();
					OnPropertyChanged("IsSelected");
				}
			}
		}

		protected virtual void OnIsSelectedChanged()
		{
		}

		public bool IsVisible
		{
			get { return isVisible; }
			set
			{
				if (value != isVisible)
				{
					isVisible = value;
					OnPropertyChanged("IsVisible");
					if (parent != null)
						parent.visibleChildren.Refresh();
				}
			}
		}

		public bool IsEnabled
		{
			get { return isEnabled; }
			set
			{
				if (value != isEnabled)
				{
					isEnabled = value;
					OnPropertyChanged("IsEnabled");
				}
			}
		}

		public int SortIndex
		{
			get { return sortIndex; }
			set
			{
				if (value != sortIndex)
				{
					sortIndex = value;
					OnPropertyChanged("SortIndex");
				}
			}
		}

		public virtual bool TryDeselect()
		{
			return true;
		}

		/// <summary>
		/// Called when the child items need to be loaded on demand. Derived classes should
		/// override this method to populate the Children collection with lazy loading.
		/// </summary>
		/// <param name="allowAsync">Allow loading the child items asynchronously.</param>
		protected virtual void LoadChildren(bool allowAsync)
		{
		}

		public TreeViewItemViewModel Parent
		{
			get { return parent; }
			set
			{
				if (value != parent)
				{
					parent = value;
					OnParentChanged();
					OnPropertyChanged("Parent");
				}
			}
		}

		protected virtual void OnParentChanged()
		{
		}

		public override string ToString()
		{
			return "{TreeViewItemViewModel " + DisplayName + "}";
		}

		#endregion Presentation Members
	}
}
