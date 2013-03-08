namespace TxEditor
{
	/// <summary>
	/// Provides static properties to access the application settings.
	/// </summary>
	static class AppSettings
	{
		/// <summary>
		/// Gets or sets the characters used in the character map input UI.
		/// </summary>
		public static string CharacterMap
		{
			get { return App.Settings.GetString("input.charmap", "©«®±·»¼½¾¿Ç×ØçñŒœ ‑‒“”„‘’‚•…‰™Ω≙≠≤≥⊂⊃∈∉★♥♦"); }
			set { App.Settings.Set("input.charmap", value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether text key comments are visible.
		/// </summary>
		public static bool Comments
		{
			get { return App.Settings.GetBool("view.comments", false); }
			set { App.Settings.Set("view.comments", value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether a monospace font is used.
		/// </summary>
		public static bool MonospaceFont
		{
			get { return App.Settings.GetBool("view.monospace-font", false); }
			set { App.Settings.Set("view.monospace-font", value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether hidden characters are marked.
		/// </summary>
		public static bool HiddenChars
		{
			get { return App.Settings.GetBool("view.hidden-chars", false); }
			set { App.Settings.Set("view.hidden-chars", value); }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the character map is visible.
		/// </summary>
		public static bool ShowCharacterMap
		{
			get { return App.Settings.GetBool("view.charmap", false); }
			set { App.Settings.Set("view.charmap", value); }
		}

		/// <summary>
		/// Gets or sets the font scaling value.
		/// </summary>
		public static double FontScale
		{
			get { return App.Settings.GetDouble("view.font-scale", 100); }
			set { App.Settings.Set("view.font-scale", value); }
		}
	}
}
