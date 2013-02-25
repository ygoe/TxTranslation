using System.Windows;

namespace Unclassified.UI
{
	/// <summary>
	/// Defines properties to declare how items can be collapsed by a layout algorithm.
	/// </summary>
	public interface ICollapsableToolbarItem
	{
		/// <summary>
		/// Gets or sets the visibility of the full content. If this is Collapsed, then the item
		/// content is reduced to the minimal layout.
		/// </summary>
		Visibility ContentVisibility { get; set; }
		/// <summary>
		/// Gets or sets the priority with which the item is collapsed.
		/// TODO: Test and describe whether smaller or greater values are collapsed first. Should be greater.
		/// </summary>
		int CollapsePriority { get; set; }
	}
}
