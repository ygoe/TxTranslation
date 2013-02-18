using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Unclassified.UI;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using System.Globalization;

namespace TxEditor.ViewModel
{
	class MainWindowViewModel : ViewModelBase
	{
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

		public TextKeyViewModel RootTextKey { get; protected set; }

		#region Constructors

		public MainWindowViewModel()
		{
			RootTextKey = new TextKeyViewModel(null, false, null);
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

		#endregion Toolbar commands

		#region Toolbar command handlers

		private void OnLoadFolder()
		{
			string path = @"C:\Source\TxTranslator\tmp\wsz-lang\";
			string filePrefix = "wsz";

			fileVersion = 0;
			string regex = @"(\.(([a-z]{2})([-][a-z]{2})?))?\.xml$";
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

		private void OnLoadFile()
		{
			string fileName = "";

			LoadFromXmlFile(fileName);
		}

		#endregion Toolbar command handlers

		#region XML loading methods

		private void LoadFromXmlFile(string fileName)
		{
			// First load the XML file into an XmlDocument for further processing
			XmlDocument xmlDoc = new XmlDocument();
			xmlDoc.Load(fileName);

			// Try to recognise the culture name from the file name
			Match m = Regex.Match(fileName, @"\.(([a-z]{2})([-][a-z]{2})?)\.xml$", RegexOptions.IgnoreCase);
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

		private void LoadFromXml(string culture, XmlElement xe)
		{
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
						subtk = new TextKeyViewModel(key, false, tk);
						subtk.DisplayName = nsParts[0];
						tk.Children.Add(subtk);
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
						subtk = new TextKeyViewModel(key, i == keySegments.Length - 1, tk);
						subtk.DisplayName = keySegment;
						tk.Children.Add(subtk);
					}
					tk = subtk;
				}

				tk.IsLeafNode = true;
				
				// Find the current culture
				// TODO: Ensure that all loaded cultures exist in every text key so that they can be entered
				//       -> Determine the cultures to load before loading text keys and store that list for use here
				var ct = tk.CultureTextVMs.FirstOrDefault(vm => vm.CultureName == culture);
				if (ct == null)
				{
					// Culture item does not exist yet, create it
					ct = new CultureTextViewModel(culture);
					tk.CultureTextVMs.Add(ct);   // TODO: InsertSorted | -> Adding cultures is done elsewhere, here everything already exists
				}

				if (count == -1)
				{
					// Default text, store it directly in the item
					ct.Text = text;
				}
				else
				{
					// Quantified text, go deeper
					// TODO
				}
			}
		}

		#endregion XML loading methods
	}
}
