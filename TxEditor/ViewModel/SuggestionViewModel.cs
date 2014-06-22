using System;
using System.Linq;
using System.Windows;

namespace Unclassified.TxEditor.ViewModel
{
	internal class SuggestionViewModel : ViewModelBase
	{
		public MainViewModel MainWindowVM { get; private set; }

		private string textKey;
		public string TextKey
		{
			get { return textKey; }
			set { CheckUpdate(value, ref textKey, "TextKey"); }
		}

		private string baseText;
		public string BaseText
		{
			get { return baseText; }
			set { CheckUpdate(value, ref baseText, "BaseText"); }
		}

		private string translatedText;
		public string TranslatedText
		{
			get { return translatedText; }
			set { CheckUpdate(value, ref translatedText, "TranslatedText"); }
		}

		private string score;
		public string Score
		{
			get { return score; }
			set { CheckUpdate(value, ref score, "Score"); }
		}

		private bool isExactMatch;
		public bool IsExactMatch
		{
			get { return isExactMatch; }
			set { CheckUpdate(value, ref isExactMatch, "IsExactMatch", "BaseWeight"); }
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
			set { CheckUpdate(value, ref isDummy, "IsDummy"); }
		}

		public SuggestionViewModel(MainViewModel mainWindowVM)
		{
			MainWindowVM = mainWindowVM;
		}

		public override string ToString()
		{
			return "SuggestionViewModel";
		}
	}
}
