// TxLib – Tx Translation & Localisation for .NET and WPF
// © Yves Goergen, Made in Germany
// Website: http://unclassified.software/source/txtranslation
//
// This library is free software: you can redistribute it and/or modify it under the terms of
// the GNU Lesser General Public License as published by the Free Software Foundation, version 3.
//
// This library is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License along with this
// library. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Unclassified.TxLib
{
	/// <summary>
	/// Represents an interval from a starting to an ending date and time.
	/// </summary>
	public struct DateTimeInterval
	{
		#region Private data

		private DateTime start;
		private DateTime end;
		private bool negative;
		private int years;
		private int months;
		private int days;
		private int hours;
		private int minutes;
		private int seconds;
		private int milliseconds;

		#endregion Private data

		#region Constructors

		/// <summary>
		/// Initialises a new instance of the DateTimeInterval structure with the specified start
		/// and end value.
		/// </summary>
		/// <param name="start">Start value of the interval.</param>
		/// <param name="end">End value of the interval.</param>
		public DateTimeInterval(DateTime start, DateTime end)
		{
			this.start = start;
			this.end = end;
			negative = false;
			years = months = days = hours = minutes = seconds = milliseconds = 0;
			UpdateCountValues();
		}

		/// <summary>
		/// Initialises a new instance of the DateTimeInterval structure with the specified start
		/// and duration value.
		/// </summary>
		/// <param name="start">Start value of the interval.</param>
		/// <param name="duration">Duration between the start and end value in ISO 8601 format.</param>
		public DateTimeInterval(DateTime start, string duration)
		{
			this.start = this.end = start;
			negative = false;
			years = months = days = hours = minutes = seconds = milliseconds = 0;
			Duration = duration;
		}

		/// <summary>
		/// Initialises a new instance of the DateTimeInterval structure with the specified start
		/// value and years, months and days.
		/// </summary>
		/// <param name="start">Start value of the interval.</param>
		/// <param name="years">Years to add to the start value. Can be negative.</param>
		/// <param name="months">Months to add to the start value.</param>
		/// <param name="days">Days to add to the start value.</param>
		public DateTimeInterval(DateTime start, int years, int months, int days)
		{
			if (months < 0) throw new ArgumentOutOfRangeException("months", "Interval components cannot be negative.");
			if (days < 0) throw new ArgumentOutOfRangeException("days", "Interval components cannot be negative.");
			this.start = this.end = start;
			if (years < 0)
			{
				negative = true;
				this.years = -years;
			}
			else
			{
				negative = false;
				this.years = years;
			}
			this.months = months;
			this.days = days;
			hours = minutes = seconds = milliseconds = 0;
			UpdateEnd();
		}

		/// <summary>
		/// Initialises a new instance of the DateTimeInterval structure with the specified start
		/// value and years, months, days, hours, minutes and seconds.
		/// </summary>
		/// <param name="start">Start value of the interval.</param>
		/// <param name="years">Years to add to the start value. Can be negative.</param>
		/// <param name="months">Months to add to the start value.</param>
		/// <param name="days">Days to add to the start value.</param>
		/// <param name="hours">Hours to add to the start value.</param>
		/// <param name="minutes">Minutes to add to the start value.</param>
		/// <param name="seconds">Seconds to add to the start value.</param>
		public DateTimeInterval(DateTime start, int years, int months, int days, int hours, int minutes, int seconds)
		{
			if (months < 0) throw new ArgumentOutOfRangeException("months", "Interval components cannot be negative.");
			if (days < 0) throw new ArgumentOutOfRangeException("days", "Interval components cannot be negative.");
			if (hours < 0) throw new ArgumentOutOfRangeException("hours", "Interval components cannot be negative.");
			if (minutes < 0) throw new ArgumentOutOfRangeException("minutes", "Interval components cannot be negative.");
			if (seconds < 0) throw new ArgumentOutOfRangeException("seconds", "Interval components cannot be negative.");
			this.start = this.end = start;
			if (years < 0)
			{
				negative = true;
				this.years = -years;
			}
			else
			{
				negative = false;
				this.years = years;
			}
			this.months = months;
			this.days = days;
			this.hours = hours;
			this.minutes = minutes;
			this.seconds = seconds;
			milliseconds = 0;
			UpdateEnd();
		}

		/// <summary>
		/// Initialises a new instance of the DateTimeInterval structure with the specified start
		/// value and years, months, days, hours, minutes, seconds and milliseconds.
		/// </summary>
		/// <param name="start">Start value of the interval.</param>
		/// <param name="years">Years to add to the start value. Can be negative.</param>
		/// <param name="months">Months to add to the start value.</param>
		/// <param name="days">Days to add to the start value.</param>
		/// <param name="hours">Hours to add to the start value.</param>
		/// <param name="minutes">Minutes to add to the start value.</param>
		/// <param name="seconds">Seconds to add to the start value.</param>
		/// <param name="milliseconds">Milliseconds to add to the start value.</param>
		public DateTimeInterval(DateTime start, int years, int months, int days, int hours, int minutes, int seconds, int milliseconds)
		{
			if (months < 0) throw new ArgumentOutOfRangeException("months", "Interval components cannot be negative.");
			if (days < 0) throw new ArgumentOutOfRangeException("days", "Interval components cannot be negative.");
			if (hours < 0) throw new ArgumentOutOfRangeException("hours", "Interval components cannot be negative.");
			if (minutes < 0) throw new ArgumentOutOfRangeException("minutes", "Interval components cannot be negative.");
			if (seconds < 0) throw new ArgumentOutOfRangeException("seconds", "Interval components cannot be negative.");
			if (milliseconds < 0) throw new ArgumentOutOfRangeException("milliseconds", "Interval components cannot be negative.");
			this.start = this.end = start;
			if (years < 0)
			{
				negative = true;
				this.years = -years;
			}
			else
			{
				negative = false;
				this.years = years;
			}
			this.months = months;
			this.days = days;
			this.hours = hours;
			this.minutes = minutes;
			this.seconds = seconds;
			this.milliseconds = milliseconds;
			UpdateEnd();
		}

		#endregion Constructors

		#region Public properties

		/// <summary>
		/// Gets or sets the start value. Setting this value updates the duration components.
		/// </summary>
		public DateTime Start
		{
			get
			{
				return start;
			}
			set
			{
				if (value != start)
				{
					start = value;
					UpdateCountValues();
				}
			}
		}

		/// <summary>
		/// Gets or sets the end value. Setting this value updates the duration components.
		/// </summary>
		public DateTime End
		{
			get
			{
				return end;
			}
			set
			{
				if (value != end)
				{
					end = value;
					UpdateCountValues();
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether the interval is negative. Setting this value
		/// updates the End value.
		/// </summary>
		public bool Negative
		{
			get
			{
				return negative;
			}
			set
			{
				if (value != negative)
				{
					negative = value;
					UpdateEnd();
				}
			}
		}

		/// <summary>
		/// Gets or sets the years value. This value cannot be negative. Setting this value updates
		/// the End value.
		/// </summary>
		public int Years
		{
			get
			{
				return years;
			}
			set
			{
				if (value < 0) throw new ArgumentOutOfRangeException("Years", "Interval components cannot be negative.");
				if (value != years)
				{
					years = value;
					UpdateEnd();
				}
			}
		}

		/// <summary>
		/// Gets or sets the months value. This value cannot be negative. Setting this value
		/// updates the End value.
		/// </summary>
		public int Months
		{
			get
			{
				return months;
			}
			set
			{
				if (value < 0) throw new ArgumentOutOfRangeException("Months", "Interval components cannot be negative.");
				if (value != months)
				{
					months = value;
					UpdateEnd();
				}
			}
		}

		/// <summary>
		/// Gets or sets the days value. This value cannot be negative. Setting this value updates
		/// the End value.
		/// </summary>
		public int Days
		{
			get
			{
				return days;
			}
			set
			{
				if (value < 0) throw new ArgumentOutOfRangeException("Days", "Interval components cannot be negative.");
				if (value != days)
				{
					days = value;
					UpdateEnd();
				}
			}
		}

		/// <summary>
		/// Gets or sets the hours value. This value cannot be negative. Setting this value updates
		/// the End value.
		/// </summary>
		public int Hours
		{
			get
			{
				return hours;
			}
			set
			{
				if (value < 0) throw new ArgumentOutOfRangeException("Hours", "Interval components cannot be negative.");
				if (value != hours)
				{
					hours = value;
					UpdateEnd();
				}
			}
		}

		/// <summary>
		/// Gets or sets the minutes value. This value cannot be negative. Setting this value
		/// updates the End value.
		/// </summary>
		public int Minutes
		{
			get
			{
				return minutes;
			}
			set
			{
				if (value < 0) throw new ArgumentOutOfRangeException("Minutes", "Interval components cannot be negative.");
				if (value != minutes)
				{
					minutes = value;
					UpdateEnd();
				}
			}
		}

		/// <summary>
		/// Gets or sets the seconds value. This value cannot be negative. Setting this value
		/// updates the End value.
		/// </summary>
		public int Seconds
		{
			get
			{
				return seconds;
			}
			set
			{
				if (value < 0) throw new ArgumentOutOfRangeException("Seconds", "Interval components cannot be negative.");
				if (value != seconds)
				{
					seconds = value;
					UpdateEnd();
				}
			}
		}

		/// <summary>
		/// Gets or sets the milliseconds value. This value cannot be negative. Setting this value
		/// updates the End value.
		/// </summary>
		public int Milliseconds
		{
			get
			{
				return milliseconds;
			}
			set
			{
				if (value < 0) throw new ArgumentOutOfRangeException("Milliseconds", "Interval components cannot be negative.");
				if (value != milliseconds)
				{
					milliseconds = value;
					UpdateEnd();
				}
			}
		}

		/// <summary>
		/// Gets or sets the interval's duration value as specified by ISO 8601. Negative values
		/// are prefixed with a minus (hyphen) character.
		/// </summary>
		public string Duration
		{
			get
			{
				StringBuilder sb = new StringBuilder();
				if (negative)
					sb.Append("-");
				sb.Append("P");
				if (years > 0)
					sb.Append(years + "Y");
				if (months > 0)
					sb.Append(months + "M");
				if (days > 0)
					sb.Append(days + "D");
				if (hours > 0 || minutes > 0 || seconds > 0 || milliseconds > 0)
					sb.Append("T");
				if (hours > 0)
					sb.Append(hours + "H");
				if (minutes > 0)
					sb.Append(minutes + "M");
				if (milliseconds > 0)
					sb.Append(seconds + "." + milliseconds.ToString("000", CultureInfo.InvariantCulture).TrimEnd('0') + "S");
				else if (seconds > 0)
					sb.Append(seconds + "S");
				return sb.ToString();
			}
			set
			{
				Match m = Regex.Match(value, "^(-?)P(?:([0-9]+)Y)?(?:([0-9]+)M)?(?:([0-9]+)D)?(?:T(?:([0-9]+)H)?(?:([0-9]+)M)?(?:([0-9.,]+)S)?)?$");
				if (m.Success)
				{
					negative = m.Groups[1].Value == "-";
					years = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : 0;
					months = m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : 0;
					days = m.Groups[4].Success ? int.Parse(m.Groups[4].Value) : 0;
					hours = m.Groups[5].Success ? int.Parse(m.Groups[5].Value) : 0;
					minutes = m.Groups[6].Success ? int.Parse(m.Groups[6].Value) : 0;
					double seconds = m.Groups[7].Success ? double.Parse(m.Groups[7].Value.Replace(',', '.'), CultureInfo.InvariantCulture) : 0;
					this.seconds = (int)seconds;
					milliseconds = (int)Math.Round((seconds - this.seconds) * 1000);
				}
				else
				{
					throw new FormatException("Invalid or unsupported duration format.");
				}
			}
		}

		/// <summary>
		/// Gets or sets the TimeSpan value between the Start and End values. Setting this value
		/// updates the duration components.
		/// </summary>
		public TimeSpan TimeSpan
		{
			get
			{
				return end - start;
			}
			set
			{
				end = start + value;
				UpdateCountValues();
			}
		}

		#endregion Public properties

		#region Private calculation methods

		/// <summary>
		/// Updates the duration components from the Start and End values.
		/// </summary>
		private void UpdateCountValues()
		{
			DateTime d0, d1;
			if (start <= end)
			{
				negative = false;
				d0 = start;
				d1 = end;
			}
			else
			{
				negative = true;
				d0 = end;
				d1 = start;
			}

			years = 0;
			while (d0.AddYears(years + 1) <= d1)
			{
				years++;
			}
			d0 = d0.AddYears(years);
			months = 0;
			while (d0.AddMonths(months + 1) <= d1)
			{
				months++;
			}
			d0 = d0.AddMonths(months);
			days = 0;
			while (d0.AddDays(days + 1) <= d1)
			{
				days++;
			}
			d0 = d0.AddDays(days);
			hours = 0;
			while (d0.AddHours(hours + 1) <= d1)
			{
				hours++;
			}
			d0 = d0.AddHours(hours);
			minutes = 0;
			while (d0.AddMinutes(minutes + 1) <= d1)
			{
				minutes++;
			}
			d0 = d0.AddMinutes(minutes);
			seconds = 0;
			while (d0.AddSeconds(seconds + 1) <= d1)
			{
				seconds++;
			}
			d0 = d0.AddSeconds(seconds);
			milliseconds = (int)(d1 - d0).TotalMilliseconds;
		}

		/// <summary>
		/// Updates the End value from the duration components.
		/// </summary>
		private void UpdateEnd()
		{
			end = start
				.AddYears(negative ? -years : years)
				.AddMonths(negative ? -months : months)
				.AddDays(negative ? -days : days)
				.AddHours(negative ? -hours : hours)
				.AddMinutes(negative ? -minutes : minutes)
				.AddSeconds(negative ? -seconds : seconds)
				.AddMilliseconds(negative ? -milliseconds : milliseconds);
		}

		#endregion Private calculation methods
	}
}
