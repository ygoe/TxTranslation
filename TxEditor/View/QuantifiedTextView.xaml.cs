using System;
using System.Linq;
using System.Windows.Controls;
using Unclassified.TxEditor.ViewModel;
using Unclassified.UI;

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
