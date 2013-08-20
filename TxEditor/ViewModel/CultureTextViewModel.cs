using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Globalization;
using TxLib;
using System.Windows.Media;
using Unclassified.UI;
using System.Collections.Specialized;

namespace TxEditor.ViewModel
{
	class CultureTextViewModel : ViewModelBase, IViewCommandSource
	{
		private ViewCommandManager viewCommandManager = new ViewCommandManager();
		public ViewCommandManager ViewCommandManager { get { return viewCommandManager; } }

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

		private string text;
		public string Text
		{
			get { return text; }
			set
			{
				if (value != text)
				{
					text = value;
					OnPropertyChanged("Text");
					TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
					TextKeyVM.MainWindowVM.FileModified = true;
				}
			}
		}

		public string CursorChar
		{
			get { return null; }
			set
			{
				TextKeyVM.MainWindowVM.CursorChar = value;
			}
		}

		private StringCollection textKeyReferences;
		public StringCollection TextKeyReferences
		{
			get { return textKeyReferences; }
			set
			{
				if (value != textKeyReferences)
				{
					textKeyReferences = value;
					OnPropertyChanged("TextKeyReferences");
					TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
				}
			}
		}

		private bool lastOfLanguage;
		public bool LastOfLanguage
		{
			get { return lastOfLanguage; }
			set
			{
				if (value != lastOfLanguage)
				{
					lastOfLanguage = value;
					OnPropertyChanged("LastOfLanguage");
					if (lastOfLanguage)
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

		private Brush separatorBrush;
		public Brush SeparatorBrush
		{
			get { return separatorBrush; }
			set
			{
				if (value != separatorBrush)
				{
					separatorBrush = value;
					OnPropertyChanged("SeparatorBrush");
				}
			}
		}

		private Brush backgroundBrush;
		public Brush BackgroundBrush
		{
			get { return backgroundBrush; }
			set
			{
				if (value != backgroundBrush)
				{
					backgroundBrush = value;
					OnPropertyChanged("BackgroundBrush");
				}
			}
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

		private void quantifiedTextVMs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			TextKeyVM.UpdateIcon();
		}

		public CultureTextViewModel(string cultureName, TextKeyViewModel textKeyVM)
		{
			this.cultureName = cultureName;

			TextKeyVM = textKeyVM;
			LastOfLanguage = true;   // Change value once to set the brush value

			BackgroundBrush = cultureName == TextKeyVM.MainWindowVM.PrimaryCulture ?
				new SolidColorBrush(Color.FromArgb(20, 0, 192, 0)) :
				new SolidColorBrush(Color.FromArgb(20, 0, 192, 0));

			if (App.Settings.NativeCultureNames)
				cultureNativeName = Tx.U(CultureInfo.GetCultureInfo(cultureName).NativeName);
			else
				cultureNativeName = Tx.U(CultureInfo.GetCultureInfo(cultureName).DisplayName);
		}

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

		#region Commands

		private DelegateCommand addCount0Command;
		public DelegateCommand AddCount0Command
		{
			get
			{
				if (addCount0Command == null)
				{
					addCount0Command = new DelegateCommand(OnAddCount0);
				}
				return addCount0Command;
			}
		}

		private DelegateCommand addCount1Command;
		public DelegateCommand AddCount1Command
		{
			get
			{
				if (addCount1Command == null)
				{
					addCount1Command = new DelegateCommand(OnAddCount1);
				}
				return addCount1Command;
			}
		}

		private DelegateCommand addCommand;
		public DelegateCommand AddCommand
		{
			get
			{
				if (addCommand == null)
				{
					addCommand = new DelegateCommand(OnAdd);
				}
				return addCommand;
			}
		}

		private DelegateCommand refreshCommand;
		public DelegateCommand RefreshCommand
		{
			get
			{
				if (refreshCommand == null)
				{
					refreshCommand = new DelegateCommand(OnRefresh);
				}
				return refreshCommand;
			}
		}

		#endregion Commands

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
			var arr = QuantifiedTextVMs.ToArray();
			QuantifiedTextVMs.Clear();
			foreach (var item in arr)
			{
				QuantifiedTextVMs.InsertSorted(item, (a, b) => QuantifiedTextViewModel.Compare(a, b));
			}
		}

		#endregion Command handlers

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
	}
}
