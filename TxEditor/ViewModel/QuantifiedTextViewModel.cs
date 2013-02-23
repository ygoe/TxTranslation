using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TxEditor.ViewModel
{
	class QuantifiedTextViewModel : ViewModelBase
	{
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
					CultureTextVM.TextKeyVM.Validate();
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

		public QuantifiedTextViewModel(CultureTextViewModel cultureTextVM)
		{
			CultureTextVM = cultureTextVM;
		}

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
