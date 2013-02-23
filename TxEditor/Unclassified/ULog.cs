#define WPF

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.Diagnostics;

namespace Unclassified
{
	static class ULog
	{
		private static object lockObject = new object();
		private static bool processExitHandlerSet;

		private static StreamWriter logWriter;

		private static string logFileName;
		public static string LogFileName
		{
			get
			{
				lock (lockObject)
				{
					if (logFileName == null)
					{
						// Automatically set a log file name
						logFileName = "ULog.log";
					}
					return logFileName;
				}
			}
			set
			{
				lock (lockObject)
				{
					if (value != logFileName)
					{
						if (logWriter != null)
						{
							logWriter.Close();
							logWriter = null;
						}

						logFileName = value;
					}
				}
			}
		}

		private static void EnsureLogWriter()
		{
			lock (lockObject)
			{
				if (logWriter == null)
				{
					logWriter = new StreamWriter(LogFileName, true);
					if (!processExitHandlerSet)
					{
						AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
						processExitHandlerSet = true;
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
			lock (lockObject)
			{
				// Close the log file if it is open
				if (logWriter != null)
				{
					logWriter.Close();
					logWriter = null;
				}
			}
		}

		public static void Write(string message)
		{
			lock (lockObject)
			{
				EnsureLogWriter();
				DateTime now = DateTime.Now;
				logWriter.WriteLine(now.ToString("yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture) + "  " + message);
				logWriter.Flush();
			}
		}

#if WPF

		public static void SetDataBindingLog()
		{
			// Enable external tracing if not in Visual Studio
			if (!Debugger.IsAttached)
				PresentationTraceSources.Refresh();
			// Switch the tracing level back on
			// (This is Warning in the debugger and Off anywhere else by default.)
			PresentationTraceSources.DataBindingSource.Switch.Level = SourceLevels.Verbose /*| SourceLevels.ActivityTracing*/;

			// Add log listener
			LogTraceListener listener = new LogTraceListener("DataBinding");
			PresentationTraceSources.DataBindingSource.Listeners.Add(listener);
		}

		private class LogTraceListener : DefaultTraceListener
		{
			private StringBuilder sb = new StringBuilder();
			private string prefix;

			public LogTraceListener(string prefix)
			{
				this.prefix = prefix;
			}

			public override void Write(string message)
			{
				sb.Append(message);
			}

			public override void WriteLine(string message)
			{
				sb.Append(message);
				string completeMessage = sb.ToString();
				sb.Length = 0;
				ULog.Write(prefix + ": " + completeMessage);
			}
		}

#endif // WPF
	}
}
