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
using Unclassified.TxEditor.ViewModel;

namespace Unclassified.TxEditor.View
{
	public partial class QuantifiedTextView : UserControl
	{
		static QuantifiedTextView()
		{
			ViewCommandManager.SetupMetadata<QuantifiedTextView>();
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

		private void MyTextBox_ValidateKey(object sender, ValidateKeyEventArgs e)
		{
			QuantifiedTextViewModel vm = DataContext as QuantifiedTextViewModel;
			if (vm != null)
				e.IsValid =
					e.TextKey != vm.CultureTextVM.TextKeyVM.TextKey &&
					vm.CultureTextVM.TextKeyVM.MainWindowVM.TextKeys.ContainsKey(e.TextKey);
		}
	}
}
