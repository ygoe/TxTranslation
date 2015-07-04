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
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;

namespace Unclassified.TxLib
{
	/// <summary>
	/// Provides translation and localisation methods.
	/// </summary>
	public static class Tx
	{
		#region Constants

		/// <summary>
		/// Environment variable name that specifies the log file name. If the environment variable
		/// is set but empty, the log messages are sent to Trace. The environment variable is only
		/// evaluated when the process starts.
		/// </summary>
		private const string LogFileEnvironmentVariableName = "TX_LOG_FILE";

		/// <summary>
		/// Environment variable name that indicates whether the usage of text keys is tracked and
		/// unused text keys from the primary culture are logged before the process ends. To enable
		/// this option, set the environment variable value to "1". This will only work if a log
		/// file is specified (not Trace) and a primary culture has been set. If this option is
		/// enabled, placeholder names provided by the application but unused by the translated
		/// text will also be logged to the current log target. The environment variable is only
		/// evaluated when the process starts.
		/// </summary>
		private const string LogUnusedEnvironmentVariableName = "TX_LOG_UNUSED";

		private const int WriteLockTimeout = 10000;
		private const int ReadLockTimeout = 1000;
		private const int ReloadChangesDelay = 2000;

		/// <summary>
		/// Defines text keys that are used by various format methods of the Tx class.
		/// </summary>
		public static class SystemKeys
		{
			/// <summary>The system text key for a colon.</summary>
			public const string Colon = "Tx:colon";
			/// <summary>The system text key for an opening parenthesis.</summary>
			public const string ParenthesisBegin = "Tx:parenthesis begin";
			/// <summary>The system text key for a closing parenthesis.</summary>
			public const string ParenthesisEnd = "Tx:parenthesis end";
			/// <summary>The system text key for an opening quotation mark.</summary>
			public const string QuoteBegin = "Tx:quote begin";
			/// <summary>The system text key for a closing quotation mark.</summary>
			public const string QuoteEnd = "Tx:quote end";
			/// <summary>The system text key for an opening nested quotation mark.</summary>
			public const string QuoteNestedBegin = "Tx:quote nested begin";
			/// <summary>The system text key for a closing nested quotation mark.</summary>
			public const string QuoteNestedEnd = "Tx:quote nested end";
			/// <summary>The system text key for the unit name of a byte.</summary>
			public const string ByteUnit = "Tx:byte unit";
			
			/// <summary>The system text key for a negative number indicator.</summary>
			public const string NumberNegative = "Tx:number.negative";
			/// <summary>The system text key for a number decimal separator.</summary>
			public const string NumberDecimalSeparator = "Tx:number.decimal separator";
			/// <summary>The system text key for a number thousands group separator.</summary>
			public const string NumberGroupSeparator = "Tx:number.group separator";
			/// <summary>The system text key for the threshold value from which on to use the thousands group separator.</summary>
			public const string NumberGroupSeparatorThreshold = "Tx:number.group separator threshold";
			/// <summary>The system text key for a separator between a number and a unit name.</summary>
			public const string NumberUnitSeparator = "Tx:number.unit separator";
			/// <summary>The system text key for an ordinal number suffix.</summary>
			public const string NumberOrdinal = "Tx:number.ordinal";
			/// <summary>The system text key for a female ordinal number suffix.</summary>
			public const string NumberOrdinalFeminin = "Tx:number.ordinal f";
			
			/// <summary>The system text key for the date format containing a year only.</summary>
			public const string DateYear = "Tx:date.year";
			/// <summary>The system text key for the date format containing a year and month.</summary>
			public const string DateYearMonth = "Tx:date.year month";
			/// <summary>The system text key for the date format containing a year and month in tabular form (fixed-length).</summary>
			public const string DateYearMonthTab = "Tx:date.year month.tab";
			/// <summary>The system text key for the date format containing a year and abbreviated month.</summary>
			public const string DateYearMonthAbbr = "Tx:date.year month.abbr";
			/// <summary>The system text key for the date format containing a year and long month.</summary>
			public const string DateYearMonthLong = "Tx:date.year month.long";
			/// <summary>The system text key for the date format containing a year, month and day.</summary>
			public const string DateYearMonthDay = "Tx:date.year month day";
			/// <summary>The system text key for the date format containing a year, month and day in tabular form (fixed-length).</summary>
			public const string DateYearMonthDayTab = "Tx:date.year month day.tab";
			/// <summary>The system text key for the date format containing a year, abbreviated month and day.</summary>
			public const string DateYearMonthDayAbbr = "Tx:date.year month day.abbr";
			/// <summary>The system text key for the date format containing a year, long month and day.</summary>
			public const string DateYearMonthDayLong = "Tx:date.year month day.long";
			/// <summary>The system text key for the date format containing a month only.</summary>
			public const string DateMonth = "Tx:date.month";
			/// <summary>The system text key for the date format containing a month only in tabular form (fixed-length).</summary>
			public const string DateMonthTab = "Tx:date.month.tab";
			/// <summary>The system text key for the date format containing an abbreviated month only.</summary>
			public const string DateMonthAbbr = "Tx:date.month.abbr";
			/// <summary>The system text key for the date format containing a long month only.</summary>
			public const string DateMonthLong = "Tx:date.month.long";
			/// <summary>The system text key for the date format containing a month and day.</summary>
			public const string DateMonthDay = "Tx:date.month day";
			/// <summary>The system text key for the date format containing a month and day in tabular form (fixed-length).</summary>
			public const string DateMonthDayTab = "Tx:date.month day.tab";
			/// <summary>The system text key for the date format containing an abbreviated month and day.</summary>
			public const string DateMonthDayAbbr = "Tx:date.month day.abbr";
			/// <summary>The system text key for the date format containing a long month and day.</summary>
			public const string DateMonthDayLong = "Tx:date.month day.long";
			/// <summary>The system text key for the date format containing a day only.</summary>
			public const string DateDay = "Tx:date.day";
			/// <summary>The system text key for the date format containing a day only in tabular form (fixed-length).</summary>
			public const string DateDayTab = "Tx:date.day.tab";
			/// <summary>The system text key for the date format containing a year and quarter.</summary>
			public const string DateYearQuarter = "Tx:date.year quarter";
			/// <summary>The system text key for the date format containing a quarter only.</summary>
			public const string DateQuarter = "Tx:date.quarter";
			/// <summary>The system text key for the date format containing a day of week with date.</summary>
			public const string DateDowWithDate = "Tx:date.dow with date";

			/// <summary>The system text key for the time format containing an hour, minute, second and millisecond.</summary>
			public const string TimeHourMinuteSecondMs = "Tx:time.hour minute second ms";
			/// <summary>The system text key for the time format containing an hour, minute, second and millisecond in tabular form (fixed-length).</summary>
			public const string TimeHourMinuteSecondMsTab = "Tx:time.hour minute second ms.tab";
			/// <summary>The system text key for the time format containing an hour, minute and second.</summary>
			public const string TimeHourMinuteSecond = "Tx:time.hour minute second";
			/// <summary>The system text key for the time format containing an hour, minute and second in tabular form (fixed-length).</summary>
			public const string TimeHourMinuteSecondTab = "Tx:time.hour minute second.tab";
			/// <summary>The system text key for the time format containing an hour and minute.</summary>
			public const string TimeHourMinute = "Tx:time.hour minute";
			/// <summary>The system text key for the time format containing an hour and minute in tabular form (fixed-length).</summary>
			public const string TimeHourMinuteTab = "Tx:time.hour minute.tab";
			/// <summary>The system text key for the time format containing an hour only.</summary>
			public const string TimeHour = "Tx:time.hour";
			/// <summary>The system text key for the time format containing an hour only in tabular form (fixed-length).</summary>
			public const string TimeHourTab = "Tx:time.hour.tab";
			/// <summary>The system text key for the time AM indicator.</summary>
			public const string TimeAM = "Tx:time.am";
			/// <summary>The system text key for the time PM indicator.</summary>
			public const string TimePM = "Tx:time.pm";

			/// <summary>The system text key for the separator between two levels of a relative time.</summary>
			public const string TimeRelativeSeparator = "Tx:time.relative separator";
			/// <summary>The system text key for the current time.</summary>
			public const string TimeNow = "Tx:time.now";
			/// <summary>The system text key for the unset time.</summary>
			public const string TimeNever = "Tx:time.never";
			
			/// <summary>The system text key for a relative point in time in the future. Uses the {interval} placeholder.</summary>
			public const string TimeRelative = "Tx:time.relative";
			/// <summary>The system text key for years of a relative point in time in the future. Uses the {#} count placeholder.</summary>
			public const string TimeRelativeYears = "Tx:time.relative.years";
			/// <summary>The system text key for months of a relative point in time in the future. Uses the {#} count placeholder.</summary>
			public const string TimeRelativeMonths = "Tx:time.relative.months";
			/// <summary>The system text key for days of a relative point in time in the future. Uses the {#} count placeholder.</summary>
			public const string TimeRelativeDays = "Tx:time.relative.days";
			/// <summary>The system text key for hours of a relative point in time in the future. Uses the {#} count placeholder.</summary>
			public const string TimeRelativeHours = "Tx:time.relative.hours";
			/// <summary>The system text key for minutes of a relative point in time in the future. Uses the {#} count placeholder.</summary>
			public const string TimeRelativeMinutes = "Tx:time.relative.minutes";
			/// <summary>The system text key for seconds of a relative point in time in the future. Uses the {#} count placeholder.</summary>
			public const string TimeRelativeSeconds = "Tx:time.relative.seconds";
			
			/// <summary>The system text key for a relative point in time in the past. Uses the {interval} placeholder.</summary>
			public const string TimeRelativeNeg = "Tx:time.relative neg";
			/// <summary>The system text key for years of a relative point in time in the past. Uses the {#} count placeholder.</summary>
			public const string TimeRelativeNegYears = "Tx:time.relative neg.years";
			/// <summary>The system text key for months of a relative point in time in the past. Uses the {#} count placeholder.</summary>
			public const string TimeRelativeNegMonths = "Tx:time.relative neg.months";
			/// <summary>The system text key for days of a relative point in time in the past. Uses the {#} count placeholder.</summary>
			public const string TimeRelativeNegDays = "Tx:time.relative neg.days";
			/// <summary>The system text key for hours of a relative point in time in the past. Uses the {#} count placeholder.</summary>
			public const string TimeRelativeNegHours = "Tx:time.relative neg.hours";
			/// <summary>The system text key for minutes of a relative point in time in the past. Uses the {#} count placeholder.</summary>
			public const string TimeRelativeNegMinutes = "Tx:time.relative neg.minutes";
			/// <summary>The system text key for seconds of a relative point in time in the past. Uses the {#} count placeholder.</summary>
			public const string TimeRelativeNegSeconds = "Tx:time.relative neg.seconds";
			
			/// <summary>The system text key for a relative time span into the future. Uses the {interval} placeholder.</summary>
			public const string TimeSpanRelative = "Tx:time.relative span";
			/// <summary>The system text key for years of a relative time span into the future. Uses the {#} count placeholder.</summary>
			public const string TimeSpanRelativeYears = "Tx:time.relative span.years";
			/// <summary>The system text key for months of a relative time span into the future. Uses the {#} count placeholder.</summary>
			public const string TimeSpanRelativeMonths = "Tx:time.relative span.months";
			/// <summary>The system text key for days of a relative time span into the future. Uses the {#} count placeholder.</summary>
			public const string TimeSpanRelativeDays = "Tx:time.relative span.days";
			/// <summary>The system text key for hours of a relative time span into the future. Uses the {#} count placeholder.</summary>
			public const string TimeSpanRelativeHours = "Tx:time.relative span.hours";
			/// <summary>The system text key for minutes of a relative time span into the future. Uses the {#} count placeholder.</summary>
			public const string TimeSpanRelativeMinutes = "Tx:time.relative span.minutes";
			/// <summary>The system text key for seconds of a relative time span into the future. Uses the {#} count placeholder.</summary>
			public const string TimeSpanRelativeSeconds = "Tx:time.relative span.seconds";

			/// <summary>The system text key for a relative time span into the past. Uses the {interval} placeholder.</summary>
			public const string TimeSpanRelativeNeg = "Tx:time.relative span neg";
			/// <summary>The system text key for years of a relative time span into the past. Uses the {#} count placeholder.</summary>
			public const string TimeSpanRelativeNegYears = "Tx:time.relative span neg.years";
			/// <summary>The system text key for months of a relative time span into the past. Uses the {#} count placeholder.</summary>
			public const string TimeSpanRelativeNegMonths = "Tx:time.relative span neg.months";
			/// <summary>The system text key for days of a relative time span into the past. Uses the {#} count placeholder.</summary>
			public const string TimeSpanRelativeNegDays = "Tx:time.relative span neg.days";
			/// <summary>The system text key for hours of a relative time span into the past. Uses the {#} count placeholder.</summary>
			public const string TimeSpanRelativeNegHours = "Tx:time.relative span neg.hours";
			/// <summary>The system text key for minutes of a relative time span into the past. Uses the {#} count placeholder.</summary>
			public const string TimeSpanRelativeNegMinutes = "Tx:time.relative span neg.minutes";
			/// <summary>The system text key for seconds of a relative time span into the past. Uses the {#} count placeholder.</summary>
			public const string TimeSpanRelativeNegSeconds = "Tx:time.relative span neg.seconds";
			
			/// <summary>The system text key for a relative time span going into the future. Uses the {interval} placeholder.</summary>
			public const string TimeSpan = "Tx:time.span";
			/// <summary>The system text key for years of a relative time span going into the future. Uses the {#} count placeholder.</summary>
			public const string TimeSpanYears = "Tx:time.span.years";
			/// <summary>The system text key for months of a relative time span going into the future. Uses the {#} count placeholder.</summary>
			public const string TimeSpanMonths = "Tx:time.span.months";
			/// <summary>The system text key for days of a relative time span going into the future. Uses the {#} count placeholder.</summary>
			public const string TimeSpanDays = "Tx:time.span.days";
			/// <summary>The system text key for hours of a relative time span going into the future. Uses the {#} count placeholder.</summary>
			public const string TimeSpanHours = "Tx:time.span.hours";
			/// <summary>The system text key for minutes of a relative time span going into the future. Uses the {#} count placeholder.</summary>
			public const string TimeSpanMinutes = "Tx:time.span.minutes";
			/// <summary>The system text key for seconds of a relative time span going into the future. Uses the {#} count placeholder.</summary>
			public const string TimeSpanSeconds = "Tx:time.span.seconds";

			/// <summary>The system text key for a relative time span going into the past. Uses the {interval} placeholder.</summary>
			public const string TimeSpanNeg = "Tx:time.span neg";
			/// <summary>The system text key for years of a relative time span going into the past. Uses the {#} count placeholder.</summary>
			public const string TimeSpanNegYears = "Tx:time.span neg.years";
			/// <summary>The system text key for months of a relative time span going into the past. Uses the {#} count placeholder.</summary>
			public const string TimeSpanNegMonths = "Tx:time.span neg.months";
			/// <summary>The system text key for days of a relative time span going into the past. Uses the {#} count placeholder.</summary>
			public const string TimeSpanNegDays = "Tx:time.span neg.days";
			/// <summary>The system text key for hours of a relative time span going into the past. Uses the {#} count placeholder.</summary>
			public const string TimeSpanNegHours = "Tx:time.span neg.hours";
			/// <summary>The system text key for minutes of a relative time span going into the past. Uses the {#} count placeholder.</summary>
			public const string TimeSpanNegMinutes = "Tx:time.span neg.minutes";
			/// <summary>The system text key for seconds of a relative time span going into the past. Uses the {#} count placeholder.</summary>
			public const string TimeSpanNegSeconds = "Tx:time.span neg.seconds";
			
			/// <summary>The system text key for combining items in a conjunctive (AND) enumeration.</summary>
			public const string EnumAndCombiner = "Tx:enum.and.combiner";
			/// <summary>The system text key for combining the last item in a conjunctive (AND) enumeration.</summary>
			public const string EnumAndLastCombiner = "Tx:enum.and.last combiner";
			/// <summary>The system text key for combining items in a disjunctive (OR) enumeration.</summary>
			public const string EnumOrCombiner = "Tx:enum.or.combiner";
			/// <summary>The system text key for combining the last item in a disjunctive (OR) enumeration.</summary>
			public const string EnumOrLastCombiner = "Tx:enum.or.last combiner";
		}

		#endregion Constants

		#region Global data

		/// <summary>
		/// Reader/writer lock object that controls access to the global dictionary and the
		/// fileWatchers list.
		/// </summary>
		/// <remarks>
		/// A note on locking access to the dictionary: There is two places where a read lock must
		/// be acquired. One is the deepest level of helper functions. This is basically only
		/// GetText which tries several cultures to find a text. This lock at the deepest level of
		/// the call hierarchy ensures that every read access of the dictionary is secured. The
		/// other place is every method that has multiple calls to functions that will eventually
		/// access the dictionary. This is to ensure that every retrieved text belongs to the same
		/// generation of texts and that the dictionary was not changed for example between the
		/// opening and closing quotation mark. Other methods that only call GetText, ResolveData
		/// or the like a single time need not be locked.
		/// </remarks>
		private static ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim();

		/// <summary>
		/// Contains all loaded languages associated with their culture name.
		/// The first level defines the culture name.
		/// The second level defines the text key.
		/// The third level defines the quantifier.
		/// The fourth level contains the actual text value.
		/// </summary>
		/// <remarks>
		/// Access to this object is synchronised through rwlock.
		/// </remarks>
		private static Dictionary<string, Dictionary<string, Dictionary<int, string>>> languages =
			new Dictionary<string, Dictionary<string, Dictionary<int, string>>>();

		/// <summary>
		/// Dictionary backup. Used to store the original data while the system texts are
		/// temporarily replaced for TxEditor purposed for date and time preview.
		/// </summary>
		private static Dictionary<string, Dictionary<string, Dictionary<int, string>>> languagesBackup;

		/// <remarks>
		/// Access to this object is synchronised through rwlock.
		/// </remarks>
		private static bool useFileSystemWatcher;

		/// <remarks>
		/// Access to this object is synchronised through rwlock.
		/// </remarks>
		private static string primaryCulture;

		/// <summary>
		/// A list of all culture names accepted by the browser, sorted by descending preference.
		/// This must be updated by the calling ASP.NET application for every page request from the
		/// HTTP_ACCEPT_LANGUAGE header value through the SetWebCulture method.
		/// </summary>
		/// <remarks>
		/// No locking is required for this field because every thread has its own instance.
		/// </remarks>
		[ThreadStatic]
		private static string[] httpPreferredCultures;

		/// <summary>
		/// Contains a FileSystemWatcher instance monitoring each loaded file for changes.
		/// </summary>
		/// <remarks>
		/// Access to this object is synchronised through rwlock.
		/// </remarks>
		private static Dictionary<string, FileSystemWatcher> fileWatchers =
			new Dictionary<string, FileSystemWatcher>();

		private static object reloadTimerLock = new object();

		/// <remarks>
		/// Access to this object is synchronised through reloadTimerLock.
		/// </remarks>
		private static Timer reloadTimer;

		/// <summary>
		/// Contains the last write time of every loaded file to compare it for updates.
		/// </summary>
		/// <remarks>
		/// Access to this object is synchronised through rwlock.
		/// </remarks>
		private static Dictionary<string, DateTime> fileTimes =
			new Dictionary<string, DateTime>();

		private static object logLock = new object();

		/// <summary>
		/// File name to write log messages to. If it is empty (but not null), Trace is the log target.
		/// </summary>
		/// <remarks>
		/// Access to this object is synchronised through logLock.
		/// </remarks>
		private static string logFileName;

		/// <remarks>
		/// Access to this object is synchronised through logLock.
		/// </remarks>
		private static StreamWriter logWriter;

		/// <summary>
		/// Contains all text keys that have been requested. Used to determine unused text keys.
		/// </summary>
		/// <remarks>
		/// Access to this object is synchronised through logLock.
		/// </remarks>
		private static HashSet<string> usedKeys;

		#endregion Global data

		#region Events

		/// <summary>
		/// Fired when the dictionary has changed.
		/// </summary>
		public static event EventHandler DictionaryChanged;

		private static void RaiseDictionaryChanged()
		{
			EventHandler handler = DictionaryChanged;
			if (handler != null)
			{
				// Sanity check to prevent later failures
				if (rwlock.IsWriteLockHeld)
					throw new InvalidOperationException("Internal error: DictionaryChanged event raised with writer lock still set.");

				handler(null, EventArgs.Empty);
			}
		}

		#endregion Events

		#region Static constructor

		/// <summary>
		/// Initialises the Tx class.
		/// </summary>
		static Tx()
		{
			LogFileName =
				Environment.GetEnvironmentVariable(LogFileEnvironmentVariableName) ??
				Environment.GetEnvironmentVariable(LogFileEnvironmentVariableName, EnvironmentVariableTarget.User);

			if (Environment.GetEnvironmentVariable(LogUnusedEnvironmentVariableName) == "1" ||
				Environment.GetEnvironmentVariable(LogUnusedEnvironmentVariableName, EnvironmentVariableTarget.User) == "1")
			{
				usedKeys = new HashSet<string>();
			}
		}

		#endregion Static constructor

		#region Properties

		/// <summary>
		/// Gets or sets a value indicating whether the loaded files should be monitored for
		/// changes using FileSystemWatcher instances and reloaded automatically. Only files loaded
		/// after setting this property can be monitored.
		/// </summary>
		public static bool UseFileSystemWatcher
		{
			get
			{
				using (new ReadLock(rwlock))
				{
					return useFileSystemWatcher;
				}
			}
			set
			{
				using (new UpgradeableReadLock(rwlock))
				{
					if (value != useFileSystemWatcher)
					{
						using (new WriteLock(rwlock))
						{
							useFileSystemWatcher = value;
							if (!useFileSystemWatcher)
							{
								// Dispose all created FileSystemWatcher instances for the previously loaded files
								foreach (FileSystemWatcher fsw in fileWatchers.Values)
								{
									fsw.Dispose();
								}
								fileWatchers.Clear();

								lock (reloadTimerLock)
								{
									if (reloadTimer != null)
									{
										// Cancel running timer (pending callbacks may still be invoked)
										reloadTimer.Dispose();
										reloadTimer = null;
									}
								}
							}
						}
					}
				}
			}
		}

		private static TxMode globalMode;

		[ThreadStatic]
		private static TxMode threadMode;

		/// <summary>
		/// Gets or sets the operation mode for the current application domain.
		/// </summary>
		public static TxMode GlobalMode
		{
			get { return globalMode; }
			set { globalMode = value; }
		}

		/// <summary>
		/// Gets or sets the operation mode for the current thread.
		/// </summary>
		public static TxMode ThreadMode
		{
			get { return threadMode; }
			set { threadMode = value; }
		}

		#endregion Properties

		#region Load methods

		/// <summary>
		/// Loads all XML files in a directory into the global dictionary.
		/// </summary>
		/// <param name="path">Directory to load files from.</param>
		/// <param name="filePrefix">File name prefix to limit loading to.</param>
		/// <remarks>
		/// <para>
		/// This method searches the directory for files that end with the extension .txd (for Tx
		/// dictionary) and an optional culture name directly before the extension. If filePrefix
		/// is set, the matching file names additionally need to begin with this prefix and may not
		/// contain additional characters between the prefix and the culture and extension. For
		/// example, the files "demo.en-us.txd" and "demo.txd" match the prefix "demo" as well as
		/// no prefix at all, but no other prefix.
		/// </para>
		/// <para>
		/// Since the filePrefix value is directly used in a regular expression, it should not
		/// contain characters that are special to a regular expression pattern. Safe characters
		/// include letters, digits, underline (_) and hyphen (-). You may use regular expression
		/// syntax to specify wildcards, for example ".*".
		/// </para>
		/// </remarks>
		public static void LoadDirectory(string path, string filePrefix = null)
		{
			string regex = @"(\.(([a-z]{2})([-][a-z]{2})?))?\.(?:txd|xml)$";
			if (!string.IsNullOrEmpty(filePrefix))
			{
				regex = "^" + filePrefix + regex;
			}
			using (new WriteLock(rwlock))
			{
				foreach (string fileName in Directory.GetFiles(path))
				{
					Match m = Regex.Match(Path.GetFileName(fileName), regex, RegexOptions.IgnoreCase);
					if (m.Success)
					{
						LoadFromXmlFile(fileName);
					}
				}
			}
		}

		/// <summary>
		/// Loads all text definitions from an XML file into the global dictionary.
		/// </summary>
		/// <param name="fileName">Name of the XML file to load.</param>
		/// <remarks>
		/// <para>
		/// If the file name ends with ".[culture name].txd", then the culture name of the file
		/// name is used. Otherwise a combined file is assumed that contains a "culture" element
		/// with a "name" attribute for each defined culture.
		/// </para>
		/// <para>
		/// Culture names follow the format that the CultureInfo class supports and are actually
		/// validated that way. Examples are "de", "en", "de-de", "en-us" or "pt-br". Culture
		/// names are handled case-insensitively for this method.
		/// </para>
		/// </remarks>
		public static void LoadFromXmlFile(string fileName)
		{
			using (new WriteLock(rwlock))
			{
				LoadFromXmlFile(fileName, languages);
			}
		}

		/// <summary>
		/// Loads all text definitions from an XML file into a dictionary.
		/// </summary>
		/// <param name="fileName">Name of the XML file to load.</param>
		/// <param name="dict">Dictionary to load the texts into.</param>
		private static void LoadFromXmlFile(string fileName, Dictionary<string, Dictionary<string, Dictionary<int, string>>> dict)
		{
			if (!Path.IsPathRooted(fileName))
			{
				fileName = Path.GetFullPath(fileName);
			}

			using (new WriteLock(rwlock))
			{
				if (UseFileSystemWatcher)
				{
					// Monitor this file to automatically reload it when it is changed
					if (!fileWatchers.ContainsKey(fileName))
					{
						FileSystemWatcher fsw = new FileSystemWatcher(Path.GetDirectoryName(fileName), Path.GetFileName(fileName));
						fsw.InternalBufferSize = 4096;   // Minimum possible value
						fsw.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Security | NotifyFilters.Size | NotifyFilters.FileName;
						fsw.Changed += fsw_Changed;
						fsw.Created += fsw_Changed;
						fsw.Renamed += fsw_Changed;
						fsw.EnableRaisingEvents = true;
						fileWatchers[fileName] = fsw;
					}
				}

				// Remember last write time of the file to be able to compare it later
				fileTimes[fileName] = new FileInfo(fileName).LastWriteTimeUtc;
			}

			// First load the XML file into an XmlDocument for further processing
			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.Load(fileName);

			// Try to recognise the culture name from the file name
			Match m = Regex.Match(fileName, @"\.(([a-z]{2})([-][a-z]{2})?)\.(?:txd|xml)$", RegexOptions.IgnoreCase);
			if (m.Success)
			{
				CultureInfo ci = CultureInfo.GetCultureInfo(m.Groups[1].Value);
				LoadFromXml(ci.Name, xmlDoc.DocumentElement, dict);

				// Set the primary culture if a file claims to be it
				XmlAttribute primaryAttr = xmlDoc.DocumentElement.Attributes["primary"];
				if (primaryAttr != null && primaryAttr.Value == "true")
				{
					PrimaryCulture = ci.Name;
				}
				return;
			}

			// Try to find the culture name inside a combined XML document
			foreach (XmlElement xe in xmlDoc.DocumentElement.SelectNodes("culture[@name]"))
			{
				CultureInfo ci = CultureInfo.GetCultureInfo(xe.Attributes["name"].Value);
				LoadFromXml(ci.Name, xe, dict);

				// Set the primary culture if a culture in the file claims to be it
				XmlAttribute primaryAttr = xe.Attributes["primary"];
				if (primaryAttr != null && primaryAttr.Value == "true")
				{
					PrimaryCulture = ci.Name;
				}
			}
		}

		private static void fsw_Changed(object sender, FileSystemEventArgs e)
		{
			// A Renamed event is called twice when saving the file with TxEditor. The first
			// renaming is from .txd to .txd.bak when creating the original backup file. This does
			// not change the loaded dictionary file actually. The second renaming is from .txd.tmp
			// to .txd when safe-writing the new dictionary file. This happens directly after the
			// first event, before the reload timer has elapsed, so it doesn't hurt to handle both
			// events.

			lock (reloadTimerLock)
			{
				if (reloadTimer != null)
				{
					// Cancel running timer (pending callbacks may still be invoked)
					reloadTimer.Dispose();
				}
				reloadTimer = new Timer(reloadTimer_Callback, null, ReloadChangesDelay, Timeout.Infinite);
			}
		}

		private static void reloadTimer_Callback(object state)
		{
			lock (reloadTimerLock)
			{
				if (reloadTimer == null) return;   // Timer has been cancelled, do nothing

				reloadTimer.Dispose();
				reloadTimer = null;
			}

			try
			{
				// Read all loaded files into a temporary dictionary
				Dictionary<string, Dictionary<string, Dictionary<int, string>>> newLanguages =
					new Dictionary<string, Dictionary<string, Dictionary<int, string>>>();
				foreach (string fileName in new List<string>(fileTimes.Keys))
				{
					LoadFromXmlFile(fileName, newLanguages);
				}

				// Replace the global dictionary with the new one to apply the new texts
				using (new WriteLock(rwlock))
				{
					languages = newLanguages;
				}

				Log("{0} files reloaded.", fileTimes.Count);
				RaiseDictionaryChanged();
			}
			catch (Exception ex)
			{
				// Catch and log any exceptions, Visual Studio will instantly terminate debugging
				// if something unexcepted happens in this thread.
				// TODO: Test behaviour with a global unhandled exception handler in place.
				System.Diagnostics.Trace.WriteLine("Unhandled " + ex.GetType().Name + " while reloading the dictionary files: " + ex.Message);
			}
		}

		/// <summary>
		/// Loads all text definitions from an embedded resource XML file into the global
		/// dictionary. Only the combined format with all cultures in one document is supported by
		/// this method.
		/// </summary>
		/// <param name="name">Name of the embedded resource file to load.</param>
		/// <remarks>
		/// The resource name is the project's default namespace and the file path relative to the
		/// project, combined with dots (.) and all path separators also replaced with dots.
		/// </remarks>
		public static void LoadFromEmbeddedResource(string name)
		{
			// First load the XML file into an XmlDocument for further processing
			Stream stream = System.Reflection.Assembly.GetCallingAssembly().GetManifestResourceStream(name);
			if (stream == null)
			{
				throw new ArgumentException("The embedded resource name was not found in the calling assembly.");
			}
			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.Load(stream);

			// Try to find the culture name inside a combined XML document
			using (new WriteLock(rwlock))
			{
				foreach (XmlElement xe in xmlDoc.DocumentElement.SelectNodes("culture[@name]"))
				{
					CultureInfo ci = CultureInfo.GetCultureInfo(xe.Attributes["name"].Value);
					LoadFromXml(ci.Name, xe, languages);

					// Set the primary culture if a culture in the file claims to be it
					XmlAttribute primaryAttr = xe.Attributes["primary"];
					if (primaryAttr != null && primaryAttr.Value == "true")
					{
						PrimaryCulture = ci.Name;
					}
				}
			}
			// Need to raise the event after releasing the write lock
			RaiseDictionaryChanged();
		}

		/// <summary>
		/// Loads all text definitions from an XML element into a dictionary.
		/// </summary>
		/// <param name="culture">Culture name to add the text definitions to. Must be the exact name of an existing culture in .NET.</param>
		/// <param name="xe">XML element to read the text definitions from.</param>
		/// <param name="dict">Dictionary to add texts to.</param>
		private static void LoadFromXml(string culture, XmlElement xe, Dictionary<string, Dictionary<string, Dictionary<int, string>>> dict)
		{
			// We only need the lock if we're writing directly to the global dictionary
			bool isGlobalDict;
			using (new ReadLock(rwlock))
			{
				isGlobalDict = dict == languages;
			}
			using (new WriteLock(isGlobalDict ? rwlock : null))
			{
				// Get a reference to the specified language dictionary
				Dictionary<string, Dictionary<int, string>> language;
				if (!dict.TryGetValue(culture, out language))
				{
					language = new Dictionary<string, Dictionary<int, string>>();
					dict.Add(culture, language);
				}

				// Read the XML document
				foreach (XmlNode textNode in xe.SelectNodes("text[@key]"))
				{
					string text = textNode.InnerText;
					string key = textNode.Attributes["key"].Value;

					if (key == "")
					{
						Log("Load XML: Key attribute is empty. Ignoring definition.");
						continue;
					}

					int count = -1;
					XmlAttribute countAttr = textNode.Attributes["count"];
					if (countAttr != null)
					{
						if (!int.TryParse(countAttr.Value, out count))
						{
							// Count value unparsable. Skip invalid entries
							Log("Load XML: Count attribute value of key {0} is not an integer. Ignoring definition.", key);
							continue;
						}
						if (count < 0 || count > ushort.MaxValue)
						{
							// Count value out of range. Skip invalid entries
							Log("Load XML: Count attribute value of key {0} is out of range. Ignoring definition.", key);
							continue;
						}
					}

					int modulo = 0;
					XmlAttribute moduloAttr = textNode.Attributes["mod"];
					if (moduloAttr != null)
					{
						if (!int.TryParse(moduloAttr.Value, out modulo))
						{
							// Modulo value unparsable. Skip invalid entries
							Log("Load XML: Modulo attribute of key {0} is not an integer. Ignoring definition.", key);
							continue;
						}
						if (modulo < 2 || modulo > 1000)
						{
							// Modulo value out of range. Skip invalid entries
							Log("Load XML: Modulo attribute of key {0} is out of range. Ignoring definition.", key);
							continue;
						}
					}

					Dictionary<int, string> textItem;
					if (language.TryGetValue(key, out textItem))
					{
						// Key has already been read, add the new text item.
						// Existing text items are overwritten.
						if (count != -1 && modulo != 0)
						{
							// Encode the modulo value into the quantifier.
							count = (modulo << 16) | count;
						}
						textItem[count] = text;
					}
					else
					{
						// New key.
						textItem = new Dictionary<int, string>();
						textItem.Add(count, text);
						language.Add(key, textItem);
					}
				}
			}

			if (isGlobalDict && !rwlock.IsWriteLockHeld)
			{
				// Raise the changed event if the texts have been loaded into the global
				// dictionary, but not if a write lock is held because then, nothing could be read
				// from the dictionary by others. In a situation where this method is called with
				// a write lock, the caller must raise the changed event after releasing the lock.
				RaiseDictionaryChanged();
			}
		}

		/// <summary>
		/// Checks the last write time of all loaded files and reloads all files if something has
		/// changed. This method should be called to reload the text files if necessary and only if
		/// UseFileSystemWatcher is false. In an ASP.NET environment, this could be a new page
		/// loading.
		/// </summary>
		public static void CheckReloadFiles()
		{
			using (new UpgradeableReadLock(rwlock))
			{
				bool changed = false;

				if (languages.Count == 0)
				{
					// Sometimes this seems to happen in an ASP.NET environment.
					changed = true;
				}
				else
				{
					try
					{
						foreach (KeyValuePair<string, DateTime> kvp in fileTimes)
						{
							if (new FileInfo(kvp.Key).LastWriteTimeUtc > kvp.Value)
							{
								changed = true;
								break;
							}
						}
					}
					catch
					{
						changed = true;
					}
				}

				if (changed)
				{
					// Read all known files into a temporary dictionary
					Dictionary<string, Dictionary<string, Dictionary<int, string>>> newLanguages =
						new Dictionary<string, Dictionary<string, Dictionary<int, string>>>();
					foreach (string fileName in new List<string>(fileTimes.Keys))
					{
						LoadFromXmlFile(fileName, newLanguages);
					}

					// Replace the global dictionary with the new one to apply the new texts
					using (new WriteLock(rwlock))
					{
						languages = newLanguages;
					}

					RaiseDictionaryChanged();
				}
			}
		}

		/// <summary>
		/// Adds a text to the dictionary.
		/// </summary>
		/// <param name="culture">New or existing culture name to add the text definition to. Must be the exact name of an existing culture in .NET.</param>
		/// <param name="key">Text key to add or update.</param>
		/// <param name="text">Text value to add.</param>
		public static void AddText(string culture, string key, string text)
		{
			AddText(culture, key, -1, 0, text);
		}

		/// <summary>
		/// Adds a text to the dictionary.
		/// </summary>
		/// <param name="culture">New or existing culture name to add the text definition to. Must be the exact name of an existing culture in .NET.</param>
		/// <param name="key">Text key to add or update.</param>
		/// <param name="count">Count value for the text.</param>
		/// <param name="text">Text value to add.</param>
		public static void AddText(string culture, string key, int count, string text)
		{
			AddText(culture, key, count, 0, text);
		}

		/// <summary>
		/// Adds a text to the dictionary.
		/// </summary>
		/// <param name="culture">New or existing culture name to add the text definition to. Must be the exact name of an existing culture in .NET.</param>
		/// <param name="key">Text key to add or update.</param>
		/// <param name="count">Count value for the text.</param>
		/// <param name="modulo">Modulo value for the text.</param>
		/// <param name="text">Text value to add.</param>
		public static void AddText(string culture, string key, int count, int modulo, string text)
		{
			if (count < -1 || count > ushort.MaxValue)
			{
				throw new ArgumentOutOfRangeException("count", "The count value must be in the range of 0 to ushort.MaxValue.");
			}
			if (modulo != 0 && (modulo < 2 || modulo > 1000))
			{
				throw new ArgumentOutOfRangeException("modulo", "The modulo value must be in the range of 2 to 1000.");
			}
			if (count == -1 && modulo > 0)
			{
				throw new ArgumentException("A modulo value cannot be used if no count value is set.");
			}

			using (new WriteLock(rwlock))
			{
				// Get a reference to the specified language dictionary
				Dictionary<string, Dictionary<int, string>> language;
				if (!languages.TryGetValue(culture, out language))
				{
					language = new Dictionary<string, Dictionary<int, string>>();
					languages.Add(culture, language);
				}

				Dictionary<int, string> textItem;
				if (language.TryGetValue(key, out textItem))
				{
					// Key is already defined, add the new text item.
					// Existing text items are overwritten.
					if (count != -1 && modulo != 0)
					{
						// Encode the modulo value into the quantifier.
						count = (modulo << 16) | count;
					}
					textItem[count] = text;
				}
				else
				{
					// New key.
					textItem = new Dictionary<int, string>();
					textItem.Add(count, text);
					language.Add(key, textItem);
				}
			}

			RaiseDictionaryChanged();
		}

		/// <summary>
		/// Clears the global dictionary and removes all currently loaded languages and texts.
		/// </summary>
		public static void Clear()
		{
			using (new WriteLock(rwlock))
			{
				// Dispose all created FileSystemWatcher instances for the previously loaded files
				foreach (FileSystemWatcher fsw in fileWatchers.Values)
				{
					fsw.Dispose();
				}
				fileWatchers.Clear();
				fileTimes.Clear();
				languages.Clear();
			}
			RaiseDictionaryChanged();
		}

		/// <summary>
		/// Replaces the system keys (Tx:*) by the provided texts. This is only intended to be used
		/// by the date and time preview in TxEditor.
		/// </summary>
		/// <param name="systemTexts"></param>
		public static void ReplaceSystemTexts(Dictionary<string, Dictionary<string, Dictionary<int, string>>> systemTexts)
		{
			// Keep a backup of the original data to restore it later
			if (languagesBackup == null)
			{
				languagesBackup = languages;
			}
			// And be sure to work on a copy of the dictionary before modifying it
			languages = new Dictionary<string, Dictionary<string, Dictionary<int, string>>>(languagesBackup);

			// Remove all Tx:* keys
			foreach (var language in languages)
			{
				foreach (var key in language.Value.Keys.ToArray())
				{
					if (key.StartsWith("Tx:"))
					{
						language.Value.Remove(key);
					}
				}
			}

			// Insert the new texts
			foreach (var languageCode in systemTexts.Keys)
			{
				Dictionary<string, Dictionary<int, string>> language;
				if (!languages.TryGetValue(languageCode, out language))
				{
					language = new Dictionary<string, Dictionary<int, string>>();
					languages[languageCode] = language;
				}

				var languageSystemTexts = systemTexts[languageCode];
				foreach (var kvp in languageSystemTexts)
				{
					if (kvp.Key.StartsWith("Tx:"))
					{
						language[kvp.Key] = kvp.Value;
					}
				}
			}
		}

		/// <summary>
		/// Restores the system keys (Tx:*) after they were replaced by the
		/// <see cref="ReplaceSystemTexts"/> method. This is only intended to be used by the date
		/// and time preview in TxEditor.
		/// </summary>
		public static void RestoreSystemTexts()
		{
			if (languagesBackup != null)
			{
				languages = languagesBackup;
				languagesBackup = null;
			}
		}

		#endregion Load methods

		#region Culture control

		/// <summary>
		/// Gets all culture names currently available in the global dictionary.
		/// </summary>
		public static string[] AvailableCultureNames
		{
			get
			{
				using (new ReadLock(rwlock))
				{
					string[] names = new string[languages.Count];
					int i = 0;
					foreach (string name in languages.Keys)
					{
						names[i++] = name;
					}
					return names;
				}
			}
		}

		/// <summary>
		/// Gets all culture instances currently available in the global dictionary.
		/// </summary>
		public static CultureInfo[] AvailableCultures
		{
			get
			{
				string[] names = AvailableCultureNames;
				CultureInfo[] cis = new CultureInfo[names.Length];
				for (int i = 0; i < names.Length; i++)
				{
					cis[i] = CultureInfo.GetCultureInfo(names[i]);
				}
				return cis;
			}
		}

		/// <summary>
		/// Gets the RFC 4646 name of the thread's current culture. It is not guaranteed that this
		/// culture is available in the global dictionary.
		/// </summary>
		public static string CurrentThreadCulture
		{
			get { return CultureInfo.CurrentCulture.Name; }
		}

		/// <summary>
		/// Gets the two-letter ISO 639-1 language code of the thread's current culture. It is not
		/// guaranteed that this language is available in the global dictionary.
		/// </summary>
		public static string CurrentThreadLanguage
		{
			get { return CultureInfo.CurrentCulture.TwoLetterISOLanguageName; }
		}

		/// <summary>
		/// Gets the native full name of the thread's current culture. It is not guaranteed that
		/// this culture is available in the global dictionary.
		/// </summary>
		public static string CurrentThreadCultureNativeName
		{
			get { return CultureInfo.CurrentCulture.NativeName; }
		}

		/// <summary>
		/// Gets or sets the primary culture name that serves as fallback for incomplete
		/// translations. If this is null, it will not be considered when translating texts. It is
		/// not guaranteed that this culture is available in the global dictionary.
		/// </summary>
		public static string PrimaryCulture
		{
			get
			{
				using (new ReadLock(rwlock))
				{
					return primaryCulture;
				}
			}
			set
			{
				using (new UpgradeableReadLock(rwlock))
				{
					if (value != primaryCulture)
					{
						using (new WriteLock(rwlock))
						{
							primaryCulture = value;
						}
						if (!rwlock.IsWriteLockHeld)
						{
							// Raise the changed event only if no write lock is held, because then,
							// nothing could be read from the dictionary by others. In a situation
							// where this property is set with a write lock, the caller must raise
							// the changed event after releasing the lock.
							RaiseDictionaryChanged();
						}
					}
				}
			}
		}

		/// <summary>
		/// Sets the current thread culture.
		/// </summary>
		/// <param name="culture">Culture name as supported by the CultureInfo class. This culture does not have to be available in the global dictionary.</param>
		public static void SetCulture(string culture)
		{
			CultureInfo ci = new CultureInfo(culture);
			if (ci.Name != CultureInfo.CurrentCulture.Name)
			{
				if (Environment.Version.Major < 4)
				{
					// Before .NET 4.0 the CurrentCulture could only be assigned a non-neutral
					// culture. Find the default culture in this case and use it instead.
					ci = new CultureInfo(ci.LCID | 0x0400);
				}
				Thread.CurrentThread.CurrentCulture = ci;
				RaiseDictionaryChanged();
			}
		}

		/// <summary>
		/// Gets the best supported current culture name for the thread. This can be
		/// CurrentThreadCulture, CurrentThreadLanguage, one of the browser's supported cultures
		/// (if specified), PrimaryCulture or, if none of them are available, any one of the
		/// available (loaded) cultures. This culture will be tried first to find translated texts.
		/// </summary>
		/// <returns></returns>
		public static string GetCultureName()
		{
			using (new ReadLock(rwlock))
			{
				foreach (string culture in GetCulturesToTry(3))
				{
					if (languages.ContainsKey(culture)) return culture;
				}
				// The dictionary is empty
				return null;
			}
		}

		/// <summary>
		/// Returns all culture names to search, sorted by descending preference.
		/// </summary>
		/// <param name="stages">Stages to try, combination of 1 or 2.</param>
		/// <returns></returns>
		private static IEnumerable<string> GetCulturesToTry(int stages)
		{
			using (new ReadLock(rwlock))
			{
				if ((stages & 1) != 0)
				{
					// First try with the current culture, if set.
					string cc = CultureInfo.CurrentCulture.Name;
					yield return cc;

					// If the culture name has a region set, try without it.
					if (cc.Length == 5)
					{
						yield return cc.Substring(0, 2);
					}
				}
				if ((stages & 2) != 0)
				{
					// Try other cultures, according to the preference set from the HTTP request header (cf. GetCultureName method)
					if (httpPreferredCultures != null)
					{
						for (int i = 0; i < httpPreferredCultures.Length; i++)
						{
							string cc2 = httpPreferredCultures[i];
							yield return cc2;

							// If the culture name has a region set AND the culture without a region is
							// not contained in the list of preferred cultures AND no other culture
							// with this language part is yet to come in the list of preferred
							// cultures, THEN try without the region now.
							if (cc2.Length == 5 &&
								Array.IndexOf(httpPreferredCultures, cc2.Substring(0, 2)) == -1)
							{
								bool found = false;
								for (int j = i + 1; j < httpPreferredCultures.Length; j++)
								{
									if (httpPreferredCultures[j].Substring(0, 2) == cc2.Substring(0, 2))
									{
										found = true;
										break;
									}
								}
								if (!found)
								{
									yield return cc2.Substring(0, 2);
								}
							}
						}
					}

					// Then try with the primary culture, if set.
					if (PrimaryCulture != null)
					{
						yield return PrimaryCulture;

						// If the culture name has a region set, try without it.
						if (PrimaryCulture.Length == 5)
						{
							yield return PrimaryCulture.Substring(0, 2);
						}
					}

					// Finally try every available language
					foreach (string cc2 in languages.Keys)
					{
						yield return cc2;
					}
				}
			}
		}

		#endregion Culture control

		#region Web culture control

		/// <summary>
		/// Sets the best supported culture for a web page from the browser's language preference
		/// and updates the preference list for the current thread. Only available (loaded)
		/// cultures will be regarded, so all languages must be loaded before calling this method
		/// and it must be called again after loading or reloading the translation files.
		/// </summary>
		/// <param name="httpAcceptLanguage">Value of the HTTP_ACCEPT_LANGUAGE request header.</param>
		/// <param name="configPreference">Optional culture preference by the user configuration.</param>
		/// <example>
		/// The following code example shows how to pass the HTTP header data to this method:
		/// <code>
		/// Tx.SetWebCulture(HttpContext.Current.Request.ServerVariables["HTTP_ACCEPT_LANGUAGE"] as string);
		/// </code>
		/// </example>
		public static void SetWebCulture(string httpAcceptLanguage, string configPreference = null)
		{
			if (string.IsNullOrEmpty(httpAcceptLanguage))
			{
				if (!string.IsNullOrEmpty(configPreference))
				{
					httpPreferredCultures = new[] { configPreference };
				}
				else
				{
					httpPreferredCultures = new string[0];
				}
			}
			else
			{
				// Parse every item from the list
				List<CulturePriority> cpList = new List<CulturePriority>();
				if (!string.IsNullOrEmpty(configPreference))
				{
					cpList.Add(new CulturePriority(configPreference, float.MaxValue, -1));
				}
				float priority = 1;
				int index = 0;
				using (new ReadLock(rwlock))
				{
					foreach (string item in httpAcceptLanguage.Split(','))
					{
						string[] parts = item.Split(';');

						// Parse, validate and normalise culture name
						string culture = parts[0].Trim().Replace('_', '-');
						try
						{
							CultureInfo ci = CultureInfo.GetCultureInfo(culture);
							culture = ci.Name;
						}
						catch (ArgumentException)
						{
							// .NET 4.0 will throw a CultureNotFoundException, which is inherited from ArgumentException
							// Culture not found. Skip it, we couldn't support it anyway.
							continue;
						}

						// Parse the priority value for this culture name, if set.
						// If no valid q value was found, the previous item's will be kept.
						if (parts.Length > 1)
						{
							Match m = Regex.Match(parts[1].Trim(), @"q\s*=\s*([0-9.]+)");
							if (m.Success)
							{
								priority = float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
							}
						}

						cpList.Add(new CulturePriority(culture, priority, index++));
					}
				}

				httpPreferredCultures = cpList
					.OrderByDescending(cp => cp.Priority)
					.ThenBy(cp => cp.Index)
					.Select(cp => cp.Culture)
					.ToArray();
			}

			if (httpPreferredCultures.Length > 0)
			{
				// Set the current thread's culture to the most preferred one that is supported
				foreach (string culture in httpPreferredCultures)
				{
					if (languages.ContainsKey(culture) ||
						culture.Length == 5 && languages.ContainsKey(culture.Substring(0, 2)))
					{
						SetCulture(culture);
						return;
					}
				}
			}
			// No preferred culture is supported. Set the primary dictionary culture if available.
			string pc = PrimaryCulture;
			if (pc != null)
			{
				SetCulture(pc);
			}
			else
			{
				// At least raise this event because the culture preferences have changed
				RaiseDictionaryChanged();
			}
		}

		#region Helper data structures

		/// <summary>
		/// Contains all data fields from an item of the browser's accept-language header list.
		/// </summary>
		private struct CulturePriority
		{
			public string Culture;
			public float Priority;
			public int Index;

			public CulturePriority(string culture, float priority, int index)
			{
				Culture = culture;
				Priority = priority;
				Index = index;
			}
		}

		#endregion Helper data structures

		#endregion Web culture control

		#region Public translation and formatting methods

		#region Text overloads

		/// <summary>
		/// Returns a text from the dictionary.
		/// </summary>
		/// <param name="key">Text key to search.</param>
		/// <returns>Text value if found, null otherwise.</returns>
		public static string Text(string key)
		{
			using (new ReadLock(rwlock))
			{
				return ResolveData(GetText(key, -1), key, -1, (Dictionary<string, string>) null) ?? NotFound(key);
			}
		}

		/// <summary>
		/// Returns a text from the dictionary.
		/// </summary>
		/// <param name="key">Text key to search.</param>
		/// <param name="count">Count value to consider when selecting the text value.</param>
		/// <returns>Text value if found, null otherwise.</returns>
		public static string Text(string key, int count)
		{
			using (new ReadLock(rwlock))
			{
				return ResolveData(GetText(key, count), key, count, (Dictionary<string, string>) null) ?? NotFound(key);
			}
		}

		/// <summary>
		/// Returns a text from the dictionary.
		/// </summary>
		/// <param name="key">Text key to search.</param>
		/// <param name="count">Count value to consider when selecting the text value.</param>
		/// <returns>Text value if found, null otherwise.</returns>
		public static string Text(string key, decimal count)
		{
			int icount = count == (int) count ? (int) count : -1;
			using (new ReadLock(rwlock))
			{
				return ResolveData(GetText(key, icount), key, icount, (Dictionary<string, string>) null) ?? NotFound(key);
			}
		}

		/// <summary>
		/// Returns a text from the dictionary and replaces placeholders with the specified values.
		/// </summary>
		/// <param name="key">Text key to search.</param>
		/// <param name="data">Consecutive pairs of placeholder names and values.</param>
		/// <returns>Text value if found, null otherwise.</returns>
		public static string Text(string key, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return ResolveData(GetText(key, -1), key, -1, data) ?? NotFound(key);
			}
		}

		/// <summary>
		/// Returns a text from the dictionary and replaces placeholders with the specified values.
		/// </summary>
		/// <param name="key">Text key to search.</param>
		/// <param name="data">Dictionary of placeholder names and values.</param>
		/// <returns>Text value if found, null otherwise.</returns>
		public static string Text(string key, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return ResolveData(GetText(key, -1), key, -1, data) ?? NotFound(key);
			}
		}

		/// <summary>
		/// Returns a text from the dictionary and replaces placeholders with the specified values.
		/// </summary>
		/// <param name="key">Text key to search.</param>
		/// <param name="count">Count value to consider when selecting the text value.</param>
		/// <param name="data">Consecutive pairs of placeholder names and values.</param>
		/// <returns>Text value if found, null otherwise.</returns>
		public static string Text(string key, int count, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return ResolveData(GetText(key, count), key, count, data) ?? NotFound(key);
			}
		}

		/// <summary>
		/// Returns a text from the dictionary and replaces placeholders with the specified values.
		/// </summary>
		/// <param name="key">Text key to search.</param>
		/// <param name="count">Count value to consider when selecting the text value.</param>
		/// <param name="data">Dictionary of placeholder names and values.</param>
		/// <returns>Text value if found, null otherwise.</returns>
		public static string Text(string key, int count, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return ResolveData(GetText(key, count), key, count, data) ?? NotFound(key);
			}
		}

		/// <summary>
		/// Returns a text from the dictionary and replaces placeholders with the specified values.
		/// </summary>
		/// <param name="key">Text key to search.</param>
		/// <param name="count">Count value to consider when selecting the text value.</param>
		/// <param name="data">Consecutive pairs of placeholder names and values.</param>
		/// <returns>Text value if found, null otherwise.</returns>
		public static string Text(string key, decimal count, params string[] data)
		{
			int icount = count == (int) count ? (int) count : -1;
			using (new ReadLock(rwlock))
			{
				return ResolveData(GetText(key, icount), key, icount, data) ?? NotFound(key);
			}
		}

		/// <summary>
		/// Returns a text from the dictionary and replaces placeholders with the specified values.
		/// </summary>
		/// <param name="key">Text key to search.</param>
		/// <param name="count">Count value to consider when selecting the text value.</param>
		/// <param name="data">Dictionary of placeholder names and values.</param>
		/// <returns>Text value if found, null otherwise.</returns>
		public static string Text(string key, decimal count, Dictionary<string, string> data)
		{
			int icount = count == (int) count ? (int) count : -1;
			using (new ReadLock(rwlock))
			{
				return ResolveData(GetText(key, icount), key, icount, data) ?? NotFound(key);
			}
		}

		#endregion Text overloads

		#region SafeText

		/// <summary>
		/// Returns a text from the dictionary if it exists, or the text key without its namespace
		/// specification otherwise.
		/// </summary>
		/// <param name="key">Text key to search.</param>
		/// <returns></returns>
		public static string SafeText(string key)
		{
			string text = GetText(key, -1, false, false);
			if (text == null)
			{
				// Text not found in the current language, use the key instead
				int nsPos = key.IndexOf(':');
				if (nsPos > -1)
				{
					// Strip off the namespace part
					text = key.Substring(nsPos + 1);
				}
				else
				{
					text = key;
				}
			}
			return text;
		}

		#endregion SafeText

		#region Text conversion and decoration

		/// <summary>
		/// Encloses a text in normal quotation marks.
		/// </summary>
		/// <param name="text">Text to quote.</param>
		/// <returns></returns>
		public static string Quote(string text)
		{
			using (new ReadLock(rwlock))
			{
				string quoteBegin = GetText(SystemKeys.QuoteBegin, -1, false, false);
				string quoteEnd = GetText(SystemKeys.QuoteEnd, -1, false, false);
				if (string.IsNullOrEmpty(quoteBegin) || string.IsNullOrEmpty(quoteEnd))
				{
					// Use ASCII quotes as fallback
					quoteBegin = quoteEnd = "\"";
				}
				return quoteBegin + text + quoteEnd;
			}
		}

		/// <summary>
		/// Encloses a text in nested quotation marks. These marks are used within normal quotation marks.
		/// </summary>
		/// <param name="text">Text to quote.</param>
		/// <returns></returns>
		public static string QuoteNested(string text)
		{
			using (new ReadLock(rwlock))
			{
				string quoteBegin = GetText(SystemKeys.QuoteNestedBegin, -1, false, false);
				string quoteEnd = GetText(SystemKeys.QuoteNestedEnd, -1, false, false);
				if (string.IsNullOrEmpty(quoteBegin) || string.IsNullOrEmpty(quoteEnd))
				{
					// Use ASCII quotes as fallback
					quoteBegin = quoteEnd = "'";
				}
				return quoteBegin + text + quoteEnd;
			}
		}

		/// <summary>
		/// Encloses a text in parentheses (round brackets).
		/// </summary>
		/// <param name="text">Text to enclose.</param>
		/// <returns></returns>
		public static string Parentheses(string text)
		{
			using (new ReadLock(rwlock))
			{
				string parBegin = GetText(SystemKeys.ParenthesisBegin, -1, false, false);
				string parEnd = GetText(SystemKeys.ParenthesisEnd, -1, false, false);
				if (string.IsNullOrEmpty(parBegin) || string.IsNullOrEmpty(parEnd))
				{
					// Use ASCII parentheses as fallback
					parBegin = "(";
					parEnd = ")";
				}
				return parBegin + text + parEnd;
			}
		}

		/// <summary>
		/// Returns a colon string to put after another text.
		/// </summary>
		/// <returns></returns>
		public static string Colon()
		{
			string colon = GetText(SystemKeys.Colon, -1, false, false);
			if (string.IsNullOrEmpty(colon))
			{
				// Use normal colon as fallback
				colon = ":";
			}
			return colon;
		}

		/// <summary>
		/// Transforms the first character of a text to upper case.
		/// </summary>
		/// <param name="text">Text to transform.</param>
		/// <returns></returns>
		public static string UpperCase(string text)
		{
			if (string.IsNullOrEmpty(text)) return text;
			return char.ToUpper(text[0]) + text.Substring(1);
		}

		#endregion Text conversion and decoration

		#region Number formatting

		/// <summary>
		/// Formats an integer number.
		/// </summary>
		/// <param name="number">Number value to format.</param>
		/// <returns></returns>
		public static string Number(long number)
		{
			using (new ReadLock(rwlock))
			{
				NumberFormatInfo nfi = (NumberFormatInfo) CultureInfo.CurrentCulture.NumberFormat.Clone();
				nfi.NegativeSign = GetText(SystemKeys.NumberNegative, false, nfi.NegativeSign);
				string sepThreshold = GetText(SystemKeys.NumberGroupSeparatorThreshold, -1, false, false);
				if (sepThreshold == null || Math.Abs(number) >= int.Parse(sepThreshold))
				{
					nfi.NumberGroupSeparator = GetText(SystemKeys.NumberGroupSeparator, false, nfi.NumberGroupSeparator);
				}
				else
				{
					nfi.NumberGroupSeparator = "";
				}
				return number.ToString("N0", nfi);
			}
		}

		/// <summary>
		/// Formats a real number.
		/// </summary>
		/// <param name="number">Number value to format with as many decimal digits as are non-zero.</param>
		/// <returns></returns>
		public static string Number(decimal number)
		{
			string numStr = Number(number, 29);   // decimal has at most 29 siginificant digits
			numStr = numStr.TrimEnd('0');
			string decSep = GetText(SystemKeys.NumberDecimalSeparator, false, CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);
			if (numStr.EndsWith(decSep))
			{
				numStr = numStr.Substring(0, numStr.Length - decSep.Length);
			}
			return numStr;
		}

		/// <summary>
		/// Formats a real number.
		/// </summary>
		/// <param name="number">Number value to format.</param>
		/// <param name="decimals">Fixed number of decimal digits to use.</param>
		/// <returns></returns>
		public static string Number(decimal number, int decimals)
		{
			if (decimals < 0) throw new ArgumentOutOfRangeException("decimals", "Decimals must be greater than or equal to 0.");

			using (new ReadLock(rwlock))
			{
				NumberFormatInfo nfi = (NumberFormatInfo) CultureInfo.CurrentCulture.NumberFormat.Clone();
				nfi.NegativeSign = GetText(SystemKeys.NumberNegative, false, nfi.NegativeSign);
				nfi.NumberDecimalSeparator = GetText(SystemKeys.NumberDecimalSeparator, false, nfi.NumberDecimalSeparator);
				string sepThreshold = GetText(SystemKeys.NumberGroupSeparatorThreshold, -1, false, false);
				if (sepThreshold == null || Math.Abs(number) >= int.Parse(sepThreshold))
				{
					nfi.NumberGroupSeparator = GetText(SystemKeys.NumberGroupSeparator, false, nfi.NumberGroupSeparator);
				}
				else
				{
					nfi.NumberGroupSeparator = "";
				}
				return number.ToString("N" + decimals, nfi);
			}
		}

		/// <summary>
		/// Combines a formatted number and a unit string with a separator.
		/// </summary>
		/// <param name="number">Formatted number string.</param>
		/// <param name="unit">Unit string.</param>
		/// <returns></returns>
		public static string NumberUnit(string number, string unit)
		{
			if (string.IsNullOrEmpty(unit))
			{
				return number;
			}
			// Certain unit characters have no spacing
			// Source: http://de.wikipedia.org/wiki/Schreibweise_von_Zahlen#Ma.C3.9Feinheit
			if (unit[0] == '°' || unit[0] == '′' || unit[0] == '″' || unit[0] == '"')
			{
				return number + unit;
			}
			return number + GetText(SystemKeys.NumberUnitSeparator, false, " ") + unit;
		}

		/// <summary>
		/// Formats a data size in bytes to a shorter and less precise presentation using the
		/// IEC prefixes to base 2.
		/// </summary>
		/// <param name="bytes">Number of bytes to format.</param>
		/// <returns></returns>
		public static string DataSize(long bytes)
		{
			string byteUnit = GetText(SystemKeys.ByteUnit, false, "B");

			long absBytes = Math.Abs(bytes);
			if (absBytes < 0.9 * 1024)   // < 0.9 KiB -> "0 B"
				return NumberUnit(Number(bytes, 0), byteUnit);
			if (absBytes < 50 * 1024)   // < 50 KiB -> "0.0 KiB"
				return NumberUnit(Number((decimal) bytes / 1024, 1), "Ki" + byteUnit);
			if (absBytes < 0.9 * 1024 * 1024)   // < 0.9 MiB -> "0 KiB"
				return NumberUnit(Number((decimal) bytes / 1024, 0), "Ki" + byteUnit);
			if (absBytes < 50 * 1024 * 1024)   // < 50 MiB -> "0.0 MiB"
				return NumberUnit(Number((decimal) bytes / 1024 / 1024, 1), "Mi" + byteUnit);
			if (absBytes < 0.9 * 1024 * 1024 * 1024)   // < 0.9 GiB -> "0 MiB"
				return NumberUnit(Number((decimal) bytes / 1024 / 1024, 0), "Mi" + byteUnit);
			if (absBytes < 50L * 1024 * 1024 * 1024)   // < 50 GiB -> "0.0 GiB"
				return NumberUnit(Number((decimal) bytes / 1024 / 1024 / 1024, 1), "Gi" + byteUnit);
			if (absBytes < 0.9 * 1024 * 1024 * 1024 * 1024)   // < 0.9 TiB -> "0 GiB"
				return NumberUnit(Number((decimal) bytes / 1024 / 1024 / 1024, 0), "Gi" + byteUnit);
			if (absBytes < 50L * 1024 * 1024 * 1024 * 1024)   // < 50 TiB -> "0.0 TiB"
				return NumberUnit(Number((decimal) bytes / 1024 / 1024 / 1024 / 1024, 1), "Ti" + byteUnit);
			if (absBytes < 0.9 * 1024 * 1024 * 1024 * 1024 * 1024)   // < 0.9 PiB -> "0 TiB"
				return NumberUnit(Number((decimal) bytes / 1024 / 1024 / 1024 / 1024, 0), "Ti" + byteUnit);
			if (absBytes < 50L * 1024 * 1024 * 1024 * 1024 * 1024)   // < 50 PiB -> "0.0 PiB"
				return NumberUnit(Number((decimal) bytes / 1024 / 1024 / 1024 / 1024 / 1024, 1), "Pi" + byteUnit);
			if (absBytes < 0.9 * 1024 * 1024 * 1024 * 1024 * 1024 * 1024)   // < 0.9 EiB -> "0 PiB"
				return NumberUnit(Number((decimal) bytes / 1024 / 1024 / 1024 / 1024 / 1024, 0), "Pi" + byteUnit);
			// >= 0.9 EiB -> "0.0 EiB"
			return NumberUnit(Number((decimal) bytes / 1024 / 1024 / 1024 / 1024 / 1024 / 1024, 1), "Ei" + byteUnit);
			// A long (Int64) value cannot get greater than this
		}

		/// <summary>
		/// Formats an ordinal number.
		/// </summary>
		/// <param name="number">Number to format.</param>
		/// <returns></returns>
		public static string Ordinal(int number)
		{
			return Ordinal(number, false);
		}

		/// <summary>
		/// Formats an ordinal number with the specified grammatical gender (masculine or feminine).
		/// </summary>
		/// <param name="number">Number to format.</param>
		/// <param name="femininGender">true to specify the feminine form, false for the masculine form.</param>
		/// <returns></returns>
		public static string Ordinal(int number, bool femininGender)
		{
			string text = null;
			if (femininGender)
			{
				text = ResolveData(GetText(SystemKeys.NumberOrdinalFeminin, number, false, false), SystemKeys.NumberOrdinalFeminin, number, (Dictionary<string, string>) null);
			}
			if (text == null)
			{
				text = ResolveData(GetText(SystemKeys.NumberOrdinal, number, false, true), SystemKeys.NumberOrdinal, number, (Dictionary<string, string>) null);
			}
			if (text == null)
			{
				text = number.ToString() + ".";
			}
			return text;
		}

		#endregion Number formatting

		#region Date and time formatting

		/// <summary>
		/// Formats a date with the specified level of detail.
		/// </summary>
		/// <param name="time">DateTime value to format.</param>
		/// <param name="details">Details to include in the formatted string.</param>
		/// <param name="returnFormat">true to return the time format string instead of the formatted value.
		/// The <paramref name="time"/> value is ignored here.</param>
		/// <returns></returns>
		public static string Time(DateTime time, TxTime details, bool returnFormat = false)
		{
			string dowStr = null;
			StringBuilder sb = new StringBuilder();

			// Day of week
			if ((details & TxTime.DowAbbr) != 0)
			{
				if (returnFormat)
					dowStr = "ddd";
				else
					dowStr = time.ToString("ddd");
			}
			if ((details & TxTime.DowLong) != 0)
			{
				if (returnFormat)
					dowStr = "dddd";
				else
					dowStr = time.ToString("dddd");
			}

			// Date combinations
			Dictionary<string, string> data;
			string dateFormat = null;
			switch (details & TxTime.AnyDate)
			{
				case TxTime.Year:
					dateFormat = GetText(SystemKeys.DateYear, false, "yyyy");
					break;
				case TxTime.YearMonth:
					dateFormat = GetText(SystemKeys.DateYearMonth, false, "yyyy-MM");
					break;
				case TxTime.YearMonthTab:
					dateFormat = GetText(SystemKeys.DateYearMonthTab, false, "yyyy-MM");
					break;
				case TxTime.YearMonthAbbr:
					dateFormat = GetText(SystemKeys.DateYearMonthAbbr, false, "MMM yyyy");
					break;
				case TxTime.YearMonthLong:
					dateFormat = GetText(SystemKeys.DateYearMonthLong, false, "Y");
					break;
				case TxTime.YearMonthDay:
					dateFormat = GetText(SystemKeys.DateYearMonthDay, false, "d");
					break;
				case TxTime.YearMonthDayTab:
					dateFormat = GetText(SystemKeys.DateYearMonthDayTab, false, "d");
					break;
				case TxTime.YearMonthDayAbbr:
					dateFormat = GetText(SystemKeys.DateYearMonthDayAbbr, false, "d MMM yyyy");
					break;
				case TxTime.YearMonthDayLong:
					dateFormat = GetText(SystemKeys.DateYearMonthDayLong, false, "d MMMM yyyy");
					break;
				case TxTime.Month:
					dateFormat = GetText(SystemKeys.DateMonth, false, "%M");
					break;
				case TxTime.MonthTab:
					dateFormat = GetText(SystemKeys.DateMonthTab, false, "MM");
					break;
				case TxTime.MonthAbbr:
					dateFormat = GetText(SystemKeys.DateMonthAbbr, false, "MMM");
					break;
				case TxTime.MonthLong:
					dateFormat = GetText(SystemKeys.DateMonthLong, false, "MMMM");
					break;
				case TxTime.MonthDay:
					dateFormat = GetText(SystemKeys.DateMonthDay, false, "MM-dd");
					break;
				case TxTime.MonthDayTab:
					dateFormat = GetText(SystemKeys.DateMonthDayTab, false, "MM-dd");
					break;
				case TxTime.MonthDayAbbr:
					dateFormat = GetText(SystemKeys.DateMonthDayAbbr, false, "d MMM");
					break;
				case TxTime.MonthDayLong:
					dateFormat = GetText(SystemKeys.DateMonthDayLong, false, "M");
					break;
				case TxTime.Day:
					dateFormat = GetText(SystemKeys.DateDay, false, "%d");
					break;
				case TxTime.DayTab:
					dateFormat = GetText(SystemKeys.DateDayTab, false, "dd");
					break;
				case TxTime.YearQuarter:
					if (returnFormat) return "?";   // This cannot be expressed as a date format
					data = new Dictionary<string, string>();
					data["year"] = time.Year.ToString();
					data["quarter"] = ((time.Month - 1) / 3 + 1).ToString();
					sb.Append(ResolveData(GetText(SystemKeys.DateYearQuarter, false, "Q{quarter}'/'{year}"), "", -1, data));
					break;
				case TxTime.Quarter:
					if (returnFormat) return "?";   // This cannot be expressed as a date format
					data = new Dictionary<string, string>();
					data["quarter"] = ((time.Month - 1) / 3 + 1).ToString();
					sb.Append(ResolveData(GetText(SystemKeys.DateQuarter, false, "Q{quarter}"), "", -1, data));
					break;
			}
			if (dateFormat != null)
			{
				if (returnFormat)
					sb.Append(dateFormat);
				else
					sb.Append(time.ToString(dateFormat));
			}

			if (dowStr != null && sb.Length > 0)
			{
				// Day of week and date are used together, combine them
				data = new Dictionary<string, string>();
				data["dow"] = dowStr;
				data["date"] = sb.ToString();
				string dowAndDate = ResolveData(GetText(SystemKeys.DateDowWithDate, false, "{dow}, {date}"), SystemKeys.DateDowWithDate, -1, data);
				sb.Clear();
				sb.Append(dowAndDate);
			}

			if ((details & TxTime.AnyDate) != 0 && (details & TxTime.AnyTime) != 0)
			{
				sb.Append("\u2004");   // Three-Per-Em Space, or Thick Space
			}

			if ((details & TxTime.AnyTime) != 0)
			{
				DateTimeFormatInfo fi = (DateTimeFormatInfo) CultureInfo.CurrentCulture.DateTimeFormat.Clone();
				fi.AMDesignator = GetText(SystemKeys.TimeAM, false, fi.AMDesignator);
				fi.PMDesignator = GetText(SystemKeys.TimePM, false, fi.PMDesignator);

				// Time combinations
				string timeFormat = null;
				switch (details & TxTime.AnyTime)
				{
					case TxTime.HourMinuteSecondMs:
						timeFormat = GetText(SystemKeys.TimeHourMinuteSecondMs, false, "H:mm:ss.fff");
						break;
					case TxTime.HourMinuteSecondMsTab:
						timeFormat = GetText(SystemKeys.TimeHourMinuteSecondMsTab, false, "HH:mm:ss.fff");
						break;
					case TxTime.HourMinuteSecond:
						timeFormat = GetText(SystemKeys.TimeHourMinuteSecond, false, "T");
						break;
					case TxTime.HourMinuteSecondTab:
						timeFormat = GetText(SystemKeys.TimeHourMinuteSecondTab, false, "T");
						break;
					case TxTime.HourMinute:
						timeFormat = GetText(SystemKeys.TimeHourMinute, false, "t");
						break;
					case TxTime.HourMinuteTab:
						timeFormat = GetText(SystemKeys.TimeHourMinuteTab, false, "t");
						break;
					case TxTime.Hour:
						timeFormat = GetText(SystemKeys.TimeHour, false, "%H");
						break;
					case TxTime.HourTab:
						timeFormat = GetText(SystemKeys.TimeHourTab, false, "HH");
						break;
				}
				if (timeFormat != null)
				{
					if (returnFormat)
						sb.Append(timeFormat);
					else
						sb.Append(time.ToString(timeFormat, fi));
				}
			}

			if ((details & TxTime.Zone) != 0)
			{
				if (details != TxTime.Zone)
				{
					sb.Append(" ");
				}
				if (returnFormat)
					sb.Append("zzz");
				else
					sb.Append(time.ToString("zzz"));
			}

			// Simple hack to remove all % characters that are required for single-character custom date formats
			if (returnFormat && sb.Length > 2)
				return sb.ToString().Replace("%", "");

			return sb.ToString();
		}

		/// <summary>
		/// Returns a verbose description of a relative point in time.
		/// </summary>
		/// <param name="time">Time to compare with now. This can be in the future or past. If Kind is not Utc, the value is converted to UTC for comparison.</param>
		/// <returns></returns>
		public static string RelativeTime(DateTime time)
		{
			if (time == DateTime.MinValue)
			{
				return GetText(SystemKeys.TimeNever, -1) ?? NotFound(SystemKeys.TimeNever);
			}

			// Calculate time span between specified time and now
			if (time.Kind != DateTimeKind.Utc)
			{
				time = time.ToUniversalTime();
			}
			DateTimeInterval interval = GetRoundedInterval(DateTime.UtcNow, time);

			if (Math.Abs(interval.TimeSpan.TotalSeconds) <= 3)
			{
				return GetText(SystemKeys.TimeNow, -1) ?? NotFound(SystemKeys.TimeNow);
			}

			string[] keys = new string[6];
			keys[0] = !interval.Negative ? SystemKeys.TimeRelativeYears : SystemKeys.TimeRelativeNegYears;
			keys[1] = !interval.Negative ? SystemKeys.TimeRelativeMonths : SystemKeys.TimeRelativeNegMonths;
			keys[2] = !interval.Negative ? SystemKeys.TimeRelativeDays : SystemKeys.TimeRelativeNegDays;
			keys[3] = !interval.Negative ? SystemKeys.TimeRelativeHours : SystemKeys.TimeRelativeNegHours;
			keys[4] = !interval.Negative ? SystemKeys.TimeRelativeMinutes : SystemKeys.TimeRelativeNegMinutes;
			keys[5] = !interval.Negative ? SystemKeys.TimeRelativeSeconds : SystemKeys.TimeRelativeNegSeconds;
			string intervalStr = FormatTimeInterval(interval, keys, true);

			Dictionary<string, string> data = new Dictionary<string, string>();
			data["interval"] = intervalStr;
			string key = !interval.Negative ? SystemKeys.TimeRelative : SystemKeys.TimeRelativeNeg;
			return ResolveData(GetText(key, -1), key, -1, data);
		}

		/// <summary>
		/// Returns a verbose description of a time span from now to the specified point in time.
		/// </summary>
		/// <param name="time">The other end of the time span. This can be in the future or past.</param>
		/// <returns></returns>
		public static string TimeSpan(DateTime time)
		{
			if (time == DateTime.MinValue)   // TODO: Actually this should not be allowed
			{
				return GetText(SystemKeys.TimeNever, -1) ?? NotFound(SystemKeys.TimeNever);
			}

			// Calculate time span between specified time and now
			if (time.Kind != DateTimeKind.Utc)
			{
				time = time.ToUniversalTime();
			}
			DateTimeInterval interval = GetRoundedInterval(DateTime.UtcNow, time);

			string[] keys = new string[6];
			keys[0] = !interval.Negative ? SystemKeys.TimeSpanRelativeYears : SystemKeys.TimeSpanRelativeNegYears;
			keys[1] = !interval.Negative ? SystemKeys.TimeSpanRelativeMonths : SystemKeys.TimeSpanRelativeNegMonths;
			keys[2] = !interval.Negative ? SystemKeys.TimeSpanRelativeDays : SystemKeys.TimeSpanRelativeNegDays;
			keys[3] = !interval.Negative ? SystemKeys.TimeSpanRelativeHours : SystemKeys.TimeSpanRelativeNegHours;
			keys[4] = !interval.Negative ? SystemKeys.TimeSpanRelativeMinutes : SystemKeys.TimeSpanRelativeNegMinutes;
			keys[5] = !interval.Negative ? SystemKeys.TimeSpanRelativeSeconds : SystemKeys.TimeSpanRelativeNegSeconds;
			string intervalStr = FormatTimeInterval(interval, keys, true);

			Dictionary<string, string> data = new Dictionary<string, string>();
			data["interval"] = intervalStr;
			string key = !interval.Negative ? SystemKeys.TimeSpanRelative : SystemKeys.TimeSpanRelativeNeg;
			return ResolveData(GetText(key, -1), key, -1, data);
		}

		/// <summary>
		/// Returns a verbose description of a time span that is not related to the current time,
		/// including introductory wording.
		/// </summary>
		/// <param name="span">Time span to describe. This can be positive or negative.</param>
		/// <returns></returns>
		public static string TimeSpan(TimeSpan span)
		{
			string intervalStr = TimeSpanRaw(span, true);
			Dictionary<string, string> data = new Dictionary<string, string>();
			data["interval"] = intervalStr;
			string key = span.Ticks >= 0 ? SystemKeys.TimeSpan : SystemKeys.TimeSpanNeg;
			return ResolveData(GetText(key, -1), key, -1, data);
		}

		/// <summary>
		/// Returns a verbose description of a time span that is not related to the current time,
		/// without introductory wording.
		/// </summary>
		/// <param name="span">Time span to describe. This can be positive or negative.</param>
		/// <param name="singleSpecial">Specifies whether single-unit values are more verbose ("a day" instead of "1 day").</param>
		/// <returns></returns>
		public static string TimeSpanRaw(TimeSpan span, bool singleSpecial)
		{
			// Calculate time span between specified time and now
			DateTime now = DateTime.UtcNow;
			DateTimeInterval interval = GetRoundedInterval(now, now + span);

			string[] keys = new string[6];
			keys[0] = !interval.Negative ? SystemKeys.TimeSpanYears : SystemKeys.TimeSpanNegYears;
			keys[1] = !interval.Negative ? SystemKeys.TimeSpanMonths : SystemKeys.TimeSpanNegMonths;
			keys[2] = !interval.Negative ? SystemKeys.TimeSpanDays : SystemKeys.TimeSpanNegDays;
			keys[3] = !interval.Negative ? SystemKeys.TimeSpanHours : SystemKeys.TimeSpanNegHours;
			keys[4] = !interval.Negative ? SystemKeys.TimeSpanMinutes : SystemKeys.TimeSpanNegMinutes;
			keys[5] = !interval.Negative ? SystemKeys.TimeSpanSeconds : SystemKeys.TimeSpanNegSeconds;
			string intervalStr = FormatTimeInterval(interval, keys, singleSpecial);
			return intervalStr;
		}

		/// <summary>
		/// Calculates a DateTimeInterval from two DateTime values and rounds it to a resolution of
		/// not more than 2 levels.
		/// </summary>
		/// <param name="start">Start time of the interval.</param>
		/// <param name="end">End time of the interval.</param>
		/// <returns></returns>
		private static DateTimeInterval GetRoundedInterval(DateTime start, DateTime end)
		{
			// Round difference to seconds, update end time
			const long ticksPerSecond = 10000000;
			long newEnd = (long) Math.Round((double) (end.Ticks - start.Ticks) / ticksPerSecond) * ticksPerSecond;
			end = new DateTime(start.Ticks + newEnd);

			DateTimeInterval interval = new DateTimeInterval(start, end);

			// Snap to grid
			// Only snap a unit downwards if there is a greater unit.
			if ((interval.Years != 0 || interval.Months != 0 || interval.Days != 0 || interval.Hours != 0 || interval.Minutes != 0) && interval.Seconds <= 3)
			{
				interval.Seconds = 0;
			}
			else if (interval.Seconds >= 57)
			{
				interval.Minutes++;
				interval.Seconds = 0;
			}
			if ((interval.Years != 0 || interval.Months != 0 || interval.Days != 0 || interval.Hours != 0) && interval.Minutes <= 3)
			{
				interval.Minutes = 0;
			}
			else if (interval.Minutes >= 57)
			{
				interval.Hours++;
				interval.Minutes = 0;
			}
			if ((interval.Years != 0 || interval.Months != 0 || interval.Days != 0) && interval.Hours <= 1)
			{
				interval.Hours = 0;
			}
			else if (interval.Hours >= 23)
			{
				interval.Days++;
				interval.Hours = 0;
			}
			if ((interval.Years != 0 || interval.Months != 0) && interval.Days <= 2)
			{
				interval.Days = 0;
			}
			else if (interval.Days >= 28)
			{
				interval.Months++;
				interval.Days = 0;
			}
			// (Months are not snapped)

			// Decrease resolution over time, also ensure that only one smaller unit is set
			if (interval.Years >= 5)
			{
				if (interval.Months >= 7)
					interval.Years++;
				interval.Months = 0;
			}
			if (interval.Years >= 1)
			{
				interval.Days = 0;
				interval.Hours = 0;
				interval.Minutes = 0;
				interval.Seconds = 0;
			}
			if (interval.Months >= 5)
			{
				if (interval.Days >= 16)
					interval.Months++;
				interval.Days = 0;
			}
			if (interval.Months >= 1)
			{
				interval.Hours = 0;
				interval.Minutes = 0;
				interval.Seconds = 0;
			}
			if (interval.Days >= 5)
			{
				if (interval.Hours >= 12)
					interval.Days++;
				interval.Hours = 0;
			}
			if (interval.Days >= 1)
			{
				interval.Minutes = 0;
				interval.Seconds = 0;
			}
			if (interval.Hours >= 5)
			{
				if (interval.Minutes >= 30)
					interval.Hours++;
				interval.Minutes = 0;
			}
			if (interval.Hours >= 1)
			{
				interval.Seconds = 0;
			}
			if (interval.Minutes >= 5)
			{
				if (interval.Seconds >= 30)
					interval.Minutes++;
				interval.Seconds = 0;
			}

			return interval;
		}

		/// <summary>
		/// Formats a DateTimeInterval value with the specified set of text keys for a specific
		/// grammatical situation.
		/// </summary>
		/// <param name="interval">Interval data.</param>
		/// <param name="keys">Text keys for years, months, days, hours, minutes and seconds.</param>
		/// <param name="singleSpecial">Specifies whether single-unit values are more verbose ("a day" instead of "1 day").</param>
		/// <returns></returns>
		private static string FormatTimeInterval(DateTimeInterval interval, string[] keys, bool singleSpecial)
		{
			// First count the number of levels we will have
			int levelCount = 0;
			if (interval.Years > 0) levelCount++;
			if (interval.Months > 0) levelCount++;
			if (interval.Days > 0) levelCount++;
			if (interval.Hours > 0) levelCount++;
			if (interval.Minutes > 0) levelCount++;
			if (interval.Seconds > 0) levelCount++;

			// If it's one, first try to use the wording for a single level
			if (levelCount == 1)
			{
				string suffix = singleSpecial ? ".single" : "";
				if (interval.Years > 0)
				{
					string text = GetText(keys[0] + suffix, interval.Years);
					if (text != null)
						return ResolveData(text, keys[0] + suffix, interval.Years, (Dictionary<string, string>) null);
				}
				else if (interval.Months > 0)
				{
					string text = GetText(keys[1] + suffix, interval.Months);
					if (text != null)
						return ResolveData(text, keys[1] + suffix, interval.Months, (Dictionary<string, string>) null);
				}
				else if (interval.Days > 0)
				{
					string text = GetText(keys[2] + suffix, interval.Days);
					if (text != null)
						return ResolveData(text, keys[2] + suffix, interval.Days, (Dictionary<string, string>) null);
				}
				else if (interval.Hours > 0)
				{
					string text = GetText(keys[3] + suffix, interval.Hours);
					if (text != null)
						return ResolveData(text, keys[3] + suffix, interval.Hours, (Dictionary<string, string>) null);
				}
				else if (interval.Minutes > 0)
				{
					string text = GetText(keys[4] + suffix, interval.Minutes);
					if (text != null)
						return ResolveData(text, keys[4] + suffix, interval.Minutes, (Dictionary<string, string>) null);
				}
				else if (interval.Seconds > 0)
				{
					string text = GetText(keys[5] + suffix, interval.Seconds);
					if (text != null)
						return ResolveData(text, keys[5] + suffix, interval.Seconds, (Dictionary<string, string>) null);
				}
			}

			// Now use the regular texts for every other case
			List<string> levels = new List<string>();
			if (interval.Years > 0 && levels.Count < 2)
				levels.Add(ResolveData(GetText(keys[0], interval.Years), keys[0], interval.Years, (Dictionary<string, string>) null));
			if (interval.Months > 0 && levels.Count < 2)
				levels.Add(ResolveData(GetText(keys[1], interval.Months), keys[1], interval.Months, (Dictionary<string, string>) null));
			if (interval.Days > 0 && levels.Count < 2)
				levels.Add(ResolveData(GetText(keys[2], interval.Days), keys[2], interval.Days, (Dictionary<string, string>) null));
			if (interval.Hours > 0 && levels.Count < 2)
				levels.Add(ResolveData(GetText(keys[3], interval.Hours), keys[3], interval.Hours, (Dictionary<string, string>) null));
			if (interval.Minutes > 0 && levels.Count < 2)
				levels.Add(ResolveData(GetText(keys[4], interval.Minutes), keys[4], interval.Minutes, (Dictionary<string, string>) null));
			if (interval.Seconds > 0 && levels.Count < 2)
				levels.Add(ResolveData(GetText(keys[5], interval.Seconds), keys[5], interval.Seconds, (Dictionary<string, string>) null));
			return string.Join(GetText(SystemKeys.TimeRelativeSeparator, " "), levels.ToArray());
		}

		#endregion Date and time formatting

		#region String enumeration formatting

		/// <summary>
		/// Formats a conjunctive enumeration of strings.
		/// </summary>
		/// <param name="items">The string items to enumerate.</param>
		/// <returns>The formatted string enumeration.</returns>
		public static string EnumAnd(params string[] items)
		{
			return FormatEnum(true, false, false, items);
		}

		/// <summary>
		/// Formats a conjunctive enumeration of strings.
		/// </summary>
		/// <param name="items">The string items to enumerate.</param>
		/// <returns>The formatted string enumeration.</returns>
		public static string EnumAnd(IEnumerable<string> items)
		{
			return FormatEnum(true, false, false, items);
		}

		/// <summary>
		/// Formats a conjunctive enumeration of strings and transforms the first character of the
		/// first item to upper case.
		/// </summary>
		/// <param name="items">The string items to enumerate.</param>
		/// <returns>The formatted string enumeration.</returns>
		public static string UEnumAnd(params string[] items)
		{
			return FormatEnum(true, true, false, items);
		}

		/// <summary>
		/// Formats a conjunctive enumeration of strings and transforms the first character of the
		/// first item to upper case.
		/// </summary>
		/// <param name="items">The string items to enumerate.</param>
		/// <returns>The formatted string enumeration.</returns>
		public static string UEnumAnd(IEnumerable<string> items)
		{
			return FormatEnum(true, true, false, items);
		}

		/// <summary>
		/// Formats a conjunctive enumeration of strings and puts each item in quotation marks.
		/// </summary>
		/// <param name="items">The string items to enumerate.</param>
		/// <returns>The formatted string enumeration.</returns>
		public static string QEnumAnd(params string[] items)
		{
			return FormatEnum(true, false, true, items);
		}

		/// <summary>
		/// Formats a conjunctive enumeration of strings and puts each item in quotation marks.
		/// </summary>
		/// <param name="items">The string items to enumerate.</param>
		/// <returns>The formatted string enumeration.</returns>
		public static string QEnumAnd(IEnumerable<string> items)
		{
			return FormatEnum(true, false, true, items);
		}

		/// <summary>
		/// Formats a conjunctive enumeration of strings, transforms the first character of the
		/// first item to upper case and puts each item in quotation marks.
		/// </summary>
		/// <param name="items">The string items to enumerate.</param>
		/// <returns>The formatted string enumeration.</returns>
		public static string QUEnumAnd(params string[] items)
		{
			return FormatEnum(true, true, true, items);
		}

		/// <summary>
		/// Formats a conjunctive enumeration of strings, transforms the first character of the
		/// first item to upper case and puts each item in quotation marks.
		/// </summary>
		/// <param name="items">The string items to enumerate.</param>
		/// <returns>The formatted string enumeration.</returns>
		public static string QUEnumAnd(IEnumerable<string> items)
		{
			return FormatEnum(true, true, true, items);
		}

		/// <summary>
		/// Formats a disjunctive enumeration of strings.
		/// </summary>
		/// <param name="items">The string items to enumerate.</param>
		/// <returns>The formatted string enumeration.</returns>
		public static string EnumOr(params string[] items)
		{
			return FormatEnum(false, false, false, items);
		}

		/// <summary>
		/// Formats a disjunctive enumeration of strings.
		/// </summary>
		/// <param name="items">The string items to enumerate.</param>
		/// <returns>The formatted string enumeration.</returns>
		public static string EnumOr(IEnumerable<string> items)
		{
			return FormatEnum(false, false, false, items);
		}

		/// <summary>
		/// Formats a disjunctive enumeration of strings and transforms the first character of the
		/// first item to upper case.
		/// </summary>
		/// <param name="items">The string items to enumerate.</param>
		/// <returns>The formatted string enumeration.</returns>
		public static string UEnumOr(params string[] items)
		{
			return FormatEnum(false, true, false, items);
		}

		/// <summary>
		/// Formats a disjunctive enumeration of strings and transforms the first character of the
		/// first item to upper case.
		/// </summary>
		/// <param name="items">The string items to enumerate.</param>
		/// <returns>The formatted string enumeration.</returns>
		public static string UEnumOr(IEnumerable<string> items)
		{
			return FormatEnum(false, true, false, items);
		}

		/// <summary>
		/// Formats a disjunctive enumeration of strings and puts each item in quotation marks.
		/// </summary>
		/// <param name="items">The string items to enumerate.</param>
		/// <returns>The formatted string enumeration.</returns>
		public static string QEnumOr(params string[] items)
		{
			return FormatEnum(false, false, true, items);
		}

		/// <summary>
		/// Formats a disjunctive enumeration of strings and puts each item in quotation marks.
		/// </summary>
		/// <param name="items">The string items to enumerate.</param>
		/// <returns>The formatted string enumeration.</returns>
		public static string QEnumOr(IEnumerable<string> items)
		{
			return FormatEnum(false, false, true, items);
		}

		/// <summary>
		/// Formats a disjunctive enumeration of strings, transforms the first character of the
		/// first item to upper case and puts each item in quotation marks.
		/// </summary>
		/// <param name="items">The string items to enumerate.</param>
		/// <returns>The formatted string enumeration.</returns>
		public static string QUEnumOr(params string[] items)
		{
			return FormatEnum(false, true, true, items);
		}

		/// <summary>
		/// Formats a disjunctive enumeration of strings, transforms the first character of the
		/// first item to upper case and puts each item in quotation marks.
		/// </summary>
		/// <param name="items">The string items to enumerate.</param>
		/// <returns>The formatted string enumeration.</returns>
		public static string QUEnumOr(IEnumerable<string> items)
		{
			return FormatEnum(false, true, true, items);
		}

		/// <summary>
		/// Formats an enumeration of strings.
		/// </summary>
		/// <param name="and">true to use conjunctive (AND) combination, false to use disjunctive (OR) combination.</param>
		/// <param name="upperCase">Indicates whether the first character of the first item is transformed to upper case.</param>
		/// <param name="quote">Indicates whether each item is put in quotation marks.</param>
		/// <param name="items">The string items to enumerate.</param>
		/// <returns>The formatted string enumeration.</returns>
		private static string FormatEnum(bool and, bool upperCase, bool quote, IEnumerable<string> items)
		{
			int count = items.Count();
			int index = 0;
			string combinerKey = and ? SystemKeys.EnumAndCombiner : SystemKeys.EnumOrCombiner;
			string lastCombinerKey = and ? SystemKeys.EnumAndLastCombiner : SystemKeys.EnumOrLastCombiner;
			StringBuilder sb = new StringBuilder();
			foreach (string item in items)
			{
				if (index > 0)
				{
					if (index == count - 1)
					{
						sb.Append(GetText(lastCombinerKey, -1));
					}
					else
					{
						sb.Append(GetText(combinerKey, -1));
					}
				}
				string item2 = item;
				if (upperCase && index == 0)
				{
					item2 = UpperCase(item2);
				}
				if (quote)
				{
					item2 = Quote(item2);
				}
				sb.Append(item2);
				index++;
			}
			return sb.ToString();
		}

		#endregion String enumeration formatting

		#endregion Public translation and formatting methods

		#region Abbreviated public methods

		#region T overloads

		/// <summary>
		/// Abbreviation for the <see cref="Text(string)"/> method.
		/// </summary>
		public static string T(string key)
		{
			return Text(key);
		}

		/// <summary>
		/// Abbreviation for the <see cref="Text(string,int)"/> method.
		/// </summary>
		public static string T(string key, int count)
		{
			return Text(key, count);
		}

		/// <summary>
		/// Abbreviation for the <see cref="Text(string,decimal)"/> method.
		/// </summary>
		public static string T(string key, decimal count)
		{
			return Text(key, count);
		}

		/// <summary>
		/// Abbreviation for the <see cref="Text(string,string[])"/> method.
		/// </summary>
		public static string T(string key, params string[] data)
		{
			return Text(key, data);
		}

		/// <summary>
		/// Abbreviation for the <see cref="Text(string,Dictionary{string,string})"/> method.
		/// </summary>
		public static string T(string key, Dictionary<string, string> data)
		{
			return Text(key, data);
		}

		/// <summary>
		/// Abbreviation for the <see cref="Text(string,int,string[])"/> method.
		/// </summary>
		public static string T(string key, int count, params string[] data)
		{
			return Text(key, count, data);
		}

		/// <summary>
		/// Abbreviation for the <see cref="Text(string,int,Dictionary{string,string})"/> method.
		/// </summary>
		public static string T(string key, int count, Dictionary<string, string> data)
		{
			return Text(key, count, data);
		}

		/// <summary>
		/// Abbreviation for the <see cref="Text(string,decimal,string[])"/> method.
		/// </summary>
		public static string T(string key, decimal count, params string[] data)
		{
			return Text(key, count, data);
		}

		/// <summary>
		/// Abbreviation for the <see cref="Text(string,decimal,Dictionary{string,string})"/> method.
		/// </summary>
		public static string T(string key, decimal count, Dictionary<string, string> data)
		{
			return Text(key, count, data);
		}

		#endregion T overloads

		/// <summary>
		/// Abbreviation for the <see cref="Quote"/> method.
		/// </summary>
		public static string Q(string text)
		{
			return Quote(text);
		}

		/// <summary>
		/// Abbreviation for the <see cref="Parentheses"/> method.
		/// </summary>
		public static string P(string text)
		{
			return Parentheses(text);
		}

		/// <summary>
		/// Abbreviation for the <see cref="Colon"/> method.
		/// </summary>
		public static string C()
		{
			return Colon();
		}

		/// <summary>
		/// Abbreviation for the <see cref="UpperCase"/> method.
		/// </summary>
		public static string U(string text)
		{
			return UpperCase(text);
		}

		#region N overloads

		/// <summary>
		/// Abbreviation for the <see cref="Number(long)"/> method.
		/// </summary>
		public static string N(long number)
		{
			return Number(number);
		}

		/// <summary>
		/// Abbreviation for the <see cref="Number(decimal)"/> method.
		/// </summary>
		public static string N(decimal number)
		{
			return Number(number);
		}

		/// <summary>
		/// Abbreviation for the <see cref="Number(decimal,int)"/> method.
		/// </summary>
		public static string N(decimal number, int decimals)
		{
			return Number(number, decimals);
		}

		#endregion N overloads

		#endregion Abbreviated public methods

		#region Combined abbreviated public methods

		#region TC overloads

		/// <summary>
		/// Combined abbreviation for the Text and Colon methods.
		/// </summary>
		public static string TC(string key)
		{
			using (new ReadLock(rwlock))
			{
				return T(key) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Text and Colon methods.
		/// </summary>
		public static string TC(string key, int count)
		{
			using (new ReadLock(rwlock))
			{
				return T(key, count) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Text and Colon methods.
		/// </summary>
		public static string TC(string key, decimal count)
		{
			using (new ReadLock(rwlock))
			{
				return T(key, count) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Text and Colon methods.
		/// </summary>
		public static string TC(string key, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return T(key, data) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Text and Colon methods.
		/// </summary>
		public static string TC(string key, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return T(key, data) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Text and Colon methods.
		/// </summary>
		public static string TC(string key, int count, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return T(key, count, data) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Text and Colon methods.
		/// </summary>
		public static string TC(string key, int count, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return T(key, count, data) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Text and Colon methods.
		/// </summary>
		public static string TC(string key, decimal count, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return T(key, count, data) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Text and Colon methods.
		/// </summary>
		public static string TC(string key, decimal count, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return T(key, count, data) + C();
			}
		}

		#endregion TC overloads

		#region UT overloads

		/// <summary>
		/// Combined abbreviation for the UpperCase and Text methods.
		/// </summary>
		public static string UT(string key)
		{
			return U(T(key));
		}

		/// <summary>
		/// Combined abbreviation for the UpperCase and Text methods.
		/// </summary>
		public static string UT(string key, int count)
		{
			return U(T(key, count));
		}

		/// <summary>
		/// Combined abbreviation for the UpperCase and Text methods.
		/// </summary>
		public static string UT(string key, decimal count)
		{
			return U(T(key, count));
		}

		/// <summary>
		/// Combined abbreviation for the UpperCase and Text methods.
		/// </summary>
		public static string UT(string key, params string[] data)
		{
			return U(T(key, data));
		}

		/// <summary>
		/// Combined abbreviation for the UpperCase and Text methods.
		/// </summary>
		public static string UT(string key, Dictionary<string, string> data)
		{
			return U(T(key, data));
		}

		/// <summary>
		/// Combined abbreviation for the UpperCase and Text methods.
		/// </summary>
		public static string UT(string key, int count, params string[] data)
		{
			return U(T(key, count, data));
		}

		/// <summary>
		/// Combined abbreviation for the UpperCase and Text methods.
		/// </summary>
		public static string UT(string key, int count, Dictionary<string, string> data)
		{
			return U(T(key, count, data));
		}

		/// <summary>
		/// Combined abbreviation for the UpperCase and Text methods.
		/// </summary>
		public static string UT(string key, decimal count, params string[] data)
		{
			return U(T(key, count, data));
		}

		/// <summary>
		/// Combined abbreviation for the UpperCase and Text methods.
		/// </summary>
		public static string UT(string key, decimal count, Dictionary<string, string> data)
		{
			return U(T(key, count, data));
		}

		#endregion UT overloads

		#region UTC overloads

		/// <summary>
		/// Combined abbreviation for the UpperCase, Text and Colon methods.
		/// </summary>
		public static string UTC(string key)
		{
			using (new ReadLock(rwlock))
			{
				return U(T(key)) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the UpperCase, Text and Colon methods.
		/// </summary>
		public static string UTC(string key, int count)
		{
			using (new ReadLock(rwlock))
			{
				return U(T(key, count)) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the UpperCase, Text and Colon methods.
		/// </summary>
		public static string UTC(string key, decimal count)
		{
			using (new ReadLock(rwlock))
			{
				return U(T(key, count)) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the UpperCase, Text and Colon methods.
		/// </summary>
		public static string UTC(string key, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return U(T(key, data)) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the UpperCase, Text and Colon methods.
		/// </summary>
		public static string UTC(string key, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return U(T(key, data)) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the UpperCase, Text and Colon methods.
		/// </summary>
		public static string UTC(string key, int count, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return U(T(key, count, data)) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the UpperCase, Text and Colon methods.
		/// </summary>
		public static string UTC(string key, int count, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return U(T(key, count, data)) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the UpperCase, Text and Colon methods.
		/// </summary>
		public static string UTC(string key, decimal count, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return U(T(key, count, data)) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the UpperCase, Text and Colon methods.
		/// </summary>
		public static string UTC(string key, decimal count, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return U(T(key, count, data)) + C();
			}
		}

		#endregion UTC overloads

		#region QT overloads

		/// <summary>
		/// Combined abbreviation for the Quote and Text methods.
		/// </summary>
		public static string QT(string key)
		{
			using (new ReadLock(rwlock))
			{
				return Q(T(key));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote and Text methods.
		/// </summary>
		public static string QT(string key, int count)
		{
			using (new ReadLock(rwlock))
			{
				return Q(T(key, count));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote and Text methods.
		/// </summary>
		public static string QT(string key, decimal count)
		{
			using (new ReadLock(rwlock))
			{
				return Q(T(key, count));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote and Text methods.
		/// </summary>
		public static string QT(string key, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return Q(T(key, data));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote and Text methods.
		/// </summary>
		public static string QT(string key, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return Q(T(key, data));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote and Text methods.
		/// </summary>
		public static string QT(string key, int count, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return Q(T(key, count, data));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote and Text methods.
		/// </summary>
		public static string QT(string key, int count, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return Q(T(key, count, data));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote and Text methods.
		/// </summary>
		public static string QT(string key, decimal count, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return Q(T(key, count, data));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote and Text methods.
		/// </summary>
		public static string QT(string key, decimal count, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return Q(T(key, count, data));
			}
		}

		#endregion QT overloads

		#region QTC overloads

		/// <summary>
		/// Combined abbreviation for the Quote, Text and Colon methods.
		/// </summary>
		public static string QTC(string key)
		{
			using (new ReadLock(rwlock))
			{
				return Q(T(key)) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote, Text and Colon methods.
		/// </summary>
		public static string QTC(string key, int count)
		{
			using (new ReadLock(rwlock))
			{
				return Q(T(key, count)) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote, Text and Colon methods.
		/// </summary>
		public static string QTC(string key, decimal count)
		{
			using (new ReadLock(rwlock))
			{
				return Q(T(key, count)) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote, Text and Colon methods.
		/// </summary>
		public static string QTC(string key, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return Q(T(key, data)) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote, Text and Colon methods.
		/// </summary>
		public static string QTC(string key, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return Q(T(key, data)) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote, Text and Colon methods.
		/// </summary>
		public static string QTC(string key, int count, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return Q(T(key, count, data)) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote, Text and Colon methods.
		/// </summary>
		public static string QTC(string key, int count, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return Q(T(key, count, data)) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote, Text and Colon methods.
		/// </summary>
		public static string QTC(string key, decimal count, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return Q(T(key, count, data)) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote, Text and Colon methods.
		/// </summary>
		public static string QTC(string key, decimal count, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return Q(T(key, count, data)) + C();
			}
		}

		#endregion QTC overloads

		#region QUT overloads

		/// <summary>
		/// Combined abbreviation for the Quote, UpperCase and Text methods.
		/// </summary>
		public static string QUT(string key)
		{
			using (new ReadLock(rwlock))
			{
				return Q(U(T(key)));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote, UpperCase and Text methods.
		/// </summary>
		public static string QUT(string key, int count)
		{
			using (new ReadLock(rwlock))
			{
				return Q(U(T(key, count)));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote, UpperCase and Text methods.
		/// </summary>
		public static string QUT(string key, decimal count)
		{
			using (new ReadLock(rwlock))
			{
				return Q(U(T(key, count)));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote, UpperCase and Text methods.
		/// </summary>
		public static string QUT(string key, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return Q(U(T(key, data)));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote, UpperCase and Text methods.
		/// </summary>
		public static string QUT(string key, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return Q(U(T(key, data)));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote, UpperCase and Text methods.
		/// </summary>
		public static string QUT(string key, int count, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return Q(U(T(key, count, data)));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote, UpperCase and Text methods.
		/// </summary>
		public static string QUT(string key, int count, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return Q(U(T(key, count, data)));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote, UpperCase and Text methods.
		/// </summary>
		public static string QUT(string key, decimal count, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return Q(U(T(key, count, data)));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote, UpperCase and Text methods.
		/// </summary>
		public static string QUT(string key, decimal count, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return Q(U(T(key, count, data)));
			}
		}

		#endregion QUT overloads

		#region QUTC overloads

		/// <summary>
		/// Combined abbreviation for the Quote, UpperCase, Text and Colon methods.
		/// </summary>
		public static string QUTC(string key)
		{
			using (new ReadLock(rwlock))
			{
				return Q(U(T(key))) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote, UpperCase, Text and Colon methods.
		/// </summary>
		public static string QUTC(string key, int count)
		{
			using (new ReadLock(rwlock))
			{
				return Q(U(T(key, count))) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote, UpperCase, Text and Colon methods.
		/// </summary>
		public static string QUTC(string key, decimal count)
		{
			using (new ReadLock(rwlock))
			{
				return Q(U(T(key, count))) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote, UpperCase, Text and Colon methods.
		/// </summary>
		public static string QUTC(string key, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return Q(U(T(key, data))) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote, UpperCase, Text and Colon methods.
		/// </summary>
		public static string QUTC(string key, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return Q(U(T(key, data))) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote, UpperCase, Text and Colon methods.
		/// </summary>
		public static string QUTC(string key, int count, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return Q(U(T(key, count, data))) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote, UpperCase, Text and Colon methods.
		/// </summary>
		public static string QUTC(string key, int count, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return Q(U(T(key, count, data))) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote, UpperCase, Text and Colon methods.
		/// </summary>
		public static string QUTC(string key, decimal count, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return Q(U(T(key, count, data))) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Quote, UpperCase, Text and Colon methods.
		/// </summary>
		public static string QUTC(string key, decimal count, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return Q(U(T(key, count, data))) + C();
			}
		}

		#endregion QUTC overloads

		#region PT overloads

		/// <summary>
		/// Combined abbreviation for the Parentheses and Text methods.
		/// </summary>
		public static string PT(string key)
		{
			using (new ReadLock(rwlock))
			{
				return P(T(key));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses and Text methods.
		/// </summary>
		public static string PT(string key, int count)
		{
			using (new ReadLock(rwlock))
			{
				return P(T(key, count));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses and Text methods.
		/// </summary>
		public static string PT(string key, decimal count)
		{
			using (new ReadLock(rwlock))
			{
				return P(T(key, count));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses and Text methods.
		/// </summary>
		public static string PT(string key, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return P(T(key, data));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses and Text methods.
		/// </summary>
		public static string PT(string key, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return P(T(key, data));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses and Text methods.
		/// </summary>
		public static string PT(string key, int count, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return P(T(key, count, data));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses and Text methods.
		/// </summary>
		public static string PT(string key, int count, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return P(T(key, count, data));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses and Text methods.
		/// </summary>
		public static string PT(string key, decimal count, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return P(T(key, count, data));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses and Text methods.
		/// </summary>
		public static string PT(string key, decimal count, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return P(T(key, count, data));
			}
		}

		#endregion PT overloads

		#region PTC overloads

		/// <summary>
		/// Combined abbreviation for the Parentheses, Text and Colon methods.
		/// </summary>
		public static string PTC(string key)
		{
			using (new ReadLock(rwlock))
			{
				return P(T(key)) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses, Text and Colon methods.
		/// </summary>
		public static string PTC(string key, int count)
		{
			using (new ReadLock(rwlock))
			{
				return P(T(key, count)) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses, Text and Colon methods.
		/// </summary>
		public static string PTC(string key, decimal count)
		{
			using (new ReadLock(rwlock))
			{
				return P(T(key, count)) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses, Text and Colon methods.
		/// </summary>
		public static string PTC(string key, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return P(T(key, data)) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses, Text and Colon methods.
		/// </summary>
		public static string PTC(string key, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return P(T(key, data)) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses, Text and Colon methods.
		/// </summary>
		public static string PTC(string key, int count, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return P(T(key, count, data)) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses, Text and Colon methods.
		/// </summary>
		public static string PTC(string key, int count, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return P(T(key, count, data)) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses, Text and Colon methods.
		/// </summary>
		public static string PTC(string key, decimal count, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return P(T(key, count, data)) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses, Text and Colon methods.
		/// </summary>
		public static string PTC(string key, decimal count, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return P(T(key, count, data)) + C();
			}
		}

		#endregion PTC overloads

		#region PUT overloads

		/// <summary>
		/// Combined abbreviation for the Parentheses, UpperCase and Text methods.
		/// </summary>
		public static string PUT(string key)
		{
			using (new ReadLock(rwlock))
			{
				return P(U(T(key)));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses, UpperCase and Text methods.
		/// </summary>
		public static string PUT(string key, int count)
		{
			using (new ReadLock(rwlock))
			{
				return P(U(T(key, count)));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses, UpperCase and Text methods.
		/// </summary>
		public static string PUT(string key, decimal count)
		{
			using (new ReadLock(rwlock))
			{
				return P(U(T(key, count)));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses, UpperCase and Text methods.
		/// </summary>
		public static string PUT(string key, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return P(U(T(key, data)));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses, UpperCase and Text methods.
		/// </summary>
		public static string PUT(string key, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return P(U(T(key, data)));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses, UpperCase and Text methods.
		/// </summary>
		public static string PUT(string key, int count, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return P(U(T(key, count, data)));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses, UpperCase and Text methods.
		/// </summary>
		public static string PUT(string key, int count, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return P(U(T(key, count, data)));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses, UpperCase and Text methods.
		/// </summary>
		public static string PUT(string key, decimal count, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return P(U(T(key, count, data)));
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses, UpperCase and Text methods.
		/// </summary>
		public static string PUT(string key, decimal count, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return P(U(T(key, count, data)));
			}
		}

		#endregion PUT overloads

		#region PUTC overloads

		/// <summary>
		/// Combined abbreviation for the Parentheses, UpperCase, Text and Colon methods.
		/// </summary>
		public static string PUTC(string key)
		{
			using (new ReadLock(rwlock))
			{
				return P(U(T(key))) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses, UpperCase, Text and Colon methods.
		/// </summary>
		public static string PUTC(string key, int count)
		{
			using (new ReadLock(rwlock))
			{
				return P(U(T(key, count))) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses, UpperCase, Text and Colon methods.
		/// </summary>
		public static string PUTC(string key, decimal count)
		{
			using (new ReadLock(rwlock))
			{
				return P(U(T(key, count))) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses, UpperCase, Text and Colon methods.
		/// </summary>
		public static string PUTC(string key, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return P(U(T(key, data))) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses, UpperCase, Text and Colon methods.
		/// </summary>
		public static string PUTC(string key, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return P(U(T(key, data))) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses, UpperCase, Text and Colon methods.
		/// </summary>
		public static string PUTC(string key, int count, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return P(U(T(key, count, data))) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses, UpperCase, Text and Colon methods.
		/// </summary>
		public static string PUTC(string key, int count, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return P(U(T(key, count, data))) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses, UpperCase, Text and Colon methods.
		/// </summary>
		public static string PUTC(string key, decimal count, params string[] data)
		{
			using (new ReadLock(rwlock))
			{
				return P(U(T(key, count, data))) + C();
			}
		}

		/// <summary>
		/// Combined abbreviation for the Parentheses, UpperCase, Text and Colon methods.
		/// </summary>
		public static string PUTC(string key, decimal count, Dictionary<string, string> data)
		{
			using (new ReadLock(rwlock))
			{
				return P(U(T(key, count, data))) + C();
			}
		}

		#endregion PUTC overloads

		#endregion Combined abbreviated public methods

		#region Private placeholder resolution methods

		/// <summary>
		/// Resolves placeholders in the text with the specified data values.
		/// </summary>
		/// <param name="text">Text value to resolve placeholders in. Placeholder names are enclosed in curly braces.</param>
		/// <param name="key">Text key that has originally provided the text value.</param>
		/// <param name="count">Count value, if available, to replace the count placeholder.</param>
		/// <param name="data">Consecutive pairs of placeholder names and values.</param>
		/// <returns>Resolved text value.</returns>
		private static string ResolveData(string text, string key, int count, params string[] data)
		{
			if ((data.Length % 2) != 0)
			{
				LogStack("Resolve data: Uneven length of data arguments array. The last single item is ignored.");
			}
			Dictionary<string, string> dataDict = null;
			if (data != null)
			{
				dataDict = new Dictionary<string, string>();
				int i = 0;
				while (i + 1 < data.Length)
				{
					string name = data[i];
					string value = data[i + 1];
					if (dataDict.ContainsKey(name))
					{
						LogStack("Resolve data: Duplicate placeholder name {0}.", name);
					}
					dataDict[name] = value;
					i += 2;
				}
			}
			return ResolveData(text, key, count, dataDict);
		}

		/// <summary>
		/// Resolves placeholders in the text with the specified data values.
		/// </summary>
		/// <param name="text">Text value to resolve placeholders in. Placeholder names are enclosed in curly braces.</param>
		/// <param name="key">Text key that has originally provided the text value.</param>
		/// <param name="count">Count value, if available, to replace the count placeholder.</param>
		/// <param name="data">Dictionary of placeholder names and values.</param>
		/// <returns>Resolved text value.</returns>
		private static string ResolveData(string text, string key, int count, Dictionary<string, string> data)
		{
			return ResolveData(text, key, count, data, null);
		}

		/// <summary>
		/// Resolves placeholders in the text with the specified data values.
		/// </summary>
		/// <param name="text">Text value to resolve placeholders in. Placeholder names are enclosed in curly braces.</param>
		/// <param name="key">Text key that has originally provided the text value.</param>
		/// <param name="count">Count value, if available, to replace the count placeholder.</param>
		/// <param name="data">Dictionary of placeholder names and values.</param>
		/// <param name="usedPlaceholderNames">Set of used placeholder names from a parent ResolveData level.</param>
		/// <returns>Resolved text value.</returns>
		private static string ResolveData(string text, string key, int count, Dictionary<string, string> data, HashSet<string> usedPlaceholderNames)
		{
			bool newUnusedSet = false;
			if (usedKeys != null && usedPlaceholderNames == null)
			{
				usedPlaceholderNames = new HashSet<string>();
				newUnusedSet = true;
			}

			if (string.IsNullOrEmpty(text))
			{
				if (newUnusedSet && data != null)
					LogUnusedPlaceholderNames(key, data, usedPlaceholderNames);
				return text;
			}

			int openBracePos = text.IndexOf('{');
			if (openBracePos == -1)
			{
				// No placeholder found, nothing to resolve
				if (newUnusedSet)
					LogUnusedPlaceholderNames(key, data, usedPlaceholderNames);
				return text;
			}
			StringBuilder result = new StringBuilder();
			// Copy first plain text chunk if any
			if (openBracePos > 0)
			{
				result.Append(text.Substring(0, openBracePos));
			}
			using (new ReadLock(rwlock))
			{
				while (true)
				{
					// Skip if escape sequence
					if (text.Length > openBracePos + 1 && text[openBracePos + 1] == '{')
					{
						openBracePos++;
						result.Append('{');
					}
					else
					{
						int closeBracePos = text.IndexOf('}', openBracePos + 1);
						if (closeBracePos == -1)
						{
							// No matching brace found, leave the rest as-is
							result.Append(text.Substring(openBracePos));
							break;
						}
						int braceLength = closeBracePos - openBracePos - 1;
						if (braceLength == 0)
						{
							// Empty braces, leave as-is
							result.Append("{}");
						}
						else if (braceLength == 1 && text[openBracePos + 1] == '#')
						{
							// Found a {#} placeholder, insert the count value
							if (count == -1)
							{
								LogStack("Resolve data: No count value or -1 specified for {{#}} placeholder in key \"{0}\".", key);
							}
							result.Append(count.ToString());
						}
						else if (braceLength > 1 && text[openBracePos + 1] == '=')
						{
							// Found another text key inclusion {=...}, resolve that value
							string subkey;
							int subcount = -1;
							// Check for a count part
							int countPos = text.IndexOf('#', openBracePos + 2, braceLength - 1);
							if (countPos == -1)
							{
								// No count value found, interpret everything as text key
								subkey = text.Substring(openBracePos + 2, braceLength - 1);
							}
							else
							{
								// Count value {=...#...} found
								subkey = text.Substring(openBracePos + 2, countPos - (openBracePos + 2));
								string countName = text.Substring(countPos + 1, closeBracePos - (countPos + 1));
								if (countName != "")
								{
									if (usedPlaceholderNames != null)
										usedPlaceholderNames.Add(countName);
									string countValue;
									if (data != null && data.TryGetValue(countName, out countValue))
									{
										if (!int.TryParse(countValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out subcount))
										{
											// Reset value
											subcount = -1;
											LogStack("Resolve data: Subcount placeholder name \"{0}\" resolves to value \"{1}\" which is not an integer. Ignoring count.", countName, countValue);
										}
									}
									else
									{
										LogStack("Resolve data: Subcount placeholder name \"{0}\" is unset. Ignoring count.", countName);
									}
								}
								else
								{
									// Pass through the current count value
									subcount = count;
								}
							}
							// Resolve the subtext
							// (It doesn't matter which key is passed here as second argument
							// because it's not used in that call level anyway.)
							string subtext = ResolveData(GetText(subkey, subcount), key, subcount, data, usedPlaceholderNames);
							if (subtext != null)
							{
								result.Append(subtext);
							}
							else
							{
								LogStack("Resolve data: Subkey text \"{0}\" is unset.", subkey);
								result.Append(text.Substring(openBracePos, braceLength + 2));
							}
						}
						else
						{
							// Anything else is a placeholder name, insert its value
							string varName = text.Substring(openBracePos + 1, braceLength);
							if (usedPlaceholderNames != null)
								usedPlaceholderNames.Add(varName);
							string varValue;
							if (data != null && data.TryGetValue(varName, out varValue))
							{
								result.Append(varValue);
							}
							else
							{
								LogStack("Resolve data: Placeholder name \"{0}\" is unset.", varName);
								result.Append(text.Substring(openBracePos, braceLength + 2));
							}
						}
						openBracePos = closeBracePos;
					}
					int nextBracePos = text.IndexOf('{', openBracePos + 1);
					if (nextBracePos == -1)
					{
						// Nothing more found, append the remaining text
						result.Append(text.Substring(openBracePos + 1));
						break;
					}
					else if (nextBracePos > openBracePos + 1)
					{
						// Copy text between this and the next brace
						result.Append(text.Substring(openBracePos + 1, nextBracePos - (openBracePos + 1)));
					}
					openBracePos = nextBracePos;
				}
			}
			if (newUnusedSet)
				LogUnusedPlaceholderNames(key, data, usedPlaceholderNames);
			return result.ToString();
		}

		#endregion Private placeholder resolution methods

		#region Private text retrieval methods

		/// <summary>
		/// Searches a text key in the preferred language dictionaries.
		/// </summary>
		/// <param name="key">Text key to search.</param>
		/// <param name="defaultText">Text to return if the key was not found.</param>
		/// <returns>Text value if found, defaultText otherwise.</returns>
		private static string GetText(string key, string defaultText)
		{
			return GetText(key, -1, true, false) ?? defaultText;
		}

		/// <summary>
		/// Searches a text key in the preferred language dictionaries.
		/// </summary>
		/// <param name="key">Text key to search.</param>
		/// <param name="useFallback">Sepcifies whether other languages than the current language shall be searched as fallback.</param>
		/// <param name="defaultText">Text to return if the key was not found.</param>
		/// <returns>Text value if found, defaultText otherwise.</returns>
		private static string GetText(string key, bool useFallback, string defaultText)
		{
			return GetText(key, -1, useFallback, false) ?? defaultText;
		}

		/// <summary>
		/// Searches a text key in the preferred language dictionaries.
		/// </summary>
		/// <param name="key">Text key to search.</param>
		/// <param name="count">Count value to consider when selecting the text value.</param>
		/// <returns>Text value if found, null otherwise.</returns>
		private static string GetText(string key, int count)
		{
			return GetText(key, count, true, true);
		}

		/// <summary>
		/// Searches a text key in the preferred language dictionaries.
		/// </summary>
		/// <param name="key">Text key to search.</param>
		/// <param name="count">Count value to consider when selecting the text value.</param>
		/// <param name="useFallback">Sepcifies whether other languages than the current language shall be searched as fallback.</param>
		/// <param name="logMissing">true to log missing keys, false to ignore the error.</param>
		/// <returns>Text value if found, null otherwise.</returns>
		private static string GetText(string key, int count, bool useFallback, bool logMissing)
		{
			// Log used key if the option is enabled
			lock (logLock)
			{
				if (usedKeys != null)
				{
					usedKeys.Add(key);
				}
			}

			if (globalMode == TxMode.ShowKey || threadMode == TxMode.ShowKey)
			{
				return NotFound(key);
			}

			using (new ReadLock(rwlock))
			{
				string firstCulture = null;
				foreach (string culture in GetCulturesToTry(1))
				{
					string text = GetCultureText(culture, key, count);
					if (text != null)
					{
						if (globalMode == TxMode.UnicodeTest || threadMode == TxMode.UnicodeTest)
						{
							text = GetUnicodeTestString(text);
						}
						return text;
					}
					if (firstCulture == null) firstCulture = culture;
				}
				if (!useFallback)
				{
					if (logMissing)
					{
						Log("Get text: Text key \"{0}\" is unset for culture {1}. Fallback cultures will NOT be used.",
							key,
							firstCulture.Length == 5 ? firstCulture + " and " + firstCulture.Substring(0, 2) : firstCulture);
					}
					return null;
				}

				foreach (string culture in GetCulturesToTry(2))
				{
					string text = GetCultureText(culture, key, count);
					if (text != null)
					{
						if (globalMode == TxMode.UnicodeTest || threadMode == TxMode.UnicodeTest)
						{
							text = GetUnicodeTestString(text);
						}
						return text;
					}
				}
				// Nothing found, return null
				Log("Get text: Text key \"{0}\" is unset for ALL cultures.", key);
				return null;
			}
		}

		private static string GetUnicodeTestString(string text)
		{
			bool inPlaceholder = false;
			char[] chars = text.ToCharArray();
			for (int i = 0; i < chars.Length; i++)
			{
				if (chars[i] == '{')
				{
					inPlaceholder = true;
				}
				else if (chars[i] == '}')
				{
					inPlaceholder = false;
				}
				else if (!inPlaceholder)
				{
					chars[i] = GetUnicodeTestChar(chars[i], i);
				}
			}
			return new string(chars);
		}

		private static char GetUnicodeTestChar(char ch, int i)
		{
			string alternatives = null;
			switch (ch)
			{
				case 'A': alternatives = "ÀÁÂÃÄÅĀāĂĄǞǠȀȂÅ"; break;
				case 'B': alternatives = "ḂḄḆ"; break;
				case 'C': alternatives = "ÇĆĈĊČƇℂ"; break;
				case 'D': alternatives = "ĐƉƊ"; break;
				case 'E': alternatives = "ÈÉÊËĒĔĖĘĚƎȄȆ"; break;
				case 'F': alternatives = "ƑḞ"; break;
				case 'G': alternatives = "ĜĞĠĢǤǦḠ"; break;
				case 'H': alternatives = "ĤĦḢḤḦḨḪῌℍ"; break;
				case 'I': alternatives = "ĨĪĬĮİǏȈȊḬỈỊῙ"; break;
				case 'J': alternatives = "Ĵ"; break;
				case 'K': alternatives = "ҠĶḲḴ"; break;
				case 'L': alternatives = "ĹĻĽĿŁḶḸḺḼℒ"; break;
				case 'M': alternatives = "ḾṀṂℳ"; break;
				case 'N': alternatives = "ƝÑŃŅŇИЙṄṆṈṊℕ₦"; break;
				case 'O': alternatives = "ÒÓÔÕÖØŌŎŐƠǑǪȌȎΘṌṒỌỘ"; break;
				case 'P': alternatives = "ƤṔṖℙ"; break;
				case 'Q': alternatives = "Ⴓℚ"; break;
				case 'R': alternatives = "ŔŖŘȐȒЯṘṚṜṞℛℜℝ"; break;
				case 'S': alternatives = "ŚŜŞŠṠṢṤṨ"; break;
				case 'T': alternatives = "ŢŤƬṪṬṮṰ"; break;
				case 'U': alternatives = "ÙÚÛÜŨŪŬŮǓǕỦ"; break;
				case 'V': alternatives = "ƲṾ℣"; break;
				case 'W': alternatives = "ŴШѠẀẂẄẆẈ₩"; break;
				case 'X': alternatives = "ҲẊẌ"; break;
				case 'Y': alternatives = "ÝŶŸƳУҰႸჄẎ"; break;
				case 'Z': alternatives = "ŹŻŽƵẐẒẔℤ"; break;
				case 'a': alternatives = "àáâãäåāăӑạậ"; break;
				case 'b': alternatives = "ƀƅɓḃḅḇ"; break;
				case 'c': alternatives = "çćĉċčḉ"; break;
				case 'd': alternatives = "đḋḍḏḓძ"; break;
				case 'e': alternatives = "èéêëēĕėęěɘєӗḙḛệ"; break;
				case 'f': alternatives = "ƒḟ"; break;
				case 'g': alternatives = "ǥĝğġģɠḡ"; break;
				case 'h': alternatives = "ĥħɦḣḥḧḩḫႹℎℏ"; break;
				case 'i': alternatives = "ìíîïĩīĭǐȉȋɨιїḭịἰῒ"; break;
				case 'j': alternatives = "ĵʝǰ"; break;
				case 'k': alternatives = "ķĸƙʞкḱḳḵ"; break;
				case 'l': alternatives = "Ɩĺļľŀłɭḷḻḽℓ"; break;
				case 'm': alternatives = "ɱḿṁṃ₥ო"; break;
				case 'n': alternatives = "ñńņňŋɲɳήṅṇṉ"; break;
				case 'o': alternatives = "òóôõöōŏőǒǫȏόṍọ"; break;
				case 'p': alternatives = "ƥρṕṗῤῥ"; break;
				case 'q': alternatives = "ʠգզ"; break;
				case 'r': alternatives = "ŕŗřгṙṛṝṟ"; break;
				case 's': alternatives = "śŝşšʂṡṣṩ"; break;
				case 't': alternatives = "ţťŧṫṭṯṱẗ"; break;
				case 'u': alternatives = "ùúûüũūŭůűṳṵṷṻụ∪"; break;
				case 'v': alternatives = "ṽṿ∨"; break;
				case 'w': alternatives = "ẁẃẅẇẉὠὡὼώ"; break;
				case 'x': alternatives = "×ẋẍχ"; break;
				case 'y': alternatives = "ýÿŷўẏỳỵ"; break;
				case 'z': alternatives = "źżžƶẑẓẕ"; break;
				default: return ch;
			}
			return alternatives[i % alternatives.Length];
		}

		/// <summary>
		/// Searches a text key in the specified language dictionary.
		/// </summary>
		/// <param name="culture">Culture name of the language dictionary to search.</param>
		/// <param name="key">Text key to search.</param>
		/// <param name="count">Count value to consider when selecting the text value.</param>
		/// <returns>Text value if found, null otherwise.</returns>
		private static string GetCultureText(string culture, string key, int count)
		{
			if (culture == null)
				throw new ArgumentNullException("The culture name must not be null.");
			if (key == null)
				throw new ArgumentNullException("The text key must not be null.");

			// This method is only called from GetText which already holds a read lock,
			// so no additional locking is needed here.

			// Find the specified culture dictionary
			Dictionary<string, Dictionary<int, string>> language;
			if (languages.TryGetValue(culture, out language))
			{
				// Find the text key within that dictionary
				Dictionary<int, string> textItem;
				if (language.TryGetValue(key, out textItem))
				{
					string text;
					if (count > -1)
					{
						// A count value is specified, search for a matching text value.
						// First try a direct match
						if (textItem.TryGetValue(count, out text))
						{
							return text;
						}
						// Then test all available quantifiers (except -1).
						// First collect all defined quantifiers
						List<int> quantifiers = new List<int>();
						foreach (KeyValuePair<int, string> kvp in textItem)
						{
							if (kvp.Key != -1)
							{
								quantifiers.Add(kvp.Key);
							}
						}
						// Then sort them by value
						quantifiers.Sort();
						// And evaluate them, the greatest value first, then decreasing.
						// (Greatest modulo values are most specific and need to be considered first.
						// Since the modulo value is encoded in the higher bits, this can be compared
						// normally.)
						for (int i = quantifiers.Count - 1; i >= 0; i--)
						{
							int q = quantifiers[i];
							int c = q & 0xFFFF;
							int mod = (q >> 16) & 0xFFFF;
							if (mod >= 2 && c == count % mod)
							{
								return textItem[q];
							}
						}
					}
					else if (usedKeys != null)
					{
						// No count value is specified and unused logging is active.
						// Check whether the text key has quantifiers defined.
						if (textItem.Count > 1 || !textItem.ContainsKey(-1))
						{
							LogStack("Get text: Text key \"{0}\" with quantifier in culture {1} was requested without a count value.", key, culture);
						}
					}
					// Try the generic form if nothing was found by now
					if (textItem.TryGetValue(-1, out text))
					{
						return text;
					}
				}
			}

			// Nothing found, return null
			return null;
		}

		/// <summary>
		/// Returns a replacement text for text keys that were not found in the dictionary.
		/// </summary>
		/// <param name="key">Text key was was not found. This is included in the return value for further analysis.</param>
		/// <returns></returns>
		private static string NotFound(string key)
		{
			return "[" + key + "]";
		}

		#endregion Private text retrieval methods

		#region Logging

		/// <summary>
		/// Writes a message to the current logging target, if any, and includes a call stack if
		/// the target is a file.
		/// </summary>
		/// <param name="message">Message to write. May contain placeholders like {0}, {1} like for String.Format.</param>
		/// <param name="args">Placeholder arguments for String.Format.</param>
		private static void LogStack(string message, params object[] args)
		{
			lock (logLock)
			{
				if (logFileName != null)
				{
					Log(message, args);
					if (logWriter != null)
					{
						string stackTrace = Environment.StackTrace;
						// Shorten the stack trace to the effective entry point of WPF applications
						stackTrace = Regex.Replace(stackTrace, @"\r?\n[^\r\n]+System\.RuntimeTypeHandle\.CreateInstance\(.*?$", "", RegexOptions.Singleline);
						logWriter.WriteLine(stackTrace);
						logWriter.Flush();
					}
				}
			}
		}

		/// <summary>
		/// Writes a message to the current logging target, if any.
		/// </summary>
		/// <param name="message">Message to write. May contain placeholders like {0}, {1} like for String.Format.</param>
		/// <param name="args">Placeholder arguments for String.Format.</param>
		private static void Log(string message, params object[] args)
		{
			lock (logLock)
			{
				if (logFileName != null)
				{
					message = string.Format(message, args);
					if (logWriter != null)
					{
						logWriter.WriteLine(DateTime.Now.ToString(@"yyyy-MM-dd HH\:mm\:ss", CultureInfo.InvariantCulture) + " " + message);
						logWriter.Flush();
					}
					else
					{
						System.Diagnostics.Trace.WriteLine("Tx: " + message);
					}
				}
			}
		}

		/// <summary>
		/// Logs placeholder names provided by the application but not used by a text key. This is
		/// only called if the logging of unused text keys option is enabled.
		/// </summary>
		/// <param name="key">Text key that has originally been resolved.</param>
		/// <param name="data">Data dictionary with placeholder names and values as provided by the application.</param>
		/// <param name="usedPlaceholderNames">Set of placeholder names that were used while resolving the text.</param>
		private static void LogUnusedPlaceholderNames(string key, Dictionary<string, string> data, HashSet<string> usedPlaceholderNames)
		{
			if (data != null && data.Count > 0)
			{
				HashSet<string> dataNames = new HashSet<string>(data.Keys);
				dataNames.ExceptWith(usedPlaceholderNames);
				if (dataNames.Count > 0)
				{
					LogStack(
						"Placeholder names provided but unused by text key \"{0}\": {1}",
						key,
						string.Join(", ", dataNames));
				}
			}
		}

		/// <summary>
		/// Gets or sets the name of the file to log to. If this is an empty string, log messages
		/// are written to Trace with the prefix "Tx: ". If this is null, logging is disabled.
		/// Existing log files will be appended to. The directory must already exist.
		/// </summary>
		public static string LogFileName
		{
			get
			{
				lock (logLock)
				{
					return logFileName;
				}
			}
			set
			{
				lock (logLock)
				{
					if (value != logFileName)
					{
						if (logWriter != null)
						{
							// Log writer was open, close it and remove the event handler
							logWriter.Close();
							logWriter = null;
							AppDomain.CurrentDomain.ProcessExit -= CurrentDomain_ProcessExit;
						}
						logFileName = value;
						if (logFileName != null)
						{
							if (logFileName.Length > 0)
							{
								// Open the log file now and remember to close it when the process exits
								logWriter = new StreamWriter(logFileName, true, Encoding.UTF8);
								AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
							}
							// Otherwise log to Trace
						}
					}
				}
			}
		}

		/// <summary>
		/// Called when the current process exits.
		/// </summary>
		/// <remarks>
		/// The processing time in this event is limited. All handlers of this event together must
		/// not take more than ca. 3 seconds. The processing will then be terminated.
		/// </remarks>
		private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
		{
			lock (logLock)
			{
				// Close the log file if it is open
				if (logWriter != null)
				{
					if (usedKeys != null && !string.IsNullOrEmpty(primaryCulture))
					{
						// Write the unused keys of the primary culture to the log file
						try
						{
							HashSet<string> primaryKeys = new HashSet<string>(languages[primaryCulture].Keys);
							primaryKeys.ExceptWith(usedKeys);
							// Don't report system keys, most of them remain unused most of the time
							primaryKeys.RemoveWhere(s => s.StartsWith("Tx:"));
							if (primaryKeys.Count == 0)
							{
								Log("No text keys from the culture {0} were not used.", primaryCulture);
							}
							else
							{
								Log("The following {0} text keys from the culture {1} were not used:", primaryKeys.Count, primaryCulture);
								foreach (string key in primaryKeys)
								{
									logWriter.Write("    ");
									logWriter.WriteLine(key);
								}
							}
						}
						catch (KeyNotFoundException)
						{
							// No culture, no unused keys. Nothing to do.
						}
					}

					logWriter.Close();
					logWriter = null;
				}
			}
		}

		#endregion Logging

		#region Synchronisation helper classes

		/// <summary>
		/// Provides a simple disposable write lock adapter for a ReaderWriterLockSlim object.
		/// </summary>
		private struct WriteLock : IDisposable
		{
			private ReaderWriterLockSlim rwlock;
			private bool wasLocked;

			/// <summary>
			/// Acquires a write lock on a lock object. If the lock object already holds a write
			/// lock, no new lock is acquired and no lock will be released.
			/// </summary>
			/// <param name="rwlock">Lock object to work with. If this is null, no lock will be acquired.</param>
			public WriteLock(ReaderWriterLockSlim rwlock)
			{
				this.rwlock = rwlock;
				if (rwlock == null)
					wasLocked = true;
				else
					wasLocked = rwlock.IsWriteLockHeld;
				if (!wasLocked && !rwlock.TryEnterWriteLock(WriteLockTimeout))
				{
					throw new InvalidOperationException("The writer lock could not be acquired.");
				}
			}

			/// <summary>
			/// Releases the lock if one was acquired. It is safe to call this method multiple times.
			/// </summary>
			public void Dispose()
			{
				if (!wasLocked)
				{
					rwlock.ExitWriteLock();
					wasLocked = true;
				}
			}
		}

		/// <summary>
		/// Provides a simple disposable upgradeable read lock adapter for a ReaderWriterLockSlim object.
		/// </summary>
		private struct UpgradeableReadLock : IDisposable
		{
			private ReaderWriterLockSlim rwlock;
			private bool wasLocked;

			/// <summary>
			/// Acquires an upgradeable read lock on a lock object. If the lock object already
			/// holds a read lock, no new lock is acquired and no lock will be released.
			/// </summary>
			/// <param name="rwlock">Lock object to work with. If this is null, no lock will be acquired.</param>
			public UpgradeableReadLock(ReaderWriterLockSlim rwlock)
			{
				this.rwlock = rwlock;
				if (rwlock == null)
					wasLocked = true;
				else
					wasLocked = rwlock.IsUpgradeableReadLockHeld || rwlock.IsWriteLockHeld;
				if (!wasLocked && !rwlock.TryEnterUpgradeableReadLock((ReadLockTimeout + WriteLockTimeout) / 2))
				{
					throw new InvalidOperationException("The upgradeable reader lock could not be acquired.");
				}
			}

			/// <summary>
			/// Releases the lock if one was acquired. It is safe to call this method multiple times.
			/// </summary>
			public void Dispose()
			{
				if (!wasLocked)
				{
					rwlock.ExitUpgradeableReadLock();
					wasLocked = true;
				}
			}
		}

		/// <summary>
		/// Provides a simple disposable read lock adapter for a ReaderWriterLockSlim object.
		/// </summary>
		private struct ReadLock : IDisposable
		{
			private ReaderWriterLockSlim rwlock;
			private bool wasLocked;

			/// <summary>
			/// Acquires a read lock on a lock object. If the lock object already holds a read
			/// lock, no new lock is acquired and no lock will be released.
			/// </summary>
			/// <param name="rwlock">Lock object to work with. If this is null, no lock will be acquired.</param>
			public ReadLock(ReaderWriterLockSlim rwlock)
			{
				this.rwlock = rwlock;
				if (rwlock == null)
					wasLocked = true;
				else
					wasLocked = rwlock.IsReadLockHeld || rwlock.IsUpgradeableReadLockHeld || rwlock.IsWriteLockHeld;
				if (!wasLocked && !rwlock.TryEnterReadLock(ReadLockTimeout))
				{
					throw new InvalidOperationException("The reader lock could not be acquired.");
				}
			}

			/// <summary>
			/// Releases the lock if one was acquired. It is safe to call this method multiple times.
			/// </summary>
			public void Dispose()
			{
				if (!wasLocked)
				{
					rwlock.ExitReadLock();
					wasLocked = true;
				}
			}
		}

		#endregion Synchronisation helper classes
	}

	#region TxMode enumeration

	/// <summary>
	/// Defines operation modes of the Tx library.
	/// </summary>
	public enum TxMode
	{
		/// <summary>Normal translation of text keys.</summary>
		Normal,
		/// <summary>Returns the text keys instead of their translation.</summary>
		ShowKey,
		/// <summary>Adds additional Unicode characters or decorations to test the application's
		/// encoding capabilities.</summary>
		UnicodeTest
	}

	#endregion TxMode enumeration

	#region Date and time format enumeration

	/// <summary>
	/// Defines date and time components to be used with the Tx.Time method. Some of these values
	/// can be combined, other values should not be used.
	/// </summary>
	[Flags]
	public enum TxTime
	{
		/// <summary>
		/// Internal value, do not use.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		FragmentYear = 0x1,
		/// <summary>
		/// Internal value, do not use.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		FragmentMonth = 0x2,
		/// <summary>
		/// Internal value, do not use.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		FragmentMonthTab = 0x4,
		/// <summary>
		/// Internal value, do not use.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		FragmentMonthAbbr = 0x8,
		/// <summary>
		/// Internal value, do not use.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		FragmentMonthLong = 0x10,
		/// <summary>
		/// Internal value, do not use.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		FragmentDay = 0x20,
		/// <summary>
		/// Internal value, do not use.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		FragmentDayTab = 0x40,
		/// <summary>
		/// Internal value, do not use.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		FragmentQuarter = 0x80,

		/// <summary>
		/// The day of the week in abbreviated form.
		/// </summary>
		DowAbbr = 0x100,
		/// <summary>
		/// The day of the week in full.
		/// </summary>
		DowLong = 0x200,

		/// <summary>
		/// Internal value, do not use.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		FragmentHour = 0x400,
		/// <summary>
		/// Internal value, do not use.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		FragmentHourTab = 0x800,
		/// <summary>
		/// Internal value, do not use.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		FragmentMinute = 0x1000,
		/// <summary>
		/// Internal value, do not use.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		FragmentSecond = 0x2000,
		/// <summary>
		/// Internal value, do not use.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		FragmentMs = 0x4000,

		/// <summary>
		/// The time zone.
		/// </summary>
		Zone = 0x8000,

		/// <summary>
		/// Internal value, do not use. Combines all date-related fragment values.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		AnyDate = FragmentYear | FragmentMonth | FragmentMonthTab | FragmentMonthAbbr | FragmentMonthLong | FragmentDay | FragmentDayTab | FragmentQuarter,
		/// <summary>
		/// Internal value, do not use. Combines all time-related fragment values.
		/// </summary>
		[EditorBrowsable(EditorBrowsableState.Never)]
		AnyTime = FragmentHour | FragmentHourTab | FragmentMinute | FragmentSecond | FragmentMs,

		/// <summary>
		/// The year alone.
		/// </summary>
		Year = FragmentYear,
		/// <summary>
		/// The year and month in numeric form.
		/// </summary>
		YearMonth = FragmentYear | FragmentMonth,
		/// <summary>
		/// The year and month in numeric form for tabular display (fixed-length).
		/// </summary>
		YearMonthTab = FragmentYear | FragmentMonthTab,
		/// <summary>
		/// The year and month in abbreviated form.
		/// </summary>
		YearMonthAbbr = FragmentYear | FragmentMonthAbbr,
		/// <summary>
		/// The year and month in full.
		/// </summary>
		YearMonthLong = FragmentYear | FragmentMonthLong,
		/// <summary>
		/// The year, month and day in numeric form.
		/// </summary>
		YearMonthDay = FragmentYear | FragmentMonth | FragmentDay,
		/// <summary>
		/// The year, month and day in numeric form for tabular display (fixed-length).
		/// </summary>
		YearMonthDayTab = FragmentYear | FragmentMonthTab | FragmentDayTab,
		/// <summary>
		/// The year, month and day in abbreviated form.
		/// </summary>
		YearMonthDayAbbr = FragmentYear | FragmentMonthAbbr | FragmentDay,
		/// <summary>
		/// The year, month and day in full.
		/// </summary>
		YearMonthDayLong = FragmentYear | FragmentMonthLong | FragmentDay,
		/// <summary>
		/// The month alone in numeric form.
		/// </summary>
		Month = FragmentMonth,
		/// <summary>
		/// The month alone in numeric form for tabular display (fixed-length).
		/// </summary>
		MonthTab = FragmentMonthTab,
		/// <summary>
		/// The month alone in abbreviated form.
		/// </summary>
		MonthAbbr = FragmentMonthAbbr,
		/// <summary>
		/// The month alone in full.
		/// </summary>
		MonthLong = FragmentMonthLong,
		/// <summary>
		/// The month and day in numeric form.
		/// </summary>
		MonthDay = FragmentMonth | FragmentDay,
		/// <summary>
		/// The month and day in numeric form for tabular display (fixed-length).
		/// </summary>
		MonthDayTab = FragmentMonthTab | FragmentDayTab,
		/// <summary>
		/// The month and day in abbreviated form.
		/// </summary>
		MonthDayAbbr = FragmentMonthAbbr | FragmentDay,
		/// <summary>
		/// The month and day in full.
		/// </summary>
		MonthDayLong = FragmentMonthLong | FragmentDay,
		/// <summary>
		/// The day alone.
		/// </summary>
		Day = FragmentDay,
		/// <summary>
		/// The day alone for tabular display (fixed-length).
		/// </summary>
		DayTab = FragmentDayTab,
		/// <summary>
		/// The year and quarter.
		/// </summary>
		YearQuarter = FragmentYear | FragmentQuarter,
		/// <summary>
		/// The quarter alone.
		/// </summary>
		Quarter = FragmentQuarter,

		/// <summary>
		/// The time of day with hour, minute, second and millisecond.
		/// </summary>
		HourMinuteSecondMs = FragmentHour | FragmentMinute | FragmentSecond | FragmentMs,
		/// <summary>
		/// The time of day with hour, minute, second and millisecond for tabular display (fixed-length).
		/// </summary>
		HourMinuteSecondMsTab = FragmentHourTab | FragmentMinute | FragmentSecond | FragmentMs,
		/// <summary>
		/// The time of day with hour, minute and second.
		/// </summary>
		HourMinuteSecond = FragmentHour | FragmentMinute | FragmentSecond,
		/// <summary>
		/// The time of day with hour, minute and second for tabular display (fixed-length).
		/// </summary>
		HourMinuteSecondTab = FragmentHourTab | FragmentMinute | FragmentSecond,
		/// <summary>
		/// The time of day with hour and minute.
		/// </summary>
		HourMinute = FragmentHour | FragmentMinute,
		/// <summary>
		/// The time of day with hour and minute for tabular display (fixed-length).
		/// </summary>
		HourMinuteTab = FragmentHourTab | FragmentMinute,
		/// <summary>
		/// The time of day with the hour alone.
		/// </summary>
		Hour = FragmentHour,
		/// <summary>
		/// The time of day with the hour alone for tabular display (fixed-length).
		/// </summary>
		HourTab = FragmentHourTab,
	}

	#endregion Date and time format enumeration
}
