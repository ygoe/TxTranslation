using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Xml;
using Unclassified.TxEditor.View;
using Unclassified.TxEditor.ViewModel;
using Unclassified.TxLib;
using Unclassified;

namespace Unclassified.TxEditor
{
	public partial class App : Application
	{
		#region Static properties

		/// <summary>
		/// Gets the Settings instance used by the application.
		/// </summary>
		public static AppSettings Settings { get; private set; }

		private static SplashScreen splashScreen;
		/// <summary>
		/// Gets the application splash screen.
		/// </summary>
		public static SplashScreen SplashScreen { get { return splashScreen; } }

		#endregion Static properties

		#region Setup detection mutex

		private Mutex appMutex = new Mutex(false, "Unclassified.TxEditor");

		#endregion Setup detection mutex

		#region Application entry point

		/// <summary>
		/// Application entry point.
		/// </summary>
		[STAThread]
		public static void Main()
		{
			// Set the image file's build action to "Resource" and "Never copy" for this to work.
			splashScreen = new SplashScreen("Images/TxFlag_256.png");
			splashScreen.Show(false, true);

			App app = new App();
			app.InitializeComponent();
			app.Run();
		}

		#endregion Application entry point

		#region Constructors

		public App()
		{
#if !DEBUG
			DispatcherUnhandledException += App_DispatcherUnhandledException;
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
#endif

			// Initialise the settings system
			Settings = new AppSettings(
			Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"Unclassified",
				"TxTranslation",
				"TxEditor.conf"));

			// Make sure the settings are properly saved in the end
			AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

			//ULog.SetDataBindingLog();

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
				catch (Exception /*ex*/)
				{
					//MessageBox.Show("Error settings configured application UI culture.\n" + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					// Doing this leads to a XAML exception in the main window! o_O
				}
			}
		}

		#endregion Constructors

		#region Error handling

		private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
		{
			ErrorHandling.ReportException(e.Exception, "Application.DispatcherUnhandledException");
			e.Handled = true;
		}

		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
		{
			ErrorHandling.ReportException(e.ExceptionObject as Exception, "AppDomain.UnhandledException");
		}

		#endregion Error handling

		#region Startup

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			// Initialise and show the main window

			CommandLineParser clp = new CommandLineParser();

			clp.AddKnownOption("s", "scan", true);

			List<string> filesToLoad = new List<string>();
			for (int argIndex = 0; argIndex < clp.GetArgumentCount(); argIndex++)
			{
				string fileNameArg = clp.GetArgument(argIndex);
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
								Match m = Regex.Match(fileName, @"[^.]\.(([a-z]{2})([-][a-z]{2})?)\.xml$", RegexOptions.IgnoreCase);
								if (m.Success)
								{
									filesToLoad.Add(fileName);
								}
							}
						}
					}
					// Ignore anything else
				}
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
			// TODO: Loading multiple txd files is not supported. Only scan for more cultures of version 1 files.

			// Create main window and view model
			var view = new MainWindow();
			var viewModel = new MainWindowViewModel();

			if (filesToLoad.Count == 0 && clp.IsOptionSet("s"))
			{
				viewModel.ScanDirectory = clp.GetOptionValue("s");
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

		#region Event handlers

		/// <summary>
		/// Called when the current process exits.
		/// </summary>
		/// <remarks>
		/// The processing time in this event is limited. All handlers of this event together must
		/// not take more than ca. 3 seconds. The processing will then be terminated.
		/// </remarks>
		private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
		{
			if (Settings != null)
			{
				Settings.SaveNow();
				Settings.Dispose();
				Settings = null;
			}
		}

		#endregion Event handlers
	}
}
