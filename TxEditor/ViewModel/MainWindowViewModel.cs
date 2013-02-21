using System;
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
using TxEditor.View;
using Unclassified;
using Unclassified.UI;

namespace TxEditor.ViewModel
{
	class MainWindowViewModel : ViewModelBase
	{
		public MainWindow View { get; set; }

		private int fileVersion;

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

		private bool hiddenChars;
		public bool HiddenChars
		{
			get { return hiddenChars; }
			set
			{
				if (value != hiddenChars)
				{
					hiddenChars = value;
					OnPropertyChanged("HiddenChars");
				}
			}
		}

		private double fontScale = 100;
		public double FontScale
		{
			get { return fontScale; }
			set
			{
				if (value != fontScale)
				{
					fontScale = value;
					OnPropertyChanged("FontScale");
					OnPropertyChanged("FontSize");
					OnPropertyChanged("TextFormattingMode");
				}
			}
		}

		public double FontSize
		{
			get { return fontScale / 100 * 12; }
		}

		public TextFormattingMode TextFormattingMode
		{
			get { return FontSize < 16 ? TextFormattingMode.Display : TextFormattingMode.Ideal; }
		}

		private bool monospaceFont;
		public bool MonospaceFont
		{
			get { return monospaceFont; }
			set
			{
				if (value != monospaceFont)
				{
					monospaceFont = value;
					OnPropertyChanged("MonospaceFont");
					OnPropertyChanged("FontFamily");
				}
			}
		}

		private string monospaceFontFamily;
		public object FontFamily
		{
			get
			{
				if (monospaceFont)
				{
					if (monospaceFontFamily == null)
					{
						if (Fonts.SystemFontFamilies.Any(f => f.Source == "Consolas"))
						{
							monospaceFontFamily = "Consolas";
						}
						else if (Fonts.SystemFontFamilies.Any(f => f.Source == "Andale Mono"))
						{
							monospaceFontFamily = "Andale Mono";
						}
						else
						{
							monospaceFontFamily = "Courier New";
						}
					}
					return new FontFamily(monospaceFontFamily);
				}
				return DependencyProperty.UnsetValue;
			}
		}

		public HashSet<string> TextKeys { get; private set; }
		public HashSet<string> LoadedCultureNames { get; private set; }

		public TextKeyViewModel RootTextKey { get; private set; }

		#region Constructors

		public MainWindowViewModel()
		{
			TextKeys = new HashSet<string>();
			LoadedCultureNames = new HashSet<string>();
			RootTextKey = new TextKeyViewModel(null, false, null, this);
		}

		#endregion Constructors

		#region Toolbar commands

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

		#endregion Toolbar commands

		#region Toolbar command handlers

		private void OnLoadFolder()
		{
			var d = new OpenFolderDialog();
			d.Title = "Select folder to load files from";
			if (d.ShowDialog(new Wpf32Window(View)) == true)
			{
				//string path = @"C:\Source\TxTranslator\tmp\wsz-lang\";
				string path = d.Folder;
				string filePrefix = "wsz";

				fileVersion = 0;
				string regex = @"(\.(([a-z]{2})([-][a-z]{2})?))?\.txd$";
				if (!string.IsNullOrEmpty(filePrefix))
				{
					regex = "^" + filePrefix + regex;
				}
				foreach (string fileName in Directory.GetFiles(path))
				{
					Match m = Regex.Match(Path.GetFileName(fileName), regex, RegexOptions.IgnoreCase);
					if (m.Success)
					{
						LoadFromXmlFile(fileName);
					}
				}
			}
		}

		private void OnLoadFile()
		{
			var d = new OpenFileDialog();
			d.CheckFileExists = true;
			d.Filter = "Tx dictionary files (*.txd)|*.txd|XML files (*.xml)|*.xml|All files (*.*)|*.*";
			d.Multiselect = true;
			d.ShowReadOnly = false;
			d.Title = "Select files to load";
			if (d.ShowDialog(View) == true)
			{
				foreach (string fileName in d.FileNames)
				{
					LoadFromXmlFile(fileName);
				}
			}
		}

		private void OnAbout()
		{
			var root = View.Content as UIElement;

			var blur = new BlurEffect();
			blur.Radius = 0;
			root.Effect = blur;

			root.AnimateDoubleEase(UIElement.OpacityProperty, 1, 0.6, TimeSpan.FromSeconds(1));
			blur.AnimateDoubleEase(BlurEffect.RadiusProperty, 0, 4, TimeSpan.FromSeconds(0.5));

			var win = new AboutWindow();
			win.Owner = View;
			win.ShowDialog();

			root.AnimateDoubleEase(UIElement.OpacityProperty, 0.6, 1, TimeSpan.FromSeconds(0.2));
			blur.AnimateDoubleEase(BlurEffect.RadiusProperty, 4, 0, TimeSpan.FromSeconds(0.2));

			DelayedCall.Start(() =>
			{
				root.Effect = null;
			}, 500);
		}

		#endregion Toolbar command handlers

		#region XML loading methods

		public void LoadFromXmlFile(string fileName)
		{
			// First load the XML file into an XmlDocument for further processing
			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.Load(fileName);

			// Try to recognise the culture name from the file name
			Match m = Regex.Match(fileName, @"\.(([a-z]{2})([-][a-z]{2})?)\.(?:txd|xml)$", RegexOptions.IgnoreCase);
			if (m.Success)
			{
				CultureInfo ci = CultureInfo.GetCultureInfo(m.Groups[1].Value);
				LoadFromXml(ci.Name, xmlDoc.DocumentElement);

				// Set the primary culture if a file claims to be it
				XmlAttribute primaryAttr = xmlDoc.DocumentElement.Attributes["primary"];
				if (primaryAttr != null && primaryAttr.Value == "true")
				{
					PrimaryCulture = ci.Name;
				}
				if (fileVersion == 0)
					fileVersion = 1;
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
				fileVersion = 2;
		}

		private void LoadFromXml(string cultureName, XmlElement xe)
		{
			// Add the new culture everywhere
			if (!LoadedCultureNames.Contains(cultureName))
			{
				AddNewCulture(RootTextKey, cultureName);
			}
			
			// Read the XML document
			foreach (XmlNode textNode in xe.SelectNodes("text[@key]"))
			{
				string text = textNode.InnerText;
				string key = textNode.Attributes["key"].Value;

				if (key == "")
				{
					//Log("Load XML: Key attribute is empty. Ignoring definition.");
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

				// Tokenize text key to find the tree node
				TextKeyViewModel tk = RootTextKey;
				string[] nsParts = key.Split(':');
				string localKey;
				if (nsParts.Length > 1)
				{
					// Namespace set
					var subtk = tk.Children.SingleOrDefault(vm => vm.DisplayName == nsParts[0]) as TextKeyViewModel;
					if (subtk == null)
					{
						// Namespace tree item does not exist yet, create it
						subtk = new TextKeyViewModel(key, false, tk, tk.MainWindowVM);
						subtk.DisplayName = nsParts[0];
						subtk.ImageSource = "/Images/textkey_namespace.png";
						tk.Children.InsertSorted(subtk, (a, b) => TextKeyViewModel.Compare(a, b));
					}
					tk = subtk;
					// Continue with namespace-free text key
					localKey = nsParts[1];
				}
				else
				{
					// No namespace set, continue with entire key
					localKey = key;
				}

				string[] keySegments = localKey.Split('.');
				for (int i = 0; i < keySegments.Length; i++)
				{
					string keySegment = keySegments[i];

					// Search for tree item
					var subtk = tk.Children.SingleOrDefault(vm => vm.DisplayName == keySegment) as TextKeyViewModel;
					if (subtk == null)
					{
						// This level of text key item does not exist yet, create it
						subtk = new TextKeyViewModel(key, i == keySegments.Length - 1, tk, tk.MainWindowVM);
						subtk.DisplayName = keySegment;
						if (subtk.IsLeafNode)
						{
							subtk.ImageSource = "/Images/key.png";
						}
						else
						{
							subtk.ImageSource = "/Images/textkey_segment.png";
						}
						tk.Children.InsertSorted(subtk, (a, b) => TextKeyViewModel.Compare(a, b));
					}
					tk = subtk;
				}

				tk.IsLeafNode = true;
				tk.ImageSource = "/Images/key.png";

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

		#endregion XML loading methods

		#region Culture management

		private void EnsureCultureInTextKey(TextKeyViewModel tk, string cultureName)
		{
			if (!tk.CultureTextVMs.Any(vm => vm.CultureName == cultureName))
			{
				tk.CultureTextVMs.InsertSorted(new CultureTextViewModel(cultureName, tk), (a, b) => a.CompareTo(b));
			}
		}

		private void AddNewCulture(TextKeyViewModel root, string cultureName)
		{
			foreach (TextKeyViewModel tk in root.Children)
			{
				EnsureCultureInTextKey(tk, cultureName);
				tk.UpdateCultureTextSeparators();
				if (tk.Children.Count > 0)
				{
					AddNewCulture(tk, cultureName);
				}
			}
			if (!LoadedCultureNames.Contains(cultureName))
			{
				LoadedCultureNames.Add(cultureName);
			}
		}

		#endregion Culture management
	}
}
