using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using Unclassified.UI;

namespace TxEditor.ViewModel
{
	class QuantifiedTextViewModel : ViewModelBase, IViewCommandSource
	{
		private ViewCommandManager viewCommandManager = new ViewCommandManager();
		public ViewCommandManager ViewCommandManager { get { return viewCommandManager; } }

		public CultureTextViewModel CultureTextVM { get; private set; }

		private int count;
		public int Count
		{
			get { return count; }
			set
			{
				if (value != count)
				{
					count = value;
					OnPropertyChanged("Count");
					CultureTextVM.TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
					CultureTextVM.TextKeyVM.MainWindowVM.FileModified = true;
				}
			}
		}

		private int modulo;
		public int Modulo
		{
			get { return modulo; }
			set
			{
				if (value != modulo)
				{
					modulo = value;
					OnPropertyChanged("Modulo");
					CultureTextVM.TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
					CultureTextVM.TextKeyVM.MainWindowVM.FileModified = true;
				}
			}
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
					CultureTextVM.TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
					CultureTextVM.TextKeyVM.MainWindowVM.FileModified = true;
				}
			}
		}

		public string CursorChar
		{
			get { return null; }
			set
			{
				CultureTextVM.TextKeyVM.MainWindowVM.CursorChar = value;
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
					CultureTextVM.TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
				}
			}
		}

		public QuantifiedTextViewModel(CultureTextViewModel cultureTextVM)
		{
			CultureTextVM = cultureTextVM;
		}

		public bool IsEmpty()
		{
			return string.IsNullOrEmpty(Text);
		}

		#region Commands

		private DelegateCommand deleteCommand;
		public DelegateCommand DeleteCommand
		{
			get
			{
				if (deleteCommand == null)
				{
					deleteCommand = new DelegateCommand(OnDelete);
				}
				return deleteCommand;
			}
		}

		#endregion Commands

		#region Command handlers

		private void OnDelete()
		{
			CultureTextVM.QuantifiedTextVMs.Remove(this);
			CultureTextVM.TextKeyVM.MainWindowVM.ValidateTextKeysDelayed();
			CultureTextVM.TextKeyVM.MainWindowVM.FileModified = true;
		}

		#endregion Command handlers

		public static int Compare(object a, object b)
		{
			QuantifiedTextViewModel qa = a as QuantifiedTextViewModel;
			QuantifiedTextViewModel qb = b as QuantifiedTextViewModel;
			if (qa == null || qb == null) return 0;

			int cmp;
			cmp = qa.Count - qb.Count;
			if (cmp != 0) return cmp;

			cmp = qa.Modulo - qb.Modulo;
			return cmp;
		}
	}
}
