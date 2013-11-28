using System;
using System.Windows;
using System.Windows.Controls;
using TxEditor.ViewModel;
using Unclassified.UI;

namespace TxEditor.View
{
	public partial class SelectFileWindow : Window
	{
		#region Constructor

		public SelectFileWindow()
		{
			InitializeComponent();
		}

		#endregion Constructor

		protected override void OnSourceInitialized(EventArgs e)
		{
			this.HideIcon();
			base.OnSourceInitialized(e);
		}

		#region Control event handlers

		private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			OKButton.IsEnabled = FileList.SelectedItems.Count > 0;
		}

		private void OKButton_Click(object sender, RoutedEventArgs e)
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

		private void CancelButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = false;
			Close();
		}

		#endregion Control event handlers
	}
}
