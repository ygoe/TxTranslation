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
using TxEditor.ViewModel;

namespace TxEditor.View
{
	public partial class CultureTextView : UserControl
	{
		public CultureTextView()
		{
			InitializeComponent();
		}

		private void DecoratedTextBox_ValidateKey(object sender, ValidateKeyEventArgs e)
		{
			CultureTextViewModel vm = DataContext as CultureTextViewModel;
			if (vm != null)
				e.IsValid = vm.TextKeyVM.MainWindowVM.TextKeys.Contains(e.TextKey);
		}
	}
}
