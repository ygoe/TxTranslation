using Unclassified;
using System.ComponentModel;

namespace TxEditor
{
	/// <summary>
	/// Provides properties to access the application settings.
	/// </summary>
	public class AppSettings : Settings, INotifyPropertyChanged
	{
		#region Constructors

		/// <summary>
		/// Initialises a new instance of the AppSettings class and sets up all change notifications.
		/// </summary>
		/// <param name="fileName"></param>
		public AppSettings(string fileName)
			: base(fileName)
		{
			base.AddHandler("input.charmap", delegate() { OnPropertyChanged("CharacterMap"); });
			
			base.AddHandler("view.comments", delegate() { OnPropertyChanged("ShowComments"); });
			base.AddHandler("view.monospace-font", delegate() { OnPropertyChanged("MonospaceFont"); });
			base.AddHandler("view.hidden-chars", delegate() { OnPropertyChanged("ShowHiddenChars"); });
			base.AddHandler("view.charmap", delegate() { OnPropertyChanged("ShowCharacterMap"); });
			base.AddHandler("view.font-scale", delegate() { OnPropertyChanged("FontScale"); });
			base.AddHandler("view.native-culture-names", delegate() { OnPropertyChanged("NativeCultureNames"); });

			base.AddHandler("file.ask-save-upgrade", delegate() { OnPropertyChanged("AskSaveUpgrade"); });
		}

		#endregion Constructors

		#region INotifyPropertyChanged members

		public event PropertyChangedEventHandler PropertyChanged;

		protected void OnPropertyChanged(string propertyName)
		{
			var handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		#endregion INotifyPropertyChanged members

		#region Section: input

		/// <summary>
		/// Gets or sets the characters used in the character map input UI.
		/// </summary>
		public string CharacterMap
		{
			get { return GetString("input.charmap", "©«®±·»¼½¾¿Ç×ØçñŒœ ‑‒“”„‘’‚•…‰™Ω≙≠≤≥⊂⊃∈∉★♥♦"); }
			set { Set("input.charmap", value); }
		}

		#endregion Section: input

		#region Section: view

		/// <summary>
		/// Gets or sets a value indicating whether text key comments are visible.
		/// </summary>
		public bool ShowComments
		{
			get { return GetBool("view.comments", false); }
			set { Set("view.comments", value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether a monospace font is used.
		/// </summary>
		public bool MonospaceFont
		{
			get { return GetBool("view.monospace-font", false); }
			set { Set("view.monospace-font", value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether hidden characters are marked.
		/// </summary>
		public bool ShowHiddenChars
		{
			get { return GetBool("view.hidden-chars", false); }
			set { Set("view.hidden-chars", value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the character map is visible.
		/// </summary>
		public bool ShowCharacterMap
		{
			get { return GetBool("view.charmap", false); }
			set { Set("view.charmap", value); }
		}

		/// <summary>
		/// Gets or sets the font scaling value.
		/// </summary>
		public double FontScale
		{
			get { return GetDouble("view.font-scale", 100); }
			set { Set("view.font-scale", value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether native culture names are displayed instead of
		/// those in the current UI language.
		/// </summary>
		public bool NativeCultureNames
		{
			get { return GetBool("view.native-culture-names", false); }
			set { Set("view.native-culture-names", value); }
		}

		#endregion Section: view

		#region Section: file

		/// <summary>
		/// Gets or sets a value indicating whether to ask for upgrading the format when saving a
		/// file that was loaded in an older format.
		/// </summary>
		public bool AskSaveUpgrade
		{
			get { return GetBool("file.ask-save-upgrade", true); }
			set { Set("file.ask-save-upgrade", value); }
		}

		#endregion Section: file
	}
}
