using System;
using System.Windows;

namespace Unclassified.UI
{
	public static class xxx_FocusManager
	{
		public static bool GetIsFocused(DependencyObject obj)
		{
			return (bool) obj.GetValue(IsFocusedProperty);
		}

		public static void SetIsFocused(DependencyObject obj, bool value)
		{
			obj.SetValue(IsFocusedProperty, value);
		}

		public static readonly DependencyProperty IsFocusedProperty = DependencyProperty.RegisterAttached(
			 "IsFocused",
			 typeof(bool),
			 typeof(xxx_FocusManager),
			 new UIPropertyMetadata(false, OnIsFocusedPropertyChanged));

		private static void OnIsFocusedPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var uie = (UIElement) d;
			if ((bool) e.NewValue)
			{
				uie.Focus();   // Don't care about false values.
			}
		}
	}
}


//        public static readonly DependencyProperty IsFocusedProperty = DependencyProperty.RegisterAttached(
//            "IsFocused",
//            typeof(bool?),
//            typeof(FocusManager),
//            new FrameworkPropertyMetadata(IsFocusedChanged));

//        public static bool? GetIsFocused(DependencyObject element)
//        {
//            if (element == null)
//            {
//                throw new ArgumentNullException("element");
//            }

//            return (bool?) element.GetValue(IsFocusedProperty);
//        }

//        public static void SetIsFocused(DependencyObject element, bool? value)
//        {
//            if (element == null)
//            {
//                throw new ArgumentNullException("element");
//            }

//            element.SetValue(IsFocusedProperty, value);
//        }

//        private static void IsFocusedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
//        {
//            FrameworkElement fe = (FrameworkElement) d;

//            if (e.OldValue == null)
//            {
//                fe.GotFocus += FrameworkElement_GotFocus;
//                fe.LostFocus += FrameworkElement_LostFocus;
//            }

//            if ((bool) e.NewValue)
//            {
//                fe.Focus();
//            }
//        }

//        private static void FrameworkElement_GotFocus(object sender, RoutedEventArgs e)
//        {
//            ((FrameworkElement) sender).SetValue(IsFocusedProperty, true);
//        }

//        private static void FrameworkElement_LostFocus(object sender, RoutedEventArgs e)
//        {
//            ((FrameworkElement) sender).SetValue(IsFocusedProperty, false);
//        }
