using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Xml;
using Microsoft.Win32;
using TaskDialogInterop;
using TxEditor.View;
using TxLib;
using Unclassified;
using Unclassified.UI;

namespace TxEditor.ViewModel
{
	class MainWindowViewModel : ViewModelBase, IViewCommandSource
	{
		#region Static data

		public static MainWindowViewModel Instance { get; private set; }

		#endregion Static data

		#region Private data

		private int fileVersion;
		private string loadedFilePath;
		private string loadedFilePrefix;
		private IList selectedTextKeys;

		#endregion Private data

		#region Constructors

		public MainWindowViewModel()
		{
			Instance = this;
			
			TextKeys = new HashSet<string>();
			LoadedCultureNames = new HashSet<string>();
			DeletedCultureNames = new HashSet<string>();
			RootTextKey = new TextKeyViewModel(null, false, null, this);
			ProblemKeys = new ObservableHashSet<TextKeyViewModel>();

			SearchText = "";   // Change value once to set the clear button visibility

			FontScale = App.Settings.FontScale;
		}

		#endregion Constructors

		#region Public properties

		public HashSet<string> TextKeys { get; private set; }
		public HashSet<string> LoadedCultureNames { get; private set; }
		public HashSet<string> DeletedCultureNames { get; private set; }
		public TextKeyViewModel RootTextKey { get; private set; }
		public ObservableHashSet<TextKeyViewModel> ProblemKeys { get; private set; }

		#endregion Public properties

		#region Data properties

		public AppSettings Settings
		{
			get { return App.Settings; }
		}

		private bool fileModified;
		public bool FileModified
		{
			get { return fileModified; }
			set
			{
				if (value != fileModified)
				{
					fileModified = value;
					UpdateTitle();
					OnPropertyChanged("FileModified");
					SaveCommand.RaiseCanExecuteChanged();
				}
			}
		}

		private string primaryCulture;
		public string PrimaryCulture
		{
			get { return primaryCulture; }
			set
			{
				if (value != primaryCulture)
				{
					primaryCulture = value;
					OnPropertyChanged("PrimaryCulture");
				}
			}
		}

		private bool problemFilterActive;
		public bool ProblemFilterActive
		{
			get { return problemFilterActive; }
			set
			{
				if (value != problemFilterActive)
				{
					problemFilterActive = value;
					OnPropertyChanged("ProblemFilterActive");
					UpdateSearch();
				}
			}
		}

		private string cursorChar;
		public string CursorChar
		{
			get { return cursorChar; }
			set
			{
				if (value != cursorChar)
				{
					cursorChar = value;
					OnPropertyChanged("CursorChar");
					OnPropertyChanged("CursorCharCodePoint");
					OnPropertyChanged("CursorCharName");
					OnPropertyChanged("CursorCharCategory");
					OnPropertyChanged("CursorCharVisibility");
				}
			}
		}

		public string CursorCharCodePoint
		{
			get { return cursorChar != null ? "U+" + ((int) cursorChar[0]).ToString("X4") : ""; }
		}

		public string CursorCharName
		{
			get { return cursorChar != null ? UnicodeInfo.GetChar(cursorChar[0]).Name : ""; }
		}

		public string CursorCharCategory
		{
			get { return cursorChar != null ? UnicodeInfo.GetChar(cursorChar[0]).Category : "No character at cursor"; }
		}

		public Visibility CursorCharVisibility
		{
			get { return cursorChar != null ? Visibility.Visible : Visibility.Collapsed; }
		}

		// TODO: Move entirely to AppSettings
		private double fontScale = 100;
		public double FontScale
		{
			get { return fontScale; }
			set
			{
				if (value != fontScale)
				{
					fontScale = value;
					App.Settings.FontScale = fontScale;
					OnPropertyChanged("FontScale");
					OnPropertyChanged("FontSize");
					OnPropertyChanged("TextFormattingMode");
				}
			}
		}

		// TODO: Move to separate converter
		public double FontSize
		{
			get { return fontScale / 100 * 12; }
		}

		// TODO: Move to separate converter (use FontSize as input, make it universally usable)
		public TextFormattingMode TextFormattingMode
		{
			get { return FontSize < 16 ? TextFormattingMode.Display : TextFormattingMode.Ideal; }
		}

		private string statusText;
		public string StatusText
		{
			get { return statusText; }
			set
			{
				if (value != statusText)
				{
					statusText = value;
					OnPropertyChanged("StatusText");
				}
			}
		}

		private string selectedCulture;
		public string SelectedCulture
		{
			get { return selectedCulture; }
			set
			{
				if (value != selectedCulture)
				{
					selectedCulture = value;
					OnPropertyChanged("SelectedCulture");
					DeleteCultureCommand.RaiseCanExecuteChanged();
					SetPrimaryCultureCommand.RaiseCanExecuteChanged();
				}
			}
		}

		#endregion Data properties

		#region Toolbar commands

		#region File section

		private DelegateCommand newFileCommand;
		public DelegateCommand NewFileCommand
		{
			get
			{
				if (newFileCommand == null)
				{
					newFileCommand = new DelegateCommand(OnNewFile);
				}
				return newFileCommand;
			}
		}

		private DelegateCommand loadFolderCommand;
		public DelegateCommand LoadFolderCommand
		{
			get
			{
				if (loadFolderCommand == null)
				{
					loadFolderCommand = new DelegateCommand(OnLoadFolder);
				}
				return loadFolderCommand;
			}
		}

		private DelegateCommand loadFileCommand;
		public DelegateCommand LoadFileCommand
		{
			get
			{
				if (loadFileCommand == null)
				{
					loadFileCommand = new DelegateCommand(OnLoadFile);
				}
				return loadFileCommand;
			}
		}

		private DelegateCommand saveCommand;
		public DelegateCommand SaveCommand
		{
			get
			{
				if (saveCommand == null)
				{
					saveCommand = new DelegateCommand(OnSave, () => FileModified);
				}
				return saveCommand;
			}
		}

		private DelegateCommand importFileCommand;
		public DelegateCommand ImportFileCommand
		{
			get
			{
				if (importFileCommand == null)
				{
					importFileCommand = new DelegateCommand(OnImportFile);
				}
				return importFileCommand;
			}
		}

		private DelegateCommand exportKeysCommand;
		public DelegateCommand ExportKeysCommand
		{
			get
			{
				if (exportKeysCommand == null)
				{
					exportKeysCommand = new DelegateCommand(OnExportKeys, CanExportKeys);
				}
				return exportKeysCommand;
			}
		}

		#endregion File section

		#region Culture section

		private DelegateCommand newCultureCommand;
		public DelegateCommand NewCultureCommand
		{
			get
			{
				if (newCultureCommand == null)
				{
					newCultureCommand = new DelegateCommand(OnNewCulture);
				}
				return newCultureCommand;
			}
		}

		private DelegateCommand deleteCultureCommand;
		public DelegateCommand DeleteCultureCommand
		{
			get
			{
				if (deleteCultureCommand == null)
				{
					deleteCultureCommand = new DelegateCommand(OnDeleteCulture, CanDeleteCulture);
				}
				return deleteCultureCommand;
			}
		}

		private DelegateCommand replaceCultureCommand;
		public DelegateCommand ReplaceCultureCommand
		{
			get
			{
				if (replaceCultureCommand == null)
				{
					replaceCultureCommand = new DelegateCommand(OnReplaceCulture);
				}
				return replaceCultureCommand;
			}
		}

		private DelegateCommand insertSystemKeysCommand;
		public DelegateCommand InsertSystemKeysCommand
		{
			get
			{
				if (insertSystemKeysCommand == null)
				{
					insertSystemKeysCommand = new DelegateCommand(OnInsertSystemKeys);
				}
				return insertSystemKeysCommand;
			}
		}

		private DelegateCommand viewDateTimeFormatsCommand;
		public DelegateCommand ViewDateTimeFormatsCommand
		{
			get
			{
				if (viewDateTimeFormatsCommand == null)
				{
					viewDateTimeFormatsCommand = new DelegateCommand(OnViewDateTimeFormats);
				}
				return viewDateTimeFormatsCommand;
			}
		}

		private DelegateCommand setPrimaryCultureCommand;
		public DelegateCommand SetPrimaryCultureCommand
		{
			get
			{
				if (setPrimaryCultureCommand == null)
				{
					setPrimaryCultureCommand = new DelegateCommand(OnSetPrimaryCulture, CanSetPrimaryCulture);
				}
				return setPrimaryCultureCommand;
			}
		}

		#endregion Culture section

		#region Text key section

		private DelegateCommand newTextKeyCommand;
		public DelegateCommand NewTextKeyCommand
		{
			get
			{
				if (newTextKeyCommand == null)
				{
					newTextKeyCommand = new DelegateCommand(OnNewTextKey);
				}
				return newTextKeyCommand;
			}
		}

		private DelegateCommand deleteTextKeyCommand;
		public DelegateCommand DeleteTextKeyCommand
		{
			get
			{
				if (deleteTextKeyCommand == null)
				{
					deleteTextKeyCommand = new DelegateCommand(OnDeleteTextKey, CanDeleteTextKey);
				}
				return deleteTextKeyCommand;
			}
		}

		private DelegateCommand textKeyWizardCommand;
		public DelegateCommand TextKeyWizardCommand
		{
			get
			{
				if (textKeyWizardCommand == null)
				{
					textKeyWizardCommand = new DelegateCommand(OnTextKeyWizard);
				}
				return textKeyWizardCommand;
			}
		}

		private DelegateCommand renameTextKeyCommand;
		public DelegateCommand RenameTextKeyCommand
		{
			get
			{
				if (renameTextKeyCommand == null)
				{
					renameTextKeyCommand = new DelegateCommand(OnRenameTextKey, CanRenameTextKey);
				}
				return renameTextKeyCommand;
			}
		}

		private DelegateCommand duplicateTextKeyCommand;
		public DelegateCommand DuplicateTextKeyCommand
		{
			get
			{
				if (duplicateTextKeyCommand == null)
				{
					duplicateTextKeyCommand = new DelegateCommand(OnDuplicateTextKey, CanDuplicateTextKey);
				}
				return duplicateTextKeyCommand;
			}
		}

		#endregion Text key section

		#region View section

		private DelegateCommand navigateBackCommand;
		public DelegateCommand NavigateBackCommand
		{
			get
			{
				if (navigateBackCommand == null)
				{
					navigateBackCommand = new DelegateCommand(OnNavigateBack, CanNavigateBack);
				}
				return navigateBackCommand;
			}
		}

		private DelegateCommand navigateForwardCommand;
		public DelegateCommand NavigateForwardCommand
		{
			get
			{
				if (navigateForwardCommand == null)
				{
					navigateForwardCommand = new DelegateCommand(OnNavigateForward, CanNavigateForward);
				}
				return navigateForwardCommand;
			}
		}

		private DelegateCommand gotoDefinitionCommand;
		public DelegateCommand GotoDefinitionCommand
		{
			get
			{
				if (gotoDefinitionCommand == null)
				{
					gotoDefinitionCommand = new DelegateCommand(OnGotoDefinition, CanGotoDefinition);
				}
				return gotoDefinitionCommand;
			}
		}

		#endregion View section

		#region Filter section

		private DelegateCommand clearSearchCommand;
		public DelegateCommand ClearSearchCommand
		{
			get
			{
				if (clearSearchCommand == null)
				{
					clearSearchCommand = new DelegateCommand(() => { SearchText = ""; });
				}
				return clearSearchCommand;
			}
		}

		#endregion Filter section

		#region Application section

		private DelegateCommand settingsCommand;
		public DelegateCommand SettingsCommand
		{
			get
			{
				if (settingsCommand == null)
				{
					settingsCommand = new DelegateCommand(OnSettings);
				}
				return settingsCommand;
			}
		}

		private DelegateCommand aboutCommand;
		public DelegateCommand AboutCommand
		{
			get
			{
				if (aboutCommand == null)
				{
					aboutCommand = new DelegateCommand(OnAbout);
				}
				return aboutCommand;
			}
		}

		#endregion Application section

		#endregion Toolbar commands

		#region Toolbar command handlers

		#region File section

		private bool CheckModifiedSaved()
		{
			if (fileModified)
			{
				var result = TaskDialog.Show(
					owner: MainWindow.Instance,
					title: "TxEditor",
					mainInstruction: "Would you like to save the changes to the loaded dictionary?",
					content: "There are unsaved changes to the currently loaded dictionary. If you load new files, you must either save or discard those changes.",
					customButtons: new string[] { "&Save", "Do&n't save", "&Cancel" },
					allowDialogCancellation: true);

				if (result.CustomButtonResult == 0)
				{
					// Save
					// TODO
				}
				else if (result.CustomButtonResult != 1)
				{
					// Cancel or unset
					return false;
				}
			}
			return true;
		}

		private void OnNewFile()
		{
			if (!CheckModifiedSaved()) return;

			RootTextKey.Children.Clear();
			TextKeys.Clear();
			LoadedCultureNames.Clear();
			DeletedCultureNames.Clear();
			PrimaryCulture = null;
			ProblemKeys.Clear();
			StatusText = "";
			loadedFilePath = null;
			loadedFilePrefix = null;
			UpdateTitle();
		}

		private void OnLoadFolder()
		{
			if (!CheckModifiedSaved()) return;

			var d = new OpenFolderDialog();
			d.Title = "Load files from folder";
			if (d.ShowDialog(new Wpf32Window(MainWindow.Instance)) == true)
			{
				bool foundFiles = false;
				string regex = @"^(.+?)(\.(([a-z]{2})([-][a-z]{2})?))?\.(txd|xml)$";
				List<string> prefixes = new List<string>();
				string prefix = null;
				foreach (string fileName in Directory.GetFiles(d.Folder))
				{
					Match m = Regex.Match(Path.GetFileName(fileName), regex, RegexOptions.IgnoreCase);
					if (m.Success)
					{
						if (!foundFiles)
						{
							foundFiles = true;
						}
						prefix = m.Groups[1].Value;
						if (!prefixes.Contains(prefix))
						{
							prefixes.Add(prefix);
						}
					}
				}
				if (prefixes.Count > 1)
				{
					prefixes.Sort();
					var result = TaskDialog.Show(
						owner: MainWindow.Instance,
						title: "TxEditor",
						mainInstruction: "There are multiple dictionaries in the selected folder.",
						content: "Please choose the dictionary to load:",
						radioButtons: prefixes.ToArray(),
						customButtons: new string[] { "&Load", "&Cancel" },
						allowDialogCancellation: true);
					if (result.CustomButtonResult != 0 ||
						result.RadioButtonResult == null)
					{
						// Cancel or unset
						return;
					}
					prefix = prefixes[result.RadioButtonResult.Value];
				}
				int fileCount = 0;
				if (prefix != null)
				{
					foundFiles = false;
					fileVersion = 0;
					regex = @"(\.(([a-z]{2})([-][a-z]{2})?))?\.(txd|xml)$";
					if (!string.IsNullOrEmpty(prefix))
					{
						regex = "^" + prefix + regex;
					}
					foreach (string fileName in Directory.GetFiles(d.Folder))
					{
						Match m = Regex.Match(Path.GetFileName(fileName), regex, RegexOptions.IgnoreCase);
						if (m.Success)
						{
							if (!foundFiles)
							{
								foundFiles = true;
								OnNewFile();
							}
							LoadFromXmlFile(fileName);
							fileCount++;
						}
					}
				}
				if (foundFiles)
				{
					SortCulturesInTextKey(RootTextKey);
					DeletedCultureNames.Clear();
					ValidateTextKeys();
					StatusText = fileCount + " file(s) loaded, " + TextKeys.Count + " text keys defined.";
					FileModified = false;
				}
				else
				{
					MessageBox.Show("No files were found in the selected folder.", "TxEditor", MessageBoxButton.OK, MessageBoxImage.Warning);
				}
			}
		}

		private void OnLoadFile()
		{
			if (!CheckModifiedSaved()) return;

			var dlg = new OpenFileDialog();
			dlg.CheckFileExists = true;
			dlg.Filter = "Tx dictionary files (*.txd)|*.txd|XML files (*.xml)|*.xml|All files (*.*)|*.*";
			dlg.Multiselect = true;
			dlg.ShowReadOnly = false;
			dlg.Title = "Select files to load";
			if (dlg.ShowDialog(MainWindow.Instance) == true)
			{
				// Check for same prefix and reject mixed files
				string regex = @"^(.+?)(\.(([a-z]{2})([-][a-z]{2})?))?\.(?:txd|xml)$";
				List<string> prefixes = new List<string>();
				string prefix = null;
				foreach (string fileName in dlg.FileNames)
				{
					Match m = Regex.Match(Path.GetFileName(fileName), regex, RegexOptions.IgnoreCase);
					if (m.Success)
					{
						prefix = m.Groups[1].Value;
						if (!prefixes.Contains(prefix))
						{
							prefixes.Add(prefix);
						}
					}
				}
				if (prefixes.Count > 1)
				{
					MessageBox.Show(
						"You cannot load files with different prefixes.",
						"Error",
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
					return;
				}

				// TODO: Scan for similar files and ask if not all of them are selected

				bool foundFiles = false;
				fileVersion = 0;
				foreach (string fileName in dlg.FileNames)
				{
					if (!foundFiles)
					{
						foundFiles = true;
						OnNewFile();
					}
					LoadFromXmlFile(fileName);
				}
				// TODO: Display a warning if multiple files claimed to be the primary culture, and which has won

				SortCulturesInTextKey(RootTextKey);
				DeletedCultureNames.Clear();
				ValidateTextKeys();
				StatusText = dlg.FileNames.Length + " file(s) loaded, " + TextKeys.Count + " text keys defined.";
				FileModified = false;
			}
		}

		private void OnSave()
		{
			string newFilePath = null;
			string newFilePrefix = null;

			if (loadedFilePath == null || loadedFilePrefix == null)
			{
				// Ask for new file name and version
				SaveFileDialog dlg = new SaveFileDialog();
				dlg.AddExtension = true;
				dlg.CheckPathExists = true;
				dlg.DefaultExt = ".txd";
				dlg.Filter = "Tx dictionary files (*.txd)|*.txd|XML files (*.xml)|*.xml|All files (*.*)|*.*";
				dlg.OverwritePrompt = true;
				dlg.Title = "Select file to save as";
				if (dlg.ShowDialog(MainWindow.Instance) == true)
				{
					newFilePath = Path.GetDirectoryName(dlg.FileName);
					newFilePrefix = Path.GetFileNameWithoutExtension(dlg.FileName);
					fileVersion = 2;
					if (Path.GetExtension(dlg.FileName) == ".xml")
						fileVersion = 1;
				}
				else
				{
					return;
				}
			}
			else if (fileVersion == 1)
			{
				// Saving existing format 1 file.
				// Ask to upgrade to version 2 format.

				if (App.Settings.AskSaveUpgrade)
				{
					var result = TaskDialog.Show(
						owner: MainWindow.Instance,
						allowDialogCancellation: true,
						title: "TxEditor",
						mainInstruction: "Would you like to upgrade the file to format version 2?",
						customButtons: new string[] { "&Upgrade", "&Save in original format" },
						verificationText: "Don’t ask again");

					if (result.CustomButtonResult == null)
					{
						return;
					}
					if (result.CustomButtonResult == 0)
					{
						fileVersion = 2;
					}
					if (result.VerificationChecked == true)
					{
						// Remember to not ask again
						App.Settings.AskSaveUpgrade = false;
					}
				}
			}

			if (fileVersion == 1)
			{
				// Check for usage of version 2 features
				bool foundIncompatibleFeatures = false;
				Action<TextKeyViewModel> checkTextKey = null;
				checkTextKey = (vm) =>
				{
					foreach (var ct in vm.CultureTextVMs)
					{
						// Find modulo values and new placeholders {#} and {=...}
						if (ct.Text != null && Regex.IsMatch(ct.Text, @"(?<!\{)\{(?:#\}|=)"))
							foundIncompatibleFeatures = true;
						
						foreach (var qt in ct.QuantifiedTextVMs)
						{
							if (qt.Modulo != 0)
								foundIncompatibleFeatures = true;
							if (qt.Text != null && Regex.IsMatch(qt.Text, @"(?<!\{)\{(?:#\}|=)"))
								foundIncompatibleFeatures = true;
						}
					}

					foreach (TextKeyViewModel child in vm.Children)
					{
						checkTextKey(child);
					}
				};
				checkTextKey(RootTextKey);

				if (foundIncompatibleFeatures)
				{
					var result = TaskDialog.Show(
						owner: MainWindow.Instance,
						allowDialogCancellation: true,
						title: "TxEditor",
						mainInstruction: "Incompatible features used for file format version 1.",
						content: "You are about to save the dictionary in a format 1 file but have used features that are not supported by this format. " +
							"If you save in this format anyway, modulo specifications will be lost and advanced placeholders may not work at runtime.",
						customButtons: new string[] { "&Save anyway", "Do&n’t save" });

					if (result.CustomButtonResult != 0)
					{
						return;
					}
				}
			}

			if (newFilePath != null)
			{
				WriteToXmlFile(Path.Combine(newFilePath, newFilePrefix));
				loadedFilePath = newFilePath;
				loadedFilePrefix = newFilePrefix;
			}
			else
			{
				WriteToXmlFile(Path.Combine(loadedFilePath, loadedFilePrefix));
			}
			UpdateTitle();
		}

		private void OnImportFile()
		{
		}

		private bool CanExportKeys()
		{
			return false;
		}

		private void OnExportKeys()
		{
		}

		#endregion File section

		#region Culture section

		private void OnNewCulture()
		{
			CultureWindow win = new CultureWindow();
			win.Owner = MainWindow.Instance;
			if (win.ShowDialog() == true)
			{
				CultureInfo ci = new CultureInfo(win.CodeText.Text);
				AddNewCulture(RootTextKey, ci.IetfLanguageTag, true);
				// Make the very first culture the primary culture by default
				if (LoadedCultureNames.Count == 1)
				{
					PrimaryCulture = ci.IetfLanguageTag;
				}
				FileModified = true;
			}
		}

		private bool CanDeleteCulture()
		{
			return !string.IsNullOrEmpty(SelectedCulture);
		}

		private void OnDeleteCulture()
		{
			CultureInfo ci = new CultureInfo(SelectedCulture);

			if (MessageBox.Show(
				"Are you sure to delete the culture " +
					(App.Settings.NativeCultureNames ? ci.NativeName : ci.DisplayName) +
					" [" + ci.IetfLanguageTag + "]?",
				"Delete culture",
				MessageBoxButton.YesNo,
				MessageBoxImage.Question) == MessageBoxResult.Yes)
			{
				DeleteCulture(RootTextKey, SelectedCulture, true);
				FileModified = true;
			}
		}

		private void OnReplaceCulture()
		{
		}

		private void OnInsertSystemKeys()
		{
		}

		private void OnViewDateTimeFormats()
		{
		}

		private bool CanSetPrimaryCulture()
		{
			return !string.IsNullOrEmpty(SelectedCulture) && SelectedCulture != PrimaryCulture;
		}

		private void OnSetPrimaryCulture()
		{
			CultureInfo ci = new CultureInfo(SelectedCulture);
			string cultureName = Tx.U(App.Settings.NativeCultureNames ? ci.NativeName : ci.DisplayName) + " [" + ci.IetfLanguageTag + "]";

			var result = TaskDialog.Show(
				owner: MainWindow.Instance,
				title: "TxEditor",
				mainInstruction: "Are you sure to switch the primary culture to " + cultureName  + "?",
				content: "The primary culture is used as fallback to find untranslated texts. " +
					"It also serves as reference to find inconsistencies and all text key comments are assigned to it. " +
					"The comments will be moved to the new primary culture.",
				customButtons: new string[] { "&Switch", "&Cancel" },
				allowDialogCancellation: true);

			if (result.CustomButtonResult == 0)
			{
				PrimaryCulture = SelectedCulture;
				SortCulturesInTextKey(RootTextKey);
				ValidateTextKeys();
				FileModified = true;
				SetPrimaryCultureCommand.RaiseCanExecuteChanged();
			}
		}

		#endregion Culture section

		#region Text key section

		private void OnNewTextKey()
		{
			var win = new TextKeyWindow();
			win.Owner = MainWindow.Instance;
			win.Title = "Add new text key";
			win.CaptionLabel.Text = "Enter the new text key to add:";
			win.OKButton.Content = "Add text key";

			var selKey = MainWindow.Instance.TextKeysTreeView.LastSelectedItem as TextKeyViewModel;
			if (selKey != null)
			{
				win.TextKey = selKey.TextKey + (selKey.IsNamespace ? ":" : ".");
			}

			if (win.ShowDialog() == true)
			{
				string newKey = win.TextKey;

				TextKeyViewModel tk = FindOrCreateTextKey(newKey);

				bool alreadyExists = !tk.IsEmpty();

				// Ensure that all loaded cultures exist in every text key so that they can be entered
				foreach (string cn in LoadedCultureNames)
				{
					EnsureCultureInTextKey(tk, cn);
				}
				tk.UpdateCultureTextSeparators();

				ValidateTextKeys();
				FileModified = true;

				tk.IsExpanded = true;   // Expands all parents
				tk.IsExpanded = false;   // Collapses this item again
				ViewCommandManager.InvokeLoaded("SelectTextKey", tk);

				if (alreadyExists)
				{
					MessageBox.Show(
						"The entered text key already exists and is now selected.",
						"Warning",
						MessageBoxButton.OK,
						MessageBoxImage.Information);
				}

				if (tk.CultureTextVMs.Count > 0)
					tk.CultureTextVMs[0].ViewCommandManager.InvokeLoaded("FocusText");
			}
		}

		public void TextKeySelectionChanged(IList selectedItems)
		{
			selectedTextKeys = selectedItems;
			DeleteTextKeyCommand.RaiseCanExecuteChanged();
			RenameTextKeyCommand.RaiseCanExecuteChanged();
			DuplicateTextKeyCommand.RaiseCanExecuteChanged();
		}

		private bool CanDeleteTextKey()
		{
			return selectedTextKeys != null && selectedTextKeys.Count > 0;
		}

		private void OnDeleteTextKey()
		{
			if (selectedTextKeys == null || selectedTextKeys.Count == 0) return;

			int count = 0;
			foreach (TextKeyViewModel tk in selectedTextKeys)
			{
				count += CountTextKeys(tk);
			}
			if (count == 0) return;   // Means there were nodes with no full keys, should not happen

			TaskDialogResult result;
			if (count == 1)
			{
				result = TaskDialog.Show(
					owner: MainWindow.Instance,
					allowDialogCancellation: true,
					title: "TxEditor",
					mainInstruction: "Deleting text key " + Tx.Q(lastCountedTextKey) + ".",
					content: "Are you sure to delete the selected text key with the texts of all cultures? This cannot be undone.",
					customButtons: new string[] { "Delete", "Cancel" });
			}
			else
			{
				result = TaskDialog.Show(
					owner: MainWindow.Instance,
					allowDialogCancellation: true,
					title: "TxEditor",
					mainInstruction: "Deleting " + count + " text keys.",
					content: "Are you sure to delete the selected text keys with the texts of all cultures? This cannot be undone.",
					customButtons: new string[] { "Delete", "Cancel" });
			}
			if (result.CustomButtonResult == 0)
			{
				object[] selectedTextKeysArray = new object[selectedTextKeys.Count];
				selectedTextKeys.CopyTo(selectedTextKeysArray, 0);
				foreach (TextKeyViewModel tk in selectedTextKeysArray)
				{
					DeleteTextKey(tk);
					// Also remove unused partial keys
					DeletePartialParentKeys(tk.Parent as TextKeyViewModel);
					FileModified = true;
				}

				StatusText = count + " text keys deleted.";
			}
		}

		private string lastCountedTextKey;

		/// <summary>
		/// Counts all full keys within the specified subtree, including the specified text key.
		/// </summary>
		/// <param name="tk">Text key to start counting at.</param>
		/// <returns></returns>
		private int CountTextKeys(TextKeyViewModel tk)
		{
			int count = tk.IsFullKey ? 1 : 0;
			if (tk.IsFullKey)
				lastCountedTextKey = tk.TextKey;
			foreach (TextKeyViewModel child in tk.Children)
			{
				count += CountTextKeys(child);
			}
			return count;
		}

		private void DeleteTextKey(TextKeyViewModel tk)
		{
			foreach (TextKeyViewModel child in tk.Children.ToArray())
			{
				DeleteTextKey(child);
			}
			if (tk.IsFullKey)
			{
				TextKeys.Remove(tk.TextKey);
				ProblemKeys.Remove(tk);
			}
			tk.Parent.Children.Remove(tk);
		}

		/// <summary>
		/// Searches up the tree parents, starting from the specified key, and deletes the
		/// top-most unused text key, i.e. a key that is partial and has no children.
		/// </summary>
		/// <param name="tk"></param>
		private void DeletePartialParentKeys(TextKeyViewModel tk)
		{
			TextKeyViewModel nodeToDelete = null;
			TextKeyViewModel current = tk;

			while (true)
			{
				if (current == null)
				{
					// No more parents
					break;
				}
				if (current == RootTextKey)
				{
					// Don't try to delete the root key
					break;
				}
				if (current.IsFullKey || CountTextKeys(current) > 0)
				{
					// The current key is not unused
					break;
				}
				nodeToDelete = current;
				current = current.Parent as TextKeyViewModel;
			}
			if (nodeToDelete != null)
			{
				DeleteTextKey(nodeToDelete);
			}
		}

		private void OnTextKeyWizard()
		{
			TextKeyWizardWindow win = new TextKeyWizardWindow();
			win.Owner = MainWindow.Instance;

			MainWindow.Instance.Hide();

			win.ShowDialog();

			MainWindow.Instance.Show();
		}

		private bool CanRenameTextKey()
		{
			return selectedTextKeys != null && selectedTextKeys.Count == 1;
		}

		private void OnRenameTextKey()
		{
			var selKey = MainWindow.Instance.TextKeysTreeView.LastSelectedItem as TextKeyViewModel;
			if (selKey == null)
				return;   // No key selected, something is wrong

			var win = new TextKeyWindow();
			win.Owner = MainWindow.Instance;
			win.Title = "Rename text key";
			win.CaptionLabel.Text = "Enter the new name of the selected text key:";
			win.TextKey = selKey.TextKey;
			win.OKButton.Content = "Rename";
			win.RenameSelectMode = true;

			if (selKey.Children.Count > 0)
			{
				win.IncludeSubitemsCheckbox.Visibility = Visibility.Visible;
				win.IncludeSubitemsCheckbox.IsChecked = true;
				win.IncludeSubitemsCheckbox.IsEnabled = false;

				if (selKey.IsFullKey)
				{
					// TODO: win.IncludeSubitemsCheckbox.IsEnabled = true;
				}
			}

			if (win.ShowDialog() == true)
			{
				string newKey = win.TextKey;

				TextKeyViewModel tryDestKey = FindOrCreateTextKey(newKey, false, false);
				bool destExists = !tryDestKey.IsEmpty() || tryDestKey.Children.Count > 0;
				if (destExists)
				{
					// TODO: What to consider if the destination key already has children?
					// TODO: Ask user to merge, overwrite or cancel.
					MessageBox.Show(
						"The destination key already exists. Handling this is not yet implemented.",
						"Error",
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
					return;
				}
				
				var oldParent = selKey.Parent;

				// Remove the selected key from its original tree position
				oldParent.Children.Remove(selKey);

				TextKeyViewModel destKey = FindOrCreateTextKey(newKey, false);

				if (destExists)
				{
					// Key was entirely empty or is newly created.
					// Replace it with the source key.
		
					selKey.SetKeyRecursive(newKey, TextKeys);

					if (selKey.IsNamespace)
					{
						// Namespace entries are sorted differently, which was not known when
						// creating the key because it was no namespace at that time. Remove the
						// newly created key entry (all of its possibly created parent keys are
						// still useful though!) and insert the selected key at the correct
						// position in that tree level.
						destKey.Parent.Children.Remove(destKey);
						destKey.Parent.Children.InsertSorted(selKey, (a, b) => TextKeyViewModel.Compare(a, b));
					}
					else
					{
						// The sort order is already good for normal keys so we can simply replace
						// the created item with the selected key at the same position.
						destKey.Parent.Children.Replace(destKey, selKey);
					}
					// Update the key's parent reference to represent the (possible) new tree location.
					selKey.Parent = destKey.Parent;
				}
				else
				{
					// TODO
				}

				if (oldParent != selKey.Parent)
				{
					// The key has moved to another subtree.
					// Clean up possible unused partial keys at the old position.
					DeletePartialParentKeys(oldParent as TextKeyViewModel);
				}
	
				FileModified = true;

				// Fix an issue with MultiSelectTreeView: It can only know that an item is selected
				// when its TreeViewItem property IsSelected is set through a binding defined in
				// this application. The already-selected item was removed from the SelectedItems
				// list when it was removed from the tree (to be re-inserted later). Not sure how
				// to fix this right.
				selKey.IsSelected = true;

				bool wasExpanded = selKey.IsExpanded;
				selKey.IsExpanded = true;   // Expands all parents
				if (!wasExpanded)
					selKey.IsExpanded = false;   // Collapses the item again like it was before
				ViewCommandManager.InvokeLoaded("SelectTextKey", selKey);
			}
		}

		private bool CanDuplicateTextKey()
		{
			return false;
			// TODO
			//return selectedTextKeys != null && selectedTextKeys.Count == 1;
		}

		private void OnDuplicateTextKey()
		{
			// TODO
		}

		#endregion Text key section

		#region View section

		private bool CanNavigateBack()
		{
			return false;
		}

		private void OnNavigateBack()
		{
		}

		private bool CanNavigateForward()
		{
			return false;
		}

		private void OnNavigateForward()
		{
		}

		private bool CanGotoDefinition()
		{
			return false;
		}

		private void OnGotoDefinition()
		{
		}

		#endregion View section

		#region Filter section

		#endregion Filter section

		#region Application section

		private SettingsWindow settingsWindow;

		private void OnSettings()
		{
			if (settingsWindow == null || !settingsWindow.IsVisible)
			{
				settingsWindow = new SettingsWindow();
				settingsWindow.Owner = MainWindow.Instance;
				settingsWindow.Show();
			}
			else
			{
				settingsWindow.Close();
				settingsWindow = null;
			}
		}

		private void OnAbout()
		{
			var root = MainWindow.Instance.Content as UIElement;

			var blur = new BlurEffect();
			blur.Radius = 0;
			root.Effect = blur;

			root.AnimateEase(UIElement.OpacityProperty, 1, 0.6, TimeSpan.FromSeconds(1));
			blur.AnimateEase(BlurEffect.RadiusProperty, 0, 4, TimeSpan.FromSeconds(0.5));

			var win = new AboutWindow();
			win.Owner = MainWindow.Instance;
			win.ShowDialog();

			root.AnimateEase(UIElement.OpacityProperty, 0.6, 1, TimeSpan.FromSeconds(0.2));
			blur.AnimateEase(BlurEffect.RadiusProperty, 4, 0, TimeSpan.FromSeconds(0.2));

			DelayedCall.Start(() =>
			{
				root.Effect = null;
			}, 500);
		}

		#endregion Application section

		#endregion Toolbar command handlers

		#region XML loading methods

		public void LoadFiles(IEnumerable<string> fileNames)
		{
			int count = 0;
			foreach (string _fileName in fileNames.Distinct())
			{
				string fileName = _fileName;
				if (!Path.IsPathRooted(fileName))
				{
					fileName = Path.GetFullPath(fileName);
				}
				LoadFromXmlFile(fileName);
				count++;
			}
			ValidateTextKeys();
			StatusText = count + " file(s) loaded, " + TextKeys.Count + " text keys defined.";
			FileModified = false;
		}

		private void LoadFromXmlFile(string fileName)
		{
			// First load the XML file into an XmlDocument for further processing
			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.Load(fileName);

			// Try to recognise the culture name from the file name
			Match m = Regex.Match(Path.GetFileName(fileName), @"^(.+?)\.(([a-z]{2})([-][a-z]{2})?)\.(?:txd|xml)$", RegexOptions.IgnoreCase);
			if (m.Success)
			{
				CultureInfo ci = CultureInfo.GetCultureInfo(m.Groups[2].Value);
				LoadFromXml(ci.Name, xmlDoc.DocumentElement);

				// Set the primary culture if a file claims to be it
				XmlAttribute primaryAttr = xmlDoc.DocumentElement.Attributes["primary"];
				if (primaryAttr != null && primaryAttr.Value == "true")
				{
					PrimaryCulture = ci.Name;
				}
				if (fileVersion == 0)
				{
					fileVersion = 1;
					loadedFilePath = Path.GetDirectoryName(fileName);
					loadedFilePrefix = m.Groups[1].Value;
					UpdateTitle();
				}
				return;
			}

			// Try to find the culture name inside a combined XML document
			foreach (XmlElement xe in xmlDoc.DocumentElement.SelectNodes("culture[@name]"))
			{
				CultureInfo ci = CultureInfo.GetCultureInfo(xe.Attributes["name"].Value);
				LoadFromXml(ci.Name, xe);

				// Set the primary culture if a culture in the file claims to be it
				XmlAttribute primaryAttr = xe.Attributes["primary"];
				if (primaryAttr != null && primaryAttr.Value == "true")
				{
					PrimaryCulture = ci.Name;
				}
			}
			if (fileVersion == 0)
			{
				fileVersion = 2;
				loadedFilePath = Path.GetDirectoryName(fileName);
				loadedFilePrefix = Path.GetFileNameWithoutExtension(fileName);
				UpdateTitle();
			}
		}

		private void LoadFromXml(string cultureName, XmlElement xe)
		{
			// Add the new culture everywhere
			if (!LoadedCultureNames.Contains(cultureName))
			{
				AddNewCulture(RootTextKey, cultureName, false);
			}
			
			// Read the XML document
			foreach (XmlNode textNode in xe.SelectNodes("text[@key]"))
			{
				string text = textNode.InnerText;
				string key = textNode.Attributes["key"].Value;

				string errorMessage;
				if (!TextKeyViewModel.ValidateName(key, out errorMessage))
				{
					//Log("Load XML: Invalid key: " + errorMessage + " Ignoring definition.");
					continue;
				}

				int count = -1;
				XmlAttribute countAttr = textNode.Attributes["count"];
				if (countAttr != null)
				{
					if (!int.TryParse(countAttr.Value, out count))
					{
						// Count value unparsable. Skip invalid entries
						//Log("Load XML: Count attribute value of key {0} is not an integer. Ignoring definition.", key);
						continue;
					}
					if (count < 0 || count > ushort.MaxValue)
					{
						// Count value out of range. Skip invalid entries
						//Log("Load XML: Count attribute value of key {0} is out of range. Ignoring definition.", key);
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
						//Log("Load XML: Modulo attribute of key {0} is not an integer. Ignoring definition.", key);
						continue;
					}
					if (modulo < 2 || modulo > 1000)
					{
						// Modulo value out of range. Skip invalid entries
						//Log("Load XML: Modulo attribute of key {0} is out of range. Ignoring definition.", key);
						continue;
					}
				}

				TextKeyViewModel tk = FindOrCreateTextKey(key);

				// Ensure that all loaded cultures exist in every text key so that they can be entered
				foreach (string cn in LoadedCultureNames)
				{
					EnsureCultureInTextKey(tk, cn);
				}
				tk.UpdateCultureTextSeparators();
				
				// Find the current culture
				var ct = tk.CultureTextVMs.FirstOrDefault(vm => vm.CultureName == cultureName);
				if (count == -1)
				{
					// Default text, store it directly in the item
					ct.Text = text;
				}
				else
				{
					// Quantified text, go deeper
					var qt = new QuantifiedTextViewModel(ct);
					qt.Count = count;
					qt.Modulo = modulo;
					qt.Text = text;
					ct.QuantifiedTextVMs.InsertSorted(qt, (a, b) => QuantifiedTextViewModel.Compare(a, b));
				}
			}
		}

		private TextKeyViewModel FindOrCreateTextKey(string textKey, bool updateTextKeys = true, bool create = true)
		{
			// Tokenize text key to find the tree node
			string partialKey = "";
			TextKeyViewModel tk = RootTextKey;
			string[] nsParts = textKey.Split(':');
			string localKey;
			if (nsParts.Length > 1)
			{
				// Namespace set
				partialKey = nsParts[0];
				var subtk = tk.Children.OfType<TextKeyViewModel>()
					.SingleOrDefault(vm => vm.DisplayName == nsParts[0] && vm.IsNamespace);
				if (subtk == null)
				{
					// Namespace tree item does not exist yet, create it
					if (!create) return null;
					subtk = new TextKeyViewModel(nsParts[0], false, tk, tk.MainWindowVM);
					subtk.DisplayName = nsParts[0];
					subtk.IsNamespace = true;
					tk.Children.InsertSorted(subtk, (a, b) => TextKeyViewModel.Compare(a, b));
				}
				tk = subtk;
				// Continue with namespace-free text key
				localKey = nsParts[1];
				partialKey += ":";
			}
			else
			{
				// No namespace set, continue with entire key
				localKey = textKey;
			}

			string[] keySegments = localKey.Split('.');
			for (int i = 0; i < keySegments.Length; i++)
			{
				string keySegment = keySegments[i];
				partialKey += keySegment;

				// Search for tree item
				var subtk = tk.Children.OfType<TextKeyViewModel>()
					.SingleOrDefault(vm => vm.DisplayName == keySegment && !vm.IsNamespace);
				if (subtk == null)
				{
					// This level of text key item does not exist yet, create it
					if (!create) return null;
					subtk = new TextKeyViewModel(partialKey, i == keySegments.Length - 1, tk, tk.MainWindowVM);
					subtk.DisplayName = keySegment;
					tk.Children.InsertSorted(subtk, (a, b) => TextKeyViewModel.Compare(a, b));
				}
				tk = subtk;
				partialKey += ".";
			}

			if (create)
			{
				if (updateTextKeys && !TextKeys.Contains(textKey))
					TextKeys.Add(textKey);
				tk.IsFullKey = true;
			}
			return tk;
		}

		#endregion XML loading methods

		#region XML saving methods

		/// <summary>
		/// Writes all loaded text keys to a file.
		/// </summary>
		/// <param name="fileNamePrefix">Path and file name prefix, without culture name and extension.</param>
		private void WriteToXmlFile(string fileNamePrefix)
		{
			if (fileVersion == 1)
			{
				// Delete previous backups and move current files to backup
				foreach (var cultureName in LoadedCultureNames.Union(DeletedCultureNames).Distinct())
				{
					string cultureFileName = fileNamePrefix + "." + cultureName + ".xml";
					if (File.Exists(cultureFileName))
					{
						File.Delete(cultureFileName + ".bak");
						File.Move(cultureFileName, cultureFileName + ".bak");
					}
				}
				// Write new files, one for each loaded culture
				foreach (var cultureName in LoadedCultureNames)
				{
					XmlDocument xmlDoc = new XmlDocument();
					xmlDoc.AppendChild(xmlDoc.CreateElement("translation"));
					var spaceAttr = xmlDoc.CreateAttribute("xml:space");
					spaceAttr.Value = "preserve";
					xmlDoc.DocumentElement.Attributes.Append(spaceAttr);
					if (cultureName == PrimaryCulture)
					{
						var primaryAttr = xmlDoc.CreateAttribute("primary");
						primaryAttr.Value = "true";
						xmlDoc.DocumentElement.Attributes.Append(primaryAttr);
					}
					WriteToXml(cultureName, xmlDoc.DocumentElement);

					// Write xmlDoc to file
					WriteXmlToFile(xmlDoc, fileNamePrefix + "." + cultureName + ".xml");
				}
				// Delete all backup files (could also be an option)
				foreach (var cultureName in LoadedCultureNames.Union(DeletedCultureNames).Distinct())
				{
					string cultureFileName = fileNamePrefix + "." + cultureName + ".xml";
					File.Delete(cultureFileName + ".bak");
				}
				DeletedCultureNames.Clear();
			}
			else if (fileVersion == 2)
			{
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.AppendChild(xmlDoc.CreateElement("translation"));
				var spaceAttr = xmlDoc.CreateAttribute("xml:space");
				spaceAttr.Value = "preserve";
				xmlDoc.DocumentElement.Attributes.Append(spaceAttr);

				foreach (var cultureName in LoadedCultureNames)
				{
					var cultureElement = xmlDoc.CreateElement("culture");
					xmlDoc.DocumentElement.AppendChild(cultureElement);
					var nameAttr = xmlDoc.CreateAttribute("name");
					nameAttr.Value = cultureName;
					cultureElement.Attributes.Append(nameAttr);
					if (cultureName == PrimaryCulture)
					{
						var primaryAttr = xmlDoc.CreateAttribute("primary");
						primaryAttr.Value = "true";
						cultureElement.Attributes.Append(primaryAttr);
					}
					WriteToXml(cultureName, cultureElement);
				}

				// Write xmlDoc to file
				WriteXmlToFile(xmlDoc, fileNamePrefix + ".txd");
			}
			else
			{
				MessageBox.Show("Unsupported file version " + fileVersion + " cannot be saved. How was that loaded?", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
			FileModified = false;
		}

		private void WriteXmlToFile(XmlDocument xmlDoc, string fileName)
		{
			XmlWriterSettings xws = new XmlWriterSettings();
			xws.Encoding = Encoding.UTF8;
			xws.Indent = true;
			xws.IndentChars = "\t";
			xws.OmitXmlDeclaration = false;
			using (XmlWriter xw = XmlWriter.Create(fileName + ".tmp", xws))
			{
				xmlDoc.Save(xw);
			}

			File.Delete(fileName);
			File.Move(fileName + ".tmp", fileName);
		}

		private void WriteToXml(string cultureName, XmlElement xe)
		{
			WriteTextKeysToXml(cultureName, xe, RootTextKey);
		}

		private void WriteTextKeysToXml(string cultureName, XmlElement xe, TextKeyViewModel textKeyVM)
		{
			if (textKeyVM.IsFullKey && textKeyVM.TextKey != null)
			{
				var cultureTextVM = textKeyVM.CultureTextVMs.FirstOrDefault(vm => vm.CultureName == cultureName);
				if (cultureTextVM != null)
				{
					if (!string.IsNullOrEmpty(cultureTextVM.Text))
					{
						var textElement = xe.OwnerDocument.CreateElement("text");
						xe.AppendChild(textElement);
						var keyAttr = xe.OwnerDocument.CreateAttribute("key");
						keyAttr.Value = textKeyVM.TextKey;
						textElement.Attributes.Append(keyAttr);
						textElement.InnerText = cultureTextVM.Text;
					}
					foreach (var quantifiedTextVM in cultureTextVM.QuantifiedTextVMs.OrderBy(qt => qt.Count).ThenBy(qt => qt.Modulo))
					{
						var textElement = xe.OwnerDocument.CreateElement("text");
						xe.AppendChild(textElement);

						var keyAttr = xe.OwnerDocument.CreateAttribute("key");
						keyAttr.Value = textKeyVM.TextKey;
						textElement.Attributes.Append(keyAttr);

						if (quantifiedTextVM.Count < 0)
						{
							throw new Exception("Invalid count value " + quantifiedTextVM.Count + " set for text key " +
								textKeyVM.TextKey + ", culture " + cultureName);
						}
						var countAttr = xe.OwnerDocument.CreateAttribute("count");
						countAttr.Value = quantifiedTextVM.Count.ToString();
						textElement.Attributes.Append(countAttr);
						
						if (quantifiedTextVM.Modulo != 0 &&
							(quantifiedTextVM.Modulo < 2 && quantifiedTextVM.Modulo > 1000))
						{
							throw new Exception("Invalid modulo value " + quantifiedTextVM.Modulo + " set for text key " +
								textKeyVM.TextKey + ", culture " + cultureName + ", count " + quantifiedTextVM.Count);
						}
						if (quantifiedTextVM.Modulo > 1)
						{
							var modAttr = xe.OwnerDocument.CreateAttribute("mod");
							modAttr.Value = quantifiedTextVM.Modulo.ToString();
							textElement.Attributes.Append(modAttr);
						}
						
						textElement.InnerText = quantifiedTextVM.Text;
					}
				}
			}
			foreach (TextKeyViewModel child in textKeyVM.Children.OrderBy(tk => tk.DisplayName))
			{
				WriteTextKeysToXml(cultureName, xe, child);
			}
		}

		#endregion XML saving methods

		#region Text validation

		public void ValidateTextKeys()
		{
			RootTextKey.Validate();
		}

		#endregion Text validation

		#region Culture management

		private void EnsureCultureInTextKey(TextKeyViewModel tk, string cultureName)
		{
			if (!tk.CultureTextVMs.Any(vm => vm.CultureName == cultureName))
			{
				tk.CultureTextVMs.InsertSorted(new CultureTextViewModel(cultureName, tk), (a, b) => a.CompareTo(b));
			}
		}

		private void AddNewCulture(TextKeyViewModel root, string cultureName, bool validate)
		{
			foreach (TextKeyViewModel tk in root.Children)
			{
				EnsureCultureInTextKey(tk, cultureName);
				tk.UpdateCultureTextSeparators();
				if (tk.Children.Count > 0)
				{
					AddNewCulture(tk, cultureName, validate);
				}
			}
			if (!LoadedCultureNames.Contains(cultureName))
			{
				LoadedCultureNames.Add(cultureName);
			}
			DeletedCultureNames.Remove(cultureName);   // in case it's been deleted before
			if (validate)
			{
				ValidateTextKeys();
			}
		}

		private void DeleteCulture(TextKeyViewModel root, string cultureName, bool validate)
		{
			foreach (TextKeyViewModel tk in root.Children)
			{
				tk.CultureTextVMs.Filter(ct => ct.CultureName != cultureName);
				tk.UpdateCultureTextSeparators();
				if (tk.Children.Count > 0)
				{
					DeleteCulture(tk, cultureName, validate);
				}
			}
			LoadedCultureNames.Remove(cultureName);
			if (!DeletedCultureNames.Contains(cultureName))
			{
				DeletedCultureNames.Add(cultureName);
			}
			if (validate)
			{
				ValidateTextKeys();
			}
		}

		private void SortCulturesInTextKey(TextKeyViewModel root)
		{
			foreach (TextKeyViewModel tk in root.Children)
			{
				var ctList = tk.CultureTextVMs.ToArray();
				tk.CultureTextVMs.Clear();
				foreach (var ct in ctList)
				{
					tk.CultureTextVMs.InsertSorted(ct, (a, b) => a.CompareTo(b));
				}

				tk.UpdateCultureTextSeparators();
				if (tk.Children.Count > 0)
				{
					SortCulturesInTextKey(tk);
				}
			}
		}

		#endregion Culture management

		#region Window management

		private void UpdateTitle()
		{
			if (!string.IsNullOrEmpty(loadedFilePath))
			{
				DisplayName =
					loadedFilePrefix +
					(fileModified ? "*" : "") +
					(fileVersion == 1 ? " (v1)" : "") +
					" in " + loadedFilePath + " – TxEditor";
			}
			else
			{
				DisplayName = "TxEditor";
			}
		}

		#endregion Window management

		#region Text search

		private string searchText;
		public string SearchText
		{
			get
			{
				return searchText;
			}
			set
			{
				if (value != searchText)
				{
					searchText = value;
					OnPropertyChanged("SearchText");
					UpdateSearch();
				}
			}
		}

		/// <summary>
		/// Updates the visibility of all text keys in the tree, according to the entered search term.
		/// </summary>
		public void UpdateSearch()
		{
			bool isSearch = !string.IsNullOrWhiteSpace(searchText);
			UpdateTextKeyVisibility(RootTextKey, isSearch);
		}

		private void UpdateTextKeyVisibility(TextKeyViewModel tk, bool isSearch)
		{
			foreach (TextKeyViewModel child in tk.Children)
			{
				bool isVisible = !isSearch || child.TextKey.ToLower().Contains(searchText.ToLower());
				if (problemFilterActive)
				{
					isVisible &= child.HasProblem;
				}

				child.IsVisible = isVisible;
				if (isVisible)
				{
					TreeViewItemViewModel parent = child.Parent;
					while (parent != null)
					{
						parent.IsVisible = true;
						parent = parent.Parent;
					}
				}
				if (child.Children.Count > 0)
				{
					UpdateTextKeyVisibility(child, isSearch);
				}
			}
		}

		#endregion Text search

		#region IViewCommandSource members

		private ViewCommandManager viewCommandManager = new ViewCommandManager();
		public ViewCommandManager ViewCommandManager { get { return viewCommandManager; } }

		#endregion IViewCommandSource members
	}
}
