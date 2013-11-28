using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using TxLib;
using Unclassified.UI;

namespace TxEditor.View
{
	public partial class CultureWindow : Window
	{
		private bool updating;

		public CultureWindow()
		{
			InitializeComponent();

			AddButton.IsEnabled = false;
			LoadCultures();
		}

		protected override void OnSourceInitialized(EventArgs e)
		{
			this.HideIcon();
			base.OnSourceInitialized(e);
		}

		private void Window_Loaded(object sender, RoutedEventArgs e)
		{
			CodeText.Focus();
		}

		private void CodeText_TextChanged(object sender, TextChangedEventArgs e)
		{
			if (updating) return;

			LoadCultures();

			CultureItem selci = CulturesList.Items.OfType<CultureItem>()
				.FirstOrDefault(ci => ci.CultureInfo.IetfLanguageTag.Equals(CodeText.Text, StringComparison.InvariantCultureIgnoreCase));

			AddButton.IsEnabled = selci != null;
			if (selci != null)
			{
				CulturesList.SelectedItem = selci;
				CulturesList.ScrollIntoView(selci);
			}
		}

		private void CulturesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (updating) return;

			updating = true;
			if (CulturesList.SelectedItem is CultureItem)
			{
				CodeText.Text = ((CultureItem) CulturesList.SelectedItem).CultureInfo.IetfLanguageTag;
				AddButton.IsEnabled = true;
			}
			else
			{
				AddButton.IsEnabled = false;
			}
			updating = false;
		}

		private void CulturesList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (CulturesList.SelectedIndex != -1)
			{
				DialogResult = true;
				Close();
			}
		}

		private void AddButton_Click(object sender, RoutedEventArgs e)
		{
			DialogResult = true;
			Close();
		}

		private void LoadCultures()
		{
			List<CultureItem> list = new List<CultureItem>();
			foreach (CultureInfo ci in CultureInfo.GetCultures(CultureTypes.AllCultures))
			{
				if (Regex.IsMatch(ci.IetfLanguageTag, @"^[a-z]{2}(-[A-Z]{2})?$"))
				{
					if (string.IsNullOrWhiteSpace(CodeText.Text) ||
						ci.IetfLanguageTag.ToLowerInvariant().Contains(CodeText.Text.ToLowerInvariant()))
					{
						list.Add(new CultureItem(ci));
					}
				}
			}
			list.Sort();

			string selectedCode = null;
			if (CulturesList.SelectedItem is CultureItem)
				selectedCode = ((CultureItem) CulturesList.SelectedItem).CultureInfo.IetfLanguageTag;

			updating = true;
			CulturesList.Items.Clear();
			foreach (CultureItem ci in list)
			{
				CulturesList.Items.Add(ci);
				if (ci.CultureInfo.IetfLanguageTag == selectedCode)
				{
					CulturesList.SelectedItem = ci;
				}
			}
			if (CulturesList.SelectedIndex >= 0)
			{
				CulturesList.ScrollIntoView(CulturesList.SelectedItem);
			}
			updating = false;
		}

		private class CultureItem : IComparable
		{
			public CultureInfo CultureInfo;

			public CultureItem(CultureInfo ci)
			{
				CultureInfo = ci;
			}

			public override string ToString()
			{
				return Tx.U(App.Settings.NativeCultureNames ? CultureInfo.NativeName : CultureInfo.DisplayName) +
					" [" + CultureInfo.IetfLanguageTag + "]";
			}

			public int CompareTo(object obj)
			{
				CultureItem other = (CultureItem) obj;
				if (App.Settings.NativeCultureNames)
					return CultureInfo.NativeName.CompareTo(other.CultureInfo.NativeName);
				else
					return CultureInfo.DisplayName.CompareTo(other.CultureInfo.DisplayName);
			}
		}
	}
}
