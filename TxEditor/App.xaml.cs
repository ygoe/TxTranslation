using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using Unclassified.FieldLog;
using Unclassified.TxEditor.ViewModels;
using Unclassified.TxEditor.Views;
using Unclassified.TxLib;
using Unclassified.Util;

namespace Unclassified.TxEditor
{
	public partial class App : Application
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

			InitializeSettings();

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

			string appCulture = Settings.AppCulture;
			if (!string.IsNullOrWhiteSpace(appCulture))
			{
				try
				{
					Tx.SetCulture(appCulture);
				}
				catch (Exception ex)
				{
					FL.Error(ex, "Setting application culture from configuration");
					//MessageBox.Show("Error settings configured application UI culture.\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					// Doing this leads to a XAML exception in the main window! o_O
					// TODO: Does it work now outside of the App constructor?
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

		#region Constructors

		public App()
		{
		}

		#endregion Constructors

		#region Startup

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			// Initialise and show the main window

			CommandLineHelper cmdLine = new CommandLineHelper();
			var scanOption = cmdLine.RegisterOption("scan", 1).Alias("s");

			try
			{
				cmdLine.Parse();
			}
			catch (Exception ex)
			{
				FL.Error(ex, "Parsing command line");
				MessageBox.Show("Command line error: " + ex.Message, "TxEditor", MessageBoxButton.OK, MessageBoxImage.Error);
				Application.Current.Shutdown();
			}

			List<string> filesToLoad = new List<string>();
			bool error = false;
			foreach (string fileNameArg in cmdLine.FreeArguments)
			{
				if (!string.IsNullOrWhiteSpace(fileNameArg))
				{
					// File name
					if (File.Exists(fileNameArg))
					{
						// File exists, open it
						string fileName = fileNameArg;
						if (!Path.IsPathRooted(fileName))
						{
							fileName = Path.GetFullPath(fileName);
						}
						filesToLoad.Add(fileName);
					}
					else if (Directory.Exists(fileNameArg))
					{
						// Directory specified, collect all files
						foreach (string fileName in Directory.GetFiles(fileNameArg, "*.txd"))
						{
							filesToLoad.Add(fileName);
						}
						if (filesToLoad.Count == 0)
						{
							// Nothing found, try older XML file names
							foreach (string fileName in Directory.GetFiles(fileNameArg, "*.xml"))
							{
								if (FileNameHelper.GetCulture(fileName) != null)
								{
									filesToLoad.Add(fileName);
								}
							}
						}
					}
					else
					{
						FL.Error("File/directory not found", fileNameArg);
						error = true;
					}
				}
			}
			if (error)
			{
				MessageBox.Show("At least one of the files or directories specified at the command line could not be found.", "TxEditor", MessageBoxButton.OK, MessageBoxImage.Error);
			}

			// Scan for other files near the selected files
			// (Currently only active if a single file is specified)
			//if (filesToLoad.Count == 1)
			//{
			//    foreach (string fileName in filesToLoad.Distinct().ToArray())
			//    {
			//        if (fileName.ToLowerInvariant().EndsWith(".txd") && File.Exists(fileName))
			//        {
			//            // Existing .txd file
			//            // Scan same directory for other .txd files
			//            string[] otherFiles = Directory.GetFiles(Path.GetDirectoryName(fileName), "*.txd");
			//            // otherFiles should contain fileName and may contain additional files
			//            if (otherFiles.Length > 1)
			//            {
			//                if (MessageBox.Show(
			//                    "Other Tx dictionary files are located in the same directory as the selected file. Should they also be loaded?",
			//                    "Load other files",
			//                    MessageBoxButton.YesNo,
			//                    MessageBoxImage.Question) == MessageBoxResult.Yes)
			//                {
			//                    // Duplicates will be removed later
			//                    filesToLoad.AddRange(otherFiles);
			//                }
			//            }
			//        }
			//    }
			//}
			// NOTE: Loading multiple txd files is not supported. (Only scan for more cultures of version 1 files. - Done)

			if (!FileNameHelper.FindOtherCultures(filesToLoad))
				Application.Current.Shutdown();

			// Create main window and view model
			var view = new MainWindow();
			var viewModel = new MainViewModel();

			if (filesToLoad.Count == 0 && scanOption.IsSet)
			{
				viewModel.ScanDirectory = scanOption.Value;
			}

			view.DataContext = viewModel;

			// Load selected files
			if (filesToLoad.Count > 0)
			{
				viewModel.LoadFiles(filesToLoad);
			}

			// Show the main window
			view.Show();
		}

		#endregion Startup

		#region Settings

		/// <summary>
		/// Provides properties to access the application settings.
		/// </summary>
		public static IAppSettings Settings { get; private set; }

		private static void InitializeSettings()
		{
			Settings = SettingsAdapterFactory.New<IAppSettings>(
				new FileSettingsStore(
					SettingsHelper.GetAppDataPath(@"Unclassified\TxTranslation", "TxEditor.conf")));

			// Update settings format from old version
			if (string.IsNullOrEmpty(App.Settings.LastStartedAppVersion))
			{
				Settings.SettingsStore.Rename("app-culture", "AppCulture");
				Settings.SettingsStore.Rename("file.ask-save-upgrade", "File.AskSaveUpgrade");
				Settings.SettingsStore.Rename("input.charmap", "Input.CharacterMap");
				Settings.SettingsStore.Rename("view.comments", "View.ShowComments");
				Settings.SettingsStore.Rename("view.monospace-font", "View.MonospaceFont");
				Settings.SettingsStore.Rename("view.hidden-chars", "View.ShowHiddenChars");
				Settings.SettingsStore.Rename("view.charmap", "View.ShowCharacterMap");
				Settings.SettingsStore.Rename("view.font-scale", "View.FontScale");
				Settings.SettingsStore.Rename("view.native-culture-names", "View.NativeCultureNames");
				Settings.SettingsStore.Rename("view.suggestions", "View.ShowSuggestions");
				Settings.SettingsStore.Rename("view.suggestions.horizontal-layout", "View.SuggestionsHorizontalLayout");
				Settings.SettingsStore.Rename("view.suggestions.width", "View.SuggestionsWidth");
				Settings.SettingsStore.Rename("view.suggestions.height", "View.SuggestionsHeight");
				Settings.SettingsStore.Rename("wizard.source-code", "Wizard.SourceCode");
				Settings.SettingsStore.Rename("wizard.remember-location", "Wizard.RememberLocation");
				Settings.SettingsStore.Rename("wizard.hotkey-in-visual-studio-only", "Wizard.HotkeyInVisualStudioOnly");
				Settings.SettingsStore.Rename("window.left", "View.MainWindowState.Left");
				Settings.SettingsStore.Rename("window.top", "View.MainWindowState.Top");
				Settings.SettingsStore.Rename("window.width", "View.MainWindowState.Width");
				Settings.SettingsStore.Rename("window.height", "View.MainWindowState.Height");
				Settings.View.MainWindowState.IsMaximized = Settings.SettingsStore.GetInt("window.state") == 2;
				Settings.SettingsStore.Remove("window.state");
				Settings.SettingsStore.Rename("wizard.window.left", "Wizard.WindowLeft");
				Settings.SettingsStore.Rename("wizard.window.top", "Wizard.WindowTop");
			}

			// Remember the version of the application.
			// If we need to react on settings changes from previous application versions, here is
			// the place to check the version currently in the settings, before it's overwritten.
			App.Settings.LastStartedAppVersion = FL.AppVersion;
		}

		#endregion Settings

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
			if (Settings != null)
			{
				Settings.SettingsStore.Dispose();
			}
		}

		#endregion Event handlers
	}
}
