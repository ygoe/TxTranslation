using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using TxEditor.ViewModel;
using Unclassified;
using TxEditor.View;

namespace TxEditor
{
	public partial class App : Application
	{
		#region Static properties

		/// <summary>
		/// Gets the Settings instance used by the application.
		/// </summary>
		public static Settings Settings { get; private set; }

		#endregion Static properties

		#region Setup detection mutex

		private Mutex appMutex = new Mutex(false, "Unclassified.TxEditor");

		#endregion Setup detection mutex

		#region Constructors

		public App()
		{
#if !DEBUG
			DispatcherUnhandledException += App_DispatcherUnhandledException;
			AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
#endif

			// Initialise the settings system
			Settings = new Settings(
			Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"Unclassified",
				"TxTranslation",
				"TxEditor.conf"));

			// Make sure the settings are properly saved in the end
			AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
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

			List<string> filesToLoad = new List<string>();
			foreach (string argv in e.Args)
			{
				if (argv.Length > 1 && argv[0] == '/')
				{
					// Option
					// (No options supported yet, ignore unknown)
				}
				else
				{
					// File name
					if (File.Exists(argv))
					{
						// File exists, open it
						string fileName = argv;
						if (!Path.IsPathRooted(fileName))
						{
							fileName = Path.GetFullPath(fileName);
						}
						filesToLoad.Add(fileName);
					}
					else if (Directory.Exists(argv))
					{
						// Directory specified, collect all files
						foreach (string fileName in Directory.GetFiles(argv, "*.txd"))
						{
							filesToLoad.Add(fileName);
						}
					}
					else if (argv.Contains('?') || argv.Contains('*'))
					{
						// File name with wildcards, find matching files
						// (Not implemented yet)
					}
					// Ignore anything else
				}
			}

			// Scan for other files near the selected files
			// (Currently only active if a single file is specified)
			if (filesToLoad.Count == 1)
			{
				foreach (string fileName in filesToLoad.Distinct().ToArray())
				{
					if (fileName.ToLowerInvariant().EndsWith(".txd") && File.Exists(fileName))
					{
						// Existing .txd file
						// Scan same directory for other .txd files
						string[] otherFiles = Directory.GetFiles(Path.GetDirectoryName(fileName), "*.txd");
						// otherFiles should contain fileName and may contain additional files
						if (otherFiles.Length > 1)
						{
							if (MessageBox.Show(
								"Other Tx dictionary files are located in the same directory as the selected file. Should they also be loaded?",
								"Load other files",
								MessageBoxButton.YesNo,
								MessageBoxImage.Question) == MessageBoxResult.Yes)
							{
								// Duplicates will be removed later
								filesToLoad.AddRange(otherFiles);
							}
						}
					}
				}
			}

			// Create main window and view model
			var view = new MainWindow();
			var viewModel = new MainWindowViewModel();
			viewModel.View = view;
			view.DataContext = viewModel;

			// Load selected files
			foreach (string fileName in filesToLoad.Distinct())
			{
				viewModel.LoadFromXmlFile(fileName);
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
