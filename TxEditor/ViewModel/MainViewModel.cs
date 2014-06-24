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
using System.Windows.Media.Effects;
using System.Xml;
using Microsoft.Win32;
using TaskDialogInterop;
using Unclassified.FieldLog;
using Unclassified.TxEditor.View;
using Unclassified.TxLib;
using Unclassified.UI;
using Unclassified.Util;

namespace Unclassified.TxEditor.ViewModel
{
	internal class MainViewModel : ViewModelBase, IViewCommandSource
	{
		#region Static data

		public static MainViewModel Instance { get; private set; }

		#endregion Static data

		#region Private data

		private int fileVersion;
		private string loadedFilePath;
		private string loadedFilePrefix;
		private int readonlyFilesCount;
		private List<TextKeyViewModel> selectedTextKeys;
		private List<TextKeyViewModel> viewHistory = new List<TextKeyViewModel>();
		private int viewHistoryIndex;
		private OpFlag navigatingHistory = new OpFlag();

		#endregion Private data

		#region Constructor

		public MainViewModel()
		{
			Instance = this;

			InitializeCommands();

			TextKeys = new Dictionary<string, TextKeyViewModel>();
			LoadedCultureNames = new HashSet<string>();
			DeletedCultureNames = new HashSet<string>();
			RootTextKey = new TextKeyViewModel(null, false, null, this);
			ProblemKeys = new ObservableHashSet<TextKeyViewModel>();

			searchDc = DelayedCall.Create(UpdateSearch, 250);
			SearchText = "";   // Change value once to set the clear button visibility
			ClearViewHistory();
			UpdateTitle();

			FontScale = App.Settings.FontScale;

			App.Settings.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler((o, e) =>
			{
				if (e.PropertyName == "ShowSuggestions") UpdateSuggestionsLayout();
				if (e.PropertyName == "SuggestionsHorizontalLayout") UpdateSuggestionsLayout();
			});
			UpdateSuggestionsLayout();
		}

		#endregion Constructor

		#region Public properties

		/// <summary>
		/// Dictionary of all loaded text keys, associating a text key string with its TextKeyViewModel instance.
		/// </summary>
		public Dictionary<string, TextKeyViewModel> TextKeys { get; private set; }
		public HashSet<string> LoadedCultureNames { get; private set; }
		public HashSet<string> DeletedCultureNames { get; private set; }
		public TextKeyViewModel RootTextKey { get; private set; }
		public ObservableHashSet<TextKeyViewModel> ProblemKeys { get; private set; }

		public string ScanDirectory { get; set; }

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
				if (CheckUpdate(value, ref fileModified, "FileModified"))
				{
					UpdateTitle();
					SaveCommand.RaiseCanExecuteChanged();
				}
			}
		}

		private string primaryCulture;
		public string PrimaryCulture
		{
			get { return primaryCulture; }
			set { CheckUpdate(value, ref primaryCulture, "PrimaryCulture"); }
		}

		private bool problemFilterActive;
		public bool ProblemFilterActive
		{
			get { return problemFilterActive; }
			set
			{
				if (CheckUpdate(value, ref problemFilterActive, "ProblemFilterActive"))
				{
					UpdateSearch();
				}
			}
		}

		private string cursorChar;
		public string CursorChar
		{
			get { return cursorChar; }
			set { CheckUpdate(value, ref cursorChar, "CursorChar", "CursorCharCodePoint", "CursorCharName", "CursorCharCategory", "CursorCharVisibility"); }
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
				if (CheckUpdate(value, ref fontScale, "FontScale", "FontSize", "TextFormattingMode"))
				{
					App.Settings.FontScale = fontScale;
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
				if (CheckUpdate(value, ref statusText, "StatusText"))
				{
					ViewCommandManager.Invoke("AnimateStatusText", statusText);
				}
			}
		}

		private string selectedCulture;
		public string SelectedCulture
		{
			get { return selectedCulture; }
			set
			{
				if (CheckUpdate(value, ref selectedCulture, "SelectedCulture"))
				{
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
				if (CheckUpdate(value, ref lastSelectedCulture, "LastSelectedCulture"))
				{
					UpdateSuggestionsLater();
				}
			}
		}

		private bool haveComment;
		public bool HaveComment
		{
			get { return haveComment; }
			set { CheckUpdate(value, ref haveComment, "HaveComment"); }
		}

		private double suggestionsPanelWidth;
		public double SuggestionsPanelWidth
		{
			get { return suggestionsPanelWidth; }
			set
			{
				if (CheckUpdate(value, ref suggestionsPanelWidth, "SuggestionsPanelWidth"))
				{
					if (App.Settings.ShowSuggestions && App.Settings.SuggestionsHorizontalLayout)
					{
						App.Settings.SuggestionsWidth = suggestionsPanelWidth;
					}
				}
			}
		}

		private double suggestionsPanelHeight;
		public double SuggestionsPanelHeight
		{
			get { return suggestionsPanelHeight; }
			set
			{
				if (CheckUpdate(value, ref suggestionsPanelHeight, "SuggestionsPanelHeight"))
				{
					if (App.Settings.ShowSuggestions && !App.Settings.SuggestionsHorizontalLayout)
					{
						App.Settings.SuggestionsHeight = suggestionsPanelHeight;
					}
				}
			}
		}

		private double suggestionsSplitterWidth;
		public double SuggestionsSplitterWidth
		{
			get { return suggestionsSplitterWidth; }
			set { CheckUpdate(value, ref suggestionsSplitterWidth, "SuggestionsSplitterWidth"); }
		}

		private double suggestionsSplitterHeight;
		public double SuggestionsSplitterHeight
		{
			get { return suggestionsSplitterHeight; }
			set { CheckUpdate(value, ref suggestionsSplitterHeight, "SuggestionsSplitterHeight"); }
		}

		private ObservableCollection<SuggestionViewModel> suggestions = new ObservableCollection<SuggestionViewModel>();
		public ObservableCollection<SuggestionViewModel> Suggestions
		{
			get { return suggestions; }
		}

		private bool haveSuggestions;
		public bool HaveSuggestions
		{
			get { return haveSuggestions; }
			set { CheckUpdate(value, ref haveSuggestions, "HaveSuggestions"); }
		}

		private string suggestionsCulture;
		public string SuggestionsCulture
		{
			get { return suggestionsCulture; }
			set { CheckUpdate(value, ref suggestionsCulture, "SuggestionsCulture", "SuggestionsCultureCaption"); }
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

		#region Commands

		#region Definition and initialisation

		// Toolbar commands
		// File section
		public DelegateCommand NewFileCommand { get; private set; }
		public DelegateCommand LoadFolderCommand { get; private set; }
		public DelegateCommand LoadFileCommand { get; private set; }
		public DelegateCommand SaveCommand { get; private set; }
		public DelegateCommand ImportFileCommand { get; private set; }
		public DelegateCommand ExportKeysCommand { get; private set; }
		// Culture section
		public DelegateCommand NewCultureCommand { get; private set; }
		public DelegateCommand DeleteCultureCommand { get; private set; }
		public DelegateCommand ReplaceCultureCommand { get; private set; }
		public DelegateCommand InsertSystemKeysCommand { get; private set; }
		public DelegateCommand ViewDateTimeFormatsCommand { get; private set; }
		public DelegateCommand SetPrimaryCultureCommand { get; private set; }
		// Text key section
		public DelegateCommand NewTextKeyCommand { get; private set; }
		public DelegateCommand DeleteTextKeyCommand { get; private set; }
		public DelegateCommand TextKeyWizardCommand { get; private set; }
		public DelegateCommand RenameTextKeyCommand { get; private set; }
		public DelegateCommand DuplicateTextKeyCommand { get; private set; }
		// View section
		public DelegateCommand NavigateBackCommand { get; private set; }
		public DelegateCommand NavigateForwardCommand { get; private set; }
		public DelegateCommand GotoDefinitionCommand { get; private set; }
		// Filter section
		public DelegateCommand ClearSearchCommand { get; private set; }
		// Application section
		public DelegateCommand SettingsCommand { get; private set; }
		public DelegateCommand AboutCommand { get; private set; }
		public DelegateCommand HelpCommand { get; private set; }
		public DelegateCommand LibFolderCommand { get; private set; }

		// Context menu
		public DelegateCommand ConvertToNamespaceCommand { get; private set; }
		public DelegateCommand ConvertToTextKeyCommand { get; private set; }

		// Other commands
		public DelegateCommand CopyTextKeyCommand { get; private set; }
		public DelegateCommand SelectPreviousTextKeyCommand { get; private set; }
		public DelegateCommand SelectNextTextKeyCommand { get; private set; }

		private void InitializeCommands()
		{
			// Toolbar
			// File section
			NewFileCommand = new DelegateCommand(OnNewFile);
			LoadFolderCommand = new DelegateCommand(OnLoadFolder);
			LoadFileCommand = new DelegateCommand(OnLoadFile);
			SaveCommand = new DelegateCommand(OnSave, () => FileModified);
			ImportFileCommand = new DelegateCommand(OnImportFile);
			ExportKeysCommand = new DelegateCommand(OnExportKeys, CanExportKeys);
			// Culture section
			NewCultureCommand = new DelegateCommand(OnNewCulture);
			DeleteCultureCommand = new DelegateCommand(OnDeleteCulture, CanDeleteCulture);
			ReplaceCultureCommand = new DelegateCommand(OnReplaceCulture);
			InsertSystemKeysCommand = new DelegateCommand(OnInsertSystemKeys);
			ViewDateTimeFormatsCommand = new DelegateCommand(OnViewDateTimeFormats);
			SetPrimaryCultureCommand = new DelegateCommand(OnSetPrimaryCulture, CanSetPrimaryCulture);
			// Text key section
			NewTextKeyCommand = new DelegateCommand(OnNewTextKey);
			DeleteTextKeyCommand = new DelegateCommand(OnDeleteTextKey, CanDeleteTextKey);
			TextKeyWizardCommand = new DelegateCommand(OnTextKeyWizard);
			RenameTextKeyCommand = new DelegateCommand(OnRenameTextKey, CanRenameTextKey);
			DuplicateTextKeyCommand = new DelegateCommand(OnDuplicateTextKey, CanDuplicateTextKey);
			// View section
			NavigateBackCommand = new DelegateCommand(OnNavigateBack, CanNavigateBack);
			NavigateForwardCommand = new DelegateCommand(OnNavigateForward, CanNavigateForward);
			GotoDefinitionCommand = new DelegateCommand(OnGotoDefinition, CanGotoDefinition);
			// Filter section
			ClearSearchCommand = new DelegateCommand(() => { SearchText = ""; });
			// Application section
			SettingsCommand = new DelegateCommand(OnSettings);
			AboutCommand = new DelegateCommand(OnAbout);
			HelpCommand = new DelegateCommand(OnHelp);
			LibFolderCommand = new DelegateCommand(OnLibFolder);

			// Context menu
			ConvertToNamespaceCommand = new DelegateCommand(OnConvertToNamespace, CanConvertToNamespace);
			ConvertToTextKeyCommand = new DelegateCommand(OnConvertToTextKey, CanConvertToTextKey);

			// Other commands
			CopyTextKeyCommand = new DelegateCommand(OnCopyTextKey);
			SelectPreviousTextKeyCommand = new DelegateCommand(OnSelectPreviousTextKey);
			SelectNextTextKeyCommand = new DelegateCommand(OnSelectNextTextKey);
		}

		#endregion Definition and initialisation

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

			ClearReadonlyFiles();
			var folderDlg = new OpenFolderDialog();
			folderDlg.Title = Tx.T("msg.load folder.title");
			if (folderDlg.ShowDialog(new Wpf32Window(MainWindow.Instance)) == true)
			{
				DoLoadFolder(folderDlg.Folder);
			}
		}

		public void DoLoadFolder(string folder)
		{
			bool foundFiles = false;
			string regex = @"^(.+?)(\.(([a-z]{2})([-][a-z]{2})?))?\.(txd|xml)$";
			List<string> prefixes = new List<string>();
			string prefix = null;
			foreach (string fileName in Directory.GetFiles(folder))
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
					regex = "^" + Regex.Escape(prefix) + regex;
				}
				foreach (string fileName in Directory.GetFiles(folder))
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
						if (!LoadFromXmlFile(fileName))
						{
							break;
						}
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
				CheckNotifyReadonlyFiles();
			}
			else
			{
				MessageBox.Show(Tx.T("msg.load folder.no files found"), "TxEditor", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}

		private void OnLoadFile()
		{
			if (!CheckModifiedSaved()) return;

			ClearReadonlyFiles();
			var fileDlg = new OpenFileDialog();
			fileDlg.CheckFileExists = true;
			fileDlg.Filter = Tx.T("file filter.tx dictionary files") + " (*.txd)|*.txd|" +
				Tx.T("file filter.xml files") + " (*.xml)|*.xml|" +
				Tx.T("file filter.all files") + " (*.*)|*.*";
			fileDlg.Multiselect = true;
			fileDlg.ShowReadOnly = false;
			fileDlg.Title = Tx.T("msg.load file.title");
			if (fileDlg.ShowDialog(MainWindow.Instance) == true)
			{
				DoLoadFiles(fileDlg.FileNames);
			}
		}

		public void DoLoadFiles(string[] fileNames)
		{
			// Check for same prefix and reject mixed files
			List<string> prefixes = new List<string>();
			foreach (string fileName in fileNames)
			{
				string prefix = FileNameHelper.GetPrefix(fileName);
				if (!prefixes.Contains(prefix))
				{
					prefixes.Add(prefix);
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

			List<string> filesToLoad = new List<string>(fileNames);

			if (!FileNameHelper.FindOtherCultures(filesToLoad)) return;

			bool foundFiles = false;
			fileVersion = 0;
			string prevPrimaryCulture = null;
			List<string> primaryCultureFiles = new List<string>();
			foreach (string fileName in filesToLoad)
			{
				if (!foundFiles)
				{
					foundFiles = true;
					fileModified = false;   // Prevent another unsaved warning from OnNewFile
					OnNewFile();   // Clear any currently loaded content
				}
				if (!LoadFromXmlFile(fileName))
				{
					break;
				}
				if (PrimaryCulture != prevPrimaryCulture)
				{
					primaryCultureFiles.Add(PrimaryCulture);
				}
				prevPrimaryCulture = PrimaryCulture;
			}
			if (primaryCultureFiles.Count > 1)
			{
				// Display a warning if multiple (and which) files claimed to be the primary culture, and which has won
				MessageBox.Show(
					Tx.T("msg.load file.multiple primary cultures", "list", string.Join(", ", primaryCultureFiles), "name", PrimaryCulture),
					Tx.T("msg.caption.warning"),
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
			}

			SortCulturesInTextKey(RootTextKey);
			DeletedCultureNames.Clear();
			ValidateTextKeysDelayed();
			StatusText = Tx.T("statusbar.n files loaded", filesToLoad.Count) + Tx.T("statusbar.n text keys defined", TextKeys.Count);
			FileModified = false;
			ClearViewHistory();
			CheckNotifyReadonlyFiles();
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
				dlg.Filter = Tx.T("file filter.tx dictionary files") + " (*.txd)|*.txd|" +
					Tx.T("file filter.xml files") + " (*.xml)|*.xml|" +
					Tx.T("file filter.all files") + " (*.*)|*.*";
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
				if (!WriteToXmlFile(Path.Combine(newFilePath, newFilePrefix)))
					return false;
				loadedFilePath = newFilePath;
				loadedFilePrefix = newFilePrefix;
			}
			else
			{
				if (!WriteToXmlFile(Path.Combine(loadedFilePath, loadedFilePrefix)))
					return false;
			}
			UpdateTitle();
			StatusText = Tx.T("statusbar.file saved");
			return true;
		}

		private void OnImportFile()
		{
			var dlg = new OpenFileDialog();
			dlg.CheckFileExists = true;
			dlg.Filter = Tx.T("file filter.tx dictionary files") + " (*.txd)|*.txd|" +
				Tx.T("file filter.xml files") + " (*.xml)|*.xml|" +
				Tx.T("file filter.all files") + " (*.*)|*.*";
			dlg.Multiselect = true;
			dlg.ShowReadOnly = false;
			dlg.Title = Tx.T("msg.import file.title");
			if (dlg.ShowDialog(MainWindow.Instance) == true)
			{
				int count = 0;
				foreach (string fileName in dlg.FileNames)
				{
					if (!LoadFromXmlFile(fileName, true))
					{
						break;
					}
					count++;
				}

				SortCulturesInTextKey(RootTextKey);
				ValidateTextKeysDelayed();
				StatusText = Tx.T("statusbar.n files imported", count) + Tx.T("statusbar.n text keys defined", TextKeys.Count);
				FileModified = true;
			}
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
				if (win.InsertSystemKeysCheckBox.IsChecked == true)
				{
					InsertSystemKeys(ci.Name);
				}
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
			InsertSystemKeys(SelectedCulture);
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
			win.Title = Tx.T("window.text key.create.title");
			win.CaptionLabel.Text = Tx.T("window.text key.create.caption");
			win.OKButton.Content = Tx.T("window.text key.create.accept button");

			var selKey = MainWindow.Instance.TextKeysTreeView.LastSelectedItem as TextKeyViewModel;
			if (selKey != null)
			{
				win.TextKey = selKey.TextKey + (selKey.IsNamespace ? ":" : ".");
			}

			if (win.ShowDialog() == true)
			{
				string newKey = win.TextKey;

				TextKeyViewModel tk;
				try
				{
					tk = FindOrCreateTextKey(newKey);
				}
				catch (NonNamespaceExistsException)
				{
					MessageBox.Show(
						Tx.T("msg.cannot create namespace key", "key", Tx.Q(newKey)),
						Tx.T("msg.caption.error"),
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
					return;
				}
				catch (NamespaceExistsException)
				{
					MessageBox.Show(
						Tx.T("msg.cannot create non-namespace key", "key", Tx.Q(newKey)),
						Tx.T("msg.caption.error"),
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
					return;
				}

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
					StatusText = Tx.T("statusbar.text key created");
				}

				if (tk.CultureTextVMs.Count > 0)
					tk.CultureTextVMs[0].ViewCommandManager.InvokeLoaded("FocusText");
			}
		}

		// TODO: This is not a command handler, move it elsewhere
		public void TextKeySelectionChanged(IList selectedItems)
		{
			selectedTextKeys = selectedItems.OfType<TextKeyViewModel>().ToList();
			DeleteTextKeyCommand.RaiseCanExecuteChanged();
			RenameTextKeyCommand.RaiseCanExecuteChanged();
			DuplicateTextKeyCommand.RaiseCanExecuteChanged();
			AppendViewHistory();
			UpdateNavigationButtons();
			UpdateSuggestionsLater();

			HaveComment = false;
			foreach (TextKeyViewModel tk in selectedTextKeys)
			{
				HaveComment |= !string.IsNullOrWhiteSpace(tk.Comment);
			}
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
				// TODO: Check whether any selected key is a child of another selected key -> don't count them additionally - collect all selected keys in a HashSet, then count
				// or use TreeViewItemViewModel.IsAParentOf method
				count += CountTextKeys(tk);
				if (!tk.IsFullKey)
					onlyFullKeysSelected = false;
			}
			if (count == 0)
			{
				// Means there were nodes with no full keys, should not happen
				FL.Warning("MainViewModel.OnDeleteTextKey: count == 0 (should not happen)");
				return;
			}

			TaskDialogResult result;
			bool selectedOnlyOption = false;
			if (count == 1)
			{
				result = TaskDialog.Show(
					owner: MainWindow.Instance,
					allowDialogCancellation: true,
					title: "TxEditor",
					mainInstruction: Tx.T("msg.delete text key", "key", Tx.Q(lastCountedTextKey)),
					content: Tx.T("msg.delete text key.content"),
					customButtons: new string[] { Tx.T("task dialog.button.delete"), Tx.T("task dialog.button.cancel") });
			}
			else if (onlyFullKeysSelected && selectedTextKeys.Count < count)
			{
				result = TaskDialog.Show(
					owner: MainWindow.Instance,
					allowDialogCancellation: true,
					title: "TxEditor",
					mainInstruction: Tx.T("msg.delete text key.multiple", count),
					content: Tx.T("msg.delete text key.multiple.content mixed"),
					radioButtons: new string[] { Tx.T("msg.delete text key.multiple.also subkeys"), Tx.T("msg.delete text key.multiple.only selected") },
					customButtons: new string[] { Tx.T("task dialog.button.delete"), Tx.T("task dialog.button.cancel") });
				selectedOnlyOption = result.RadioButtonResult == 1;
			}
			else
			{
				result = TaskDialog.Show(
					owner: MainWindow.Instance,
					allowDialogCancellation: true,
					title: "TxEditor",
					mainInstruction: Tx.T("msg.delete text key.multiple", count),
					content: Tx.T("msg.delete text key.multiple.content"),
					customButtons: new string[] { Tx.T("task dialog.button.delete"), Tx.T("task dialog.button.cancel") });
			}
			if (result.CustomButtonResult == 0)
			{
				// Determine the remaining text key to select after deleting
				TextKeyViewModel lastSelectedTk = selectedTextKeys[selectedTextKeys.Count - 1];
				var remainingItem = lastSelectedTk.FindRemainingItem(t => !selectedTextKeys.Contains(t) && !selectedTextKeys.Any(s => s.IsAParentOf(t)));

				bool isAnySelectedRemaining = false;
				foreach (TextKeyViewModel tk in selectedTextKeys.ToArray())
				{
					DeleteTextKey(tk, !selectedOnlyOption);
					// Also remove unused partial keys
					DeletePartialParentKeys(tk.Parent as TextKeyViewModel);
					if (tk.Parent.Children.Contains(tk))
						isAnySelectedRemaining = true;
					FileModified = true;
				}
				if (!isAnySelectedRemaining)
				{
					// Select and focus other key in the tree
					ViewCommandManager.InvokeLoaded("SelectTextKey", remainingItem);
				}
				ValidateTextKeysDelayed();

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
		private IDataObject clipboardBackup;

		public void TextKeyWizardFromHotKey()
		{
			// Determine the currently active window
			fgWin = WinApi.GetForegroundWindow();

			// Require it to be Visual Studio, otherwise do nothing more
			if (App.Settings.WizardHotkeyInVisualStudioOnly)
			{
				StringBuilder sb = new StringBuilder(1000);
				WinApi.GetWindowText(fgWin, sb, 1000);
				if (!sb.ToString().EndsWith(" - Microsoft Visual Studio")) return;
			}

			// Backup current clipboard content
			clipboardBackup = ClipboardHelper.GetDataObject();

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

			DelayedCall.Start(TextKeyWizardFromHotKey2, 50);
		}

		private void TextKeyWizardFromHotKey2()
		{
			// Create the wizard window
			TextKeyWizardWindow win = new TextKeyWizardWindow();
			//win.Owner = MainWindow.Instance;
			win.ShowInTaskbar = true;
			win.ClipboardBackup = clipboardBackup;

			MainWindow.Instance.Hide();

			bool ok = false;
			if (win.ShowDialog() == true)
			{
				ok = HandleWizardInput(win.TextKeyText.Text, win.TranslationText.Text);
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

			clipboardBackup = win.ClipboardBackup;
			if (clipboardBackup != null)
			{
				DelayedCall.Start(TextKeyWizardFromHotKey3, 200);
			}
		}

		private void TextKeyWizardFromHotKey3()
		{
			// Restore clipboard
			Clipboard.SetDataObject(clipboardBackup, true);
		}

		private bool HandleWizardInput(string keyName, string text)
		{
			TextKeyViewModel tk;
			try
			{
				tk = FindOrCreateTextKey(keyName);
			}
			catch (NonNamespaceExistsException)
			{
				MessageBox.Show(
					Tx.T("msg.cannot create namespace key", "key", Tx.Q(keyName)),
					Tx.T("msg.caption.error"),
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				return false;
			}
			catch (NamespaceExistsException)
			{
				MessageBox.Show(
					Tx.T("msg.cannot create non-namespace key", "key", Tx.Q(keyName)),
					Tx.T("msg.caption.error"),
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				return false;
			}

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
			return true;
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
			win.Title = Tx.T("window.text key.rename.title");
			win.CaptionLabel.Text = Tx.T("window.text key.rename.caption");
			win.TextKey = selKey.TextKey;
			win.OKButton.Content = Tx.T("window.text key.rename.accept button");
			win.RenameSelectMode = true;

			if (selKey.Children.Count > 0)
			{
				// There are other keys below the selected key
				// Initially indicate that all subkeys will also be renamed
				win.IncludeSubitemsCheckbox.Visibility = Visibility.Visible;
				win.IncludeSubitemsCheckbox.IsChecked = true;
				win.IncludeSubitemsCheckbox.IsEnabled = false;

				if (selKey.IsFullKey)
				{
					// The selected key is a full key
					// Allow it to be renamed independently of the subkeys
					win.IncludeSubitemsCheckbox.IsEnabled = true;
				}
			}

			if (win.ShowDialog() == true)
			{
				// The dialog was confirmed
				string newKey = win.TextKey;

				// Was the name changed at all?
				if (newKey == selKey.TextKey) return;

				// Don't allow namespace nodes to be moved elsewhere
				if (selKey.IsNamespace && (newKey.Contains('.') || newKey.Contains(':')))
				{
					MessageBox.Show(
						Tx.T("msg.cannot move namespace"),
						Tx.T("msg.caption.error"),
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
					return;
				}

				bool needDuplicateForChildren = win.IncludeSubitemsCheckbox.IsChecked == false && selKey.Children.Count > 0;

				// Test whether the entered text key already exists with content or subkeys
				TextKeyViewModel tryDestKey;
				try
				{
					tryDestKey = FindOrCreateTextKey(newKey, false, false);
				}
				catch (NonNamespaceExistsException)
				{
					MessageBox.Show(
						Tx.T("msg.cannot create namespace key", "key", Tx.Q(newKey)),
						Tx.T("msg.caption.error"),
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
					return;
				}
				catch (NamespaceExistsException)
				{
					MessageBox.Show(
						Tx.T("msg.cannot create non-namespace key", "key", Tx.Q(newKey)),
						Tx.T("msg.caption.error"),
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
					return;
				}
				bool destExists = tryDestKey != null && (!tryDestKey.IsEmpty() || tryDestKey.Children.Count > 0);
				bool destWasFullKey = false;
				if (destExists)
				{
					// FindOrCreateTextKey below will make it a full key, no matter whether it
					// should be one. Remember this state to reset it afterwards.
					destWasFullKey = tryDestKey.IsFullKey;

					TaskDialogResult result = TaskDialog.Show(
						owner: MainWindow.Instance,
						allowDialogCancellation: true,
						title: "TxEditor",
						mainInstruction: Tx.T("msg.rename text key.exists", "key", Tx.Q(newKey)),
						content: Tx.T("msg.rename text key.exists.content"),
						customButtons: new string[] { Tx.T("task dialog.button.merge"), Tx.T("task dialog.button.cancel") });
					if (result.CustomButtonResult != 0)
					{
						return;
					}
				}

				var oldParent = selKey.Parent;
				int affectedKeyCount = selKey.IsFullKey ? 1 : 0;

				if (!needDuplicateForChildren)
				{
					// Remove the selected key from its original tree position
					oldParent.Children.Remove(selKey);
				}

				// Create the new text key if needed
				TextKeyViewModel destKey = FindOrCreateTextKey(newKey, false);

				if (!destExists)
				{
					// Key was entirely empty or is newly created.

					if (needDuplicateForChildren)
					{
						// Keep new key but copy all data from the source key
						destKey.MergeFrom(selKey);
						// The source key is now no longer a full key
						selKey.IsFullKey = false;
					}
					else
					{
						// Replace it with the source key
						affectedKeyCount = selKey.SetKeyRecursive(newKey, TextKeys);

						if (selKey.IsNamespace)
						{
							// We're renaming a namespace item. But we've created a temporary
							// normal key (destKey) which is now at the wrong position.
							// Namespace entries are sorted differently, which was not known when
							// creating the key because it was no namespace at that time. Remove the
							// newly created key entry (all of its possibly created parent keys are
							// still useful though!) and insert the selected key at the correct
							// position in that tree level.
							destKey.Parent.Children.Remove(destKey);
							destKey.Parent.Children.InsertSorted(selKey, TextKeyViewModel.Compare);
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
				}
				else
				{
					// Key already has some text or child keys.

					// Restore original full key state first
					destKey.IsFullKey = destWasFullKey;
					// Merge data into destKey, overwriting conflicts
					destKey.MergeFrom(selKey);

					if (win.IncludeSubitemsCheckbox.IsChecked == true)
					{
						// Add/merge all subkeys as well
						destKey.MergeChildrenRecursive(selKey);
						// Delete the source key after it has been merged into destKey
						DeleteTextKey(selKey);
					}
					else
					{
						// The source key will be kept but is now no longer a full key
						selKey.IsFullKey = false;
						TextKeys.Remove(selKey.TextKey);
					}
				}

				if (!needDuplicateForChildren && oldParent != selKey.Parent)
				{
					// The key has moved to another subtree.
					// Clean up possible unused partial keys at the old position.
					DeletePartialParentKeys(oldParent as TextKeyViewModel);
				}

				FileModified = true;
				StatusText = Tx.T("statusbar.text keys renamed", affectedKeyCount);

				// Fix an issue with MultiSelectTreeView: It can only know that an item is selected
				// when its TreeViewItem property IsSelected is set through a binding defined in
				// this application. The already-selected item was removed from the SelectedItems
				// list when it was removed from the tree (to be re-inserted later). Not sure how
				// to fix this right.
				selKey.IsSelected = true;

				if (needDuplicateForChildren || destExists)
				{
					bool wasExpanded = selKey.IsExpanded;
					destKey.IsExpanded = true;   // Expands all parents
					if (!wasExpanded)
						destKey.IsExpanded = false;   // Collapses the item again like it was before
					ViewCommandManager.InvokeLoaded("SelectTextKey", destKey);
				}
				else
				{
					bool wasExpanded = selKey.IsExpanded;
					selKey.IsExpanded = true;   // Expands all parents
					if (!wasExpanded)
						selKey.IsExpanded = false;   // Collapses the item again like it was before
					ViewCommandManager.InvokeLoaded("SelectTextKey", selKey);
				}
				ValidateTextKeysDelayed();
			}
		}

		private bool CanDuplicateTextKey()
		{
			return selectedTextKeys != null && selectedTextKeys.Count == 1;
		}

		private void OnDuplicateTextKey()
		{
			var selKey = MainWindow.Instance.TextKeysTreeView.LastSelectedItem as TextKeyViewModel;
			if (selKey == null)
				return;   // No key selected, something is wrong

			var win = new TextKeyWindow();
			win.Owner = MainWindow.Instance;
			win.Title = Tx.T("window.text key.duplicate.title");
			win.CaptionLabel.Text = Tx.T("window.text key.duplicate.caption");
			win.TextKey = selKey.TextKey;
			win.OKButton.Content = Tx.T("window.text key.duplicate.accept button");
			win.RenameSelectMode = true;

			if (selKey.Children.Count > 0)
			{
				// There are other keys below the selected key
				// Initially indicate that all subkeys will also be duplicated
				win.IncludeSubitemsCheckbox.Visibility = Visibility.Visible;
				win.IncludeSubitemsCheckbox.IsChecked = true;
				win.IncludeSubitemsCheckbox.IsEnabled = false;

				if (selKey.IsFullKey)
				{
					// The selected key is a full key
					// Allow it to be duplicated independently of the subkeys
					win.IncludeSubitemsCheckbox.IsEnabled = true;
				}
			}

			if (win.ShowDialog() == true)
			{
				// The dialog was confirmed
				string newKey = win.TextKey;
				bool includeChildren = win.IncludeSubitemsCheckbox.IsChecked == true;

				// Don't allow namespace nodes to be copied elsewhere
				if (selKey.IsNamespace && (newKey.Contains('.') || newKey.Contains(':')))
				{
					MessageBox.Show(
						Tx.T("msg.cannot copy namespace"),
						Tx.T("msg.caption.error"),
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
					return;
				}

				// Test whether the entered text key already exists with content or subkeys
				TextKeyViewModel tryDestKey;
				try
				{
					tryDestKey = FindOrCreateTextKey(newKey, false, false, selKey.IsNamespace);
				}
				catch (NonNamespaceExistsException)
				{
					MessageBox.Show(
						Tx.T("msg.cannot create namespace key", "key", Tx.Q(newKey)),
						Tx.T("msg.caption.error"),
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
					return;
				}
				catch (NamespaceExistsException)
				{
					MessageBox.Show(
						Tx.T("msg.cannot create non-namespace key", "key", Tx.Q(newKey)),
						Tx.T("msg.caption.error"),
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
					return;
				}
				bool destExists = tryDestKey != null && (!tryDestKey.IsEmpty() || tryDestKey.Children.Count > 0);
				bool destWasFullKey = false;
				if (destExists)
				{
					// FindOrCreateTextKey below will make it a full key, no matter whether it
					// should be one. Remember this state to reset it afterwards.
					destWasFullKey = tryDestKey.IsFullKey;

					TaskDialogResult result = TaskDialog.Show(
						owner: MainWindow.Instance,
						allowDialogCancellation: true,
						title: "TxEditor",
						mainInstruction: Tx.T("msg.rename text key.exists", "key", Tx.Q(newKey)),
						content: Tx.T("msg.rename text key.exists.content"),
						customButtons: new string[] { Tx.T("task dialog.button.merge"), Tx.T("task dialog.button.cancel") });
					if (result.CustomButtonResult != 0)
					{
						return;
					}
				}

				int affectedKeys = selKey.IsFullKey ? 1 : 0;

				// Create the new text key if needed
				TextKeyViewModel destKey = FindOrCreateTextKey(newKey, true, true, selKey.IsNamespace);

				// Restore original full key state first
				destKey.IsFullKey = destWasFullKey;
				if (!destWasFullKey)
				{
					TextKeys.Remove(destKey.TextKey);
				}
				// Merge data into destKey, overwriting conflicts
				destKey.MergeFrom(selKey);

				if (includeChildren)
				{
					if (!destExists)
					{
						// Key was entirely empty or is newly created.

						foreach (TextKeyViewModel child in selKey.Children)
						{
							affectedKeys += DuplicateTextKeyRecursive(child, destKey);
						}
					}
					else
					{
						// Key already has some text or child keys.

						// Add/merge all subkeys as well
						destKey.MergeChildrenRecursive(selKey);
					}
				}

				FileModified = true;
				StatusText = Tx.T("statusbar.text keys duplicated", affectedKeys);

				destKey.IsSelected = true;

				bool wasExpanded = selKey.IsExpanded;
				destKey.IsExpanded = true;   // Expands all parents
				if (!wasExpanded)
					destKey.IsExpanded = false;   // Collapses the item again like it was before
				ViewCommandManager.InvokeLoaded("SelectTextKey", destKey);
				ValidateTextKeysDelayed();
			}
		}

		private int DuplicateTextKeyRecursive(TextKeyViewModel srcTextKey, TextKeyViewModel destParent)
		{
			string destKeyName = destParent.TextKey + (destParent.IsNamespace ? ":" : ".") + srcTextKey.DisplayName;
			TextKeyViewModel destKey = FindOrCreateTextKey(destKeyName);
			destKey.MergeFrom(srcTextKey);
			int affectedKeys = srcTextKey.IsFullKey ? 1 : 0;

			foreach (TextKeyViewModel child in srcTextKey.Children)
			{
				affectedKeys += DuplicateTextKeyRecursive(child, destKey);
			}
			return affectedKeys;
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

		private void OnHelp()
		{
			string docFileName = Path.Combine(
				Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location),
				"Tx Documentation.pdf");

			try
			{
				System.Diagnostics.Process.Start(docFileName);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Tx.T("msg.caption.error"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void OnLibFolder()
		{
			string libFolder = Path.Combine(
				Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location),
				"TxLib source code");

			try
			{
				System.Diagnostics.Process.Start(libFolder);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, Tx.T("msg.caption.error"), MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		#endregion Application section

		#endregion Toolbar command handlers

		#region Context menu command handlers

		private bool CanConvertToNamespace()
		{
			return selectedTextKeys != null && selectedTextKeys.Count == 1 && !selectedTextKeys[0].IsNamespace;
		}

		private void OnConvertToNamespace()
		{
			var selKey = selectedTextKeys[0];

			if (selKey.IsFullKey)
			{
				MessageBox.Show(
					Tx.T("msg.convert to namespace.is full key", "key", Tx.Q(selKey.TextKey)),
					Tx.T("msg.caption.error"),
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				return;
			}
			if (selKey.Parent != RootTextKey)
			{
				MessageBox.Show(
					Tx.T("msg.convert to namespace.not a root child", "key", Tx.Q(selKey.TextKey)),
					Tx.T("msg.caption.error"),
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				return;
			}

			selKey.IsNamespace = true;
			foreach (var child in selKey.Children.OfType<TextKeyViewModel>())
			{
				child.SetKeyRecursive(selKey.TextKey + ":" + child.DisplayName, TextKeys);
			}
			selKey.Parent.Children.Remove(selKey);
			selKey.Parent.Children.InsertSorted(selKey, TextKeyViewModel.Compare);

			FileModified = true;
			StatusText = Tx.T("statusbar.text key converted to namespace");

			ViewCommandManager.InvokeLoaded("SelectTextKey", selKey);
			ValidateTextKeysDelayed();
		}

		private bool CanConvertToTextKey()
		{
			return selectedTextKeys != null && selectedTextKeys.Count == 1 && selectedTextKeys[0].IsNamespace;
		}

		private void OnConvertToTextKey()
		{
			var selKey = selectedTextKeys[0];

			if (selKey.DisplayName.IndexOf('.') != -1)
			{
				MessageBox.Show(
					Tx.T("msg.convert to text key.contains point", "key", Tx.Q(selKey.TextKey)),
					Tx.T("msg.caption.error"),
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
				return;
			}

			selKey.IsNamespace = false;
			foreach (var child in selKey.Children.OfType<TextKeyViewModel>())
			{
				child.SetKeyRecursive(selKey.TextKey + "." + child.DisplayName, TextKeys);
			}
			selKey.Parent.Children.Remove(selKey);
			selKey.Parent.Children.InsertSorted(selKey, TextKeyViewModel.Compare);

			FileModified = true;
			StatusText = Tx.T("statusbar.namespace converted to text key");

			ViewCommandManager.InvokeLoaded("SelectTextKey", selKey);
			ValidateTextKeysDelayed();
		}

		#endregion Context menu command handlers

		#region Other command handlers

		private void OnCopyTextKey()
		{
			string str = selectedTextKeys
				.Select(tk => tk.TextKey)
				.Aggregate((a, b) => a + Environment.NewLine + b);
			Clipboard.SetText(str);
			StatusText = Tx.T("statusbar.text key copied");
		}

		private void OnSelectPreviousTextKey()
		{
			ViewCommandManager.Invoke("SelectPreviousTextKey", LastSelectedCulture);
		}

		private void OnSelectNextTextKey()
		{
			ViewCommandManager.Invoke("SelectNextTextKey", LastSelectedCulture);
		}

		#endregion Other command handlers

		#endregion Commands

		#region XML loading methods

		public void LoadFiles(IEnumerable<string> fileNames)
		{
			OnNewFile();
			int count = 0;
			ClearReadonlyFiles();
			string prevPrimaryCulture = null;
			List<string> primaryCultureFiles = new List<string>();
			foreach (string _fileName in fileNames.Distinct())
			{
				string fileName = _fileName;
				if (!Path.IsPathRooted(fileName))
				{
					fileName = Path.GetFullPath(fileName);
				}
				if (!LoadFromXmlFile(fileName))
				{
					break;
				}
				count++;
				if (PrimaryCulture != prevPrimaryCulture)
				{
					primaryCultureFiles.Add(PrimaryCulture);
				}
				prevPrimaryCulture = PrimaryCulture;
			}
			if (primaryCultureFiles.Count > 1)
			{
				// Display a warning if multiple (and which) files claimed to be the primary culture, and which has won
				App.SplashScreen.Close(TimeSpan.Zero);
				MessageBox.Show(
					Tx.T("msg.load file.multiple primary cultures", "list", string.Join(", ", primaryCultureFiles), "name", PrimaryCulture),
					Tx.T("msg.caption.warning"),
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
			}
			ValidateTextKeysDelayed();
			StatusText = Tx.T("statusbar.n files loaded", count) + Tx.T("statusbar.n text keys defined", TextKeys.Count);
			FileModified = false;
			// CheckNotifyReadonlyFiles will be called with the InitCommand
		}

		private bool LoadFromXmlFile(string fileName, bool importing = false)
		{
			FileInfo fi = new FileInfo(fileName);
			if (fi.Exists && fi.IsReadOnly && !importing)
			{
				SetReadonlyFiles();
			}

			// First load the XML file into an XmlDocument for further processing
			XmlDocument xmlDoc = new XmlDocument();
			try
			{
				xmlDoc.Load(fileName);
				if (xmlDoc.DocumentElement.Name != "translation")
				{
					throw new Exception("Unexpected XML root element.");
				}
				// After the file is valid XML and the root element has the expected name, no more
				// major errors should occur. If this really is no Tx dictionary, no cultures and
				// text keys should be found.
			}
			catch (Exception ex)
			{
				FL.Error("Error loading file", fileName);
				FL.Error(ex, "Loading XML dictionary file");
				var result = TaskDialog.Show(
					owner: MainWindow.Instance,
					allowDialogCancellation: true,
					title: "TxEditor",
					mainIcon: VistaTaskDialogIcon.Error,
					mainInstruction: Tx.T("msg.load file.invalid file"),
					content: Tx.T("msg.load file.invalid file.desc", "name", fileName, "msg", ex.Message),
					customButtons: new string[] { Tx.T("task dialog.button.skip file"), Tx.T("task dialog.button.cancel") });

				if (result.CustomButtonResult == 0)
				{
					return true;
				}
				else
				{
					return false;
				}
			}

			// Try to recognise the culture name from the file name
			Match m = Regex.Match(Path.GetFileName(fileName), @"^(.+?)\.(([a-z]{2})([-][a-z]{2})?)\.(?:txd|xml)$", RegexOptions.IgnoreCase);
			if (m.Success)
			{
				CultureInfo ci = CultureInfo.GetCultureInfo(m.Groups[2].Value);
				if (importing && !LoadedCultureNames.Contains(ci.Name))
				{
					var result = TaskDialog.Show(
						owner: MainWindow.Instance,
						allowDialogCancellation: true,
						title: "TxEditor",
						mainInstruction: Tx.T("msg.import file.add new culture", "culture", ci.Name),
						content: Tx.T("msg.import file.add new culture.desc", "name", fileName, "culture", ci.Name),
						customButtons: new string[] { Tx.T("task dialog.button.add culture"), Tx.T("task dialog.button.skip culture"), Tx.T("task dialog.button.cancel") });

					if (result.CustomButtonResult == 0)
					{
					}
					else if (result.CustomButtonResult == 1)
					{
						return true;
					}
					else
					{
						return false;
					}
				}
				LoadFromXml(ci.Name, xmlDoc.DocumentElement);

				if (!importing)
				{
					// Set the primary culture if a file claims to be it
					XmlAttribute primaryAttr = xmlDoc.DocumentElement.Attributes["primary"];
					if (primaryAttr != null && primaryAttr.Value == "true")
					{
						PrimaryCulture = ci.Name;
						SortCulturesInTextKey(RootTextKey);
					}
					if (fileVersion == 0)
					{
						fileVersion = 1;
						loadedFilePath = Path.GetDirectoryName(fileName);
						loadedFilePrefix = m.Groups[1].Value;
						UpdateTitle();
					}
				}
				return true;
			}

			if (!importing)
			{
				// Find primary culture and set it already so that all loaded keys can be generated in
				// the final order already
				foreach (XmlElement xe in xmlDoc.DocumentElement.SelectNodes("culture[@primary='true']"))
				{
					PrimaryCulture = xe.Attributes["name"].Value;
					break;
				}
			}

			// Try to find the culture name inside a combined XML document
			foreach (XmlElement xe in xmlDoc.DocumentElement.SelectNodes("culture[@name]"))
			{
				CultureInfo ci = CultureInfo.GetCultureInfo(xe.Attributes["name"].Value);
				if (importing && !LoadedCultureNames.Contains(ci.Name))
				{
					var result = TaskDialog.Show(
						owner: MainWindow.Instance,
						allowDialogCancellation: true,
						title: "TxEditor",
						mainInstruction: Tx.T("msg.import file.add new culture", "culture", ci.Name),
						content: Tx.T("msg.import file.add new culture.desc", "name", fileName, "culture", ci.Name),
						customButtons: new string[] { Tx.T("task dialog.button.add culture"), Tx.T("task dialog.button.skip culture"), Tx.T("task dialog.button.cancel") });

					if (result.CustomButtonResult == 0)
					{
					}
					else if (result.CustomButtonResult == 1)
					{
						return true;
					}
					else
					{
						return false;
					}
				}
				LoadFromXml(ci.Name, xe);
			}
			if (fileVersion == 0 && !importing)
			{
				fileVersion = 2;
				loadedFilePath = Path.GetDirectoryName(fileName);
				loadedFilePrefix = Path.GetFileNameWithoutExtension(fileName);
				UpdateTitle();
			}
			return true;
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

				XmlAttribute acceptMissingAttr = textNode.Attributes["acceptmissing"];
				bool acceptMissing = acceptMissingAttr != null && acceptMissingAttr.Value == "true";

				XmlAttribute acceptPlaceholdersAttr = textNode.Attributes["acceptplaceholders"];
				bool acceptPlaceholders = acceptPlaceholdersAttr != null && acceptPlaceholdersAttr.Value == "true";

				XmlAttribute acceptPunctuationAttr = textNode.Attributes["acceptpunctuation"];
				bool acceptPunctuation = acceptPunctuationAttr != null && acceptPunctuationAttr.Value == "true";

				// TODO: Catch exceptions NonNamespaceExistsException and NamespaceExistsException for invalid files
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
					ct.AcceptMissing = acceptMissing;
					ct.AcceptPlaceholders = acceptPlaceholders;
					ct.AcceptPunctuation = acceptPunctuation;
				}
				else
				{
					// Quantified text, go deeper
					// Update existing entry or create and add a new one
					QuantifiedTextViewModel qt = ct.QuantifiedTextVMs
						.FirstOrDefault(q => q.Count == count && q.Modulo == modulo);

					bool newQt = qt == null;
					if (qt == null)
					{
						qt = new QuantifiedTextViewModel(ct);
						qt.Count = count;
						qt.Modulo = modulo;
					}
					qt.Text = text;
					qt.AcceptMissing = acceptMissing;
					qt.AcceptPlaceholders = acceptPlaceholders;
					qt.AcceptPunctuation = acceptPunctuation;
					if (newQt)
					{
						ct.QuantifiedTextVMs.InsertSorted(qt, (a, b) => QuantifiedTextViewModel.Compare(a, b));
					}
				}
			}
		}

		/// <summary>
		/// Finds an existing TextKeyViewModel or creates a new one in the correct place.
		/// </summary>
		/// <param name="textKey">The full text key to find or create.</param>
		/// <param name="updateTextKeys">true to add the new text key to the TextKeys dictionary. (Only if <paramref name="create"/> is set.)</param>
		/// <param name="create">true to create a new full text key if it doesn't exist yet, false to return null or partial TextKeyViewModels instead.</param>
		/// <param name="isNamespace">true to indicate that a single key segment is meant to be a namespace key.</param>
		/// <returns></returns>
		private TextKeyViewModel FindOrCreateTextKey(string textKey, bool updateTextKeys = true, bool create = true, bool isNamespace = false)
		{
			// Tokenize text key to find the tree node
			string partialKey = "";
			TextKeyViewModel tk = RootTextKey;
			if (!textKey.Contains(':') && isNamespace)
			{
				// Fake the separator to use existing code; clean up later
				textKey += ":";
			}
			string[] nsParts = textKey.Split(':');
			string localKey;
			if (nsParts.Length > 1)
			{
				// Namespace set
				partialKey = nsParts[0];
				var subtk = tk.Children.OfType<TextKeyViewModel>()
					.SingleOrDefault(vm => vm.DisplayName == nsParts[0]);
				if (subtk != null && !subtk.IsNamespace)
				{
					throw new NonNamespaceExistsException();
				}
				if (subtk == null)
				{
					// Namespace tree item does not exist yet, create it
					if (!create) return null;
					subtk = new TextKeyViewModel(nsParts[0], false, tk, tk.MainWindowVM);
					subtk.DisplayName = nsParts[0];
					subtk.IsNamespace = true;
					tk.Children.InsertSorted(subtk, TextKeyViewModel.Compare);
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

			if (localKey != "")
			{
				string[] keySegments = localKey.Split('.');
				for (int i = 0; i < keySegments.Length; i++)
				{
					string keySegment = keySegments[i];
					partialKey += keySegment;

					// Search for tree item
					var subtk = tk.Children.OfType<TextKeyViewModel>()
						.SingleOrDefault(vm => vm.DisplayName == keySegment);
					if (subtk != null && subtk.IsNamespace)
					{
						throw new NamespaceExistsException();
					}
					if (subtk == null)
					{
						// This level of text key item does not exist yet, create it
						if (!create) return null;
						subtk = new TextKeyViewModel(partialKey, i == keySegments.Length - 1, tk, tk.MainWindowVM);
						subtk.DisplayName = keySegment;
						tk.Children.InsertSorted(subtk, TextKeyViewModel.Compare);
					}
					tk = subtk;
					partialKey += ".";
				}
			}

			if (create)
			{
				if (updateTextKeys && !TextKeys.ContainsKey(textKey))
					TextKeys.Add(textKey, tk);
				tk.IsFullKey = true;
			}
			return tk;
		}

		/// <summary>
		/// Clears the flag about loaded files that are read-only.
		/// </summary>
		private void ClearReadonlyFiles()
		{
			readonlyFilesCount = 0;
		}

		/// <summary>
		/// Sets the flag about loaded files that are read-only. Call CheckNotifyReadonlyFiles()
		/// after loading all files to notify the user about read-only files.
		/// </summary>
		private void SetReadonlyFiles()
		{
			readonlyFilesCount++;
		}

		/// <summary>
		/// Notifies the user about read-only files, if any were loaded.
		/// </summary>
		private void CheckNotifyReadonlyFiles()
		{
			if (readonlyFilesCount > 0)
			{
				MessageBox.Show(
					Tx.T("msg.read-only files loaded", readonlyFilesCount),
					Tx.T("msg.caption.warning"),
					MessageBoxButton.OK,
					MessageBoxImage.Warning);
			}
		}

		#endregion XML loading methods

		#region XML saving methods

		/// <summary>
		/// Writes all loaded text keys to a file.
		/// </summary>
		/// <param name="fileNamePrefix">Path and file name prefix, without culture name and extension.</param>
		/// <returns>true, if the file was saved successfully, false otherwise.</returns>
		private bool WriteToXmlFile(string fileNamePrefix)
		{
			if (fileVersion == 1)
			{
				// Check all files for read-only attribute
				foreach (var cultureName in LoadedCultureNames.Union(DeletedCultureNames).Distinct())
				{
					string cultureFileName = fileNamePrefix + "." + cultureName + ".xml";
					FileInfo fi = new FileInfo(cultureFileName);
					if (fi.Exists && fi.IsReadOnly)
					{
						MessageBox.Show(
							Tx.T("msg.cannot write to read-only file"),
							Tx.T("msg.caption.error"),
							MessageBoxButton.OK,
							MessageBoxImage.Error);
						return false;
					}
				}

				// Delete previous backups and move current files to backup
				foreach (var cultureName in LoadedCultureNames.Union(DeletedCultureNames).Distinct())
				{
					string cultureFileName = fileNamePrefix + "." + cultureName + ".xml";
					if (File.Exists(cultureFileName))
					{
						try
						{
							File.Delete(cultureFileName + ".bak");
						}
						catch (Exception ex)
						{
							MessageBox.Show(
								Tx.T("msg.cannot delete backup file", "name", cultureFileName + ".bak", "msg", ex.Message),
								Tx.T("msg.caption.error"),
								MessageBoxButton.OK,
								MessageBoxImage.Error);
							return false;
						}
						try
						{
							File.Move(cultureFileName, cultureFileName + ".bak");
						}
						catch (Exception ex)
						{
							MessageBox.Show(
								Tx.T("msg.cannot backup file.v1", "name", cultureFileName, "msg", ex.Message),
								Tx.T("msg.caption.error"),
								MessageBoxButton.OK,
								MessageBoxImage.Error);
							return false;
						}
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
					try
					{
						WriteXmlToFile(xmlDoc, fileNamePrefix + "." + cultureName + ".xml");
					}
					catch (Exception ex)
					{
						MessageBox.Show(
							Tx.T("msg.cannot write file.v1", "name", fileNamePrefix + "." + cultureName + ".xml", "msg", ex.Message),
							Tx.T("msg.caption.error"),
							MessageBoxButton.OK,
							MessageBoxImage.Error);
						return false;
					}
				}

				// Delete all backup files (could also be an option)
				int deleteErrorCount = 0;
				foreach (var cultureName in LoadedCultureNames.Union(DeletedCultureNames).Distinct())
				{
					string cultureFileName = fileNamePrefix + "." + cultureName + ".xml";
					try
					{
						File.Delete(cultureFileName + ".bak");
					}
					catch
					{
						deleteErrorCount++;
					}
				}
				if (deleteErrorCount > 0)
				{
					MessageBox.Show(
						Tx.T("msg.cannot delete new backup file.v1"),
						Tx.T("msg.caption.error"),
						MessageBoxButton.OK,
						MessageBoxImage.Error);
				}
				DeletedCultureNames.Clear();
			}
			else if (fileVersion == 2)
			{
				// Check file for read-only attribute
				FileInfo fi = new FileInfo(fileNamePrefix + ".txd");
				if (fi.Exists && fi.IsReadOnly)
				{
					MessageBox.Show(
						Tx.T("msg.cannot write to read-only file"),
						Tx.T("msg.caption.error"),
						MessageBoxButton.OK,
						MessageBoxImage.Error);
					return false;
				}

				// Delete previous backup and move current file to backup
				if (File.Exists(fileNamePrefix + ".txd"))
				{
					try
					{
						File.Delete(fileNamePrefix + ".txd.bak");
					}
					catch (Exception ex)
					{
						MessageBox.Show(
							Tx.T("msg.cannot delete backup file", "name", fileNamePrefix + ".txd.bak", "msg", ex.Message),
							Tx.T("msg.caption.error"),
							MessageBoxButton.OK,
							MessageBoxImage.Error);
						return false;
					}
					try
					{
						File.Move(fileNamePrefix + ".txd", fileNamePrefix + ".txd.bak");
					}
					catch (Exception ex)
					{
						MessageBox.Show(
							Tx.T("msg.cannot backup file.v2", "name", fileNamePrefix + ".txd", "msg", ex.Message),
							Tx.T("msg.caption.error"),
							MessageBoxButton.OK,
							MessageBoxImage.Error);
						return false;
					}
				}

				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.AppendChild(xmlDoc.CreateElement("translation"));
				var spaceAttr = xmlDoc.CreateAttribute("xml:space");
				spaceAttr.Value = "preserve";
				xmlDoc.DocumentElement.Attributes.Append(spaceAttr);

				foreach (var cultureName in LoadedCultureNames.OrderBy(cn => cn))
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
				try
				{
					WriteXmlToFile(xmlDoc, fileNamePrefix + ".txd");
				}
				catch (Exception ex)
				{
					// Try to restore the backup file
					try
					{
						File.Delete(fileNamePrefix + ".txd");
						File.Move(fileNamePrefix + ".txd.bak", fileNamePrefix + ".txd");

						MessageBox.Show(
							Tx.T("msg.cannot write file.v2 restored", "name", fileNamePrefix + ".txd", "msg", ex.Message),
							Tx.T("msg.caption.error"),
							MessageBoxButton.OK,
							MessageBoxImage.Error);
					}
					catch (Exception ex2)
					{
						MessageBox.Show(
							Tx.T("msg.cannot write file.v2", "name", fileNamePrefix + ".txd", "msg", ex2.Message, "firstmsg", ex.Message),
							Tx.T("msg.caption.error"),
							MessageBoxButton.OK,
							MessageBoxImage.Error);
					}
					return false;
				}

				// Delete backup file (could also be an option)
				try
				{
					File.Delete(fileNamePrefix + ".txd.bak");
				}
				catch (Exception ex)
				{
					MessageBox.Show(
						Tx.T("msg.cannot delete new backup file.v2", "name", fileNamePrefix + ".txd.bak", "msg", ex.Message),
						Tx.T("msg.caption.error"),
						MessageBoxButton.OK,
						MessageBoxImage.Error);
				}
			}
			else
			{
				MessageBox.Show(
					Tx.T("msg.cannot save unsupported file version", "ver", fileVersion.ToString()),
					Tx.T("msg.caption.error"),
					MessageBoxButton.OK,
					MessageBoxImage.Error);
				return false;
			}
			FileModified = false;
			return true;
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
					if (!string.IsNullOrEmpty(cultureTextVM.Text) ||
						textKeyVM.IsEmpty() && cultureName == PrimaryCulture && !textKeyVM.TextKey.StartsWith("Tx:") ||   // Save empty text keys in the primary culture at least (not for system keys)
						cultureTextVM.AcceptMissing || cultureTextVM.AcceptPlaceholders || cultureTextVM.AcceptPunctuation)   // Keep accept flags
					{
						var textElement = xe.OwnerDocument.CreateElement("text");
						xe.AppendChild(textElement);
						var keyAttr = xe.OwnerDocument.CreateAttribute("key");
						keyAttr.Value = textKeyVM.TextKey;
						textElement.Attributes.Append(keyAttr);
						if (!string.IsNullOrEmpty(cultureTextVM.Text))
						{
							textElement.InnerText = cultureTextVM.Text;
						}

						if (cultureTextVM.AcceptMissing)
						{
							var acceptMissingAttr = xe.OwnerDocument.CreateAttribute("acceptmissing");
							acceptMissingAttr.Value = "true";
							textElement.Attributes.Append(acceptMissingAttr);
						}
						if (cultureTextVM.AcceptPlaceholders)
						{
							var acceptPlaceholdersAttr = xe.OwnerDocument.CreateAttribute("acceptplaceholders");
							acceptPlaceholdersAttr.Value = "true";
							textElement.Attributes.Append(acceptPlaceholdersAttr);
						}
						if (cultureTextVM.AcceptPunctuation)
						{
							var acceptPunctuationAttr = xe.OwnerDocument.CreateAttribute("acceptpunctuation");
							acceptPunctuationAttr.Value = "true";
							textElement.Attributes.Append(acceptPunctuationAttr);
						}

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

						if (quantifiedTextVM.AcceptMissing)
						{
							var acceptMissingAttr = xe.OwnerDocument.CreateAttribute("acceptmissing");
							acceptMissingAttr.Value = "true";
							textElement.Attributes.Append(acceptMissingAttr);
						}
						if (quantifiedTextVM.AcceptPlaceholders)
						{
							var acceptPlaceholdersAttr = xe.OwnerDocument.CreateAttribute("acceptplaceholders");
							acceptPlaceholdersAttr.Value = "true";
							textElement.Attributes.Append(acceptPlaceholdersAttr);
						}
						if (quantifiedTextVM.AcceptPunctuation)
						{
							var acceptPunctuationAttr = xe.OwnerDocument.CreateAttribute("acceptpunctuation");
							acceptPunctuationAttr.Value = "true";
							textElement.Attributes.Append(acceptPunctuationAttr);
						}

						if (!string.IsNullOrEmpty(quantifiedTextVM.Text))
						{
							textElement.InnerText = quantifiedTextVM.Text;
						}
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

		/// <summary>
		/// Validates all text keys and updates the suggestions later.
		/// </summary>
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

		/// <summary>
		/// Validates all text keys now and updates the suggestions later.
		/// </summary>
		public void ValidateTextKeys()
		{
			RootTextKey.Validate();
			UpdateSuggestionsLater();
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

		private void InsertSystemKeys(string culture)
		{
			if (!string.IsNullOrEmpty(culture))
			{
				Stream templateStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Unclassified.TxEditor.Template.txd");
				if (templateStream == null)
				{
					throw new Exception("The template dictionary is not an embedded resource in this assembly. This is a build error.");
				}
				XmlDocument xmlDoc = new XmlDocument();
				xmlDoc.Load(templateStream);

				var xe = xmlDoc.DocumentElement.SelectSingleNode("culture[@name='" + culture + "']") as XmlElement;
				if (xe != null)
				{
					LoadFromXml(culture, xe);

					if (culture.Length == 5)
					{
						MessageBox.Show(
							Tx.T("msg.insert system keys.base culture", "name", culture.Substring(0, 2)),
							Tx.T("msg.insert system keys.base culture.title"),
							MessageBoxButton.OK,
							MessageBoxImage.Information);
					}
				}
				else
				{
					MessageBox.Show(
						Tx.T("msg.insert system keys.not available", "name", culture),
						Tx.T("msg.insert system keys.not available.title"),
						MessageBoxButton.OK,
						MessageBoxImage.Warning);
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
					" " + Tx.T("window.title.in path") + " " + loadedFilePath + " – TxEditor";
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
				viewHistory.Add(selectedTextKeys[0]);
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
				viewHistory.Add(selectedTextKeys[0]);
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

		private DelegateCommand initCommand;
		public DelegateCommand InitCommand
		{
			get
			{
				if (initCommand == null)
				{
					initCommand = new DelegateCommand(OnInit);
				}
				return initCommand;
			}
		}

		private void OnInit()
		{
			App.SplashScreen.Close(TimeSpan.FromMilliseconds(300));
			// Work-around for implementation bug in SplashScreen.Close that steals the focus
			MainWindow.Instance.Focus();

			CheckNotifyReadonlyFiles();

			if (!string.IsNullOrWhiteSpace(ScanDirectory))
			{
				SelectFileViewModel sfVM = new SelectFileViewModel(ScanDirectory);
				SelectFileWindow sfw = new SelectFileWindow();
				sfw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
				sfw.Owner = MainWindow.Instance;
				sfw.DataContext = sfVM;
				if (sfw.ShowDialog() == true)
				{
					LoadFiles(sfVM.SelectedFileNames);
				}
			}
		}

		#endregion Window management

		#region Text search

		private DelayedCall searchDc;
		private string searchText = "";   // Initialise so that it's not changed at startup
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
					isVisible &= child.HasOwnProblem || child.HasProblem;
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

		private void UpdateSuggestionsLayout()
		{
			if (App.Settings.ShowSuggestions)
			{
				if (App.Settings.SuggestionsHorizontalLayout)
				{
					SuggestionsSplitterHeight = 0;
					SuggestionsPanelHeight = 0;
					SuggestionsSplitterWidth = 6;
					SuggestionsPanelWidth = App.Settings.SuggestionsWidth;
				}
				else
				{
					SuggestionsSplitterHeight = 6;
					SuggestionsPanelHeight = App.Settings.SuggestionsHeight;
					SuggestionsSplitterWidth = 0;
					SuggestionsPanelWidth = 0;
				}
			}
			else
			{
				SuggestionsSplitterHeight = 0;
				SuggestionsPanelHeight = 0;
				SuggestionsSplitterWidth = 0;
				SuggestionsPanelWidth = 0;
			}
		}

		private void AddDummySuggestion()
		{
			SuggestionViewModel suggestion = new SuggestionViewModel(this);
			suggestion.IsDummy = true;
			suggestion.BaseText = Tx.T("suggestions.none");
			suggestions.Add(suggestion);
		}

		private void UpdateSuggestionsLater()
		{
			TaskHelper.Background(UpdateSuggestions);
		}

		private void UpdateSuggestions()
		{
			Match m;

			suggestions.Clear();
			HaveSuggestions = false;

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

			TextKeyViewModel tk = selectedTextKeys != null && selectedTextKeys.Count > 0 ? selectedTextKeys[0] : null;
			if (tk == null || tk.CultureTextVMs.Count < 1)
			{
				AddDummySuggestion();
				return;
			}

			// The text we're finding translation suggestions for
			string refText = tk.CultureTextVMs[0].Text;
			string origRefText = refText;
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
				bool isExactMatch = false;
				string otherBaseText = kvp.Value.CultureTextVMs[0].Text;
				string otherTranslatedText = kvp.Value.CultureTextVMs.First(ct => ct.CultureName == lastSelectedCulture).Text;

				if (string.IsNullOrEmpty(otherBaseText)) continue;
				if (string.IsNullOrEmpty(otherTranslatedText)) continue;

				if (otherBaseText == origRefText)
				{
					// Both keys' primary translation matches exactly
					isExactMatch = true;
				}

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
					// There are no significant words in the other text
					score = 0;
				}

				if (isExactMatch)
				{
					score = 100000;
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
					suggestion.IsExactMatch = kvp.Value >= 100000;
					suggestion.ScoreNum = kvp.Value;
					if (suggestion.IsExactMatch)
						suggestion.Score = Tx.T("suggestions.exact match");
					else
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
			else
			{
				HaveSuggestions = true;
			}
		}

		#endregion Suggestions

		#region IViewCommandSource members

		private ViewCommandManager viewCommandManager = new ViewCommandManager();
		public ViewCommandManager ViewCommandManager { get { return viewCommandManager; } }

		#endregion IViewCommandSource members
	}
}
