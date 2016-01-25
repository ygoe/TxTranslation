using System;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Unclassified.TxEditor.Controls;
using Unclassified.TxEditor.ViewModels;
using Unclassified.UI;

namespace Unclassified.TxEditor.Views
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

		[Obfuscation(Exclude = true, Feature = "renaming")]
		private void MyTextBox_ValidateKey(object sender, ValidateKeyEventArgs args)
		{
			QuantifiedTextViewModel vm = DataContext as QuantifiedTextViewModel;
			if (vm != null)
				args.IsValid =
					args.TextKey != vm.CultureTextVM.TextKeyVM.TextKey &&
					vm.CultureTextVM.TextKeyVM.MainWindowVM.TextKeys.ContainsKey(args.TextKey);
		}

		private void CountTextBox_PreviewTextInput(object sender, TextCompositionEventArgs args)
		{
			int i;
			args.Handled = !int.TryParse(args.Text, out i) || i < 0;
		}

		private void CountTextBox_Pasting(object sender, DataObjectPastingEventArgs args)
		{
			if (args.DataObject.GetDataPresent(typeof(string)))
			{
				string text = (string)args.DataObject.GetData(typeof(string));
				int i;
				if (!int.TryParse(text, out i) || i < 0)
				{
					args.CancelCommand();
				}
			}
			else
			{
				args.CancelCommand();
			}
		}
	}
}
