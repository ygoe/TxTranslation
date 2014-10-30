using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Unclassified.TxEditor.ViewModels;
using Unclassified.TxLib;
using Unclassified.UI;
using Unclassified.Util;

namespace Unclassified.TxEditor.Views
{
	public partial class DateTimeWindow : Window
	{
		private string culture;
		private DelayedCall timerDc;

		public DateTimeWindow()
		{
			InitializeComponent();
			this.HideIcon();

			foreach (string cultureName in MainViewModel.Instance.LoadedCultureNames)
			{
				CultureInfo ci = new CultureInfo(cultureName);
				CulturesList.Items.Add(new ValueViewModel<string>(ci.DisplayName, cultureName));
			}

			timerDc = DelayedCall.Create(UpdateView, 1000);
		}

		public string Culture
		{
			get { return culture; }
			set
			{
				this.culture = value;
				CulturesList.SelectedItem = CulturesList.Items.OfType<ValueViewModel<string>>().FirstOrDefault(v => v.Value == culture);
			}
		}

		private void Window_Loaded(object sender, RoutedEventArgs args)
		{
		}

		private void Window_KeyDown(object sender, KeyEventArgs args)
		{
			if (args.Key == Key.Escape && Keyboard.Modifiers == ModifierKeys.None)
			{
				args.Handled = true;
				Close();
			}
		}

		private void CulturesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			culture = ((ValueViewModel<string>) CulturesList.SelectedItem).Value;
			UpdateView();
		}

		private void DateTimeTextBox_SelectedTimeChanged(object sender, EventArgs args)
		{
			UpdateView();
		}

		private void TxDowAbbrCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			if (TxDowAbbrCheckBox.IsChecked == true)
			{
				TxDowLongCheckBox.IsChecked = false;
			}
			UpdateView();
		}

		private void TxDowLongCheckBox_Checked(object sender, RoutedEventArgs e)
		{
			if (TxDowLongCheckBox.IsChecked == true)
			{
				TxDowAbbrCheckBox.IsChecked = false;
			}
			UpdateView();
		}

		public void UpdateView()
		{
			timerDc.Cancel();

			DateTime d = DateTimeSelector.SelectedTime;
			CultureInfo originalCulture = Thread.CurrentThread.CurrentCulture;
			try
			{
				// Copy the Tx:* keys from the loaded dictionary file into TxLib and restore them later
				Tx.ReplaceSystemTexts(MainViewModel.Instance.GetSystemTexts());

				Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);

				// .NET formats

				YearMonthLabel.Text = d.ToString(CultureInfo.CurrentCulture.DateTimeFormat.YearMonthPattern);
				YearMonthFormatLabel.Text = CultureInfo.CurrentCulture.DateTimeFormat.YearMonthPattern + " [Y]";

				ShortDateLabel.Text = d.ToShortDateString();
				ShortDateFormatLabel.Text = CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern + " [d]";
				LongDateLabel.Text = d.ToLongDateString();
				LongDateFormatLabel.Text = CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern + " [D]";

				MonthDayLabel.Text = d.ToString(CultureInfo.CurrentCulture.DateTimeFormat.MonthDayPattern);
				MonthDayFormatLabel.Text = CultureInfo.CurrentCulture.DateTimeFormat.MonthDayPattern + " [M]";

				ShortTimeLabel.Text = d.ToShortTimeString();
				ShortTimeFormatLabel.Text = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern + " [t]";
				LongTimeLabel.Text = d.ToLongTimeString();
				LongTimeFormatLabel.Text = CultureInfo.CurrentCulture.DateTimeFormat.LongTimePattern + " [T]";

				// Tx formats

				TxTime addDow = 0;
				if (TxDowAbbrCheckBox.IsChecked == true)
					addDow = TxTime.DowAbbr;
				if (TxDowLongCheckBox.IsChecked == true)
					addDow = TxTime.DowLong;

				// First only the format strings
				// (They should be finished if the data can't be displayed because of format string
				// errors, possibly while the user is currently editing them.)

				TxYMShortFormatLabel.Text = Tx.Time(d, TxTime.YearMonth | addDow, true);
				TxYMTabFormatLabel.Text = Tx.Time(d, TxTime.YearMonthTab | addDow, true);
				TxYMAbbrFormatLabel.Text = Tx.Time(d, TxTime.YearMonthAbbr | addDow, true);
				TxYMLongFormatLabel.Text = Tx.Time(d, TxTime.YearMonthLong | addDow, true);

				TxYMDShortFormatLabel.Text = Tx.Time(d, TxTime.YearMonthDay | addDow, true);
				TxYMDTabFormatLabel.Text = Tx.Time(d, TxTime.YearMonthDayTab | addDow, true);
				TxYMDAbbrFormatLabel.Text = Tx.Time(d, TxTime.YearMonthDayAbbr | addDow, true);
				TxYMDLongFormatLabel.Text = Tx.Time(d, TxTime.YearMonthDayLong | addDow, true);

				TxMShortFormatLabel.Text = Tx.Time(d, TxTime.Month | addDow, true);
				TxMTabFormatLabel.Text = Tx.Time(d, TxTime.MonthTab | addDow, true);
				TxMAbbrFormatLabel.Text = Tx.Time(d, TxTime.MonthAbbr | addDow, true);
				TxMLongFormatLabel.Text = Tx.Time(d, TxTime.MonthLong | addDow, true);

				TxMDShortFormatLabel.Text = Tx.Time(d, TxTime.MonthDay | addDow, true);
				TxMDTabFormatLabel.Text = Tx.Time(d, TxTime.MonthDayTab | addDow, true);
				TxMDAbbrFormatLabel.Text = Tx.Time(d, TxTime.MonthDayAbbr | addDow, true);
				TxMDLongFormatLabel.Text = Tx.Time(d, TxTime.MonthDayLong | addDow, true);

				TxDShortFormatLabel.Text = Tx.Time(d, TxTime.Day | addDow, true);
				TxDTabFormatLabel.Text = Tx.Time(d, TxTime.DayTab | addDow, true);

				TxHShortFormatLabel.Text = Tx.Time(d, TxTime.Hour, true);
				TxHTabFormatLabel.Text = Tx.Time(d, TxTime.HourTab, true);

				TxHMShortFormatLabel.Text = Tx.Time(d, TxTime.HourMinute, true);
				TxHMTabFormatLabel.Text = Tx.Time(d, TxTime.HourMinuteTab, true);

				TxHMSShortFormatLabel.Text = Tx.Time(d, TxTime.HourMinuteSecond, true);
				TxHMSTabFormatLabel.Text = Tx.Time(d, TxTime.HourMinuteSecondTab, true);

				// Now the data

				TxYMShortLabel.Text = Tx.Time(d, TxTime.YearMonth | addDow);
				TxYMTabLabel.Text = Tx.Time(d, TxTime.YearMonthTab | addDow);
				TxYMAbbrLabel.Text = Tx.Time(d, TxTime.YearMonthAbbr | addDow);
				TxYMLongLabel.Text = Tx.Time(d, TxTime.YearMonthLong | addDow);

				TxYMDShortLabel.Text = Tx.Time(d, TxTime.YearMonthDay | addDow);
				TxYMDTabLabel.Text = Tx.Time(d, TxTime.YearMonthDayTab | addDow);
				TxYMDAbbrLabel.Text = Tx.Time(d, TxTime.YearMonthDayAbbr | addDow);
				TxYMDLongLabel.Text = Tx.Time(d, TxTime.YearMonthDayLong | addDow);

				TxMShortLabel.Text = Tx.Time(d, TxTime.Month | addDow);
				TxMTabLabel.Text = Tx.Time(d, TxTime.MonthTab | addDow);
				TxMAbbrLabel.Text = Tx.Time(d, TxTime.MonthAbbr | addDow);
				TxMLongLabel.Text = Tx.Time(d, TxTime.MonthLong | addDow);

				TxMDShortLabel.Text = Tx.Time(d, TxTime.MonthDay | addDow);
				TxMDTabLabel.Text = Tx.Time(d, TxTime.MonthDayTab | addDow);
				TxMDAbbrLabel.Text = Tx.Time(d, TxTime.MonthDayAbbr | addDow);
				TxMDLongLabel.Text = Tx.Time(d, TxTime.MonthDayLong | addDow);

				TxDShortLabel.Text = Tx.Time(d, TxTime.Day | addDow);
				TxDTabLabel.Text = Tx.Time(d, TxTime.DayTab | addDow);

				TxHShortLabel.Text = Tx.Time(d, TxTime.Hour);
				TxHTabLabel.Text = Tx.Time(d, TxTime.HourTab);

				TxHMShortLabel.Text = Tx.Time(d, TxTime.HourMinute);
				TxHMTabLabel.Text = Tx.Time(d, TxTime.HourMinuteTab);

				TxHMSShortLabel.Text = Tx.Time(d, TxTime.HourMinuteSecond);
				TxHMSTabLabel.Text = Tx.Time(d, TxTime.HourMinuteSecondTab);

				RelativeTimeLabel.Text = Tx.RelativeTime(d);
				RelativeTimeSpanLabel.Text = Tx.TimeSpan(d);
			}
			catch (FormatException)
			{
				// Ignore invalid date formats, but clear any already displayed or now outdated data
				TxYMShortLabel.Text = "";
				TxYMTabLabel.Text = "";
				TxYMAbbrLabel.Text = "";
				TxYMLongLabel.Text = "";

				TxYMDShortLabel.Text = "";
				TxYMDTabLabel.Text = "";
				TxYMDAbbrLabel.Text = "";
				TxYMDLongLabel.Text = "";

				TxMShortLabel.Text = "";
				TxMTabLabel.Text = "";
				TxMAbbrLabel.Text = "";
				TxMLongLabel.Text = "";

				TxMDShortLabel.Text = "";
				TxMDTabLabel.Text = "";
				TxMDAbbrLabel.Text = "";
				TxMDLongLabel.Text = "";

				TxDShortLabel.Text = "";
				TxDTabLabel.Text = "";

				TxHShortLabel.Text = "";
				TxHTabLabel.Text = "";

				TxHMShortLabel.Text = "";
				TxHMTabLabel.Text = "";

				TxHMSShortLabel.Text = "";
				TxHMSTabLabel.Text = "";

				RelativeTimeLabel.Text = "";
				RelativeTimeSpanLabel.Text = "";
			}
			finally
			{
				Thread.CurrentThread.CurrentCulture = originalCulture;
				Tx.RestoreSystemTexts();
			}

			timerDc.Start();
		}
	}
}
