using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace Unclassified.TxEditor.ViewModels
{
	internal class TreeViewItemViewModel : ViewModelBase, IEditableObject
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

		/// <summary>
		/// Gets or sets a value indicating whether the item is visible in the tree view. Visibility
		/// by this property is evaluated in the filter of the VisibleChildren CollectionView, to
		/// which the UI control is bound. It does not set the TreeViewItem's IsVisible property.
		/// </summary>
		public bool IsVisible
		{
			get { return isVisible; }
			set
			{
				if (value != isVisible)
				{
					isVisible = value;
					OnPropertyChanged("IsVisible");
					if (!value)
					{
						// Invisible items can no longer be selected
						IsSelected = false;
					}
					if (parent != null)
					{
						parent.RefreshVisibleChildrenItem(this);
					}
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

		#region Navigation helper

		public TreeViewItemViewModel FindPreviousSibling()
		{
			int myIndex = Parent.Children.IndexOf(this);
			if (myIndex > 0)
			{
				return Parent.Children[myIndex - 1];
			}
			return null;
		}

		public TreeViewItemViewModel FindNextSibling()
		{
			int myIndex = Parent.Children.IndexOf(this);
			int count = Parent.Children.Count;
			if (myIndex < count - 1)
			{
				return Parent.Children[myIndex + 1];
			}
			return null;
		}

		/// <summary>
		/// Finds the remaining item after some items have been deleted.
		/// </summary>
		/// <param name="predicate">Function that determines whether an item is acceptable.</param>
		/// <returns>The first accepted item, or null.</returns>
		public TreeViewItemViewModel FindRemainingItem(Predicate<TreeViewItemViewModel> predicate)
		{
			// Start on the current tree level
			TreeViewItemViewModel level = this;

			do
			{
				// Try the following siblings
				TreeViewItemViewModel position = level;
				do
				{
					position = position.FindNextSibling();
					if (position != null && predicate(position)) return position;
				}
				while (position != null);

				// Try the preceding siblings
				position = this;
				do
				{
					position = position.FindPreviousSibling();
					if (position != null && predicate(position)) return position;
				}
				while (position != null);

				// Continue on the parent level (stop on the root item)
				level = level.Parent;
			}
			while (level != null && level.Parent != null);

			// Nothing remains
			return null;
		}

		/// <summary>
		/// Returns a value indicating whether the current instance is a parent or grand-parent of
		/// the specified child item.
		/// </summary>
		/// <param name="child"></param>
		/// <returns></returns>
		public bool IsAParentOf(TreeViewItemViewModel child)
		{
			TreeViewItemViewModel parent = child;
			do
			{
				parent = parent.Parent;
				if (parent != null && parent == this) return true;
			}
			while (parent != null);
			// All parents compared, it's not our (grand-)child
			return false;
		}

		#endregion Navigation helper

		#region IEditableObject members

		/// <summary>
		/// Refreshes a single item in the CollectionView of all (filtered) visible children.
		/// </summary>
		/// <param name="item">The changed item.</param>
		/// <remarks>
		/// This makes use of the IEditableObject implementation of the item. When an item is
		/// edited through this mechanism, and the update is committed, the CollectionView will
		/// re-evaluate the item and apply the filtering accordingly. This method must be called
		/// for each item that may have been updated. Changes that are signalled through
		/// INotifyPropertyChanged are not considered by a CollectionView. Updating each single
		/// log item avoids the Reset type change notification and the focused item issue.
		/// </remarks>
		private void RefreshVisibleChildrenItem(TreeViewItemViewModel item)
		{
			var ev = visibleChildren as IEditableCollectionView;
			if (ev != null)
			{
				ev.EditItem(item);
				ev.CommitEdit();
			}
		}

		public void BeginEdit()
		{
			// Does nothing. IEditableObject is just used for signalling the CollectionView to
			// update the item.
		}

		public void CancelEdit()
		{
			// Does nothing. IEditableObject is just used for signalling the CollectionView to
			// update the item.
		}

		public void EndEdit()
		{
			// Does nothing. IEditableObject is just used for signalling the CollectionView to
			// update the item.
		}

		#endregion IEditableObject members
	}
}
