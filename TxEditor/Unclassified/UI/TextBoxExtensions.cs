using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Unclassified.Util;

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
		public static readonly DependencyProperty DisableInsertKeyProperty = DependencyProperty.RegisterAttached(
			name: "DisableInsertKey",
			propertyType: typeof(bool),
			ownerType: typeof(TextBoxExtensions),
			defaultMetadata: new PropertyMetadata(false, OnDisableInsertKeyChanged));

		private static void OnDisableInsertKeyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
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

		#region TextBox delayed update attached property

		/// <summary>
		/// Gets the value of the UpdateDelay XAML attached property from the specified
		/// DependencyObject.
		/// </summary>
		/// <param name="obj">The object from which to read the property value.</param>
		/// <returns></returns>
		public static int GetUpdateDelay(DependencyObject obj)
		{
			return (int) obj.GetValue(UpdateDelayProperty);
		}

		/// <summary>
		/// Sets the value of the UpdateDelay XAML attached property on the specified
		/// DependencyObject.
		/// </summary>
		/// <param name="obj">The target object on which to set the UpdateDelay XAML attached property.</param>
		/// <param name="value">The property value to set.</param>
		public static void SetUpdateDelay(DependencyObject obj, int value)
		{
			obj.SetValue(UpdateDelayProperty, value);
		}

		/// <summary>
		/// Identifies the UpdateDelay XAML attached property.
		/// </summary>
		public static readonly DependencyProperty UpdateDelayProperty = DependencyProperty.RegisterAttached(
			name: "UpdateDelay",
			propertyType: typeof(int),
			ownerType: typeof(TextBoxExtensions),
			defaultMetadata: new PropertyMetadata(0, OnUpdateDelayChanged));

		private static readonly DependencyProperty UpdateDelayCallProperty = DependencyProperty.RegisterAttached(
			name: "UpdateDelayCall",
			propertyType: typeof(DelayedCall),
			ownerType: typeof(TextBoxExtensions));

		private static void OnUpdateDelayChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			if (d is TextBox && e != null)
			{
				if ((int) e.NewValue > 0)
				{
					(d as TextBox).GotFocus += TextBox_GotFocus;
					(d as TextBox).TextChanged += TextBox_TextChanged;
					(d as TextBox).LostFocus += TextBox_LostFocus;
				}
				else
				{
					(d as TextBox).GotFocus -= TextBox_GotFocus;
					(d as TextBox).TextChanged -= TextBox_TextChanged;
					(d as TextBox).LostFocus -= TextBox_LostFocus;
				}
			}
		}

		private static void TextBox_GotFocus(object sender, EventArgs e)
		{
			TextBox textBox = sender as TextBox;
			if (textBox != null)
			{
				DelayedCall dc = DelayedCall<TextBox>.Create(TextBoxUpdateBinding, textBox, GetUpdateDelay(textBox));
				textBox.SetValue(UpdateDelayCallProperty, dc);
			}
		}

		private static void TextBoxUpdateBinding(TextBox textBox)
		{
			// Source: http://stackoverflow.com/a/5631292/143684
			var be = textBox.GetBindingExpression(TextBox.TextProperty);
			if (be != null)
			{
				be.UpdateSource();
			}
		}

		private static void TextBox_TextChanged(object sender, TextChangedEventArgs e)
		{
			TextBox textBox = sender as TextBox;
			if (textBox != null)
			{
				DelayedCall dc = textBox.GetValue(UpdateDelayCallProperty) as DelayedCall;
				if (dc != null)
				{
					dc.Reset();
				}
			}
		}

		private static void TextBox_LostFocus(object sender, EventArgs e)
		{
			TextBox textBox = sender as TextBox;
			if (textBox != null)
			{
				DelayedCall dc = textBox.GetValue(UpdateDelayCallProperty) as DelayedCall;
				if (dc != null)
				{
					dc.Dispose();
				}
				textBox.SetValue(UpdateDelayCallProperty, null);
			}
		}

		#endregion TextBox delayed update attached property

		#region On-demand focused binding update

		/// <summary>
		/// Updates the data binding source of the TextBox to update the model instance with the
		/// current user input.
		/// </summary>
		/// <param name="textBox"></param>
		public static void UpdateBindingSource(this TextBox textBox)
		{
			var be = textBox.GetBindingExpression(TextBox.TextProperty);
			if (be != null)
			{
				be.UpdateSource();
			}
		}

		/// <summary>
		/// Updates the data binding source of the currently focused TextBox to update the model
		/// instance with the current user input.
		/// </summary>
		public static void UpdateFocusedTextBox()
		{
			// Source: http://stackoverflow.com/a/5631292/143684
			TextBox focusedTextBox = Keyboard.FocusedElement as TextBox;
			if (focusedTextBox != null)
			{
				UpdateBindingSource(focusedTextBox);
			}
		}

		#endregion On-demand focused binding update
	}
}
