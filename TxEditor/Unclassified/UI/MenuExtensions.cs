using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Unclassified.UI
{
	/// <summary>
	/// Provides extension methods for WPF menus and menu items.
	/// </summary>
	internal static class MenuExtensions
	{
		/// <summary>
		/// Sets the visibility of each menu item by the availability of its command.
		/// </summary>
		/// <param name="menu">The menu to update.</param>
		/// <remarks>
		/// Before calling this method, the data binding must be completed.
		/// </remarks>
		public static void SetVisibilityFromCommands(this MenuBase menu)
		{
			foreach (var menuItem in menu.Items.OfType<MenuItem>())
			{
				menuItem.Visibility =
					menuItem.Command != null && menuItem.Command.CanExecute(null) ?
					Visibility.Visible : Visibility.Collapsed;
			}
		}

		/// <summary>
		/// Hides all separators at the beginning and end of the menu and reduces multiple
		/// subsequent separators to a single one.
		/// </summary>
		/// <param name="menu">The menu to update.</param>
		public static void ReduceSeparators(this MenuBase menu)
		{
			// First, show all separators
			for (int i = 0; i < menu.Items.Count; i++)
			{
				var sep = menu.Items[i] as Separator;
				if (sep != null)
					sep.Visibility = Visibility.Visible;
			}
			// Hide separators before first visible non-separator
			for (int i = 0; i < menu.Items.Count; i++)
			{
				var item = menu.Items[i] as FrameworkElement;
				if (item.Visibility == Visibility.Collapsed) continue;
				var sep = item as Separator;
				if (sep != null)
					sep.Visibility = Visibility.Collapsed;
				else
					break;
			}
			// Hide separators after last visible non-separator
			for (int i = menu.Items.Count - 1; i >= 0; i--)
			{
				var item = menu.Items[i] as FrameworkElement;
				if (item.Visibility == Visibility.Collapsed) continue;
				var sep = item as Separator;
				if (sep != null)
					sep.Visibility = Visibility.Collapsed;
				else
					break;
			}
			// Hide multiple subsequent separators but the first one
			bool prevSeparatorVisible = false;
			for (int i = 0; i < menu.Items.Count; i++)
			{
				var item = menu.Items[i] as FrameworkElement;
				if (item.Visibility == Visibility.Collapsed) continue;
				var sep = item as Separator;
				if (sep != null)
				{
					bool visible = sep.Visibility == Visibility.Visible;
					if (visible && prevSeparatorVisible)
						sep.Visibility = Visibility.Collapsed;
					prevSeparatorVisible = visible;
				}
				else
				{
					prevSeparatorVisible = false;
				}
			}
		}

		/// <summary>
		/// Returns a value indicating whether the menu has any visible items.
		/// </summary>
		/// <param name="menu"></param>
		/// <returns></returns>
		public static bool AnyVisibleItems(this MenuBase menu)
		{
			return menu.Items.OfType<MenuItem>().Any(mi => mi.Visibility == Visibility.Visible);
		}
	}
}
