using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Unclassified.UI
{
	public partial class DateTimeTextBox : UserControl
	{
		//public static DependencyProperty SelectedTimeProperty = DependencyProperty.Register(
		//    "SelectedTime",
		//    typeof(DateTime),
		//    typeof(DateTimeTextBox),
		//    new PropertyMetadata(SelectedTimeChanged));

		//public DateTime SelectedTime
		//{
		//    get { return (DateTime) GetValue(SelectedTimeProperty); }
		//    set { SetValue(SelectedTimeProperty, value); }
		//}

		//private static void SelectedTimeChanged(DependencyObject obj, DependencyPropertyChangedEventArgs args)
		//{
		//    DateTimeTextBox control = (DateTimeTextBox) obj;
		//    control.intendedDay = control.SelectedTime.Day;   // Exclude this with an updating flag where now the private field is updated
		//    control.UpdateTexts();
		//}

		private int intendedDay;
		private TextBox lastFocusedTextBox;

		public event EventHandler SelectedTimeChanged;

		public DateTimeTextBox()
		{
			InitializeComponent();

			SelectedTime = DateTime.Now;
		}

		private DateTime selectedTime;
		public DateTime SelectedTime
		{
			get
			{
				return selectedTime;
			}
			set
			{
				if (value != selectedTime)
				{
					selectedTime = value;
					intendedDay = selectedTime.Day;
					UpdateTexts();
					OnSelectedTimeChanged();
				}
			}
		}

		private void UpdateTexts()
		{
			YearText.Text = SelectedTime.Year.ToString("0000");
			MonthText.Text = SelectedTime.Month.ToString("00");
			DayText.Text = SelectedTime.Day.ToString("00");
			HourText.Text = SelectedTime.Hour.ToString("00");
			MinuteText.Text = SelectedTime.Minute.ToString("00");
			SecondText.Text = SelectedTime.Second.ToString("00");

			YearText.SelectAll();
			MonthText.SelectAll();
			DayText.SelectAll();
			HourText.SelectAll();
			MinuteText.SelectAll();
			SecondText.SelectAll();
		}

		private void Text_GotFocus(object sender, RoutedEventArgs args)
		{
			TextBox source = (TextBox)args.Source;
			source.SelectAll();
			lastFocusedTextBox = source;
		}

		private void Text_LostFocus(object sender, RoutedEventArgs args)
		{
			TextBox source = (TextBox)args.Source;
			if (!Regex.IsMatch(source.Text.Trim(), @"^[0-9]+$"))
			{
				// Invalid state
				return;
			}

			int value, min, max;
			GetFieldInfo(source, out value, out min, out max);
			if (value > max) value = max;
			if (value < min) value = min;

			SetTimeComponent(value, source);
		}

		private void Text_PreviewKeyDown(object sender, KeyEventArgs args)
		{
			TextBox source = (TextBox)args.Source;
			int value, min, max;
			GetFieldInfo(source, out value, out min, out max);

			if (args.Key == Key.Down && Keyboard.Modifiers == ModifierKeys.None)
			{
				value--;
				if (value < min) value = max;
				args.Handled = true;
				SetTimeComponent(value, source);
				UpdateTexts();
			}
			if (args.Key == Key.Up && Keyboard.Modifiers == ModifierKeys.None)
			{
				value++;
				if (value > max) value = min;
				args.Handled = true;
				SetTimeComponent(value, source);
				UpdateTexts();
			}
			if (args.Key == Key.Left && Keyboard.Modifiers == ModifierKeys.None)
			{
				if (source.SelectionLength == source.Text.Length ||
					source.SelectionLength == 0 && source.SelectionStart == 0)
				{
					if (source == YearText)
					{
						SecondText.Focus();
					}
					else
					{
						source.MoveFocus(new TraversalRequest(FocusNavigationDirection.Previous));
					}
					args.Handled = true;
				}
			}
			if (args.Key == Key.Right && Keyboard.Modifiers == ModifierKeys.None)
			{
				if (source.SelectionLength == source.Text.Length ||
					source.SelectionLength == 0 && source.SelectionStart == source.Text.Length)
				{
					if (source == SecondText)
					{
						YearText.Focus();
					}
					else
					{
						source.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
					}
					args.Handled = true;
				}
			}
		}

		private void Text_PreviewTextInput(object sender, TextCompositionEventArgs args)
		{
			args.Handled = !IsTextAllowed(args.Text);
		}

		private void Text_Pasting(object sender, DataObjectPastingEventArgs args)
		{
			if (args.DataObject.GetDataPresent(typeof(string)))
			{
				string text = (string)args.DataObject.GetData(typeof(string));
				if (!IsTextAllowed(text))
				{
					args.CancelCommand();
				}
			}
			else
			{
				args.CancelCommand();
			}
		}

		private void GetFieldInfo(TextBox source, out int value, out int min, out int max)
		{
			if (source == YearText)
			{
				int.TryParse(source.Text, out value);
				min = 1;
				max = 9999;
			}
			else if (source == MonthText)
			{
				int.TryParse(source.Text, out value);
				min = 1;
				max = 12;
			}
			else if (source == DayText)
			{
				int.TryParse(source.Text, out value);
				min = 1;
				max = DateTime.DaysInMonth(selectedTime.Year, selectedTime.Month);
			}
			else if (source == HourText)
			{
				int.TryParse(source.Text, out value);
				min = 0;
				max = 23;
			}
			else if (source == MinuteText)
			{
				int.TryParse(source.Text, out value);
				min = 0;
				max = 59;
			}
			else if (source == SecondText)
			{
				int.TryParse(source.Text, out value);
				min = 0;
				max = 59;
			}
			else
			{
				// Invalid state
				value = 0;
				min = 0;
				max = 0;
			}
		}

		private void SetTimeComponent(int value, TextBox source)
		{
			int day = intendedDay;

			if (source == YearText)
			{
				int maxDay = DateTime.DaysInMonth(value, selectedTime.Month);
				if (day > maxDay) day = maxDay;
				selectedTime = new DateTime(value, selectedTime.Month, day, selectedTime.Hour, selectedTime.Minute, selectedTime.Second);
				OnSelectedTimeChanged();
			}
			else if (source == MonthText)
			{
				int maxDay = DateTime.DaysInMonth(selectedTime.Year, value);
				if (day > maxDay) day = maxDay;
				selectedTime = new DateTime(selectedTime.Year, value, day, selectedTime.Hour, selectedTime.Minute, selectedTime.Second);
				OnSelectedTimeChanged();
			}
			else if (source == DayText)
			{
				selectedTime = new DateTime(selectedTime.Year, selectedTime.Month, value, selectedTime.Hour, selectedTime.Minute, selectedTime.Second);
				intendedDay = value;
				OnSelectedTimeChanged();
			}
			else if (source == HourText)
			{
				selectedTime = new DateTime(selectedTime.Year, selectedTime.Month, selectedTime.Day, value, selectedTime.Minute, selectedTime.Second);
				OnSelectedTimeChanged();
			}
			else if (source == MinuteText)
			{
				selectedTime = new DateTime(selectedTime.Year, selectedTime.Month, selectedTime.Day, selectedTime.Hour, value, selectedTime.Second);
				OnSelectedTimeChanged();
			}
			else if (source == SecondText)
			{
				selectedTime = new DateTime(selectedTime.Year, selectedTime.Month, selectedTime.Day, selectedTime.Hour, selectedTime.Minute, value);
				OnSelectedTimeChanged();
			}
		}

		private static bool IsTextAllowed(string text)
		{
			return Regex.IsMatch(text, @"^[0-9]+$");
		}

		protected void OnSelectedTimeChanged()
		{
			var handler = SelectedTimeChanged;
			if (handler != null)
			{
				handler(this, EventArgs.Empty);
			}
		}

		private void UpButton_Click(object sender, RoutedEventArgs args)
		{
			if (lastFocusedTextBox == null) return;

			int value, min, max;
			GetFieldInfo(lastFocusedTextBox, out value, out min, out max);

			value++;
			if (value > max) value = min;
			SetTimeComponent(value, lastFocusedTextBox);
			UpdateTexts();
		}

		private void DownButton_Click(object sender, RoutedEventArgs args)
		{
			if (lastFocusedTextBox == null) return;

			int value, min, max;
			GetFieldInfo(lastFocusedTextBox, out value, out min, out max);

			value--;
			if (value < min) value = max;
			SetTimeComponent(value, lastFocusedTextBox);
			UpdateTexts();
		}
	}
}
