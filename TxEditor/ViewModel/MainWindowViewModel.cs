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
		private List<TextKeyViewModel> viewHistory = new List<TextKeyViewModel>();
		private int viewHistoryIndex;
		private OpFlag navigatingHistory = new OpFlag();

		#endregion Private data

		#region Constructors

		public MainWindowViewModel()
		{
			Instance = this;
			
			TextKeys = new Dictionary<string, TextKeyViewModel>();
			LoadedCultureNames = new HashSet<string>();
			DeletedCultureNames = new HashSet<string>();
			RootTextKey = new TextKeyViewModel(null, false, null, this);
			ProblemKeys = new ObservableHashSet<TextKeyViewModel>();

			searchDc = DelayedCall.Create(UpdateSearch, 250);
			SearchText = "";   // Change value once to set the clear button visibility
			ClearViewHistory();

			FontScale = App.Settings.FontScale;
		}

		#endregion Constructors

		#region Public properties

		public Dictionary<string, TextKeyViewModel> TextKeys { get; private set; }
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
			get { return cursorChar != null ? UnicodeInfo.GetChar(cursorChar[0]).Category : Tx.T("statusbar.char info.no character at cursor"); }
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
					if (selectedCulture != null)
					{
						LastSelectedCulture = selectedCulture;
					}
				}
			}
		}

		private string lastSelectedCulture;
		public string LastSelectedCulture
		{
			get { return lastSelectedCulture; }
			set
			{
				if (value != lastSelectedCulture)
				{
					lastSelectedCulture = value;
					OnPropertyChanged("LastSelectedCulture");
					UpdateSuggestions();
				}
			}
		}

		private ObservableCollection<SuggestionViewModel> suggestions = new ObservableCollection<SuggestionViewModel>();
		public ObservableCollection<SuggestionViewModel> Suggestions
		{
			get { return suggestions; }
		}

		private string suggestionsCulture;
		public string SuggestionsCulture
		{
			get { return suggestionsCulture; }
			set
			{
				if (value != suggestionsCulture)
				{
					suggestionsCulture = value;
					OnPropertyChanged("SuggestionsCulture");
					OnPropertyChanged("SuggestionsCultureCaption");
				}
			}
		}

		public string SuggestionsCultureCaption
		{
			get
			{
				if (!string.IsNullOrEmpty(suggestionsCulture))
					return Tx.TC("suggestions.caption for", "name", suggestionsCulture);
				else
					return Tx.TC("suggestions.caption");
			}
		}

		public int SelectionDummy
		{
			get { return 0; }
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

		internal bool CheckModifiedSaved()
		{
			if (fileModified)
			{
				var result = TaskDialog.Show(
					owner: MainWindow.Instance,
					title: "TxEditor",
					mainInstruction: Tx.T("msg.save.save changes"),
					content: Tx.T("msg.save.save changes.desc"),
					customButtons: new string[] { Tx.T("task dialog.button.save"), Tx.T("task dialog.button.dont save"), Tx.T("task dialog.button.cancel") },
					allowDialogCancellation: true);

				if (result.CustomButtonResult == 0)
				{
					// Save
					return Save();
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
			StatusText = Tx.T("statusbar.new dictionary created");
			loadedFilePath = null;
			loadedFilePrefix = null;
			UpdateTitle();
		}

		private void OnLoadFolder()
		{
			if (!CheckModifiedSaved()) return;

			var d = new OpenFolderDialog();
			d.Title = Tx.T("msg.load folder.title");
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
						mainInstruction: Tx.T("msg.load folder.multiple dictionaries in folder"),
						content: Tx.T("msg.load folder.multiple dictionaries in folder.desc"),
						radioButtons: prefixes.ToArray(),
						customButtons: new string[] { Tx.T("task dialog.button.load"), Tx.T("task dialog.button.cancel") },
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
								fileModified = false;   // Prevent another unsaved warning from OnNewFile
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
					ValidateTextKeysDelayed();
					StatusText = Tx.T("statusbar.n files loaded", fileCount) + Tx.T("statusbar.n text keys defined", TextKeys.Count);
					FileModified = false;
					ClearViewHistory();
				}
				else
				{
					MessageBox.Show(Tx.T("msg.load folder.no files found"), "TxEditor", MessageBoxButton.OK, MessageBoxImage.Warning);
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
			dlg.Title = Tx.T("msg.load file.title");
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
						Tx.T("msg.load file.cannot load different prefixes"),
						Tx.T("msg.caption.error"),
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
						fileModified = false;   // Prevent another unsaved warning from OnNewFile
						OnNewFile();
					}
					LoadFromXmlFile(fileName);
				}
				// TODO: Display a warning if multiple files claimed to be the primary culture, and which has won

				SortCulturesInTextKey(RootTextKey);
				DeletedCultureNames.Clear();
				ValidateTextKeysDelayed();
				StatusText = Tx.T("statusbar.n files loaded", dlg.FileNames.Length) + Tx.T("statusbar.n text keys defined", TextKeys.Count);
				FileModified = false;
				ClearViewHistory();
			}
		}

		private void OnSave()
		{
			Save();
		}

		private bool Save()
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
				dlg.Filter = Tx.T("file filter.tx dictionary files") + " (*.txd)|*.txd|" + Tx.T("file filter.xml files") + " (*.xml)|*.xml|" + Tx.T("file filter.all files") + " (*.*)|*.*";
				dlg.OverwritePrompt = true;
				dlg.Title = Tx.T("msg.save.title");
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
					return false;
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
						mainInstruction: Tx.T("msg.save.upgrade to format 2"),
						customButtons: new string[] { Tx.T("task dialog.button.upgrade"), Tx.T("task dialog.button.save in original format") },
						verificationText: Tx.T("msg.save.upgrade to format 2.dont ask again"));

					if (result.CustomButtonResult == null)
					{
						return false;
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
						mainInstruction: Tx.T("msg.save.incompatible with format 1"),
						content: Tx.T("msg.save.incompatible with format 1.desc"),
						customButtons: new string[] { Tx.T("task dialog.button.save anyway"), Tx.T("task dialog.button.dont save") });

					if (result.CustomButtonResult != 0)
					{
						return false;
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
			StatusText = Tx.T("statusbar.file saved");
			return true;
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
				StatusText = Tx.T("statusbar.culture added", "name", CultureInfoName(ci));
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
				Tx.T("msg.delete culture", "name", CultureInfoName(ci)),
				Tx.T("msg.delete culture.title"),
				MessageBoxButton.YesNo,
				MessageBoxImage.Question) == MessageBoxResult.Yes)
			{
				DeleteCulture(RootTextKey, SelectedCulture, true);
				StatusText = Tx.T("statusbar.culture deleted", "name", CultureInfoName(ci));
				FileModified = true;
			}
		}

		private void OnReplaceCulture()
		{
			//SortCulturesInTextKey(RootTextKey);
			//ValidateTextKeys();
			//FileModified = true;
			//SetPrimaryCultureCommand.RaiseCanExecuteChanged();
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
			string cultureName = CultureInfoName(ci);

			var result = TaskDialog.Show(
				owner: MainWindow.Instance,
				title: "TxEditor",
				mainInstruction: Tx.T("msg.set primary culture", "name", cultureName),
				content: Tx.T("msg.set primary culture.desc"),
				customButtons: new string[] { Tx.T("task dialog.button.switch"), Tx.T("task dialog.button.cancel") },
				allowDialogCancellation: true);

			if (result.CustomButtonResult == 0)
			{
				PrimaryCulture = SelectedCulture;
				SortCulturesInTextKey(RootTextKey);
				ValidateTextKeysDelayed();
				FileModified = true;
				StatusText = Tx.T("statusbar.primary culture set", "name", CultureInfoName(ci));
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

				ValidateTextKeysDelayed();
				FileModified = true;

				bool wasExpanded = tk.IsExpanded;
				tk.IsExpanded = true;   // Expands all parents
				if (!wasExpanded)
					tk.IsExpanded = false;   // Collapses this item again
				ViewCommandManager.InvokeLoaded("SelectTextKey", tk);

				if (alreadyExists)
				{
					StatusText = Tx.T("statusbar.text key already exists");
				}
				else
				{
					StatusText = Tx.T("statusbar.text key added");
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
			AppendViewHistory();
			UpdateNavigationButtons();
			UpdateSuggestions();
		}

		private bool CanDeleteTextKey()
		{
			return selectedTextKeys != null && selectedTextKeys.Count > 0;
		}

		private void OnDeleteTextKey()
		{
			if (selectedTextKeys == null || selectedTextKeys.Count == 0) return;

			int count = 0;
			bool onlyFullKeysSelected = true;
			foreach (TextKeyViewModel tk in selectedTextKeys)
			{
				// TODO: Check whether any selected key is a child of another selected key -> don't count them additionally
				count += CountTextKeys(tk);
				if (!tk.IsFullKey)
					onlyFullKeysSelected = false;
			}
			if (count == 0) return;   // Means there were nodes with no full keys, should not happen

			TaskDialogResult result;
			bool selectedOnlyOption = false;
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
			else if (onlyFullKeysSelected && selectedTextKeys.Count < count)
			{
				result = TaskDialog.Show(
					owner: MainWindow.Instance,
					allowDialogCancellation: true,
					title: "TxEditor",
					mainInstruction: "Deleting " + count + " text keys.",
					content: "You have selected full text keys that also contain other subkeys. Are you sure to delete the selected text keys with the texts of all cultures? This cannot be undone.",
					radioButtons: new string[] { "Also delete all subkeys", "Only delete selected keys" },
					customButtons: new string[] { "Delete", "Cancel" });
				selectedOnlyOption = result.RadioButtonResult == 1;
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
					DeleteTextKey(tk, !selectedOnlyOption);
					// Also remove unused partial keys
					DeletePartialParentKeys(tk.Parent as TextKeyViewModel);
					FileModified = true;
				}

				StatusText = Tx.T("statusbar.n text keys deleted", count);
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

		private void DeleteTextKey(TextKeyViewModel tk, bool includeChildren = true)
		{
			if (includeChildren)
			{
				foreach (TextKeyViewModel child in tk.Children.ToArray())
				{
					DeleteTextKey(child);
				}
			}
			if (tk.IsFullKey)
			{
				TextKeys.Remove(tk.TextKey);
				ProblemKeys.Remove(tk);
			}
			if (tk.Children.Count == 0)
			{
				tk.Parent.Children.Remove(tk);
			}
			else
			{
				tk.IsFullKey = false;
				tk.CultureTextVMs.Clear();
				tk.Comment = null;
				tk.Validate();
				OnPropertyChanged("SelectionDummy");
			}
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

			if (win.ShowDialog() == true)
			{
				HandleWizardInput(win.TextKeyText.Text, win.TranslationText.Text);
			}
		}

		private IntPtr fgWin;

		public void TextKeyWizardFromHotKey()
		{
			// Determine the currently active window
			fgWin = WinApi.GetForegroundWindow();

			// Require it to be Visual Studio, otherwise do nothing more
			StringBuilder sb = new StringBuilder(1000);
			WinApi.GetWindowText(fgWin, sb, 1000);
			if (!sb.ToString().EndsWith(" - Microsoft Visual Studio")) return;

			// Send Ctrl+C keys to the active window to copy the selected text
			// (First send events to release the still-pressed hot key buttons Ctrl and Shift)
			WinApi.INPUT[] inputs = new WinApi.INPUT[]
			{
				new WinApi.INPUT() { type = WinApi.INPUT_KEYBOARD, ki = new WinApi.KEYBDINPUT() { wVk = (short) WinApi.VK.CONTROL, dwFlags = WinApi.KEYEVENTF_KEYUP } },
				new WinApi.INPUT() { type = WinApi.INPUT_KEYBOARD, ki = new WinApi.KEYBDINPUT() { wVk = (short) WinApi.VK.SHIFT, dwFlags = WinApi.KEYEVENTF_KEYUP } },
				new WinApi.INPUT() { type = WinApi.INPUT_KEYBOARD, ki = new WinApi.KEYBDINPUT() { wVk = (short) WinApi.VK.CONTROL } },
				new WinApi.INPUT() { type = WinApi.INPUT_KEYBOARD, ki = new WinApi.KEYBDINPUT() { wVk = (short) WinApi.KeyToVk(System.Windows.Forms.Keys.C) } },
				new WinApi.INPUT() { type = WinApi.INPUT_KEYBOARD, ki = new WinApi.KEYBDINPUT() { wVk = (short) WinApi.KeyToVk(System.Windows.Forms.Keys.C), dwFlags = WinApi.KEYEVENTF_KEYUP } },
				new WinApi.INPUT() { type = WinApi.INPUT_KEYBOARD, ki = new WinApi.KEYBDINPUT() { wVk = (short) WinApi.VK.CONTROL, dwFlags = WinApi.KEYEVENTF_KEYUP } },
			};
			uint ret = WinApi.SendInput((uint) inputs.Length, inputs, System.Runtime.InteropServices.Marshal.SizeOf(typeof(WinApi.INPUT)));
			//System.Diagnostics.Debug.WriteLine(ret + " inputs sent");
			
			//System.Threading.Thread.Sleep(50);
			DelayedCall.Start(TextKeyWizardFromHotKey2, 50);
		}

		private void TextKeyWizardFromHotKey2()
		{
			// Create the wizard window
			TextKeyWizardWindow win = new TextKeyWizardWindow();
			//win.Owner = MainWindow.Instance;
			win.ShowInTaskbar = true;

			MainWindow.Instance.Hide();

			bool ok = false;
			if (win.ShowDialog() == true)
			{
				HandleWizardInput(win.TextKeyText.Text, win.TranslationText.Text);

				ok = true;
			}

			MainWindow.Instance.Show();
			// Activate the window we're initially coming from
			WinApi.SetForegroundWindow(fgWin);

			if (ok)
			{
				// Send Ctrl+V keys to paste the new Tx call with the text key,
				// directly replacing the literal string that was selected before
				WinApi.INPUT[] inputs = new WinApi.INPUT[]
				{
					new WinApi.INPUT() { type = WinApi.INPUT_KEYBOARD, ki = new WinApi.KEYBDINPUT() { wVk = (short) WinApi.VK.CONTROL } },
					new WinApi.INPUT() { type = WinApi.INPUT_KEYBOARD, ki = new WinApi.KEYBDINPUT() { wVk = (short) WinApi.KeyToVk(System.Windows.Forms.Keys.V) } },
					new WinApi.INPUT() { type = WinApi.INPUT_KEYBOARD, ki = new WinApi.KEYBDINPUT() { wVk = (short) WinApi.KeyToVk(System.Windows.Forms.Keys.V), dwFlags = WinApi.KEYEVENTF_KEYUP } },
					new WinApi.INPUT() { type = WinApi.INPUT_KEYBOARD, ki = new WinApi.KEYBDINPUT() { wVk = (short) WinApi.VK.CONTROL, dwFlags = WinApi.KEYEVENTF_KEYUP } },
				};
				uint ret = WinApi.SendInput((uint) inputs.Length, inputs, System.Runtime.InteropServices.Marshal.SizeOf(typeof(WinApi.INPUT)));
			}
		}

		private void HandleWizardInput(string keyName, string text)
		{
			TextKeyViewModel tk = FindOrCreateTextKey(keyName);

			bool alreadyExists = !tk.IsEmpty();

			// Ensure that all loaded cultures exist in every text key so that they can be entered
			foreach (string cn in LoadedCultureNames)
			{
				EnsureCultureInTextKey(tk, cn);
			}
			tk.UpdateCultureTextSeparators();

			// Set the text for the new key
			tk.CultureTextVMs[0].Text = text;

			ValidateTextKeysDelayed();
			FileModified = true;

			if (alreadyExists)
			{
				StatusText = Tx.T("statusbar.text key already exists");
			}
			else
			{
				StatusText = Tx.T("statusbar.text key added");
			}

			bool wasExpanded = tk.IsExpanded;
			tk.IsExpanded = true;   // Expands all parents
			if (!wasExpanded)
				tk.IsExpanded = false;   // Collapses the item again like it was before
			ViewCommandManager.InvokeLoaded("SelectTextKey", tk);
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
				bool destExists = tryDestKey != null && (!tryDestKey.IsEmpty() || tryDestKey.Children.Count > 0);
				if (destExists)
				{
					// TODO: What to consider if the destination key already has children?
					// TODO: Ask user to merge, overwrite or cancel.
					MessageBox.Show(
						"The destination key already exists. Handling this is not yet implemented.",
						Tx.T("msg.caption.error"),
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
					return;
				}
				
				var oldParent = selKey.Parent;

				// Remove the selected key from its original tree position
				oldParent.Children.Remove(selKey);

				TextKeyViewModel destKey = FindOrCreateTextKey(newKey, false);

				if (!destExists)
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
				StatusText = Tx.T("statusbar.text key renamed");

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
			return viewHistoryIndex > 0;
		}

		private void OnNavigateBack()
		{
			ViewHistoryBack();
			UpdateNavigationButtons();
		}

		private bool CanNavigateForward()
		{
			return viewHistoryIndex < viewHistory.Count - 1;
		}

		private void OnNavigateForward()
		{
			ViewHistoryForward();
			UpdateNavigationButtons();
		}

		private bool CanGotoDefinition()
		{
			return false;
		}

		private void OnGotoDefinition()
		{
		}

		private void UpdateNavigationButtons()
		{
			NavigateBackCommand.RaiseCanExecuteChanged();
			NavigateForwardCommand.RaiseCanExecuteChanged();
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
			ValidateTextKeysDelayed();
			StatusText = Tx.T("statusbar.n files loaded", count) + Tx.T("statusbar.n text keys defined", TextKeys.Count);
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

				string comment = null;
				XmlAttribute commentAttr = textNode.Attributes["comment"];
				if (commentAttr != null)
				{
					comment = commentAttr.Value;
					if (string.IsNullOrWhiteSpace(comment))
						comment = null;
				}

				TextKeyViewModel tk = FindOrCreateTextKey(key);

				if (comment != null)
					tk.Comment = comment;

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
				if (updateTextKeys && !TextKeys.ContainsKey(textKey))
					TextKeys.Add(textKey, tk);
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

						// Add the text key comment to the primary culture
						// (If no primary culture is set, the first-displayed is used to save the comments)
						if (!string.IsNullOrWhiteSpace(textKeyVM.Comment))
						{
							if (PrimaryCulture != null && cultureName == PrimaryCulture ||
								PrimaryCulture == null && cultureName == textKeyVM.CultureTextVMs[0].CultureName)
							{
								var commentAttr = xe.OwnerDocument.CreateAttribute("comment");
								commentAttr.Value = textKeyVM.Comment;
								textElement.Attributes.Append(commentAttr);
							}
						}
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

		private DelayedCall validateDc;

		public void ValidateTextKeysDelayed()
		{
			if (validateDc == null)
			{
				validateDc = DelayedCall.Start(ValidateTextKeys, 500);
			}
			else
			{
				validateDc.Reset();
			}
		}

		public void ValidateTextKeys()
		{
			RootTextKey.Validate();
			UpdateSuggestions();
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
				ValidateTextKeysDelayed();
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
				ValidateTextKeysDelayed();
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

		public static string CultureInfoName(CultureInfo ci, bool includeCode = true)
		{
			return Tx.U(App.Settings.NativeCultureNames ? ci.NativeName : ci.DisplayName) +
				(includeCode ? " [" + ci.IetfLanguageTag + "]" : "");
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

		private void SelectTextKey(TextKeyViewModel tk, bool async = false)
		{
			if (tk != null)
			{
				bool wasExpanded = tk.IsExpanded;
				tk.IsExpanded = true;   // Expands all parents
				if (!wasExpanded)
					tk.IsExpanded = false;   // Collapses the item again like it was before
			}
			if (async)
				ViewCommandManager.InvokeLoaded("SelectTextKey", tk);
			else
				ViewCommandManager.Invoke("SelectTextKey", tk);
		}

		private void SelectTextKey(string textKey, bool async = false)
		{
			TextKeyViewModel tk;
			if (TextKeys.TryGetValue(textKey, out tk))
			{
				SelectTextKey(tk, async);
			}
		}

		private void SelectCultureText(TextKeyViewModel tk, string cultureName)
		{
			if (tk != null &&
				tk.CultureTextVMs != null)
			{
				var ct = tk.CultureTextVMs.FirstOrDefault(vm => vm.CultureName == cultureName);
				if (ct != null)
				{
					ct.ViewCommandManager.InvokeLoaded("FocusText");
				}
			}
		}

		private void ClearViewHistory()
		{
			viewHistory.Clear();
			if (selectedTextKeys != null && selectedTextKeys.Count > 0)
				viewHistory.Add(selectedTextKeys[0] as TextKeyViewModel);
			else
				viewHistory.Add(null);
			viewHistoryIndex = 0;
		}

		private void AppendViewHistory()
		{
			if (navigatingHistory.IsSet)
			{
				// Currently navigating through the history, don't interfer that
				return;
			}
			if (selectedTextKeys != null &&
				selectedTextKeys.Count > 0 &&
				selectedTextKeys[0] == viewHistory[viewHistory.Count - 1])
			{
				// First selected item has not changed, do nothing
				return;
			}
			if (selectedTextKeys != null &&
				selectedTextKeys.Count == 0)
			{
				// Nothing selected, nothing to remember
				return;
			}

			// Clear any future history
			while (viewHistory.Count > viewHistoryIndex + 1)
			{
				viewHistory.RemoveAt(viewHistory.Count - 1);
			}

			if (selectedTextKeys != null && selectedTextKeys.Count > 0)
				viewHistory.Add(selectedTextKeys[0] as TextKeyViewModel);
			else
				viewHistory.Add(null);
			viewHistoryIndex++;
		}

		private void ViewHistoryBack()
		{
			if (viewHistoryIndex > 0)
			{
				using (new OpLock(navigatingHistory))
				{
					viewHistoryIndex--;
					SelectTextKey(viewHistory[viewHistoryIndex]);
					SelectCultureText(viewHistory[viewHistoryIndex], LastSelectedCulture);
				}
			}
		}

		private void ViewHistoryForward()
		{
			if (viewHistoryIndex < viewHistory.Count - 1)
			{
				using (new OpLock(navigatingHistory))
				{
					viewHistoryIndex++;
					SelectTextKey(viewHistory[viewHistoryIndex]);
					SelectCultureText(viewHistory[viewHistoryIndex], LastSelectedCulture);
				}
			}
		}

		#endregion Window management

		#region Text search

		private DelayedCall searchDc;
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
					searchDc.Reset();
				}
			}
		}

		private string shadowSearchText;
		public string ShadowSearchText
		{
			get
			{
				return shadowSearchText;
			}
			set
			{
				if (value != shadowSearchText)
				{
					shadowSearchText = value;
					OnPropertyChanged("ShadowSearchText");
				}
			}
		}

		/// <summary>
		/// Updates the visibility of all text keys in the tree, according to the entered search term.
		/// </summary>
		public void UpdateSearch()
		{
			ShadowSearchText = SearchText;

			bool isSearch = !string.IsNullOrWhiteSpace(searchText);
			int count = UpdateTextKeyVisibility(RootTextKey, isSearch);
			if (isSearch)
				StatusText = Tx.T("statusbar.n results", count);
			else
				StatusText = "";
		}

		private int UpdateTextKeyVisibility(TextKeyViewModel tk, bool isSearch)
		{
			int count = 0;
			foreach (TextKeyViewModel child in tk.Children)
			{
				bool isVisible =
					!isSearch ||
					child.TextKey.ToLower().Contains(searchText.ToLower()) ||
					child.CultureTextVMs.Any(ct => ct.Text != null && ct.Text.ToLower().Contains(searchText.ToLower()));
				if (problemFilterActive)
				{
					isVisible &= child.HasOwnProblem;
				}

				child.IsVisible = isVisible;
				if (isVisible)
				{
					count++;
					TreeViewItemViewModel parent = child.Parent;
					while (parent != null)
					{
						parent.IsVisible = true;
						parent = parent.Parent;
					}
				}
				if (child.Children.Count > 0)
				{
					count += UpdateTextKeyVisibility(child, isSearch);
				}
			}
			return count;
		}

		#endregion Text search

		#region Suggestions

		private void AddDummySuggestion()
		{
			SuggestionViewModel suggestion = new SuggestionViewModel(this);
			suggestion.BaseText = Tx.T("suggestions.none");
			suggestions.Add(suggestion);
		}

		private void UpdateSuggestions()
		{
			Match m;

			suggestions.Clear();

			if (string.IsNullOrEmpty(lastSelectedCulture))
			{
				AddDummySuggestion();
				return;
			}
			if (!LoadedCultureNames.Contains(lastSelectedCulture))
			{
				AddDummySuggestion();
				return;
			}
			SuggestionsCulture = CultureInfoName(new CultureInfo(lastSelectedCulture), false);
			//if (lastSelectedCulture == primaryCulture) return;

			TextKeyViewModel tk = selectedTextKeys != null && selectedTextKeys.Count > 0 ? selectedTextKeys[0] as TextKeyViewModel : null;
			if (tk == null || tk.CultureTextVMs.Count < 1)
			{
				AddDummySuggestion();
				return;
			}

			// The text we're finding translation suggestions for
			string refText = tk.CultureTextVMs[0].Text;
			if (refText == null)
			{
				AddDummySuggestion();
				return;
			}

			//// Find the most common words to filter them out
			//Dictionary<string, int> wordCount = new Dictionary<string, int>();
			//foreach (var kvp in TextKeys)
			//{
			//    string otherBaseText = kvp.Value.CultureTextVMs[0].Text;
			//    if (string.IsNullOrEmpty(otherBaseText)) continue;

			//    // Remove all placeholders and key references
			//    string otherText = Regex.Replace(otherBaseText, @"(?<!\{)\{[^{]*?\}", "");

			//    // Extract all words
			//    m = Regex.Match(otherText, @"(\w{2,})");
			//    while (m.Success)
			//    {
			//        string lcWord = m.Groups[1].Value.ToLowerInvariant();

			//        int count;
			//        if (wordCount.TryGetValue(lcWord, out count))
			//        {
			//            wordCount[lcWord] = count + 1;
			//        }
			//        else
			//        {
			//            wordCount[lcWord] = 1;
			//        }

			//        m = m.NextMatch();
			//    }
			//}

			//HashSet<string> commonWords = new HashSet<string>();
			//if (wordCount.Count > 0)
			//{
			//    int maxCount = wordCount.Select(kvp => kvp.Value).Max();
			//    foreach (var kvp in wordCount.OrderByDescending(kvp => kvp.Value))
			//    {
			//        if (commonWords.Count < (int) (wordCount.Count * 0.05) ||
			//            kvp.Value >= (int) (maxCount * 0.8))
			//        {
			//            commonWords.Add(kvp.Key);
			//        }
			//    }
			//}

			//commonWords.Clear();
			//commonWords.Add("all");
			//commonWords.Add("also");
			//commonWords.Add("an");
			//commonWords.Add("and");
			//commonWords.Add("are");
			//commonWords.Add("as");
			//commonWords.Add("at");
			//commonWords.Add("be");
			//commonWords.Add("but");
			//commonWords.Add("by");
			//commonWords.Add("can");
			//commonWords.Add("cannot");
			//commonWords.Add("do");
			//commonWords.Add("don");
			//commonWords.Add("each");
			//commonWords.Add("for");
			//commonWords.Add("from");
			//commonWords.Add("have");
			//commonWords.Add("if");
			//commonWords.Add("in");
			//commonWords.Add("into");
			//commonWords.Add("is");
			//commonWords.Add("it");
			//commonWords.Add("its");
			//commonWords.Add("may");
			//commonWords.Add("must");
			//commonWords.Add("no");
			//commonWords.Add("not");
			//commonWords.Add("of");
			//commonWords.Add("on");
			//commonWords.Add("please");
			//commonWords.Add("that");
			//commonWords.Add("the");
			//commonWords.Add("there");
			//commonWords.Add("this");
			//commonWords.Add("those");
			//commonWords.Add("to");
			//commonWords.Add("were");
			//commonWords.Add("will");
			//commonWords.Add("with");
			//commonWords.Add("would");
			//commonWords.Add("you");
			//commonWords.Add("your");

			HashSet<string> commonWords;
			if (lastSelectedCulture.StartsWith("de"))
			{
				// GERMAN STOPWORDS 
				// Zusammmengetragen von Marco Götze, Steffen Geyer
				// Last update: 2011-01-15
				// Source: http://solariz.de/649/deutsche-stopwords.htm
				// Via: http://en.wikipedia.org/wiki/Stop_words
				commonWords = new HashSet<string>(new string[]
				{
					"ab", "aber", "abgerufen", "abgerufene", "abgerufener", "abgerufenes", "acht", "ähnlich", "alle", "allein", "allem",
					"allen", "aller", "allerdings", "allerlei", "alles", "allgemein", "allmählich", "allzu", "als", "alsbald", "also",
					"am", "an", "ander", "andere", "anderem", "anderen", "anderer", "andererseits", "anderes", "anderm", "andern",
					"andernfalls", "anders", "anerkannt", "anerkannte", "anerkannter", "anerkanntes", "anfangen", "anfing", "angefangen",
					"angesetze", "angesetzt", "angesetzten", "angesetzter", "ansetzen", "anstatt", "arbeiten", "auch", "auf", "aufgehört",
					"aufgrund", "aufhören", "aufhörte", "aufzusuchen", "aus", "ausdrücken", "ausdrückt", "ausdrückte", "ausgenommen",
					"außen", "ausser", "außer", "ausserdem", "außerdem", "außerhalb", "author", "autor", "bald", "bearbeite",
					"bearbeiten", "bearbeitete", "bearbeiteten", "bedarf", "bedürfen", "bedurfte", "befragen", "befragte", "befragten",
					"befragter", "begann", "beginnen", "begonnen", "behalten", "behielt", "bei", "beide", "beiden", "beiderlei", "beides",
					"beim", "beinahe", "beitragen", "beitrugen", "bekannt", "bekannte", "bekannter", "bekennen", "benutzt", "bereits",
					"berichten", "berichtet", "berichtete", "berichteten", "besonders", "besser", "bestehen", "besteht", "beträchtlich",
					"bevor", "bezüglich", "bietet", "bin", "bis", "bis", "bisher", "bislang", "bist", "bleiben", "blieb", "bloß", "bloss",
					"böden", "brachte", "brachten", "brauchen", "braucht", "bräuchte", "bringen", "bsp", "bzw", "ca", "da", "dabei",
					"dadurch", "dafür", "dagegen", "daher", "dahin", "damals", "damit", "danach", "daneben", "dank", "danke", "danken",
					"dann", "dannen", "daran", "darauf", "daraus", "darf", "darfst", "darin", "darüber", "darüberhinaus", "darum",
					"darunter", "das", "daß", "dass", "dasselbe", "davon", "davor", "dazu", "dein", "deine", "deinem", "deinen", "deiner",
					"deines", "dem", "demnach", "demselben", "den", "denen", "denn", "dennoch", "denselben", "der", "derart", "derartig",
					"derem", "deren", "derer", "derjenige", "derjenigen", "derselbe", "derselben", "derzeit", "des", "deshalb",
					"desselben", "dessen", "desto", "deswegen", "dich", "die", "diejenige", "dies", "diese", "dieselbe", "dieselben",
					"diesem", "diesen", "dieser", "dieses", "diesseits", "dinge", "dir", "direkt", "direkte", "direkten", "direkter",
					"doch", "doppelt", "dort", "dorther", "dorthin", "drauf", "drei", "dreißig", "drin", "dritte", "drüber", "drunter",
					"du", "dunklen", "durch", "durchaus", "dürfen", "durfte", "dürfte", "durften", "eben", "ebenfalls", "ebenso", "ehe",
					"eher", "eigenen", "eigenes", "eigentlich", "ein", "einbaün", "eine", "einem", "einen", "einer", "einerseits",
					"eines", "einfach", "einführen", "einführte", "einführten", "eingesetzt", "einig", "einige", "einigem", "einigen",
					"einiger", "einigermaßen", "einiges", "einmal", "eins", "einseitig", "einseitige", "einseitigen", "einseitiger",
					"einst", "einstmals", "einzig", "ende", "entsprechend", "entweder", "er", "ergänze", "ergänzen", "ergänzte",
					"ergänzten", "erhält", "erhalten", "erhielt", "erhielten", "erneut", "eröffne", "eröffnen", "eröffnet", "eröffnete",
					"eröffnetes", "erst", "erste", "ersten", "erster", "es", "etc", "etliche", "etwa", "etwas", "euch", "euer", "eure",
					"eurem", "euren", "eurer", "eures", "fall", "falls", "fand", "fast", "ferner", "finden", "findest", "findet",
					"folgende", "folgenden", "folgender", "folgendes", "folglich", "fordern", "fordert", "forderte", "forderten",
					"fortsetzen", "fortsetzt", "fortsetzte", "fortsetzten", "fragte", "frau", "frei", "freie", "freier", "freies", "fuer",
					"fünf", "für", "gab", "gängig", "gängige", "gängigen", "gängiger", "gängiges", "ganz", "ganze", "ganzem", "ganzen",
					"ganzer", "ganzes", "gänzlich", "gar", "gbr", "geb", "geben", "geblieben", "gebracht", "gedurft", "geehrt", "geehrte",
					"geehrten", "geehrter", "gefallen", "gefälligst", "gefällt", "gefiel", "gegeben", "gegen", "gehabt", "gehen", "geht",
					"gekommen", "gekonnt", "gemacht", "gemäss", "gemocht", "genommen", "genug", "gern", "gesagt", "gesehen", "gestern",
					"gestrige", "getan", "geteilt", "geteilte", "getragen", "gewesen", "gewissermaßen", "gewollt", "geworden", "ggf",
					"gib", "gibt", "gleich", "gleichwohl", "gleichzeitig", "glücklicherweise", "gmbh", "gratulieren", "gratuliert",
					"gratulierte", "gute", "guten", "hab", "habe", "haben", "haette", "halb", "hallo", "hast", "hat", "hätt", "hatte",
					"hätte", "hatten", "hätten", "hattest", "hattet", "heraus", "herein", "heute", "heutige", "hier", "hiermit",
					"hiesige", "hin", "hinein", "hinten", "hinter", "hinterher", "hoch", "höchstens", "hundert", "ich", "igitt", "ihm",
					"ihn", "ihnen", "ihr", "ihre", "ihrem", "ihren", "ihrer", "ihres", "im", "immer", "immerhin", "important", "in",
					"indem", "indessen", "info", "infolge", "innen", "innerhalb", "ins", "insofern", "inzwischen", "irgend", "irgendeine",
					"irgendwas", "irgendwen", "irgendwer", "irgendwie", "irgendwo", "ist", "ja", "jährig", "jährige", "jährigen",
					"jähriges", "je", "jede", "jedem", "jeden", "jedenfalls", "jeder", "jederlei", "jedes", "jedoch", "jemand", "jene",
					"jenem", "jenen", "jener", "jenes", "jenseits", "jetzt", "kam", "kann", "kannst", "kaum", "kein", "keine", "keinem",
					"keinen", "keiner", "keinerlei", "keines", "keines", "keineswegs", "klar", "klare", "klaren", "klares", "klein",
					"kleinen", "kleiner", "kleines", "koennen", "koennt", "koennte", "koennten", "komme", "kommen", "kommt", "konkret",
					"konkrete", "konkreten", "konkreter", "konkretes", "könn", "können", "könnt", "konnte", "könnte", "konnten",
					"könnten", "künftig", "lag", "lagen", "langsam", "längst", "längstens", "lassen", "laut", "lediglich", "leer",
					"legen", "legte", "legten", "leicht", "leider", "lesen", "letze", "letzten", "letztendlich", "letztens", "letztes",
					"letztlich", "lichten", "liegt", "liest", "links", "mache", "machen", "machst", "macht", "machte", "machten", "mag",
					"magst", "mal", "man", "manche", "manchem", "manchen", "mancher", "mancherorts", "manches", "manchmal", "mann",
					"margin", "mehr", "mehrere", "mein", "meine", "meinem", "meinen", "meiner", "meines", "meist", "meiste", "meisten",
					"meta", "mich", "mindestens", "mir", "mit", "mithin", "mochte", "möchte", "möchten", "möchtest", "mögen", "möglich",
					"mögliche", "möglichen", "möglicher", "möglicherweise", "morgen", "morgige", "muessen", "muesst", "muesste", "muss",
					"muß", "müssen", "mußt", "musst", "müßt", "musste", "müßte", "müsste", "mussten", "müssten", "nach", "nachdem",
					"nacher", "nachhinein", "nächste", "nacht", "nahm", "nämlich", "natürlich", "neben", "nebenan", "nehmen", "nein",
					"neu", "neue", "neuem", "neuen", "neuer", "neues", "neun", "nicht", "nichts", "nie", "niemals", "niemand", "nimm",
					"nimmer", "nimmt", "nirgends", "nirgendwo", "noch", "nötigenfalls", "nun", "nur", "nutzen", "nutzt", "nützt",
					"nutzung", "ob", "oben", "oberhalb", "obgleich", "obschon", "obwohl", "oder", "oft", "ohne", "per", "pfui",
					"plötzlich", "pro", "reagiere", "reagieren", "reagiert", "reagierte", "rechts", "regelmäßig", "rief", "rund", "sage",
					"sagen", "sagt", "sagte", "sagten", "sagtest", "sämtliche", "sang", "sangen", "schätzen", "schätzt", "schätzte",
					"schätzten", "schlechter", "schließlich", "schnell", "schon", "schreibe", "schreiben", "schreibens", "schreiber",
					"schwierig", "sechs", "sect", "sehe", "sehen", "sehr", "sehrwohl", "seht", "sei", "seid", "sein", "seine", "seinem",
					"seinen", "seiner", "seines", "seit", "seitdem", "seite", "seiten", "seither", "selber", "selbst", "senke", "senken",
					"senkt", "senkte", "senkten", "setzen", "setzt", "setzte", "setzten", "sich", "sicher", "sicherlich", "sie", "sieben",
					"siebte", "siehe", "sieht", "sind", "singen", "singt", "so", "sobald", "sodaß", "soeben", "sofern", "sofort", "sog",
					"sogar", "solange", "solc hen", "solch", "solche", "solchem", "solchen", "solcher", "solches", "soll", "sollen",
					"sollst", "sollt", "sollte", "sollten", "solltest", "somit", "sondern", "sonst", "sonstwo", "sooft", "soviel",
					"soweit", "sowie", "sowohl", "später", "spielen", "startet", "startete", "starteten", "statt", "stattdessen", "steht",
					"steige", "steigen", "steigt", "stets", "stieg", "stiegen", "such", "suchen", "tages", "tat", "tät", "tatsächlich",
					"tatsächlichen", "tatsächlicher", "tatsächliches", "tausend", "teile", "teilen", "teilte", "teilten", "titel",
					"total", "trage", "tragen", "trägt", "trotzdem", "trug", "tun", "tust", "tut", "txt", "übel", "über", "überall",
					"überallhin", "überdies", "übermorgen", "übrig", "übrigens", "ueber", "um", "umso", "unbedingt", "und", "ungefähr",
					"unmöglich", "unmögliche", "unmöglichen", "unmöglicher", "unnötig", "uns", "unse", "unsem", "unsen", "unser", "unser",
					"unsere", "unserem", "unseren", "unserer", "unseres", "unserm", "unses", "unten", "unter", "unterbrach",
					"unterbrechen", "unterhalb", "unwichtig", "usw", "vergangen", "vergangene", "vergangener", "vergangenes", "vermag",
					"vermögen", "vermutlich", "veröffentlichen", "veröffentlicher", "veröffentlicht", "veröffentlichte",
					"veröffentlichten", "veröffentlichtes", "verrate", "verraten", "verriet", "verrieten", "version", "versorge",
					"versorgen", "versorgt", "versorgte", "versorgten", "versorgtes", "viel", "viele", "vielen", "vieler", "vieles",
					"vielleicht", "vielmals", "vier", "völlig", "vollständig", "vom", "von", "vor", "voran", "vorbei", "vorgestern",
					"vorher", "vorne", "vorüber", "wachen", "waere", "während", "während", "währenddessen", "wann", "war", "wär", "wäre",
					"waren", "wären", "warst", "warum", "was", "weder", "weg", "wegen", "weil", "weiß", "weiter", "weitere", "weiterem",
					"weiteren", "weiterer", "weiteres", "weiterhin", "welche", "welchem", "welchen", "welcher", "welches", "wem", "wen",
					"wenig", "wenige", "weniger", "wenigstens", "wenn", "wenngleich", "wer", "werde", "werden", "werdet", "weshalb",
					"wessen", "wichtig", "wie", "wieder", "wieso", "wieviel", "wiewohl", "will", "willst", "wir", "wird", "wirklich",
					"wirst", "wo", "wodurch", "wogegen", "woher", "wohin", "wohingegen", "wohl", "wohlweislich", "wolle", "wollen",
					"wollt", "wollte", "wollten", "wolltest", "wolltet", "womit", "woraufhin", "woraus", "worin", "wurde", "würde",
					"wurden", "würden", "zahlreich", "zehn", "zeitweise", "ziehen", "zieht", "zog", "zogen", "zu", "zudem", "zuerst",
					"zufolge", "zugleich", "zuletzt", "zum", "zumal", "zur", "zurück", "zusammen", "zuviel", "zwanzig", "zwar", "zwei",
					"zwischen", "zwölf"
				});
			}
			else if (lastSelectedCulture.StartsWith("en"))
			{
				// English stop words
				// Source: http://norm.al/2009/04/14/list-of-english-stop-words/ (MySQL fulltext, from 2009-10-03)
				// Via: http://en.wikipedia.org/wiki/Stop_words
				commonWords = new HashSet<string>(new string[]
				{
					"able", "about", "above", "according", "accordingly", "across", "actually", "after", "afterwards", "again", "against",
					"ain", "all", "allow", "allows", "almost", "alone", "along", "already", "also", "although", "always", "am", "among",
					"amongst", "an", "and", "another", "any", "anybody", "anyhow", "anyone", "anything", "anyway", "anyways", "anywhere",
					"apart", "appear", "appreciate", "appropriate", "are", "aren", "around", "as", "aside", "ask", "asking", "associated",
					"at", "available", "away", "awfully", "be", "became", "because", "become", "becomes", "becoming", "been", "before",
					"beforehand", "behind", "being", "believe", "below", "beside", "besides", "best", "better", "between", "beyond",
					"both", "brief", "but", "by", "mon", "came", "can", "cannot", "cause", "causes", "certain", "certainly", "changes",
					"clearly", "co", "com", "come", "comes", "concerning", "consequently", "consider", "considering", "contain",
					"containing", "contains", "corresponding", "could", "couldn", "course", "currently", "definitely", "described",
					"despite", "did", "didn", "different", "do", "does", "doesn", "doing", "don", "done", "down", "downwards", "during",
					"each", "edu", "eg", "eight", "either", "else", "elsewhere", "enough", "entirely", "especially", "et", "etc", "even",
					"ever", "every", "everybody", "everyone", "everything", "everywhere", "ex", "exactly", "example", "except", "far",
					"few", "fifth", "first", "five", "followed", "following", "follows", "for", "former", "formerly", "forth", "four",
					"from", "further", "furthermore", "get", "gets", "getting", "given", "gives", "go", "goes", "going", "gone", "got",
					"gotten", "greetings", "had", "hadn", "happens", "hardly", "has", "hasn", "have", "haven", "having", "he", "hello",
					"help", "hence", "her", "here", "hereafter", "hereby", "herein", "hereupon", "hers", "herself", "hi", "him",
					"himself", "his", "hither", "hopefully", "how", "howbeit", "however", "if", "ignored", "immediate", "in", "inasmuch",
					"inc", "indeed", "indicate", "indicated", "indicates", "inner", "insofar", "instead", "into", "inward", "is", "isn",
					"it", "its", "itself", "just", "keep", "keeps", "kept", "know", "knows", "known", "last", "lately", "later", "latter",
					"latterly", "least", "less", "lest", "let", "like", "liked", "likely", "little", "ll", "look", "looking", "looks",
					"ltd", "mainly", "many", "may", "maybe", "me", "mean", "meanwhile", "merely", "might", "more", "moreover", "most",
					"mostly", "much", "must", "my", "myself", "name", "namely", "nd", "near", "nearly", "necessary", "need", "needs",
					"neither", "never", "nevertheless", "new", "next", "nine", "no", "nobody", "non", "none", "noone", "nor", "normally",
					"not", "nothing", "novel", "now", "nowhere", "obviously", "of", "off", "often", "oh", "ok", "okay", "old", "on",
					"once", "one", "ones", "only", "onto", "or", "other", "others", "otherwise", "ought", "our", "ours", "ourselves",
					"out", "outside", "over", "overall", "own", "particular", "particularly", "per", "perhaps", "placed", "please",
					"plus", "possible", "presumably", "probably", "provides", "que", "quite", "qv", "rather", "rd", "re", "really",
					"reasonably", "regarding", "regardless", "regards", "relatively", "respectively", "right", "said", "same", "saw",
					"say", "saying", "says", "second", "secondly", "see", "seeing", "seem", "seemed", "seeming", "seems", "seen", "self",
					"selves", "sensible", "sent", "serious", "seriously", "seven", "several", "shall", "she", "should", "shouldn",
					"since", "six", "so", "some", "somebody", "somehow", "someone", "something", "sometime", "sometimes", "somewhat",
					"somewhere", "soon", "sorry", "specified", "specify", "specifying", "still", "sub", "such", "sup", "sure", "take",
					"taken", "tell", "tends", "th", "than", "thank", "thanks", "thanx", "that", "thats", "the", "their", "theirs", "them",
					"themselves", "then", "thence", "there", "thereafter", "thereby", "therefore", "therein", "theres", "thereupon",
					"these", "they", "think", "third", "this", "thorough", "thoroughly", "those", "though", "three", "through",
					"throughout", "thru", "thus", "to", "together", "too", "took", "toward", "towards", "tried", "tries", "truly", "try",
					"trying", "twice", "two", "un", "under", "unfortunately", "unless", "unlikely", "until", "unto", "up", "upon", "us",
					"use", "used", "useful", "uses", "using", "usually", "value", "various", "ve", "very", "via", "viz", "vs", "want",
					"wants", "was", "wasn", "way", "we", "welcome", "well", "went", "were", "weren", "what", "whatever", "when", "whence",
					"whenever", "where", "whereafter", "whereas", "whereby", "wherein", "whereupon", "wherever", "whether", "which",
					"while", "whither", "who", "whoever", "whole", "whom", "whose", "why", "will", "willing", "wish", "with", "within",
					"without", "won", "wonder", "would", "would", "wouldn", "yes", "yet", "you", "your", "yours", "yourself",
					"yourselves", "zero"
				});
			}
			else
			{
				commonWords = new HashSet<string>();
			}

			// Remove all placeholders and key references
			refText = Regex.Replace(refText, @"(?<!\{)\{[^{]*?\}", "");
			
			// Extract all words
			List<string> refWords = new List<string>();
			m = Regex.Match(refText, @"(\w{2,})");
			while (m.Success)
			{
				if (!commonWords.Contains(m.Groups[1].Value.ToLowerInvariant()))   // Skip common words
					refWords.Add(m.Groups[1].Value);
				m = m.NextMatch();
			}

			// Find other text keys that contain these words in their primary culture text
			Dictionary<TextKeyViewModel, float> otherKeys = new Dictionary<TextKeyViewModel, float>();
			foreach (var kvp in TextKeys)
			{
				if (kvp.Value.TextKey == tk.TextKey) continue;   // Skip currently selected item
				if (kvp.Value.TextKey.StartsWith("Tx:")) continue;   // Skip system keys

				float score = 0;
				string otherBaseText = kvp.Value.CultureTextVMs[0].Text;
				string otherTranslatedText = kvp.Value.CultureTextVMs.First(ct => ct.CultureName == lastSelectedCulture).Text;

				if (string.IsNullOrEmpty(otherBaseText)) continue;
				if (string.IsNullOrEmpty(otherTranslatedText)) continue;

				// Remove all placeholders and key references
				string otherText = Regex.Replace(otherBaseText, @"(?<!\{)\{[^{]*?\}", "");

				// Extract all words
				List<string> otherWords = new List<string>();
				m = Regex.Match(otherText, @"(\w{2,})");
				while (m.Success)
				{
					if (!commonWords.Contains(m.Groups[1].Value.ToLowerInvariant()))   // Skip common words
						otherWords.Add(m.Groups[1].Value);
					m = m.NextMatch();
				}

				// Increase score by 1 for each case-insensitively matching word
				foreach (string word in refWords)
				{
					if (otherWords.Any(w => string.Equals(w, word, StringComparison.InvariantCultureIgnoreCase)))
						score += 1;
				}
				// Increase score by 2 for each case-sensitively matching word
				foreach (string word in refWords)
				{
					if (otherWords.Any(w => string.Equals(w, word, StringComparison.InvariantCulture)))
						score += 2;
				}

				// Divide by the square root of the number of relevant words. (Using the square
				// root to reduce the effect for very long texts.)
				if (otherWords.Count > 0)
				{
					score /= (float) Math.Sqrt(otherWords.Count);
				}
				else
				{
					score = 0;   // Should not happen
				}

				// Accept every text key with a threshold score
				if (score >= 0.5f)
				{
					otherKeys.Add(kvp.Value, score);
				}
			}

			// Sort all matches by their score
			foreach (var kvp in otherKeys.OrderByDescending(kvp => kvp.Value))
			{
				try
				{
					SuggestionViewModel suggestion = new SuggestionViewModel(this);
					suggestion.TextKey = kvp.Key.TextKey;
					suggestion.BaseText = kvp.Key.CultureTextVMs[0].Text;
					if (lastSelectedCulture != primaryCulture)
						suggestion.TranslatedText = kvp.Key.CultureTextVMs.First(ct => ct.CultureName == lastSelectedCulture).Text;
					suggestion.Score = kvp.Value.ToString("0.00");
					suggestions.Add(suggestion);
				}
				catch
				{
					// Something's missing (probably a LINQ-related exception), ignore that item
				}
			}

			if (suggestions.Count == 0)
			{
				AddDummySuggestion();
			}
		}

		#endregion Suggestions

		#region IViewCommandSource members

		private ViewCommandManager viewCommandManager = new ViewCommandManager();
		public ViewCommandManager ViewCommandManager { get { return viewCommandManager; } }

		#endregion IViewCommandSource members
	}
}
