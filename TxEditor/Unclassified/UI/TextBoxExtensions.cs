using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Unclassified.UI
{
	/// <summary>
	/// Provides extension methods for WPF TextBox controls.
	/// </summary>
	public static class TextBoxExtensions
	{
		#region TextBox no-overwrite attached property

		// Source: http://stackoverflow.com/a/18345524/143684

		/// <summary>
		/// Gets the value of the DisableInsertKey XAML attached property from the specified
		/// DependencyObject.
		/// </summary>
		/// <param name="obj">The object from which to read the property value.</param>
		/// <returns></returns>
		public static bool GetDisableInsertKey(DependencyObject obj)
		{
			return (bool) obj.GetValue(DisableInsertKeyProperty);
		}

		/// <summary>
		/// Sets the value of the DisableInsertKey XAML attached property on the specified
		/// DependencyObject.
		/// </summary>
		/// <param name="obj">The target object on which to set the DisableInsertKey XAML attached property.</param>
		/// <param name="value">The property value to set.</param>
		public static void SetDisableInsertKey(DependencyObject obj, bool value)
		{
			obj.SetValue(DisableInsertKeyProperty, value);
		}

		/// <summary>
		/// Identifies the DisableInsertKey XAML attached property.
		/// </summary>
		public static readonly DependencyProperty DisableInsertKeyProperty =
			DependencyProperty.RegisterAttached("DisableInsertKey", typeof(bool), typeof(TextBoxExtensions), new PropertyMetadata(false, OnDisableInsertChanged));

		private static void OnDisableInsertChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is TextBox && e != null)
			{
				if ((bool) e.NewValue)
				{
					(d as TextBox).PreviewKeyDown += TextBox_PreviewKeyDown;
				}
				else
				{
					(d as TextBox).PreviewKeyDown -= TextBox_PreviewKeyDown;
				}
			}
		}

		private static void TextBox_PreviewKeyDown(object sender, KeyEventArgs e)
		{
			if (e.Key == Key.Insert && e.KeyboardDevice.Modifiers == ModifierKeys.None)
			{
				e.Handled = true;
			}
		}

		#endregion TextBox no-overwrite attached property
	}
}
