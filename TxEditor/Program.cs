using System;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using Unclassified.FieldLog;
using Unclassified.TxLib;
using Unclassified.Util;

namespace Unclassified.TxEditor
{
	internal class Program
	{
		/// <summary>
		/// Application entry point.
		/// </summary>
		/// <remarks>
		/// The App class is set to the build action "ApplicationDefinition" which also generates a
		/// Main method suitable as application entry point. Therefore, this class must be selected
		/// as start object in the project configuration. If the App class was set up otherwise,
		/// Visual Studio would not find the application-wide resources in the App.xaml file and
		/// mark all such StaticResource occurences in XAML files as an error.
		/// </remarks>
		[STAThread]
		public static void Main()
		{
			// Set the image file's build action to "Resource" and "Never copy" for this to work.
			if (!Debugger.IsAttached)
			{
				App.SplashScreen = new SplashScreen("Images/TxFlag_256.png");
				App.SplashScreen.Show(false, true);
			}

			// Set up FieldLog
			FL.AcceptLogFileBasePath();
			FL.RegisterPresentationTracing();
			TaskHelper.UnhandledTaskException = ex => FL.Critical(ex, "TaskHelper.UnhandledTaskException", true);

			// Keep the setup away
			GlobalMutex.Create("Unclassified.TxEditor");

			App.InitializeSettings();

			// Make sure the settings are properly saved in the end
			AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

			// Setup logging
			//Tx.LogFileName = "tx.log";
			//Tx.LogFileName = "";
			//Environment.SetEnvironmentVariable("TX_LOG_UNUSED", "1", EnvironmentVariableTarget.User);
			//Environment.SetEnvironmentVariable("TX_LOG_UNUSED", null, EnvironmentVariableTarget.User);

			InitializeLocalisation();

			App app = new App();
			app.InitializeComponent();
			app.Run();
		}

		/// <summary>
		/// Called when the current process exits.
		/// </summary>
		/// <remarks>
		/// The processing time in this event is limited. All handlers of this event together must
		/// not take more than ca. 3 seconds. The processing will then be terminated.
		/// </remarks>
		private static void CurrentDomain_ProcessExit(object sender, EventArgs args)
		{
			if (App.Settings != null)
			{
				App.Settings.SettingsStore.Dispose();
			}
		}

		private static void InitializeLocalisation()
		{
			// Setup translation data
			try
			{
				// Set the XML file's build action to "Embedded Resource" and "Never copy" for this to work.
				Tx.LoadFromEmbeddedResource("Unclassified.TxEditor.Dictionary.txd");
			}
			catch (ArgumentException)
			{
				// The file was not embedded, try reading the file. This enables file change notifications.
				Tx.UseFileSystemWatcher = true;
				Tx.LoadFromXmlFile("Dictionary.txd");
			}

			string appCulture = App.Settings.AppCulture;
			if (!string.IsNullOrWhiteSpace(appCulture))
			{
				try
				{
					Tx.SetCulture(appCulture);
				}
				catch (Exception ex)
				{
					App.ErrorMessage("The configured application UI culture cannot be set.", ex, "Setting application culture from configuration");
				}
			}

			// FieldLog application error dialog localisation
			FL.AppErrorDialogTitle = Tx.T("fieldlog.AppErrorDialogTitle");
			FL.AppErrorDialogContinuable = Tx.T("fieldlog.AppErrorDialogContinuable");
			FL.AppErrorDialogTerminating = Tx.T("fieldlog.AppErrorDialogTerminating");
			FL.AppErrorDialogContext = Tx.T("fieldlog.AppErrorDialogContext");
			FL.AppErrorDialogLogPath = Tx.T("fieldlog.AppErrorDialogLogPath");
			FL.AppErrorDialogNoLog = Tx.T("fieldlog.AppErrorDialogNoLog");
			FL.AppErrorDialogConsoleAction = Tx.T("fieldlog.AppErrorDialogConsoleAction");
			FL.AppErrorDialogTimerNote = Tx.T("fieldlog.AppErrorDialogTimerNote");
			FL.AppErrorDialogDetails = Tx.T("fieldlog.AppErrorDialogDetails");
			FL.AppErrorDialogSendLogs = Tx.T("fieldlog.AppErrorDialogSendLogs");
			FL.AppErrorDialogNext = Tx.T("fieldlog.AppErrorDialogNext");
			FL.AppErrorDialogTerminate = Tx.T("fieldlog.AppErrorDialogTerminate");
			FL.AppErrorDialogContinue = Tx.T("fieldlog.AppErrorDialogContinue");

			// Common error message box localisation
			App.UnexpectedError = Tx.T("msg.error.unexpected error");
			App.DetailsLogged = Tx.T("msg.error.details logged");
		}
	}
}
