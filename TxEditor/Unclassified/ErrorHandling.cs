#define WPF
//#define WINFORMS

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
#if WINFORMS
using System.Windows.Forms;
#elif WPF
using System.Windows;
#endif

namespace Unclassified
{
	public class ErrorHandling
	{
		#region Exception reporting functions
		/*
		Usage for Windows Forms:

		[STAThread]
		static void Main()
		{
			// MOD: Insert these 8 lines for global exception handling:
			//      (leave out the first 2 method calls for non-Windows-Forms applications)
#if !DEBUG
			// Add the event handler for handling UI thread exceptions to the event.
			Application.ThreadException += new System.Threading.ThreadExceptionEventHandler(Application_ThreadException);
			// Set the unhandled exception mode to force all Windows Forms errors to go through our handler.
			Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
			// Add the event handler for handling non-UI thread exceptions to the event.
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
#endif

			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Form1());
		}

		// MOD: Insert these 2 methods for global exception handling:
		//      (leave out the first method for non-Windows-Forms applications)
		static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
		{
			ErrorHandling.ReportException(e.Exception, "Globale Ausnahmebehandlung, ThreadException");
		}

		static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			ErrorHandling.ReportException(e.ExceptionObject as Exception, "Globale Ausnahmebehandlung, UnhandledException");
		}
		
		_____________________________________________________________
		
		Usage for WPF (in App.xaml.cs):

		public App()
		{
#if !DEBUG
			DispatcherUnhandledException += App_DispatcherUnhandledException;
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
#endif
		}

		private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			ErrorHandling.ReportException(e.Exception, "Globale Ausnahmebehandlung, DispatcherUnhandledException");
			e.Handled = true;
		}

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			ErrorHandling.ReportException(e.ExceptionObject as Exception, "Globale Ausnahmebehandlung, UnhandledException");
		}
		*/

		private static string errorFileName = null;
		private static object errorLock = new object();

		/// <summary>
		/// Gets or sets the custom location of the error report file.
		/// Relative file names are appended to the application directory.
		/// Directory paths will get the default file name appended.
		/// </summary>
		public static string ErrorFileName
		{
			get
			{
				lock (errorLock)
				{
					return errorFileName;
				}
			}
			set
			{
				lock (errorLock)
				{
					errorFileName = value;
				}
			}
		}

		/// <summary>
		/// Handle an Exception (e.g. by displaying on screen)
		/// </summary>
		/// <param name="ex">The Exception to handle. null will be ignored.</param>
		public static void ReportException(Exception ex)
		{
			ReportException(ex, "", true);
		}

		/// <summary>
		/// Handle an Exception (e.g. by displaying on screen)
		/// </summary>
		/// <param name="ex">The Exception to handle. null will be ignored.</param>
		/// <param name="context">A short description of the context in which the Exception was first caught</param>
		public static void ReportException(Exception ex, string context)
		{
			ReportException(ex, context, true);
		}

		/// <summary>
		/// Handle an Exception (e.g. by displaying on screen)
		/// </summary>
		/// <param name="ex">The Exception to handle. null will be ignored.</param>
		/// <param name="context">A short description of the context in which the Exception was first caught</param>
		/// <param name="displayMessage">Display a message on the screen. Set false to only have the error logged but not displayed.</param>
		public static void ReportException(Exception ex, string context, bool displayMessage)
		{
			if (ex == null) return;

			DateTime appStartTime = Process.GetCurrentProcess().StartTime;

			// Write diagnostic data on the disk
			string report = MyEnvironment.AssemblyProduct + " error report\n";
			report += "Report time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz") + "\n";
			report += "App started: " + appStartTime.ToString("yyyy-MM-dd HH:mm:ss.fff zzz") +
				" (up " + TimeSpanToString(DateTime.Now - appStartTime) + ")" + "\n";
			report += "System started: " + DateTime.Now.AddMilliseconds(-Environment.TickCount).ToString("yyyy-MM-dd HH:mm:ss zzz") +
				" (up " + TimeSpanToString(TimeSpan.FromMilliseconds(Environment.TickCount)) + ")" + "\n";
			report += "App version: " + MyEnvironment.AssemblyFileVersion +
				" (" + MyEnvironment.AssemblyInformationalVersion + ")\n";
			report += "CLR version: " + Environment.Version + "\n";
			report += "Process bits: " + (MyEnvironment.Is64BitProcess ? "64" : "32") + "\n";
			report += "OS version: " + MyEnvironment.OSName + " " + (MyEnvironment.Is64BitOS ? "x64" : "x86") +
				" Build " + MyEnvironment.OSBuild + " " + MyEnvironment.OSServicePackString + "\n";
			report += "OS product: " + MyEnvironment.OSProductName + "\n";
			report += "CPUs: " + Environment.ProcessorCount + "\n";
			report += "Hostname: " + Environment.MachineName + "\n";
			report += "Username: " + Environment.UserDomainName + "\\" + Environment.UserName + "\n";
			report += "Shutdown: " + (Environment.HasShutdownStarted ? "yes" : "no") + "\n";
			report += "Thread: " + Thread.CurrentThread.ManagedThreadId + ", " +
				"\"" + Thread.CurrentThread.Name + "\"" +
				(Thread.CurrentThread.IsBackground ? " (background)" : "") +
				(Thread.CurrentThread.IsThreadPoolThread ? " (thread pool)" : "") + "\n";
			if (context != null && context.Length > 0)
			{
				report += "Context: " + context + "\n";
			}
			report += ExceptionToString(ex);
			report += "________________________________________________________________________________\n\n";
			// Find appropriate place to write the file
			bool log = false;
			string errorfile = "";
#if WINFORMS
			string appPath = Application.ExecutablePath;
#elif WPF
			string appPath = Assembly.GetEntryAssembly().Location;
#endif
			try
			{
				if (string.IsNullOrEmpty(errorFileName)) throw new Exception();   // Just skip this
				errorfile = errorFileName;
				if (!Path.IsPathRooted(errorfile))
				{
					errorfile = Path.Combine(Path.GetDirectoryName(appPath), errorfile);
				}
				if (Directory.Exists(errorfile))
				{
					errorfile = Path.Combine(
						errorfile,
						Path.GetFileNameWithoutExtension(appPath) + "." + DateTime.Now.ToString("yyyy-MM") + ".errors");
				}
				AppendToFile(errorfile, report);
				log = true;
			}
			catch
			{
				try
				{
					// [Application path]\[Application name].errors
					errorfile = Path.GetDirectoryName(appPath) + "\\" +
						Path.GetFileNameWithoutExtension(appPath) + "." + DateTime.Now.ToString("yyyy-MM") + ".errors";
					AppendToFile(errorfile, report);
					log = true;
				}
				catch
				{
					try
					{
						// [My Documents]\[Application name].errors
						errorfile = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" +
							Path.GetFileNameWithoutExtension(appPath) + "." + DateTime.Now.ToString("yyyy-MM") + ".errors";
						AppendToFile(errorfile, report);
						log = true;
					}
					catch
					{
					}
				}
			}

			if (displayMessage)
			{
				string msg = "Es ist ein Fehler aufgetreten, die Anwendung kann möglicherweise nicht korrekt fortgesetzt werden. " +
					"Drücken Sie auf „OK“, um das Programm fortzusetzen, oder „Abbrechen“, um das Programm zu beenden. " +
					"Falls Sie die Ausführung fortsetzen, können weitere Fehler oder Störungen auftreten.\n\n";
				if (!string.IsNullOrEmpty(context)) msg += "• Kontext: " + context + "\n";
#if DEBUG
				msg += ex.ToString().TrimEnd() + "\n\n";
#else
				msg += "• Fehlermeldung: " + ex.Message + " (" + ex.GetType().Name + ")\n";
				if (ex.InnerException != null)
				{
					msg += "• Innere Fehlermeldung: " + ex.InnerException.Message + " (" + ex.InnerException.GetType().Name + ")\n";
				}
				msg += "\n";
#endif
				if (log)
					msg += "Ein ausführlicher Fehlerbericht wurde in der Datei " + errorfile + " gespeichert.\n\n";
				else
					msg += "Beim Speichern eines ausführlichen Fehlerberichts ist ein Fehler aufgetreten.\n\n";
				msg += "Falls das Problem weiterhin bestehen sollte, wenden Sie sich bitte an den Programmentwickler.";
#if WINFORMS
				if (MessageBox.Show(msg, "Ausnahmefehler", MessageBoxButtons.OKCancel, MessageBoxIcon.Error) == DialogResult.Cancel)
				{
					//Application.Exit();
					Environment.Exit(1);
				}
#elif WPF
				if (MessageBox.Show(msg, "Ausnahmefehler", MessageBoxButton.OKCancel, MessageBoxImage.Error) == MessageBoxResult.Cancel)
				{
					//Application.Current.Shutdown();
					Environment.Exit(1);   // This is faster
				}
#endif
			}
		}

		private static string ExceptionToString(Exception ex)
		{
			if (ex == null)
				return "null\n";

			string report = "";
			report += "Exception: " + ex.GetType().ToString() + "\n";
			report += "Message: " + ex.Message.TrimEnd() + "\n";
			report += "Stack:\n";
			if (string.IsNullOrWhiteSpace(ex.StackTrace))
				report += "   (empty)\n";
			else
				report += ex.StackTrace + "\n";
			if (ex is ExternalException)   // e.g. COMException
				report += "ErrorCode: " + (ex as ExternalException).ErrorCode.ToString("X8") + "h\n";
			if (ex.Data != null)
				foreach (DictionaryEntry x in ex.Data)
					report += "Data." + x.Key + (x.Value != null ? " (" + x.Value.GetType().Name + "): " + x.Value.ToString() : ": null") + "\n";

			// Find more properties through reflection
			PropertyInfo[] props = ex.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
			foreach (PropertyInfo prop in props)
			{
				// Known properties, already handled
				if (prop.Name == "Message") continue;
				if (prop.Name == "StackTrace") continue;
				if (prop.Name == "ErrorCode") continue;
				if (prop.Name == "Data") continue;
				if (prop.Name == "InnerException") continue;
				if (prop.Name == "InnerExceptions") continue;
				if (prop.Name == "TargetSite") continue;
				if (prop.Name == "HelpLink") continue;
				if (prop.Name == "Source") continue;

				try
				{
					object value = prop.GetValue(ex, null);   // Indexed properties are not supported here!
					report += "Property." + prop.Name + (value != null ? " (" + value.GetType().Name + "): " + value.ToString() : ": null") + "\n";
				}
				catch (Exception ex2)
				{
					report += "Exception property \"" + prop.Name + "\" cannot be retrieved. (" + ex2.GetType().Name + ": " + ex2.Message + ")\n";
				}
			}

			AggregateException aggEx = ex as AggregateException;
			if (aggEx != null)
			{
				int count = 1;
				foreach (var innerEx in aggEx.InnerExceptions)
				{
					report += "--- InnerException #" + count++ + ": ---\n";
					report += ExceptionToString(ex.InnerException);
				}
			}
			else if (ex.InnerException != null)
			{
				report += "--- InnerException: ---\n";
				report += ExceptionToString(ex.InnerException);
			}
			return report;
		}

		private static string TimeSpanToString(TimeSpan ts)
		{
			StringBuilder sb = new StringBuilder();
			if (ts.Days > 0) sb.Append(ts.Days + "d ");
			sb.Append(ts.Hours.ToString("00") + ":" + ts.Minutes.ToString("00") + ":" + ts.Seconds.ToString("00"));
			if (ts.Milliseconds > 0) sb.Append("." + ts.Milliseconds.ToString("000"));
			return sb.ToString();
		}

		/// <summary>
		/// Append a string to a file
		/// </summary>
		/// <param name="filename">Filename</param>
		/// <param name="content">Content to append. Line endings are fixed automatically.</param>
		public static void AppendToFile(string filename, string content)
		{
			int retry = 5;
			Exception myex = null;

			content = content.Replace("\r", "").Replace("\n", Environment.NewLine);

			while (retry > 0)
			{
				try
				{
					StreamWriter sw = new StreamWriter(filename, true, Encoding.UTF8);
					sw.Write(content);
					sw.Close();
					return;
				}
				catch (Exception ex)
				{
					myex = ex;
				}
				retry--;
				Thread.Sleep(100);
			}
			if (myex != null)
				throw myex;
		}
		#endregion Exception reporting functions

		#region Tracing function
		private static bool traceToFile = false;
		private static string traceFileName = null;
		private static object traceLock = new object();

		public static bool TraceToFile
		{
			get
			{
				lock (traceLock)
				{
					return traceToFile;
				}
			}
			set
			{
				lock (traceLock)
				{
					traceToFile = value;
				}
			}
		}

		public static string TraceFileName
		{
			get
			{
				lock (traceLock)
				{
					return traceFileName;
				}
			}
			set
			{
				lock (traceLock)
				{
					traceFileName = value;
				}
			}
		}
		
		public static void TraceLine(params object[] args)
		{
			lock (traceLock)
			{
				string msg = "";
				DateTime now = DateTime.Now;
				foreach (object arg in args)
				{
					msg += arg.ToString();
				}
				Trace.WriteLine(now.ToString(@"HH:mm:ss.fff") + " " + msg);
				msg = now.ToString(@"yyyy-MM-dd\THH:mm:ss.fffK") + " " + msg + Environment.NewLine;

				if (TraceToFile)
				{
					string tracefile;
#if WINFORMS
					string appPath = Application.ExecutablePath;
#elif WPF
					string appPath = Assembly.GetEntryAssembly().Location;
#endif
					try
					{
						if (string.IsNullOrEmpty(traceFileName)) throw new Exception();   // Just skip this
						tracefile = traceFileName;
						if (!Path.IsPathRooted(tracefile))
							tracefile = Path.Combine(Path.GetDirectoryName(appPath), tracefile);
						AppendToFile(tracefile, msg);
					}
					catch
					{
						try
						{
							// [Application path]\[Application name].trace
							tracefile = Path.GetDirectoryName(appPath) + "\\" +
								Path.GetFileNameWithoutExtension(appPath) + ".trace";
							AppendToFile(tracefile, msg);
						}
						catch
						{
							try
							{
								// [My Documents]\[Application name].trace
								tracefile = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\" +
									Path.GetFileNameWithoutExtension(appPath) + ".trace";
								AppendToFile(tracefile, msg);
							}
							catch
							{
								// Just don't write anything if it doesn't work
							}
						}
					}
				}
			}
		}
		#endregion Tracing function
	}
}
