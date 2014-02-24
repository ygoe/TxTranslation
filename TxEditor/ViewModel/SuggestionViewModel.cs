using System;
using System.Linq;
using System.Windows;

namespace Unclassified.TxEditor.ViewModel
{
	internal class SuggestionViewModel : ViewModelBase
	{
		public MainWindowViewModel MainWindowVM { get; private set; }

		private string textKey;
		public string TextKey
		{
			get { return textKey; }
			set
			{
				if (value != textKey)
				{
					textKey = value;
					OnPropertyChanged("TextKey");
				}
			}
		}

		private string baseText;
		public string BaseText
		{
			get { return baseText; }
			set
			{
				if (value != baseText)
				{
					baseText = value;
					OnPropertyChanged("BaseText");
				}
			}
		}

		private string translatedText;
		public string TranslatedText
		{
			get { return translatedText; }
			set
			{
				if (value != translatedText)
				{
					translatedText = value;
					OnPropertyChanged("TranslatedText");
				}
			}
		}

		private string score;
		public string Score
		{
			get { return score; }
			set
			{
				if (value != score)
				{
					score = value;
					OnPropertyChanged("Score");
				}
			}
		}

		private bool isExactMatch;
		public bool IsExactMatch
		{
			get { return isExactMatch; }
			set
			{
				if (value != isExactMatch)
				{
					isExactMatch = value;
					OnPropertyChanged("IsExactMatch");
					OnPropertyChanged("BaseWeight");
				}
			}
		}

		public FontWeight BaseWeight
		{
			get
			{
				if (isExactMatch)
				{
					return FontWeights.Bold;
				}
				else
				{
					return FontWeights.Normal;
				}
			}
		}

		private bool isDummy;
		public bool IsDummy
		{
			get { return isDummy; }
			set
			{
				if (value != isDummy)
				{
					isDummy = value;
					OnPropertyChanged("IsDummy");
				}
			}
		}

		public SuggestionViewModel(MainWindowViewModel mainWindowVM)
		{
			MainWindowVM = mainWindowVM;
		}

		public override string ToString()
		{
			return "{SuggestionViewModel}";
		}
	}
}
