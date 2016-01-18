using System;
using System.Linq;
using System.Reflection;
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

		[Obfuscation(Exclude = true, Feature = "renaming")]
		private void DecoratedTextBox_ValidateKey(object sender, ValidateKeyEventArgs args)
		{
			CultureTextViewModel vm = DataContext as CultureTextViewModel;
			if (vm != null)
				args.IsValid =
					args.TextKey != vm.TextKeyVM.TextKey &&
					vm.TextKeyVM.MainWindowVM.TextKeys.ContainsKey(args.TextKey);
		}

		private void UserControl_IsKeyboardFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs args)
		{
			bool focused = (bool)args.NewValue;
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
