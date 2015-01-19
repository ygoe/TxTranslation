using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Unclassified.FieldLog;
using Unclassified.TxEditor.ViewModels;
using Unclassified.TxEditor.Views;
using Unclassified.Util;

namespace Unclassified.TxEditor
{
	public partial class App : Application
	{
		public static SplashScreen SplashScreen { get; set; }

		#region Startup

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			// Fix WPF's built-in themes
			if (OSInfo.IsWindows8OrNewer)
			{
				ReAddResourceDictionary("/Resources/RealWindows8.xaml");
			}

			// Initialise and show the main window

			CommandLineHelper cmdLine = new CommandLineHelper();
			var scanOption = cmdLine.RegisterOption("scan", 1).Alias("s");

			try
			{
				cmdLine.Parse();
			}
			catch (Exception ex)
			{
				App.ErrorMessage("Command line error.", ex, "Parsing command line");
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
				App.ErrorMessage("At least one of the files or directories specified at the command line could not be found.");
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
			//                if (App.YesNoQuestion("Other Tx dictionary files are located in the same directory as the selected file. Should they also be loaded?"))
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

		private void ReAddResourceDictionary(string url)
		{
			var resDict = Resources.MergedDictionaries.FirstOrDefault(r => r.Source.OriginalString == url);
			if (resDict != null)
			{
				Resources.MergedDictionaries.Remove(resDict);
			}
			Resources.MergedDictionaries.Add(new ResourceDictionary
			{
				Source = new Uri(url, UriKind.RelativeOrAbsolute)
			});
		}

		#endregion Startup

		#region Settings

		/// <summary>
		/// Provides properties to access the application settings.
		/// </summary>
		public static IAppSettings Settings { get; private set; }

		/// <summary>
		/// Initialises the application settings.
		/// </summary>
		public static void InitializeSettings()
		{
			if (Settings != null) return;   // Already done

			Settings = SettingsAdapterFactory.New<IAppSettings>(
				new FileSettingsStore(
					SettingsHelper.GetAppDataPath(@"Unclassified\TxTranslation", "TxEditor.conf")));

			// Update settings format from old version
			FL.TraceData("LastStartedAppVersion", Settings.LastStartedAppVersion);
			if (string.IsNullOrEmpty(Settings.LastStartedAppVersion))
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
			Settings.LastStartedAppVersion = FL.AppVersion;
		}

		#endregion Settings

		#region Message dialog methods

		internal static string MessageBoxTitle = "FieldLogViewer";
		internal static string UnexpectedError = "An unexpected error occured.";
		internal static string DetailsLogged = "Details are written to the error log file.";

		public static void InformationMessage(string message)
		{
			FL.Info(message);
			MessageBox.Show(message, MessageBoxTitle, MessageBoxButton.OK, MessageBoxImage.Information);
		}

		public static void WarningMessage(string message)
		{
			FL.Warning(message);
			MessageBox.Show(message, MessageBoxTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
		}

		public static void WarningMessage(string message, Exception ex, string context)
		{
			FL.Warning(ex, context);
			string exMsg = ex.Message;
			var aex = ex as AggregateException;
			if (aex != null && aex.InnerExceptions.Count == 1)
			{
				exMsg = aex.InnerExceptions[0].Message;
			}
			if (message == null)
			{
				message = UnexpectedError;
			}
			MessageBox.Show(
				message + " " + exMsg,
				MessageBoxTitle,
				MessageBoxButton.OK,
				MessageBoxImage.Warning);
		}

		public static void ErrorMessage(string message)
		{
			FL.Error(message);
			MessageBox.Show(message, MessageBoxTitle, MessageBoxButton.OK, MessageBoxImage.Error);
		}

		public static void ErrorMessage(string message, Exception ex, string context)
		{
			FL.Error(ex, context);
			string exMsg = ex.Message;
			var aex = ex as AggregateException;
			if (aex != null && aex.InnerExceptions.Count == 1)
			{
				exMsg = aex.InnerExceptions[0].Message;
			}
			if (message == null)
			{
				message = UnexpectedError;
			}
			MessageBox.Show(
				message + " " + exMsg + "\n\n" + DetailsLogged,
				MessageBoxTitle,
				MessageBoxButton.OK,
				MessageBoxImage.Error);
		}

		public static bool YesNoQuestion(string message)
		{
			FL.Trace(message);
			var result = MessageBox.Show(message, MessageBoxTitle, MessageBoxButton.YesNo, MessageBoxImage.Question);
			FL.Trace("Answer: " + result);
			return result == MessageBoxResult.Yes;
		}

		public static bool YesNoInformation(string message)
		{
			FL.Info(message);
			var result = MessageBox.Show(message, MessageBoxTitle, MessageBoxButton.YesNo, MessageBoxImage.Information);
			FL.Trace("Answer: " + result);
			return result == MessageBoxResult.Yes;
		}

		public static bool YesNoWarning(string message)
		{
			FL.Warning(message);
			var result = MessageBox.Show(message, MessageBoxTitle, MessageBoxButton.YesNo, MessageBoxImage.Warning);
			FL.Trace("Answer: " + result);
			return result == MessageBoxResult.Yes;
		}

		public static bool? YesNoCancelQuestion(string message)
		{
			FL.Trace(message);
			var result = MessageBox.Show(message, MessageBoxTitle, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
			FL.Trace("Answer: " + result);
			if (result == MessageBoxResult.Yes) return true;
			if (result == MessageBoxResult.No) return false;
			return null;
		}

		#endregion Message dialog methods
	}
}
