using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TxEditor.ViewModel
{
	class QuantifiedTextViewModel : ViewModelBase
	{
		private int count;
		public int Count
		{
			get { return count; }
		}

		private int modulo;
		public int Modulo
		{
			get { return modulo; }
		}

		private string text;
		public string Text
		{
			get { return text; }
		}
	}
}
