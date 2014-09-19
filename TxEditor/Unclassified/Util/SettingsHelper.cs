using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;

namespace Unclassified.Util
{
	/// <summary>
	/// Provides methods for specialized settings situations.
	/// </summary>
	public static class SettingsHelper
	{
		/// <summary>
		/// Returns a settings file path in the user's AppData directory.
		/// </summary>
		/// <param name="directory">The directory in the AppData directory. May include backslashes or slashes for subdirectories.</param>
		/// <param name="fileName">The settings file name.</param>
		/// <returns></returns>
		public static string GetAppDataPath(string directory, string fileName)
		{
			return Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				directory
					.Replace('\\', Path.DirectorySeparatorChar)
					.Replace('/', Path.DirectorySeparatorChar),
				fileName);
		}

		// TODO: This is only for WPF windows. Add an option for Windows Forms.

		/// <summary>
		/// Binds the window location, size and state to the settings. This method should be called
		/// in the Window constructor after InitializeComponent.
		/// </summary>
		/// <param name="window">The window to update and monitor.</param>
		/// <param name="settings">The settings to use for the window.</param>
		public static void BindWindowState(Window window, IWindowStateSettings settings)
		{
			// Apply the current settings to the window, if available
			if (settings.Left != int.MinValue &&
				settings.Top != int.MinValue &&
				settings.Width != int.MinValue &&
				settings.Height != int.MinValue)
			{
				window.WindowStartupLocation = WindowStartupLocation.Manual;
				window.Left = settings.Left;
				window.Top = settings.Top;
				window.Width = settings.Width;
				window.Height = settings.Height;
				window.WindowState = settings.IsMaximized ? WindowState.Maximized : WindowState.Normal;
			}

			// Write back any changes to the settings.
			// The event signatures are different, so it's easier to just copy the handler code
			// and let the compiler figure out the inferred types.
			window.LocationChanged += (sender, args) =>
			{
				settings.Left = (int) window.RestoreBounds.Left;
				settings.Top = (int) window.RestoreBounds.Top;
				settings.Width = (int) window.RestoreBounds.Width;
				settings.Height = (int) window.RestoreBounds.Height;
				settings.IsMaximized = window.WindowState == WindowState.Maximized;
			};
			window.SizeChanged += (sender, args) =>
			{
				settings.Left = (int) window.RestoreBounds.Left;
				settings.Top = (int) window.RestoreBounds.Top;
				settings.Width = (int) window.RestoreBounds.Width;
				settings.Height = (int) window.RestoreBounds.Height;
				settings.IsMaximized = window.WindowState == WindowState.Maximized;
			};
		}
	}

	/// <summary>
	/// Defines a settings structure that represents a window location, size and state.
	/// </summary>
	public interface IWindowStateSettings : ISettings
	{
		/// <summary>Gets or sets the left edge of the window.</summary>
		[Obfuscation(Exclude = true)]
		[DefaultValue(int.MinValue)]
		int Left { get; set; }

		/// <summary>Gets or sets the top edge of the window.</summary>
		[Obfuscation(Exclude = true)]
		[DefaultValue(int.MinValue)]
		int Top { get; set; }

		/// <summary>Gets or sets the width of the window.</summary>
		[Obfuscation(Exclude = true)]
		[DefaultValue(int.MinValue)]
		int Width { get; set; }

		/// <summary>Gets or sets the height of the window.</summary>
		[Obfuscation(Exclude = true)]
		[DefaultValue(int.MinValue)]
		int Height { get; set; }

		/// <summary>Gets or sets a value indicating whether the window is maximized.</summary>
		[Obfuscation(Exclude = true)]
		[DefaultValue(false)]
		bool IsMaximized { get; set; }
	}
}
