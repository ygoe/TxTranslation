using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Globalization;

namespace TxEditor.ViewModel
{
	class CultureTextViewModel : ViewModelBase
	{
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
				}
			}
		}

		private ObservableCollection<QuantifiedTextViewModel> quantifiedTextVMs;
		public ObservableCollection<QuantifiedTextViewModel> QuantifiedTextVMs
		{
			get { return quantifiedTextVMs; }
		}

		public CultureTextViewModel(string cultureName)
		{
			this.cultureName = cultureName;

			cultureNativeName = CultureInfo.GetCultureInfo(cultureName).NativeName;
		}
	}
}
