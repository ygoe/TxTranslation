using System;
using System.Linq;
using System.Windows;
using Unclassified.UI;

namespace Unclassified.TxEditor.ViewModels
{
	internal class SuggestionViewModel : ViewModelBase
	{
		public MainViewModel MainWindowVM { get; private set; }

		public string TextKey
		{
			get { return GetValue<string>("TextKey"); }
			set { SetValue(value, "TextKey"); }
		}

		public string BaseText
		{
			get { return GetValue<string>("BaseText"); }
			set { SetValue(value, "BaseText"); }
		}

		public string TranslatedText
		{
			get { return GetValue<string>("TranslatedText"); }
			set { SetValue(value, "TranslatedText"); }
		}

		public string Score
		{
			get { return GetValue<string>("Score"); }
			set { SetValue(value, "Score"); }
		}

		public float ScoreNum
		{
			get { return GetValue<float>("ScoreNum"); }
			set { SetValue(value, "ScoreNum"); }
		}

		public bool IsExactMatch
		{
			get { return GetValue<bool>("IsExactMatch"); }
			set { SetValue(BooleanBoxes.Box(value), "IsExactMatch"); }
		}

		[NotifiesOn("IsExactMatch")]
		public FontWeight BaseWeight
		{
			get
			{
				if (IsExactMatch)
				{
					return FontWeights.Bold;
				}
				else
				{
					return FontWeights.Normal;
				}
			}
		}

		public bool IsDummy
		{
			get { return GetValue<bool>("IsDummy"); }
			set { SetValue(BooleanBoxes.Box(value), "IsDummy"); }
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
