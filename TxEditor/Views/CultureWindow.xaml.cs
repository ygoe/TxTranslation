using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Xml;
using Unclassified.TxLib;
using Unclassified.UI;

namespace Unclassified.TxEditor.Views
{
	public partial class CultureWindow : Window
	{
		private bool updating;
		private XmlDocument templateXmlDoc;

		public CultureWindow()
		{
			InitializeComponent();
			this.HideIcon();

			Stream templateStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Unclassified.TxEditor.Template.txd");
			if (templateStream != null)
			{
				templateXmlDoc = new XmlDocument();
				templateXmlDoc.Load(templateStream);
			}

			InsertSystemKeysCheckBox.IsEnabled = false;
			AddButton.IsEnabled = false;
			LoadCultures();
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
				UpdateSystemKeysCheckBox(selci.CultureInfo.Name);
			}
			else
			{
				UpdateSystemKeysCheckBox(null);
			}
		}

		private void CulturesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (updating) return;

			updating = true;
			if (CulturesList.SelectedItem is CultureItem)
			{
				int selStart = CodeText.SelectionStart;   // Keep cursor position when typing
				CodeText.Text = ((CultureItem) CulturesList.SelectedItem).CultureInfo.IetfLanguageTag;
				CodeText.SelectionStart = selStart;   // Out of range value is okay
				AddButton.IsEnabled = true;
				UpdateSystemKeysCheckBox(CodeText.Text);
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

		private void UpdateSystemKeysCheckBox(string culture)
		{
			bool wasEnabled = InsertSystemKeysCheckBox.IsEnabled;
			InsertSystemKeysCheckBox.IsEnabled =
				culture != null &&
				templateXmlDoc != null &&
				templateXmlDoc.DocumentElement.SelectSingleNode("culture[@name='" + culture + "']") is XmlElement;

			if (InsertSystemKeysCheckBox.IsEnabled && !wasEnabled)
			{
				// Pre-check this as soon as it becomes available
				InsertSystemKeysCheckBox.IsChecked = true;
			}
			if (!InsertSystemKeysCheckBox.IsEnabled)
			{
				InsertSystemKeysCheckBox.IsChecked = false;
			}
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
				return Tx.U(App.Settings.View.NativeCultureNames ? CultureInfo.NativeName : CultureInfo.DisplayName) +
					" [" + CultureInfo.IetfLanguageTag + "]";
			}

			public int CompareTo(object obj)
			{
				CultureItem other = (CultureItem) obj;
				if (App.Settings.View.NativeCultureNames)
					return CultureInfo.NativeName.CompareTo(other.CultureInfo.NativeName);
				else
					return CultureInfo.DisplayName.CompareTo(other.CultureInfo.DisplayName);
			}
		}
	}
}
