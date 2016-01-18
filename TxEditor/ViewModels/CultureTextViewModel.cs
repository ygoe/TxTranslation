using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Windows.Media;
using Unclassified.TxLib;
using Unclassified.UI;
using Unclassified.Util;

namespace Unclassified.TxEditor.ViewModels
{
	internal class CultureTextViewModel : ViewModelBase, IViewCommandSource
	{
		#region Constructor

		public CultureTextViewModel(string cultureName, TextKeyViewModel textKeyVM)
		{
			this.cultureName = cultureName;

			TextKeyVM = textKeyVM;
			LastOfLanguage = true;   // Change value once to set the brush value

			BackgroundBrush = cultureName == TextKeyVM.MainWindowVM.PrimaryCulture ?
				new SolidColorBrush(Color.FromArgb(20, 0, 192, 0)) :
				new SolidColorBrush(Color.FromArgb(20, 0, 192, 0));

			if (App.Settings.View.NativeCultureNames)
				cultureNativeName = Tx.U(CultureInfo.GetCultureInfo(cultureName).NativeName);
			else
				cultureNativeName = Tx.U(CultureInfo.GetCultureInfo(cultureName).DisplayName);
		}

		#endregion Constructor

		#region Public properties

		public TextKeyViewModel TextKeyVM { get; private set; }

		private string cultureName;
		public string CultureName
		{
			get { return cultureName; }
		}

		private string cultureNativeName;
		public string CultureNativeName
		{
			get { return cultureNativeName; }
		}

		public string Text
		{
			get
			{
				return GetValue<string>("Text");
			}
			set
			{
				if (SetValue(value, "Text"))
				{
					TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
					TextKeyVM.MainWindowVM.FileModified = true;
				}
			}
		}

		public bool IsMissing
		{
			get { return GetValue<bool>("IsMissing"); }
			set { SetValue(BooleanBoxes.Box(value), "IsMissing"); }
		}

		public bool IsPlaceholdersProblem
		{
			get { return GetValue<bool>("IsPlaceholdersProblem"); }
			set { SetValue(BooleanBoxes.Box(value), "IsPlaceholdersProblem"); }
		}

		public bool IsPunctuationProblem
		{
			get { return GetValue<bool>("IsPunctuationProblem"); }
			set { SetValue(BooleanBoxes.Box(value), "IsPunctuationProblem"); }
		}

		public bool AcceptMissing
		{
			get
			{
				return GetValue<bool>("AcceptMissing");
			}
			set
			{
				if (SetValue(BooleanBoxes.Box(value), "AcceptMissing"))
				{
					TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
					TextKeyVM.MainWindowVM.FileModified = true;
				}
			}
		}

		public bool AcceptPlaceholders
		{
			get
			{
				return GetValue<bool>("AcceptPlaceholders");
			}
			set
			{
				if (SetValue(BooleanBoxes.Box(value), "AcceptPlaceholders"))
				{
					TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
					TextKeyVM.MainWindowVM.FileModified = true;
				}
			}
		}

		public bool AcceptPunctuation
		{
			get
			{
				return GetValue<bool>("AcceptPunctuation");
			}
			set
			{
				if (SetValue(BooleanBoxes.Box(value), "AcceptPunctuation"))
				{
					TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
					TextKeyVM.MainWindowVM.FileModified = true;
				}
			}
		}

		public string CursorChar
		{
			get { return null; }
			set { TextKeyVM.MainWindowVM.CursorChar = value; }
		}

		public StringCollection TextKeyReferences
		{
			get
			{
				return GetValue<StringCollection>("TextKeyReferences");
			}
			set
			{
				if (SetValue(value, "TextKeyReferences"))
				{
					TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
				}
			}
		}

		public bool LastOfLanguage
		{
			get
			{
				return GetValue<bool>("LastOfLanguage");
			}
			set
			{
				if (SetValue(BooleanBoxes.Box(value), "LastOfLanguage"))
				{
					if (value)
					{
						SeparatorBrush = new SolidColorBrush(Color.FromArgb(0x40, 0, 0, 0));
					}
					else
					{
						SeparatorBrush = new SolidColorBrush(Color.FromArgb(0x18, 0, 0, 0));
					}
				}
			}
		}

		public Brush SeparatorBrush
		{
			get { return GetValue<Brush>("SeparatorBrush"); }
			set { SetValue(value, "SeparatorBrush"); }
		}

		public Brush BackgroundBrush
		{
			get { return GetValue<Brush>("BackgroundBrush"); }
			set { SetValue(value, "BackgroundBrush"); }
		}

		private ObservableCollection<QuantifiedTextViewModel> quantifiedTextVMs;
		public ObservableCollection<QuantifiedTextViewModel> QuantifiedTextVMs
		{
			get
			{
				if (quantifiedTextVMs == null)
				{
					quantifiedTextVMs = new ObservableCollection<QuantifiedTextViewModel>();
					quantifiedTextVMs.CollectionChanged += quantifiedTextVMs_CollectionChanged;
				}
				return quantifiedTextVMs;
			}
		}

		#endregion Public properties

		#region Event handlers

		private void quantifiedTextVMs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
		{
			TextKeyVM.UpdateIcon();
		}

		#endregion Event handlers

		#region Commands

		#region Definition and initialisation

		public DelegateCommand AddCount0Command { get; private set; }
		public DelegateCommand AddCount1Command { get; private set; }
		public DelegateCommand AddCommand { get; private set; }
		public DelegateCommand RefreshCommand { get; private set; }
		public DelegateCommand ToggleAcceptMissingCommand { get; private set; }
		public DelegateCommand ToggleAcceptPlaceholdersCommand { get; private set; }
		public DelegateCommand ToggleAcceptPunctuationCommand { get; private set; }

		protected override void InitializeCommands()
		{
			AddCount0Command = new DelegateCommand(OnAddCount0);
			AddCount1Command = new DelegateCommand(OnAddCount1);
			AddCommand = new DelegateCommand(OnAdd);
			RefreshCommand = new DelegateCommand(OnRefresh);
			ToggleAcceptMissingCommand = new DelegateCommand(() => { AcceptMissing = !AcceptMissing; });
			ToggleAcceptPlaceholdersCommand = new DelegateCommand(() => { AcceptPlaceholders = !AcceptPlaceholders; });
			ToggleAcceptPunctuationCommand = new DelegateCommand(() => { AcceptPunctuation = !AcceptPunctuation; });
		}

		#endregion Definition and initialisation

		#region Command handlers

		private void OnAddCount0()
		{
			QuantifiedTextViewModel newVM = new QuantifiedTextViewModel(this);
			newVM.Count = 0;
			QuantifiedTextVMs.Add(newVM);
			TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
			TextKeyVM.MainWindowVM.FileModified = true;
			newVM.ViewCommandManager.InvokeLoaded("FocusText");
		}

		private void OnAddCount1()
		{
			QuantifiedTextViewModel newVM = new QuantifiedTextViewModel(this);
			newVM.Count = 1;
			QuantifiedTextVMs.Add(newVM);
			TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
			TextKeyVM.MainWindowVM.FileModified = true;
			newVM.ViewCommandManager.InvokeLoaded("FocusText");
		}

		private void OnAdd()
		{
			QuantifiedTextViewModel newVM = new QuantifiedTextViewModel(this);
			newVM.Count = -1;
			QuantifiedTextVMs.Add(newVM);
			TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
			TextKeyVM.MainWindowVM.FileModified = true;
			newVM.ViewCommandManager.InvokeLoaded("FocusCount");
		}

		private void OnRefresh()
		{
			RefreshQuantifiedOrder();
			ViewCommandManager.InvokeLoaded("FocusText");
		}

		public void RefreshQuantifiedOrder()
		{
			var arr = QuantifiedTextVMs.ToArray();
			QuantifiedTextVMs.Clear();
			foreach (var item in arr)
			{
				QuantifiedTextVMs.InsertSorted(item, (a, b) => QuantifiedTextViewModel.Compare(a, b));
			}
		}

		#endregion Command handlers

		#endregion Commands

		#region Public methods

		/// <summary>
		/// Returns a value indicating whether any data was entered for this text key culture.
		/// </summary>
		/// <returns></returns>
		public bool IsEmpty()
		{
			if (!string.IsNullOrEmpty(Text)) return false;
			foreach (var qt in QuantifiedTextVMs)
			{
				if (!qt.IsEmpty()) return false;
			}
			return true;
		}

		/// <summary>
		/// Creates a new CultureTextViewModel instance with all contents of this instance.
		/// </summary>
		/// <param name="textKeyVM">New TextKeyViewModel instance to connect the clone with.</param>
		/// <returns></returns>
		public CultureTextViewModel Clone(TextKeyViewModel textKeyVM)
		{
			CultureTextViewModel clone = new CultureTextViewModel(CultureName, textKeyVM);
			clone.Text = Text;
			foreach (QuantifiedTextViewModel qtVM in QuantifiedTextVMs)
			{
				clone.QuantifiedTextVMs.Add(qtVM.Clone(clone));
			}
			return clone;
		}

		/// <summary>
		/// Copies all contents from another CultureTextViewModel instance to this one, merging all
		/// data.
		/// </summary>
		/// <param name="other"></param>
		public void MergeFrom(CultureTextViewModel other)
		{
			if (!string.IsNullOrEmpty(other.Text))
			{
				// Overwrite text AND quantified texts IF set
				this.Text = other.Text;

				this.QuantifiedTextVMs.Clear();
				foreach (QuantifiedTextViewModel qtVM in this.QuantifiedTextVMs)
				{
					this.QuantifiedTextVMs.Add(qtVM.Clone(this));
				}
			}
		}

		/// <summary>
		/// Compares this CultureTextViewModel instance with another instance to determine the sort
		/// order in the editor view.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public int CompareTo(CultureTextViewModel other)
		{
			string otherName = other.CultureName;
			if (string.Compare(CultureName, otherName, StringComparison.InvariantCultureIgnoreCase) == 0)
			{
				//System.Diagnostics.Debug.WriteLine(CultureName + " = " + otherName + " (1)");
				return 0;   // Exact match
			}

			if (CultureName.Length >= 2 && otherName.Length >= 2)
			{
				// Prefer primary culture (with or without region; if set)
				if (!string.IsNullOrEmpty(TextKeyVM.MainWindowVM.PrimaryCulture))
				{
					// tP:  thisPrimary
					// oP:  otherPrimary
					// oPR: otherPrimaryRelated
					//
					//             !tPR         tPR
					//             !tP    tP    !tP   tP
					//           --------------------------
					// !oPR  !oP | cont.  xxx | -1    -1  |
					//       oP  |  xxx   xxx | xxx   xxx |
					//           --------------------------
					// oPR   !oP |   1    xxx | cont. -1  |
					//       oP  |   1    xxx |  1    xxx |
					//           --------------------------

					bool thisPrimary = string.Compare(CultureName, TextKeyVM.MainWindowVM.PrimaryCulture, StringComparison.InvariantCultureIgnoreCase) == 0;
					bool thisPrimaryRelated = CultureName.StartsWith(TextKeyVM.MainWindowVM.PrimaryCulture.Substring(0, 2));
					bool otherPrimary = string.Compare(otherName, TextKeyVM.MainWindowVM.PrimaryCulture, StringComparison.InvariantCultureIgnoreCase) == 0;
					bool otherPrimaryRelated = otherName.StartsWith(TextKeyVM.MainWindowVM.PrimaryCulture.Substring(0, 2));

					if (thisPrimary || thisPrimaryRelated && !otherPrimaryRelated)
					{
						//System.Diagnostics.Debug.WriteLine(CultureName + " < " + otherName + " (2)");
						return -1;
					}
					if (otherPrimary || otherPrimaryRelated && !thisPrimaryRelated)
					{
						//System.Diagnostics.Debug.WriteLine(CultureName + " > " + otherName + " (2)");
						return 1;
					}
				}

				if (string.Compare(CultureName.Substring(0, 2), otherName.Substring(0, 2), StringComparison.InvariantCultureIgnoreCase) == 0)
				{
					// Same language, prefer shorter names (without region)
					if (CultureName.Length != otherName.Length)
					{
						int i = CultureName.Length - otherName.Length;
						//if (i < 0)
						//    System.Diagnostics.Debug.WriteLine(CultureName + " < " + otherName + " (3)");
						//else if (i > 0)
						//    System.Diagnostics.Debug.WriteLine(CultureName + " > " + otherName + " (3)");
						//else
						//    System.Diagnostics.Debug.WriteLine(CultureName + " = " + otherName + " (3)");
						return i;
						// If this.length < other.length, then the result is negative and this comes before other
					}
				}
			}
			return string.Compare(CultureName, otherName, StringComparison.InvariantCultureIgnoreCase);
		}

		#endregion Public methods

		#region IViewCommandSource members

		private ViewCommandManager viewCommandManager = new ViewCommandManager();
		public ViewCommandManager ViewCommandManager { get { return viewCommandManager; } }

		#endregion IViewCommandSource members
	}
}
