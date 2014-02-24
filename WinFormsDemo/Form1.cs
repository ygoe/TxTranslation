using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Unclassified.TxLib;

namespace WinFormsDemo
{
	public partial class Form1 : Form
	{
		private struct CultureLabel
		{
			public string Name;
			public string DisplayName;

			public CultureLabel(string name, string displayName)
			{
				Name = name;
				DisplayName = displayName;
			}

			public override string ToString()
			{
				return DisplayName;
			}
		}

		private bool isLoading = true;

		public Form1()
		{
			// Setup translation data
			Tx.UseFileSystemWatcher = true;
			Tx.LoadDirectory("lang");
			Tx.PrimaryCulture = "de";

			InitializeComponent();

			// Adopt UI font
			Font = SystemFonts.MessageBoxFont;

			// Statically set a control's text once
			//IntroLabel.Text = Tx.T("intro");

			// Add a custom control property binding to a text key
			//TxDictionaryBinding.AddBinding(IntroLabel, "Text", "intro");

			// Add translation dictionary bindings for all controls
			TxDictionaryBinding.AddTextBindings(this);

			// Fill the languages drop-down and select the current language
			string currentCulture = Tx.GetCultureName();
			foreach (CultureInfo ci in Tx.AvailableCultures)
			{
				CultureLabel cl = new CultureLabel(ci.Name, Tx.U(ci.NativeName));
				LanguageCombo.Items.Add(cl);
				if (ci.Name == currentCulture)
				{
					LanguageCombo.SelectedItem = cl;
				}
			}
			isLoading = false;
		}

		protected override void OnSystemColorsChanged(EventArgs e)
		{
			base.OnSystemColorsChanged(e);
			Font = SystemFonts.MessageBoxFont;
		}

		private void LoginButton_Click(object sender, EventArgs e)
		{
			MessageBox.Show(Tx.T("message"), Tx.T("message.caption"), MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void LanguageCombo_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (!isLoading)
			{
				if (LanguageCombo.SelectedItem is CultureLabel)
				{
					Tx.SetCulture(((CultureLabel) LanguageCombo.SelectedItem).Name);
				}
			}
		}
	}
}
