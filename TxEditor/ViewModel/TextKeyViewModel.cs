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

		bool isLeafNode;
		public bool IsLeafNode
		{
			get { return isLeafNode; }
			set
			{
				if (value != isLeafNode)
				{
					isLeafNode = value;
					OnPropertyChanged("IsLeafNode");
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

		private Visibility remarksVisibility = Visibility.Collapsed;
		public Visibility RemarksVisibility
		{
			get { return remarksVisibility; }
			set
			{
				if (value != remarksVisibility)
				{
					remarksVisibility = value;
					OnPropertyChanged("RemarksVisibility");
				}
			}
		}

		public TextKeyViewModel(string textKey, bool isLeafNode, TreeViewItemViewModel parent, MainWindowViewModel mainWindowVM)
			: base(parent, false)
		{
			this.textKey = textKey;
			this.isLeafNode = isLeafNode;

			MainWindowVM = mainWindowVM;

			cultureTextVMs = new ObservableCollection<CultureTextViewModel>();
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
