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
		#region Static properties

		private static SplashScreen splashScreen;
		/// <summary>
		/// Gets the application splash screen.
		/// </summary>
		public static SplashScreen SplashScreen { get { return splashScreen; } }

		#endregion Static properties

		#region Application entry point

		/// <summary>
		/// Application entry point.
		/// </summary>
		[STAThread]
		public static void Main()
		{
			// Set up FieldLog
			FL.AcceptLogFileBasePath();
			FL.RegisterPresentationTracing();
			TaskHelper.UnhandledTaskException = ex => FL.Critical(ex, "TaskHelper.UnhandledTaskException", true);

			// Keep the setup away
			GlobalMutex.Create("Unclassified.TxEditor");

			// Set the image file's build action to "Resource" and "Never copy" for this to work.
			if (!Debugger.IsAttached)
			{
				splashScreen = new SplashScreen("Images/TxFlag_256.png");
				splashScreen.Show(false, true);
			}

			App.InitializeSettings();

			// Make sure the settings are properly saved in the end
			AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

			// Setup logging
			//Tx.LogFileName = "tx.log";
			//Tx.LogFileName = "";
			//Environment.SetEnvironmentVariable("TX_LOG_UNUSED", "1", EnvironmentVariableTarget.User);
			//Environment.SetEnvironmentVariable("TX_LOG_UNUSED", null, EnvironmentVariableTarget.User);

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
					FL.Error(ex, "Setting application culture from configuration");
					MessageBox.Show("The configured application UI culture cannot be set.\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
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

			App app = new App();
			app.InitializeComponent();
			app.Run();
		}

		#endregion Application entry point

		#region Event handlers

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

		#endregion Event handlers
	}
}
