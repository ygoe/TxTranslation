using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Media;

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
					if (hasProblem)
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
					OnPropertyChanged("RemarksVisibility");
				}
			}
		}

		public Visibility RemarksVisibility
		{
			get { return !string.IsNullOrEmpty(Remarks) ? Visibility.Visible : Visibility.Collapsed; }
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

		public void Validate()
		{
			if (!IsFullKey)
			{
				HasProblem = false;
				Remarks = null;
				return;
			}
			if (TextKey.StartsWith("Tx:"))
			{
				HasProblem = false;
				Remarks = null;
				return;
			}
			var allLanguages = new HashSet<string>(CultureTextVMs.Select(vm => vm.CultureName.Substring(0, 2)).Distinct());
			var setLanguages = new HashSet<string>(CultureTextVMs.Where(vm => !string.IsNullOrEmpty(vm.Text)).Select(vm => vm.CultureName.Substring(0, 2)).Distinct());
			allLanguages.ExceptWith(setLanguages);
			if (allLanguages.Count > 0)
			{
				HasProblem = true;
				Remarks = "Missing translations";
			}
			else
			{
				HasProblem = false;
				Remarks = null;
			}
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
				ImageSource = "/Images/textkey_segment.png";
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

			// Tx:* always comes first
			if (ta.TextKey.StartsWith("Tx:") ^ tb.TextKey.StartsWith("Tx:"))
			{
				if (ta.TextKey.StartsWith("Tx:")) return -1;
				if (tb.TextKey.StartsWith("Tx:")) return 1;
			}
			return string.Compare(ta.TextKey, tb.TextKey, StringComparison.InvariantCultureIgnoreCase);
		}
	}
}
