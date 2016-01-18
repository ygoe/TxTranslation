using System;
using System.Windows;
using System.Windows.Controls;
using Unclassified.TxEditor.ViewModels;
using Unclassified.UI;

namespace Unclassified.TxEditor.Views
{
	public partial class SelectFileWindow : Window
	{
		#region Constructor

		public SelectFileWindow()
		{
			InitializeComponent();
			this.HideIcon();
		}

		#endregion Constructor

		#region Control event handlers

		private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
		{
			OKButton.IsEnabled = FileList.SelectedItems.Count > 0;
		}

		private void OKButton_Click(object sender, RoutedEventArgs args)
		{
			SelectFileViewModel vm = DataContext as SelectFileViewModel;
			string[] files = new string[FileList.SelectedItems.Count];
			for (int i = 0; i < FileList.SelectedItems.Count; i++)
			{
				files[i] = System.IO.Path.Combine(vm.BaseDir, FileList.SelectedItems[i] as string);
			}
			vm.SelectedFileNames = files;

			DialogResult = true;
			Close();
		}

		private void AllButton_Click(object sender, RoutedEventArgs args)
		{
			SelectFileViewModel vm = DataContext as SelectFileViewModel;
			string[] files = new string[FileList.Items.Count];
			for (int i = 0; i < FileList.Items.Count; i++)
			{
				files[i] = System.IO.Path.Combine(vm.BaseDir, FileList.Items[i] as string);
			}
			vm.SelectedFileNames = files;

			DialogResult = true;
			Close();
		}

		private void CancelButton_Click(object sender, RoutedEventArgs args)
		{
			DialogResult = false;
			Close();
		}

		#endregion Control event handlers
	}
}
