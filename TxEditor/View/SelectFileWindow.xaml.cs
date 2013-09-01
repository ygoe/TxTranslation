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
using System.Windows.Shapes;
using TxEditor.ViewModel;

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
