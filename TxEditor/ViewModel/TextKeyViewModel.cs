using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;
using Unclassified;
using TxLib;
using System.Text.RegularExpressions;

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

		private bool hasOwnProblem;
		/// <summary>
		/// Gets or sets a value indicating whether the text key item itself or any of its child
		/// items has a problem. This does not count towards the text keys with problems counter.
		/// </summary>
		public bool HasOwnProblem
		{
			get { return hasOwnProblem; }
			set
			{
				if (value != hasOwnProblem)
				{
					// Remove from previous list
					if (hasOwnProblem)
					{
						MainWindowVM.ProblemKeys.Remove(this);
					}

					hasOwnProblem = value;
					UpdateIcon();

					// Add to new list
					if (hasOwnProblem && IsFullKey)
					{
						MainWindowVM.ProblemKeys.Add(this);
					}

					OnPropertyChanged("HasOwnProblem");
				}
			}
		}

		private bool hasProblem;
		/// <summary>
		/// Gets or sets a value indicating whether the text key item itself has a problem. This
		/// is explained in the Remarks property and such keys are counted in the problems counter.
		/// </summary>
		public bool HasProblem
		{
			get { return hasProblem; }
			set
			{
				if (value != hasProblem)
				{
					hasProblem = value;
					UpdateIcon();
					OnPropertyChanged("HasProblem");
				}
			}
		}

		bool isNamespace;
		/// <summary>
		/// Gets or sets a value indicating whether the text key represents a namespace node.
		/// </summary>
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
		/// <summary>
		/// Gets or sets a value indicating whether the text key represents a full text key or
		/// only a key segment node.
		/// </summary>
		public bool IsFullKey
		{
			get { return isFullKey; }
			set
			{
				if (value != isFullKey)
				{
					isFullKey = value;

					if (!isFullKey)
					{
						Comment = null;
						CultureTextVMs.Clear();
					}
					
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
					MainWindowVM.HaveComment = !string.IsNullOrWhiteSpace(comment);
					MainWindowVM.FileModified = true;
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

		/// <summary>
		/// Returns a value indicating whether any data was entered for this text key.
		/// </summary>
		/// <returns></returns>
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
				HasOwnProblem = false;
				HasProblem = anyChildError;
				Remarks = null;
				return !anyChildError;
			}

			// The Tx namespace generally has no problems
			// TODO: This shall only apply to missing translations, not to content/format errors
			if (TextKey.StartsWith("Tx:"))
			{
				HasOwnProblem = false;
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
				HasOwnProblem = true;
				HasProblem = true;
				Remarks = Tx.T("validation.content.invalid count");
				return false;
			}
			// Check for invalid modulo values in any CultureText
			if (CultureTextVMs.Any(ct =>
				ct.QuantifiedTextVMs.Any(qt =>
					qt.Modulo != 0 && (qt.Modulo < 2 || qt.Modulo > 1000))))
			{
				HasOwnProblem = true;
				HasProblem = true;
				Remarks = Tx.T("validation.content.invalid modulo");
				return false;
			}
			// Check for duplicate count/modulo values in any CultureText
			if (CultureTextVMs.Any(ct =>
				ct.QuantifiedTextVMs.GroupBy(qt =>
					qt.Count << 16 | qt.Modulo).Any(grp =>
						grp.Count() > 1)))
			{
				HasOwnProblem = true;
				HasProblem = true;
				Remarks = Tx.T("validation.content.duplicate count modulo");
				return false;
			}

			// ----- Check for missing translations -----

			// First check that every non-region culture has a text set
			if (CultureTextVMs.Any(vm => vm.CultureName.Length == 2 && string.IsNullOrEmpty(vm.Text)))
			{
				HasOwnProblem = true;
				HasProblem = true;
				Remarks = Tx.T("validation.content.missing translation");
				return false;
			}
			// Then check that every region-culture with no text has a non-region culture with a
			// text set (as fallback)
			if (CultureTextVMs.Any(vm =>
				vm.CultureName.Length == 5 &&
				string.IsNullOrEmpty(vm.Text) &&
				!CultureTextVMs.Any(vm2 => vm2.CultureName == vm.CultureName.Substring(0, 2))))
			{
				HasOwnProblem = true;
				HasProblem = true;
				Remarks = Tx.T("validation.content.missing translation");
				return false;
			}

			// ----- Check referenced keys -----

			if (CultureTextVMs.Any(ct =>
				ct.TextKeyReferences != null &&
				ct.TextKeyReferences.OfType<string>().Any(key => key == TextKey)))
			{
				HasOwnProblem = true;
				HasProblem = true;
				Remarks = Tx.T("validation.content.referenced key loop");
				return false;
			}

			if (CultureTextVMs.Any(ct =>
				ct.QuantifiedTextVMs.Any(qt =>
					qt.TextKeyReferences != null &&
					qt.TextKeyReferences.OfType<string>().Any(key => key == TextKey))))
			{
				HasOwnProblem = true;
				HasProblem = true;
				Remarks = Tx.T("validation.content.referenced key loop");
				return false;
			}

			if (CultureTextVMs.Any(ct =>
				ct.TextKeyReferences != null &&
				ct.TextKeyReferences.OfType<string>().Any(key => !MainWindowVM.TextKeys.ContainsKey(key))))
			{
				HasOwnProblem = true;
				HasProblem = true;
				Remarks = Tx.T("validation.content.missing referenced key");
				return false;
			}

			if (CultureTextVMs.Any(ct =>
				ct.QuantifiedTextVMs.Any(qt =>
					qt.TextKeyReferences != null &&
					qt.TextKeyReferences.OfType<string>().Any(key => !MainWindowVM.TextKeys.ContainsKey(key)))))
			{
				HasOwnProblem = true;
				HasProblem = true;
				Remarks = Tx.T("validation.content.missing referenced key");
				return false;
			}

			// ----- Check translations consistency -----

			if (CultureTextVMs.Count > 1)
			{
				string primaryText = CultureTextVMs[0].Text;

				for (int i = 0; i < CultureTextVMs.Count; i++)
				{
					// Skip main text of primary culture
					if (i > 0)
					{
						string transText = CultureTextVMs[i].Text;
						// Ignore any empty text. If that's a problem, it'll be found as missing above.
						if (!string.IsNullOrEmpty(transText))
						{
							string message;
							if (!CheckTextConsistency(primaryText, transText, out message))
							{
								HasOwnProblem = true;
								HasProblem = true;
								Remarks = message;
								return false;
							}
						}
					}

					foreach (var qt in CultureTextVMs[i].QuantifiedTextVMs)
					{
						string transText = qt.Text;
						// Ignore any empty text. If that's a problem, it'll be found as missing above.
						if (!string.IsNullOrEmpty(transText))
						{
							string message;
							if (!CheckTextConsistency(primaryText, transText, out message, false))   // Ignore count placeholder here
							{
								HasOwnProblem = true;
								HasProblem = true;
								Remarks = message;
								return false;
							}
						}
					}
				}
			}
			
			// ----- No (new) problem -----
			
			HasOwnProblem = false;
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

		public void UpdateIcon()
		{
			if (IsNamespace)
			{
				ImageSource = "/Images/textkey_namespace.png";
			}
			else if (IsFullKey)
			{
				if (CultureTextVMs.Any(ct => ct.QuantifiedTextVMs.Count > 0))
				{
					if (HasProblem)
					{
						ImageSource = "/Images/key_q_error.png";
					}
					else
					{
						ImageSource = "/Images/key_q.png";
					}
				}
				else
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
		/// Sets a new text key value but does not update any children.
		/// </summary>
		/// <param name="newKey">New text key.</param>
		public void SetKey(string newKey, Dictionary<string, TextKeyViewModel> textKeys)
		{
			int i = newKey.LastIndexOfAny(new char[] { ':', '.' });
			if (i >= 0)
				DisplayName = newKey.Substring(i + 1);
			else
				DisplayName = newKey;

			if (textKeys.ContainsKey(textKey))
			{
				textKeys.Remove(textKey);
				textKeys.Add(newKey, this);
			}
			textKey = newKey;
			OnPropertyChanged("TextKey");
		}

		/// <summary>
		/// Sets a new text key value and updates the prefix for all children.
		/// </summary>
		/// <param name="newKey">New text key.</param>
		/// <returns>Number of affected keys.</returns>
		public int SetKeyRecursive(string newKey, Dictionary<string, TextKeyViewModel> textKeys)
		{
			int i = newKey.LastIndexOfAny(new char[] { ':', '.' });
			if (i >= 0)
				DisplayName = newKey.Substring(i + 1);
			else
				DisplayName = newKey;
			int affectedKeys = 1;

			foreach (TextKeyViewModel child in Children)
			{
				affectedKeys += child.ReplaceKeyRecursive(textKey, newKey, textKeys);
			}

			if (textKeys.ContainsKey(textKey))
			{
				textKeys.Remove(textKey);
				textKeys.Add(newKey, this);
			}
			textKey = newKey;
			OnPropertyChanged("TextKey");
			return affectedKeys;
		}

		/// <summary>
		/// Replaces the text key's prefix with a different value, also for all children.
		/// </summary>
		/// <param name="oldKey">Old text key prefix to delete.</param>
		/// <param name="newKey">New text key prefix to insert.</param>
		/// <returns>Number of affected keys.</returns>
		private int ReplaceKeyRecursive(string oldKey, string newKey, Dictionary<string, TextKeyViewModel> textKeys)
		{
			string oldTextKey = textKey;
			textKey = textKey.ReplaceStart(oldKey, newKey);
			OnPropertyChanged("TextKey");
			if (textKeys.ContainsKey(oldTextKey))
			{
				textKeys.Remove(oldTextKey);
				textKeys.Add(textKey, this);
			}
			int affectedKeys = 1;

			foreach (TextKeyViewModel child in Children)
			{
				affectedKeys += child.ReplaceKeyRecursive(oldKey, newKey, textKeys);
			}
			return affectedKeys;
		}

		/// <summary>
		/// Copies all contents from another TextKeyViewModel instance to this one, replacing all
		/// data.
		/// </summary>
		/// <param name="other"></param>
		public void CopyFrom(TextKeyViewModel other)
		{
			Comment = other.Comment;
			IsFullKey = other.IsFullKey;

			CultureTextVMs.Clear();
			if (IsFullKey)
			{
				foreach (CultureTextViewModel ctVM in other.CultureTextVMs)
				{
					CultureTextVMs.Add(ctVM.Clone(this));
				}
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
				message = Tx.T("validation.key name.empty");
				return false;
			}
			if (keyName.IndexOf(':') != keyName.LastIndexOf(':'))
			{
				message = Tx.T("validation.key name.contains multiple colons");
				return false;
			}
			if (keyName.StartsWith(":"))
			{
				message = Tx.T("validation.key name.starts with colon");
				return false;
			}
			if (keyName.EndsWith(":"))
			{
				message = Tx.T("validation.key name.ends with colon");
				return false;
			}
			if (keyName.StartsWith("."))
			{
				message = Tx.T("validation.key name.starts with point");
				return false;
			}
			if (keyName.EndsWith("."))
			{
				message = Tx.T("validation.key name.ends with point");
				return false;
			}
			if (keyName.Contains(".."))
			{
				message = Tx.T("validation.key name.contains consecutive points");
				return false;
			}

			// All checks passed
			message = null;
			return true;
		}

		public static bool CheckTextConsistency(string a, string b, out string message, bool includeCount = true)
		{
			string pattern;
			Match m1, m2;

			if (a == null) a = "";
			if (b == null) b = "";

			// ----- Compare placeholders -----

			string placeholderPattern;
			if (includeCount)
				placeholderPattern = @"(?<!\{)\{(#|[^{=#]+)\}";
			else
				placeholderPattern = @"(?<!\{)\{([^{=#]+)\}";
			
			List<string> varNamesA = new List<string>();
			Match m = Regex.Match(a, placeholderPattern);
			while (m.Success)
			{
				if (!varNamesA.Contains(m.Groups[1].Value))
					varNamesA.Add(m.Groups[1].Value);
				m = m.NextMatch();
			}

			List<string> varNamesB = new List<string>();
			m = Regex.Match(b, placeholderPattern);
			while (m.Success)
			{
				if (!varNamesB.Contains(m.Groups[1].Value))
					varNamesB.Add(m.Groups[1].Value);
				m = m.NextMatch();
			}

			foreach (string n in varNamesA)
			{
				if (!varNamesB.Remove(n))
				{
					message = Tx.T("validation.content.missing placeholder", "name", n);
					return false;
				}
			}
			if (varNamesB.Count > 0)
			{
				message = Tx.T("validation.content.additional placeholder", "name", varNamesB[0]);
				return false;
			}

			// ----- Compare spacing/punctuation -----

			pattern = "^([ \t\r\n]*)";
			m1 = Regex.Match(a, pattern);
			m2 = Regex.Match(b, pattern);
			if (!m1.Success || !m2.Success || m1.Groups[1].Value != m2.Groups[1].Value)
			{
				message = Tx.T("validation.content.inconsistent punctuation");
				return false;
			}

			pattern = "([ \t\r\n!%,.:;?]*)$";
			m1 = Regex.Match(a, pattern);
			m2 = Regex.Match(b, pattern);
			if (!m1.Success || !m2.Success || m1.Groups[1].Value != m2.Groups[1].Value)
			{
				message = Tx.T("validation.content.inconsistent punctuation");
				return false;
			}

			message = null;
			return true;
		}
	}
}
