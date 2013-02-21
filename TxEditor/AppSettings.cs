namespace TxEditor
{
	/// <summary>
	/// Provides static properties to access the application settings.
	/// </summary>
	static class AppSettings
	{
		// TODO: Dummy
		/// <summary>
		/// Ruft den Pfad der verwendeten Datenbankdabei ab oder legt diesen fest.
		/// </summary>
		public static string DatabaseFilePath
		{
			get { return App.Settings.GetString("db.filepath", ""); }
			set { App.Settings.Set("db.filepath", value); }
		}
	}
}
