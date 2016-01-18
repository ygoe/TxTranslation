using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Unclassified.UI
{
	/// <summary>
	/// Provides extension methods for WPF ListBox controls.
	/// </summary>
	// Type name used in XAML
	[Obfuscation(Exclude = true, ApplyToMembers = false, Feature = "renaming")]
	public static class ListBoxExtensions
	{
		#region ListBox fix focus attached property

		/// <summary>
		/// Identifies the FixFocus XAML attached property.
		/// </summary>
		public static readonly DependencyProperty FixFocusProperty = DependencyProperty.RegisterAttached(
			name: "FixFocus",
			propertyType: typeof(bool),
			ownerType: typeof(ListBoxExtensions),
			defaultMetadata: new PropertyMetadata(false, OnFixFocusChanged));

		/// <summary>
		/// Gets the value of the FixFocus XAML attached property from the specified DependencyObject.
		/// </summary>
		/// <param name="obj">The object from which to read the property value.</param>
		/// <returns></returns>
		public static bool GetFixFocus(DependencyObject obj)
		{
			return (bool)obj.GetValue(FixFocusProperty);
		}

		/// <summary>
		/// Sets the value of the FixFocus XAML attached property on the specified DependencyObject.
		/// </summary>
		/// <param name="obj">The target object on which to set the FixFocus XAML attached property.</param>
		/// <param name="value">The property value to set.</param>
		public static void SetFixFocus(DependencyObject obj, bool value)
		{
			obj.SetValue(FixFocusProperty, value);
		}

		private static void OnFixFocusChanged(DependencyObject depObj, DependencyPropertyChangedEventArgs args)
		{
			if (depObj is ListBox && args != null)
			{
				if ((bool)args.NewValue)
				{
					(depObj as ListBox).GotKeyboardFocus += ListBox_GotKeyboardFocus;
				}
				else
				{
					(depObj as ListBox).GotKeyboardFocus -= ListBox_GotKeyboardFocus;
				}
			}
		}

		private static void ListBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs args)
		{
			var listBox = sender as ListBox;
			if (listBox?.IsKeyboardFocused == true)
			{
				// ListBox has KeyboardFocus, it really should be on an item instead to fix keyboard navigation
				if (listBox.SelectedItem != null)
				{
					// Focus the selected item
					(listBox.ItemContainerGenerator.ContainerFromItem(listBox.SelectedItem) as UIElement)?.Focus();
				}
				else if (listBox.Items.Count > 0)
				{
					// Focus the first item. This implicitly selects it. Clear selection afterwards.
					(listBox.ItemContainerGenerator.ContainerFromIndex(0) as UIElement)?.Focus();
					listBox.SelectedItem = null;
				}
			}
		}

		#endregion ListBox fix focus attached property
	}
}
