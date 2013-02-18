using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace TxEditor.ViewModel
{
	class TextKeyViewModel : TreeViewItemViewModel
	{
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

		public string ImageSource { get; protected set; }

		public TextKeyViewModel(string textKey, bool isLeafNode, TreeViewItemViewModel parent)
			: base(parent, false)
		{
			this.textKey = textKey;
			this.isLeafNode = isLeafNode;

			cultureTextVMs = new ObservableCollection<CultureTextViewModel>();
		}
	}
}
