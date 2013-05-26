using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using Unclassified;

namespace TxEditor.ViewModel
{
	class TextKeyViewModel : TreeViewItemViewModel
	{
		public MainWindowViewModel MainWindowVM { get; private set; }

		private string textKey;
		public string TextKey
		{
			get { return textKey; }
		}

		private bool hasProblem;
		public bool HasProblem
		{
			get { return hasProblem; }
			set
			{
				if (value != hasProblem)
				{
					// Remove from previous list
					if (hasProblem)
					{
						MainWindowVM.ProblemKeys.Remove(this);
					}

					hasProblem = value;
					UpdateIcon();

					// Add to new list
					if (hasProblem && IsFullKey)
					{
						MainWindowVM.ProblemKeys.Add(this);
					}

					OnPropertyChanged("HasProblem");
				}
			}
		}

		bool isNamespace;
		public bool IsNamespace
		{
			get { return isNamespace; }
			set
			{
				if (value != isNamespace)
				{
					isNamespace = value;
					UpdateIcon();
					OnPropertyChanged("IsNamespace");
				}
			}
		}

		bool isFullKey;
		public bool IsFullKey
		{
			get { return isFullKey; }
			set
			{
				if (value != isFullKey)
				{
					isFullKey = value;
					UpdateIcon();
					OnPropertyChanged("IsFullKey");
				}
			}
		}

		private ObservableCollection<CultureTextViewModel> cultureTextVMs;
		public ObservableCollection<CultureTextViewModel> CultureTextVMs
		{
			get { return cultureTextVMs; }
		}

		private string imageSource;
		public string ImageSource
		{
			get { return imageSource; }
			set
			{
				if (value != imageSource)
				{
					imageSource = value;
					OnPropertyChanged("ImageSource");
				}
			}
		}

		private string remarks;
		public string Remarks
		{
			get { return remarks; }
			set
			{
				if (value != remarks)
				{
					remarks = value;
					OnPropertyChanged("Remarks");
				}
			}
		}

		private string comment;
		public string Comment
		{
			get { return comment; }
			set
			{
				if (value != comment)
				{
					comment = value;
					OnPropertyChanged("Comment");
				}
			}
		}

		public TextKeyViewModel(string textKey, bool isFullKey, TreeViewItemViewModel parent, MainWindowViewModel mainWindowVM)
			: base(parent, false)
		{
			this.textKey = textKey;
			this.isFullKey = isFullKey;

			MainWindowVM = mainWindowVM;

			cultureTextVMs = new ObservableCollection<CultureTextViewModel>();

			UpdateIcon();
		}

		public override string ToString()
		{
			return "{TextKeyViewModel " + textKey + "}";
		}

		public bool IsEmpty()
		{
			if (!string.IsNullOrEmpty(Comment)) return false;
			foreach (var ct in CultureTextVMs)
			{
				if (!ct.IsEmpty()) return false;
			}
			return true;
		}
		
		public bool Validate()
		{
			// First validate all children recursively and remember whether there was a problem
			// somewhere down the tree
			bool anyChildError = false;
			foreach (TextKeyViewModel child in Children)
			{
				if (!child.Validate())
				{
					anyChildError = true;
				}
			}

			// Partial keys can only indicate problems in the subtree
			if (!IsFullKey)
			{
				HasProblem = anyChildError;
				Remarks = null;
				return !anyChildError;
			}

			// The Tx namespace generally has no problems
			// TODO: This shall only apply to missing translations, not to content/format errors
			if (TextKey.StartsWith("Tx:"))
			{
				HasProblem = anyChildError;
				Remarks = null;
				return !anyChildError;
			}

			// ----- Check for count/modulo errors -----

			// Check for invalid count values in any CultureText
			if (CultureTextVMs.Any(ct =>
				ct.QuantifiedTextVMs.Any(qt =>
					qt.Count < 0 || qt.Count >= 0xFFFF)))
			{
				HasProblem = true;
				Remarks = "Invalid count";
				return false;
			}
			// Check for invalid modulo values in any CultureText
			if (CultureTextVMs.Any(ct =>
				ct.QuantifiedTextVMs.Any(qt =>
					qt.Modulo != 0 && (qt.Modulo < 2 || qt.Modulo > 1000))))
			{
				HasProblem = true;
				Remarks = "Invalid modulo";
				return false;
			}
			// Check for duplicate count/modulo values in any CultureText
			if (CultureTextVMs.Any(ct =>
				ct.QuantifiedTextVMs.GroupBy(qt =>
					qt.Count << 16 | qt.Modulo).Any(grp =>
						grp.Count() > 1)))
			{
				HasProblem = true;
				Remarks = "Duplicate count/modulo";
				return false;
			}

			// ----- Check for missing translations -----

			// First check that every non-region culture has a text set
			if (CultureTextVMs.Any(vm => vm.CultureName.Length == 2 && string.IsNullOrEmpty(vm.Text)))
			{
				HasProblem = true;
				Remarks = "Missing translations";
				return false;
			}
			// Then check that every region-culture with no text has a non-region culture with a
			// text set (as fallback)
			if (CultureTextVMs.Any(vm =>
				vm.CultureName.Length == 5 &&
				string.IsNullOrEmpty(vm.Text) &&
				!CultureTextVMs.Any(vm2 => vm2.CultureName == vm.CultureName.Substring(0, 2))))
			{
				HasProblem = true;
				Remarks = "Missing translations";
				return false;
			}

			// ----- No (new) problem -----
			
			HasProblem = anyChildError;
			Remarks = null;
			return !anyChildError;
		}

		public void UpdateCultureTextSeparators()
		{
			string prevLang = null;
			for (int i = cultureTextVMs.Count - 1; i >= 0; i--)
			{
				string lang = cultureTextVMs[i].CultureName.Substring(0, 2);
				cultureTextVMs[i].LastOfLanguage = lang != prevLang;
				prevLang = lang;
			}
		}

		private void UpdateIcon()
		{
			if (IsNamespace)
			{
				ImageSource = "/Images/textkey_namespace.png";
			}
			else if (IsFullKey)
			{
				if (HasProblem)
				{
					ImageSource = "/Images/key_error.png";
				}
				else
				{
					ImageSource = "/Images/key.png";
				}
			}
			else
			{
				if (HasProblem)
				{
					ImageSource = "/Images/textkey_segment_error.png";
				}
				else
				{
					ImageSource = "/Images/textkey_segment.png";
				}
			}
		}

		/// <summary>
		/// Sets a new text key value and updates the prefix for all children.
		/// </summary>
		/// <param name="newKey">New text key.</param>
		public void SetKeyRecursive(string newKey, HashSet<string> textKeys)
		{
			int i = newKey.LastIndexOfAny(new char[] { ':', '.' });
			if (i >= 0)
				DisplayName = newKey.Substring(i + 1);
			else
				DisplayName = newKey;

			foreach (TextKeyViewModel child in Children)
			{
				child.ReplaceKeyRecursive(textKey, newKey, textKeys);
			}

			if (textKeys.Contains(textKey))
			{
				textKeys.Remove(textKey);
				textKeys.Add(newKey);
			}
			textKey = newKey;
			OnPropertyChanged("TextKey");
		}

		/// <summary>
		/// Replaces the text key's prefix with a different value, also for all children.
		/// </summary>
		/// <param name="oldKey">Old text key prefix to delete.</param>
		/// <param name="newKey">New text key prefix to insert.</param>
		private void ReplaceKeyRecursive(string oldKey, string newKey, HashSet<string> textKeys)
		{
			string oldTextKey = textKey;
			textKey = textKey.ReplaceStart(oldKey, newKey);
			OnPropertyChanged("TextKey");
			if (textKeys.Contains(oldTextKey))
			{
				textKeys.Remove(oldTextKey);
				textKeys.Add(textKey);
			}

			foreach (TextKeyViewModel child in Children)
			{
				child.ReplaceKeyRecursive(oldKey, newKey, textKeys);
			}
		}

		/// <summary>
		/// Compares two TextKeyViewModel instances to determine the sort order in the text keys
		/// tree.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public static int Compare(object a, object b)
		{
			TextKeyViewModel ta = a as TextKeyViewModel;
			TextKeyViewModel tb = b as TextKeyViewModel;
			if (ta == null || tb == null) return 0;

			bool aNs = ta.IsNamespace;
			bool bNs = tb.IsNamespace;
			bool aTxNs = ta.IsNamespace && ta.TextKey == "Tx";
			bool bTxNs = tb.IsNamespace && tb.TextKey == "Tx";

			// Tx: always comes first
			if (aTxNs ^ bTxNs)
			{
				if (aTxNs) return -1;
				if (bTxNs) return 1;
			}
			// Namespaces come before normal keys
			if (aNs ^ bNs)
			{
				if (aNs) return -1;
				if (bNs) return 1;
			}
			// Sort everything else by full key name
			//return string.Compare(ta.TextKey, tb.TextKey, StringComparison.InvariantCultureIgnoreCase);
			return NaturalSort.NatCompare(ta.TextKey, tb.TextKey);
		}

		/// <summary>
		/// Validates the name of a text key.
		/// </summary>
		/// <param name="keyName">Text key to validate.</param>
		/// <param name="message">Receives the error message if any, otherwise null.</param>
		/// <returns>true if the name is valid, otherwise false.</returns>
		public static bool ValidateName(string keyName, out string message)
		{
			// Invalid examples:
			//    (empty)
			// :
			// abc:
			// :abc
			// abc:def:ghi
			// abc::def
			// .
			// abc.
			// .abc
			// abc..def

			if (string.IsNullOrWhiteSpace(keyName))
			{
				message = "Key is empty or only white-space.";
				return false;
			}
			if (keyName.IndexOf(':') != keyName.LastIndexOf(':'))
			{
				message = "Key contains multiple colons (:).";
				return false;
			}
			if (keyName.StartsWith(":"))
			{
				message = "Key starts with a colon (:).";
				return false;
			}
			if (keyName.EndsWith(":"))
			{
				message = "Key ends with a colon (:).";
				return false;
			}
			if (keyName.StartsWith("."))
			{
				message = "Key starts with a point (.).";
				return false;
			}
			if (keyName.EndsWith("."))
			{
				message = "Key ends with a point (.).";
				return false;
			}
			if (keyName.Contains(".."))
			{
				message = "Key contains multiple consecutive points (.).";
				return false;
			}

			// All checks passed
			message = null;
			return true;
		}
	}
}
