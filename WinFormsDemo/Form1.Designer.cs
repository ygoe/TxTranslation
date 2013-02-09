namespace WinFormsDemo
{
	partial class Form1
	{
		/// <summary>
		/// Erforderliche Designervariable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Verwendete Ressourcen bereinigen.
		/// </summary>
		/// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Vom Windows Form-Designer generierter Code

		/// <summary>
		/// Erforderliche Methode für die Designerunterstützung.
		/// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
		/// </summary>
		private void InitializeComponent()
		{
			this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
			this.IntroLabel = new System.Windows.Forms.Label();
			this.NameLabel = new System.Windows.Forms.Label();
			this.NameText = new System.Windows.Forms.TextBox();
			this.PasswordLabel = new System.Windows.Forms.Label();
			this.PasswordText = new System.Windows.Forms.TextBox();
			this.LoginButton = new System.Windows.Forms.Button();
			this.LanguageCombo = new System.Windows.Forms.ComboBox();
			this.tableLayoutPanel1.SuspendLayout();
			this.SuspendLayout();
			// 
			// tableLayoutPanel1
			// 
			this.tableLayoutPanel1.ColumnCount = 3;
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
			this.tableLayoutPanel1.Controls.Add(this.IntroLabel, 0, 0);
			this.tableLayoutPanel1.Controls.Add(this.NameLabel, 0, 1);
			this.tableLayoutPanel1.Controls.Add(this.NameText, 1, 1);
			this.tableLayoutPanel1.Controls.Add(this.PasswordLabel, 0, 2);
			this.tableLayoutPanel1.Controls.Add(this.PasswordText, 1, 2);
			this.tableLayoutPanel1.Controls.Add(this.LoginButton, 0, 3);
			this.tableLayoutPanel1.Controls.Add(this.LanguageCombo, 2, 3);
			this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
			this.tableLayoutPanel1.Name = "tableLayoutPanel1";
			this.tableLayoutPanel1.Padding = new System.Windows.Forms.Padding(12);
			this.tableLayoutPanel1.RowCount = 4;
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
			this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
			this.tableLayoutPanel1.Size = new System.Drawing.Size(341, 214);
			this.tableLayoutPanel1.TabIndex = 0;
			// 
			// IntroLabel
			// 
			this.IntroLabel.AutoSize = true;
			this.tableLayoutPanel1.SetColumnSpan(this.IntroLabel, 3);
			this.IntroLabel.Location = new System.Drawing.Point(12, 12);
			this.IntroLabel.Margin = new System.Windows.Forms.Padding(0);
			this.IntroLabel.Name = "IntroLabel";
			this.IntroLabel.Size = new System.Drawing.Size(33, 13);
			this.IntroLabel.TabIndex = 0;
			this.IntroLabel.Text = "[intro]";
			// 
			// NameLabel
			// 
			this.NameLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.NameLabel.AutoSize = true;
			this.NameLabel.Location = new System.Drawing.Point(12, 37);
			this.NameLabel.Margin = new System.Windows.Forms.Padding(0, 12, 0, 0);
			this.NameLabel.Name = "NameLabel";
			this.NameLabel.Size = new System.Drawing.Size(39, 20);
			this.NameLabel.TabIndex = 1;
			this.NameLabel.Text = "[name]";
			this.NameLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// NameText
			// 
			this.NameText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel1.SetColumnSpan(this.NameText, 2);
			this.NameText.Location = new System.Drawing.Point(78, 37);
			this.NameText.Margin = new System.Windows.Forms.Padding(8, 12, 0, 0);
			this.NameText.Name = "NameText";
			this.NameText.Size = new System.Drawing.Size(251, 20);
			this.NameText.TabIndex = 2;
			// 
			// PasswordLabel
			// 
			this.PasswordLabel.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left)));
			this.PasswordLabel.AutoSize = true;
			this.PasswordLabel.Location = new System.Drawing.Point(12, 61);
			this.PasswordLabel.Margin = new System.Windows.Forms.Padding(0, 4, 0, 0);
			this.PasswordLabel.Name = "PasswordLabel";
			this.PasswordLabel.Size = new System.Drawing.Size(58, 20);
			this.PasswordLabel.TabIndex = 3;
			this.PasswordLabel.Text = "[password]";
			this.PasswordLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
			// 
			// PasswordText
			// 
			this.PasswordText.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.tableLayoutPanel1.SetColumnSpan(this.PasswordText, 2);
			this.PasswordText.Location = new System.Drawing.Point(78, 61);
			this.PasswordText.Margin = new System.Windows.Forms.Padding(8, 4, 0, 0);
			this.PasswordText.Name = "PasswordText";
			this.PasswordText.Size = new System.Drawing.Size(251, 20);
			this.PasswordText.TabIndex = 4;
			this.PasswordText.UseSystemPasswordChar = true;
			// 
			// LoginButton
			// 
			this.LoginButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.LoginButton.AutoSize = true;
			this.LoginButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
			this.tableLayoutPanel1.SetColumnSpan(this.LoginButton, 2);
			this.LoginButton.FlatStyle = System.Windows.Forms.FlatStyle.System;
			this.LoginButton.Location = new System.Drawing.Point(12, 180);
			this.LoginButton.Margin = new System.Windows.Forms.Padding(0, 12, 0, 0);
			this.LoginButton.Name = "LoginButton";
			this.LoginButton.Size = new System.Drawing.Size(49, 22);
			this.LoginButton.TabIndex = 5;
			this.LoginButton.Text = "[login]";
			this.LoginButton.UseVisualStyleBackColor = true;
			this.LoginButton.Click += new System.EventHandler(this.LoginButton_Click);
			// 
			// LanguageCombo
			// 
			this.LanguageCombo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
			this.LanguageCombo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
			this.LanguageCombo.FormattingEnabled = true;
			this.LanguageCombo.Location = new System.Drawing.Point(149, 181);
			this.LanguageCombo.Margin = new System.Windows.Forms.Padding(12, 0, 0, 0);
			this.LanguageCombo.Name = "LanguageCombo";
			this.LanguageCombo.Size = new System.Drawing.Size(180, 21);
			this.LanguageCombo.TabIndex = 6;
			this.LanguageCombo.SelectedIndexChanged += new System.EventHandler(this.LanguageCombo_SelectedIndexChanged);
			// 
			// Form1
			// 
			this.AcceptButton = this.LoginButton;
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
			this.ClientSize = new System.Drawing.Size(341, 214);
			this.Controls.Add(this.tableLayoutPanel1);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "Form1";
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "[title]";
			this.tableLayoutPanel1.ResumeLayout(false);
			this.tableLayoutPanel1.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
		private System.Windows.Forms.Label IntroLabel;
		private System.Windows.Forms.Label NameLabel;
		private System.Windows.Forms.TextBox NameText;
		private System.Windows.Forms.Label PasswordLabel;
		private System.Windows.Forms.TextBox PasswordText;
		private System.Windows.Forms.Button LoginButton;
		private System.Windows.Forms.ComboBox LanguageCombo;
	}
}

