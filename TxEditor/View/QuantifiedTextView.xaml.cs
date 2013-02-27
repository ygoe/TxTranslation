using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Unclassified.UI;
using TxEditor.ViewModel;

namespace TxEditor.View
{
	public partial class QuantifiedTextView : UserControl
	{
		static QuantifiedTextView()
		{
			DataContextProperty.OverrideMetadata(
				typeof(QuantifiedTextView),
				new FrameworkPropertyMetadata(ViewCommandManager.ViewChangedHandler));
		}

		public QuantifiedTextView()
		{
			InitializeComponent();
		}

		[ViewCommand]
		public void FocusCount()
		{
			CountTextBox.Focus();
		}

		[ViewCommand]
		public void FocusText()
		{
			MyTextBox.Focus();
		}
	}
}
