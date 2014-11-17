using System;
using System.ComponentModel;
using Unclassified.Util;

namespace Unclassified.TxEditor
{
	public interface IAppSettings : ISettings
	{
		/// <summary>
		/// Gets or sets the last started version of the application.
		/// </summary>
		string LastStartedAppVersion { get; set; }

		/// <summary>
		/// Gets or sets the culture to use for the application user interface. Empty for default.
		/// </summary>
		string AppCulture { get; set; }

		IFileSettings File { get; }
		IInputSettings Input { get; }
		IViewSettings View { get; }
		IWizardSettings Wizard { get; }
	}

	public interface IFileSettings : ISettings
	{
		/// <summary>
		/// Gets or sets a value indicating whether to ask for upgrading the format when saving a
		/// file that was loaded in an older format.
		/// </summary>
		[DefaultValue(true)]
		bool AskSaveUpgrade { get; set; }
	}

	public interface IInputSettings : ISettings
	{
		/// <summary>
		/// Gets or sets the characters used in the character map input UI.
		/// </summary>
		[DefaultValue("©«®±·»¼½¾¿Ç×ØçñŒœ ‑‒“”„‘’‚•…‰™Ω≙≠≤≥⊂⊃∈∉★♥♦")]
		string CharacterMap { get; set; }
	}

	public interface IViewSettings : ISettings
	{
		/// <summary>
		/// Gets or sets a value indicating whether text key comments are visible.
		/// </summary>
		bool ShowComments { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether a monospace font is used.
		/// </summary>
		bool MonospaceFont { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether hidden characters are marked.
		/// </summary>
		bool ShowHiddenChars { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the character map is visible.
		/// </summary>
		bool ShowCharacterMap { get; set; }

		/// <summary>
		/// Gets or sets the font scaling value.
		/// </summary>
		[DefaultValue(100)]
		double FontScale { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether native culture names are displayed instead of
		/// those in the current UI language.
		/// </summary>
		bool NativeCultureNames { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the text suggestions are visible.
		/// </summary>
		bool ShowSuggestions { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the text suggestions layout is horizontal.
		/// </summary>
		[DefaultValue(true)]
		bool SuggestionsHorizontalLayout { get; set; }

		/// <summary>
		/// Gets or sets the width of the suggestions panel (only used in horizontal layout).
		/// </summary>
		[DefaultValue(180)]
		double SuggestionsWidth { get; set; }

		/// <summary>
		/// Gets or sets the height of the suggestions panel (only used in vertical layout).
		/// </summary>
		[DefaultValue(150)]
		double SuggestionsHeight { get; set; }

		IWindowStateSettings MainWindowState { get; }
	}

	public interface IWizardSettings : ISettings
	{
		/// <summary>
		/// Gets or sets the source code mode for the text key wizard.
		/// </summary>
		[DefaultValue("C#")]
		string SourceCode { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the text key wizard window location is restored.
		/// </summary>
		bool RememberLocation { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the text key wizard hotkey is restricted to
		/// Visual Studio windows.
		/// </summary>
		[DefaultValue(true)]
		bool HotkeyInVisualStudioOnly { get; set; }

		[DefaultValue(int.MinValue)]
		int WindowLeft { get; set; }

		[DefaultValue(int.MinValue)]
		int WindowTop { get; set; }
	}
}
