using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using Unclassified.TxLib;
using Unclassified.Util;

namespace Unclassified.TxEditor.ViewModel
{
	internal class TextKeyViewModel : TreeViewItemViewModel
	{
		#region Constructor

		public TextKeyViewModel(string textKey, bool isFullKey, TreeViewItemViewModel parent, MainViewModel mainWindowVM)
			: base(parent, false)
		{
			this.textKey = textKey;
			this.isFullKey = isFullKey;

			MainWindowVM = mainWindowVM;

			cultureTextVMs = new ObservableCollection<CultureTextViewModel>();

			UpdateIcon();
		}

		#endregion Constructor

		#region Public properties

		public MainViewModel MainWindowVM { get; private set; }

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
				if (CheckUpdate(value, ref hasProblem, "HasProblem"))
				{
					UpdateIcon();
				}
			}
		}

		private bool isAccepted;
		/// <summary>
		/// Gets or sets a value indicating whether an existing problem with this text key is
		/// marked as accepted.
		/// </summary>
		public bool IsAccepted
		{
			get { return isAccepted; }
			set
			{
				if (CheckUpdate(value, ref isAccepted, "IsAccepted"))
				{
					UpdateIcon();
				}
			}
		}

		private bool isNamespace;
		/// <summary>
		/// Gets or sets a value indicating whether the text key represents a namespace node.
		/// </summary>
		public bool IsNamespace
		{
			get { return isNamespace; }
			set
			{
				if (CheckUpdate(value, ref isNamespace, "IsNamespace"))
				{
					UpdateIcon();
				}
			}
		}

		private bool isFullKey;
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
			set { CheckUpdate(value, ref imageSource, "ImageSource"); }
		}

		private string remarks;
		public string Remarks
		{
			get { return remarks; }
			set { CheckUpdate(value, ref remarks, "Remarks"); }
		}

		private string comment;
		public string Comment
		{
			get { return comment; }
			set
			{
				if (CheckUpdate(value, ref comment, "Comment"))
				{
					MainWindowVM.HaveComment = !string.IsNullOrWhiteSpace(comment);
					MainWindowVM.FileModified = true;
				}
			}
		}

		#endregion Public properties

		#region Overridden methods

		public override string ToString()
		{
			return "TextKeyViewModel " + textKey;
		}

		protected override void OnIsSelectedChanged()
		{
			if (IsSelected)
			{
				// Refresh the sort order of all quantified texts when selecting the key
				foreach (var ct in CultureTextVMs)
				{
					ct.RefreshQuantifiedOrder();
				}
			}
		}

		#endregion Overridden methods

		#region Public methods

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

		/// <summary>
		/// Adds something to the Remarks value. If Remarks is currently empty, the new text is
		/// set. Otherwise, a "more errors" text is added (only once).
		/// </summary>
		/// <param name="str">Remarks text to add</param>
		public void AddRemarks(string str)
		{
			if (Remarks != str)
			{
				if (string.IsNullOrEmpty(Remarks))
				{
					Remarks = str;
				}
				else if (!Remarks.EndsWith(Tx.T("validation.more")))
				{
					Remarks += Tx.T("validation.more");
				}
			}
		}

		#endregion Public methods

		#region Validation

		public bool Validate()
		{
			// NOTE: All checks are performed in their decreasing order of significance. The most-
			//       severe errors come first, proceeding to more informational problems. The first
			//       determined error generates the visible message and quits the method so that
			//       no other checks are performed or could overwrite the message.

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
			if (TextKey.StartsWith("Tx:"))
			{
				HasOwnProblem = false;
				HasProblem = anyChildError;
				Remarks = null;
				return !anyChildError;
			}

			HasOwnProblem = false;
			HasProblem = anyChildError;
			Remarks = null;

			// ----- Check for count/modulo errors -----

			// Check for invalid count values in any CultureText
			if (CultureTextVMs.Any(ct =>
				ct.QuantifiedTextVMs.Any(qt =>
					qt.Count < 0 || qt.Count >= 0xFFFF)))
			{
				HasOwnProblem = true;
				HasProblem = true;
				AddRemarks(Tx.T("validation.content.invalid count"));
			}
			// Check for invalid modulo values in any CultureText
			if (CultureTextVMs.Any(ct =>
				ct.QuantifiedTextVMs.Any(qt =>
					qt.Modulo != 0 && (qt.Modulo < 2 || qt.Modulo > 1000))))
			{
				HasOwnProblem = true;
				HasProblem = true;
				AddRemarks(Tx.T("validation.content.invalid modulo"));
			}
			// Check for duplicate count/modulo values in any CultureText
			if (CultureTextVMs.Any(ct =>
				ct.QuantifiedTextVMs
					.GroupBy(qt => qt.Count << 16 | qt.Modulo)
					.Any(grp => grp.Count() > 1)))
			{
				HasOwnProblem = true;
				HasProblem = true;
				AddRemarks(Tx.T("validation.content.duplicate count modulo"));
			}

			// ----- Check referenced keys -----

			if (CultureTextVMs.Any(ct =>
				ct.TextKeyReferences != null &&
				ct.TextKeyReferences.OfType<string>().Any(key => key == TextKey)))
			{
				HasOwnProblem = true;
				HasProblem = true;
				AddRemarks(Tx.T("validation.content.referenced key loop"));
			}

			if (CultureTextVMs.Any(ct =>
				ct.QuantifiedTextVMs.Any(qt =>
					qt.TextKeyReferences != null &&
					qt.TextKeyReferences.OfType<string>().Any(key => key == TextKey))))
			{
				HasOwnProblem = true;
				HasProblem = true;
				AddRemarks(Tx.T("validation.content.referenced key loop"));
			}

			if (CultureTextVMs.Any(ct =>
				ct.TextKeyReferences != null &&
				ct.TextKeyReferences.OfType<string>().Any(key => !MainWindowVM.TextKeys.ContainsKey(key))))
			{
				HasOwnProblem = true;
				HasProblem = true;
				AddRemarks(Tx.T("validation.content.missing referenced key"));
			}

			if (CultureTextVMs.Any(ct =>
				ct.QuantifiedTextVMs.Any(qt =>
					qt.TextKeyReferences != null &&
					qt.TextKeyReferences.OfType<string>().Any(key => !MainWindowVM.TextKeys.ContainsKey(key)))))
			{
				HasOwnProblem = true;
				HasProblem = true;
				AddRemarks(Tx.T("validation.content.missing referenced key"));
			}

			// ----- Check translations consistency -----

			IsAccepted = false;

			if (CultureTextVMs.Count > 1)
			{
				string primaryText = CultureTextVMs[0].Text;

				for (int i = 0; i < CultureTextVMs.Count; i++)
				{
					// Skip main text of primary culture, it's what we compare everything else with
					if (i > 0)
					{
						string transText = CultureTextVMs[i].Text;
						// Ignore any empty text. If that's a problem, it'll be found as missing below.
						if (!String.IsNullOrEmpty(transText))
						{
							string message;
							if (!CheckPlaceholdersConsistency(primaryText, transText, true, out message))
							{
								CultureTextVMs[i].IsPlaceholdersProblem = true;
								if (CultureTextVMs[i].AcceptPlaceholders)
								{
									IsAccepted = true;
								}
								else
								{
									HasOwnProblem = true;
									HasProblem = true;
									AddRemarks(message);
								}
							}
							else
							{
								CultureTextVMs[i].IsPlaceholdersProblem = false;
							}
							if (!CheckPunctuationConsistency(primaryText, transText, out message))
							{
								CultureTextVMs[i].IsPunctuationProblem = true;
								if (CultureTextVMs[i].AcceptPunctuation)
								{
									IsAccepted = true;
								}
								else
								{
									HasOwnProblem = true;
									HasProblem = true;
									AddRemarks(message);
								}
							}
							else
							{
								CultureTextVMs[i].IsPunctuationProblem = false;
							}
						}
						else
						{
							// If there's no text, and it hasn't been checked, there is no problem
							CultureTextVMs[i].IsPlaceholdersProblem = false;
							CultureTextVMs[i].IsPunctuationProblem = false;
						}
					}

					foreach (var qt in CultureTextVMs[i].QuantifiedTextVMs)
					{
						string transText = qt.Text;
						// Ignore any empty text for quantified texts.
						if (!String.IsNullOrEmpty(transText))
						{
							string message;
							if (!CheckPlaceholdersConsistency(primaryText, transText, false, out message))   // Ignore count placeholder here
							{
								qt.IsPlaceholdersProblem = true;
								if (qt.AcceptPlaceholders)
								{
									IsAccepted = true;
								}
								else
								{
									HasOwnProblem = true;
									HasProblem = true;
									AddRemarks(message);
								}
							}
							else
							{
								qt.IsPlaceholdersProblem = false;
							}
							if (!CheckPunctuationConsistency(primaryText, transText, out message))
							{
								qt.IsPunctuationProblem = true;
								if (qt.AcceptPunctuation)
								{
									IsAccepted = true;
								}
								else
								{
									HasOwnProblem = true;
									HasProblem = true;
									AddRemarks(message);
								}
							}
							else
							{
								qt.IsPunctuationProblem = false;
							}
						}
						else
						{
							// If there's no text, and it hasn't been checked, there is no problem
							qt.IsPlaceholdersProblem = false;
							qt.IsPunctuationProblem = false;
						}
					}
				}
			}

			// ----- Check for missing translations -----

			foreach (var ctVM in CultureTextVMs)
			{
				if (ctVM.CultureName.Length == 2)
				{
					// Check that every non-region culture has a text set
					if (String.IsNullOrEmpty(ctVM.Text))
					{
						ctVM.IsMissing = true;
						if (ctVM.AcceptMissing)
						{
							IsAccepted = true;
						}
						else
						{
							HasOwnProblem = true;
							HasProblem = true;
							AddRemarks(Tx.T("validation.content.missing translation"));
						}
					}
					else
					{
						ctVM.IsMissing = false;
					}
				}
				else if (ctVM.CultureName.Length == 5)
				{
					// Check that every region-culture with no text has a non-region culture with a
					// text set (as fallback)
					if (String.IsNullOrEmpty(ctVM.Text) &&
						!CultureTextVMs.Any(vm2 => vm2.CultureName == ctVM.CultureName.Substring(0, 2)))
					{
						ctVM.IsMissing = true;
						if (ctVM.AcceptMissing)
						{
							IsAccepted = true;
						}
						else
						{
							HasOwnProblem = true;
							HasProblem = true;
							AddRemarks(Tx.T("validation.content.missing translation"));
						}
					}
					else
					{
						ctVM.IsMissing = false;
					}
				}

				// Also check quantified texts for missing texts
				foreach (var qt in ctVM.QuantifiedTextVMs)
				{
					if (String.IsNullOrEmpty(qt.Text))
					{
						qt.IsMissing = true;
						if (qt.AcceptMissing)
						{
							IsAccepted = true;
						}
						else
						{
							HasOwnProblem = true;
							HasProblem = true;
							AddRemarks(Tx.T("validation.content.missing translation"));
						}
					}
					else
					{
						qt.IsMissing = false;
					}
				}
			}

			return !HasProblem;
		}

		#endregion Validation

		#region UI support

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
				if (HasProblem)
				{
					ImageSource = "/Images/textkey_namespace_error.png";
				}
				else
				{
					ImageSource = "/Images/textkey_namespace.png";
				}
			}
			else if (IsFullKey)
			{
				if (CultureTextVMs.Any(ct => ct.QuantifiedTextVMs.Count > 0))
				{
					if (HasProblem)
					{
						ImageSource = "/Images/key_q_error.png";
					}
					else if (IsAccepted)
					{
						ImageSource = "/Images/key_q_accepted.png";
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
					else if (IsAccepted)
					{
						ImageSource = "/Images/key_accepted.png";
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

		#endregion UI support

		#region Set key

		/// <summary>
		/// Sets a new text key value but does not update any children.
		/// </summary>
		/// <param name="newKey">New text key.</param>
		/// <param name="textKeys">Text keys dictionary to update, if not null.</param>
		public void SetKey(string newKey, Dictionary<string, TextKeyViewModel> textKeys)
		{
			int i = newKey.LastIndexOfAny(new char[] { ':', '.' });
			if (i >= 0)
				DisplayName = newKey.Substring(i + 1);
			else
				DisplayName = newKey;

			if (textKeys != null && textKeys.ContainsKey(textKey))
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
		/// <param name="textKeys">Text keys dictionary to update.</param>
		/// <returns>Number of affected keys.</returns>
		public int SetKeyRecursive(string newKey, Dictionary<string, TextKeyViewModel> textKeys)
		{
			int i = newKey.LastIndexOfAny(new char[] { ':', '.' });
			if (i >= 0)
				DisplayName = newKey.Substring(i + 1);
			else
				DisplayName = newKey;
			int affectedKeys = IsFullKey ? 1 : 0;

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
		/// <param name="textKeys">Text keys dictionary to update.</param>
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
			int affectedKeys = IsFullKey ? 1 : 0;

			foreach (TextKeyViewModel child in Children)
			{
				affectedKeys += child.ReplaceKeyRecursive(oldKey, newKey, textKeys);
			}
			return affectedKeys;
		}

		#endregion Set key

		#region Clone and merge

		/// <summary>
		/// Creates a new TextKeyViewModel instance with all contents of this instance.
		/// </summary>
		/// <returns></returns>
		public TextKeyViewModel Clone()
		{
			TextKeyViewModel clone = new TextKeyViewModel(this.textKey, this.isFullKey, this.Parent, this.MainWindowVM);
			clone.comment = this.comment;
			clone.isNamespace = this.isNamespace;
			foreach (CultureTextViewModel ctVM in CultureTextVMs)
			{
				clone.CultureTextVMs.Add(ctVM.Clone(clone));
			}
			return clone;
		}

		/// <summary>
		/// Copies all contents from another TextKeyViewModel instance to this one, merging all
		/// data, overwriting conflicts. Subitems are not copied to the new item.
		/// </summary>
		/// <param name="sourceKey"></param>
		public void MergeFrom(TextKeyViewModel sourceKey)
		{
			// Set or append comment
			if (!string.IsNullOrWhiteSpace(sourceKey.Comment))
			{
				if (!string.IsNullOrWhiteSpace(Comment))
				{
					Comment += Environment.NewLine;
				}
				Comment += sourceKey.Comment;
			}

			// Set or keep full key state
			IsFullKey |= sourceKey.IsFullKey;

			if (sourceKey.IsFullKey)
			{
				foreach (CultureTextViewModel otherctVM in sourceKey.CultureTextVMs)
				{
					var ctVM = CultureTextVMs.FirstOrDefault(c => c.CultureName == otherctVM.CultureName);
					if (ctVM == null)
					{
						// Culture doesn't exist here, just copy-add it
						CultureTextVMs.Add(otherctVM.Clone(this));
					}
					else
					{
						// Merge culture text data
						ctVM.MergeFrom(otherctVM);
					}
				}
				UpdateCultureTextSeparators();
			}
		}

		/// <summary>
		/// Copies all child keys recursively from another TextKeyViewModel instance to this one,
		/// also merging all text data, overwriting conflicts.
		/// </summary>
		/// <param name="sourceKey"></param>
		public void MergeChildrenRecursive(TextKeyViewModel sourceKey)
		{
			foreach (TextKeyViewModel sourceChild in sourceKey.Children)
			{
				var myChild = Children.FirstOrDefault(c => c.DisplayName == sourceChild.DisplayName) as TextKeyViewModel;
				if (myChild != null)
				{
					myChild.MergeFrom(sourceChild);
				}
				else
				{
					myChild = sourceChild.Clone();
					myChild.Parent = this;
					myChild.SetKey(this.textKey + "." + sourceChild.DisplayName, null);
					Children.Add(myChild);
				}
				myChild.MergeChildrenRecursive(sourceChild);
			}
		}

		#endregion Clone and merge

		#region Static methods

		/// <summary>
		/// Determines the root key of the specified text key.
		/// </summary>
		/// <param name="textKey"></param>
		/// <returns></returns>
		public static string GetRootKey(string textKey)
		{
			int index = textKey.IndexOfAny(new char[] { '.', ':' });
			if (index >= 0)
			{
				return textKey.Substring(0, index);
			}
			return textKey;
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

		public static bool CheckPlaceholdersConsistency(string a, string b, bool includeCount, out string message)
		{
			if (a == null) a = "";
			if (b == null) b = "";

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

			message = null;
			return true;
		}

		public static bool CheckPunctuationConsistency(string a, string b, out string message)
		{
			if (a == null) a = "";
			if (b == null) b = "";

			string pattern;
			Match m1, m2;

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

		#endregion Static methods
	}
}
