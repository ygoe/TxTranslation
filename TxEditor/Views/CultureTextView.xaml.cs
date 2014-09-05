using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Unclassified.TxEditor.Controls;
using Unclassified.TxEditor.ViewModels;
using Unclassified.UI;

namespace Unclassified.TxEditor.Views
{
	public partial class CultureTextView : UserControl
	{
		#region Static constructor

		static CultureTextView()
		{
			ViewCommandManager.SetupMetadata<CultureTextView>();
		}

		#endregion Static constructor

		#region Constructors

		public CultureTextView()
		{
			InitializeComponent();
		}

		#endregion Constructors

		#region Control event handlers

		private void DecoratedTextBox_ValidateKey(object sender, ValidateKeyEventArgs e)
		{
			CultureTextViewModel vm = DataContext as CultureTextViewModel;
			if (vm != null)
				e.IsValid =
					e.TextKey != vm.TextKeyVM.TextKey &&
					vm.TextKeyVM.MainWindowVM.TextKeys.ContainsKey(e.TextKey);
		}

		private void UserControl_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
		{
			bool focused = (bool) e.NewValue;
			CultureTextViewModel vm = DataContext as CultureTextViewModel;
			if (vm != null)
				vm.TextKeyVM.MainWindowVM.SelectedCulture = focused ? vm.CultureName : null;
		}

		#endregion Control event handlers

		#region View commands

		[ViewCommand]
		public void FocusText()
		{
			MyTextBox.Focus();
		}

		#endregion View commands
	}
}
