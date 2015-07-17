using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Unclassified.Util
{
	/// <summary>
	/// Provides methods for specialized settings situations.
	/// </summary>
	public static partial class SettingsHelper
	{
		#region Settings file path methods

		/// <summary>
		/// Returns a settings file path in the user's AppData directory.
		/// </summary>
		/// <param name="directory">The directory in the AppData directory. May include backslashes or slashes for subdirectories.</param>
		/// <param name="fileName">The settings file name.</param>
		/// <returns></returns>
		/// <remarks>
		/// This method generates platform-default paths for Windows (%AppData%) and Unix/Linux
		/// systems ($HOME). On Unix/Linux, a period (.) is prepended to the directory name to keep
		/// it hidden in the user's home directory.
		/// </remarks>
		public static string GetAppDataPath(string directory, string fileName)
		{
			directory = directory
				.Replace('\\', Path.DirectorySeparatorChar)
				.Replace('/', Path.DirectorySeparatorChar);
			string baseDir;

			switch (Environment.OSVersion.Platform)
			{
				case PlatformID.Win32NT:
				case PlatformID.Win32S:
				case PlatformID.Win32Windows:
				case PlatformID.WinCE:
					baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
					if (baseDir.StartsWith(Environment.SystemDirectory, StringComparison.OrdinalIgnoreCase))
					{
						throw new InvalidOperationException("Trying to access per-user settings from the SYSTEM or NETWORK SERVICE account.");
					}
					break;
				default:   // Unix/Linux
					baseDir = Environment.GetEnvironmentVariable("HOME");
					if (string.IsNullOrEmpty(baseDir))
						throw new InvalidOperationException("The HOME environment variable is not set.");
					directory = "." + directory;
					break;
			}
			return Path.Combine(baseDir, directory, fileName);
		}

		/// <summary>
		/// Returns a settings file path in the system's ProgramData directory.
		/// </summary>
		/// <param name="directory">The directory in the ProgramData directory. May include backslashes or slashes for subdirectories.</param>
		/// <param name="fileName">The settings file name.</param>
		/// <returns></returns>
		/// <remarks>
		/// This method generates platform-default paths for Windows (%ProgramData%) and Unix/Linux
		/// systems (/etc).
		/// </remarks>
		public static string GetProgramDataPath(string directory, string fileName)
		{
			directory = directory
				.Replace('\\', Path.DirectorySeparatorChar)
				.Replace('/', Path.DirectorySeparatorChar);
			string baseDir;

			switch (Environment.OSVersion.Platform)
			{
				case PlatformID.Win32NT:
				case PlatformID.Win32S:
				case PlatformID.Win32Windows:
				case PlatformID.WinCE:
					baseDir = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
					break;
				default:   // Unix/Linux
					baseDir = "/etc";
					break;
			}
			return Path.Combine(baseDir, directory, fileName);
		}

		/// <summary>
		/// Returns an object for the specified interface type with a backing file store.
		/// </summary>
		/// <typeparam name="TSettings">The settings interface type to implement.</typeparam>
		/// <param name="directory">The directory in the application data base directory. May include backslashes or slashes for subdirectories.</param>
		/// <param name="fileName">The settings file name.</param>
		/// <param name="userLevel">Specifies whether a user-level path is returned instead of a system-level path.</param>
		/// <returns></returns>
		/// <remarks>
		/// This method generates platform-default paths for Windows (%AppData% and %ProgramData%)
		/// and Unix/Linux systems ($HOME/.* and /etc).
		/// </remarks>
		public static TSettings NewFile<TSettings>(string directory, string fileName, bool userLevel)
			where TSettings : class
		{
			string path;
			if (userLevel)
			{
				path = GetAppDataPath(directory, fileName);
			}
			else
			{
				path = GetProgramDataPath(directory, fileName);
			}
			FileSettingsStore store = new FileSettingsStore(path);
			return SettingsAdapterFactory.New<TSettings>(store);
		}

		#endregion Settings file path methods

		#region ISettingsStore extension methods

		/// <summary>
		/// Gets the display name of the storage location for an <see cref="ISettingsStore"/>
		/// instance. This includes optional flags.
		/// </summary>
		/// <param name="settingsStore">The settings store instance of which to return the location.</param>
		/// <returns></returns>
		public static string GetLocationDisplayName(this ISettingsStore settingsStore)
		{
			if (settingsStore == null) throw new ArgumentNullException("settingsStore");

			FileSettingsStore fileStore = settingsStore as FileSettingsStore;
			if (fileStore != null)
			{
				return fileStore.FileName +
					(fileStore.IsReadOnly ? " [RO]" : "") +
					(fileStore.IsEncrypted ? " [ENC]" : "");
			}
			return settingsStore.ToString();
		}

		/// <summary>
		/// Removes multiple setting keys from the settings store, matching a regular expression
		/// pattern.
		/// </summary>
		/// <param name="settingsStore">The settings store instance in which to remove keys.</param>
		/// <param name="keyRegex">The Regex pattern of the setting keys to remove.</param>
		/// <returns>true if any key was removed, false if none existed or matched.</returns>
		public static bool RemovePattern(this ISettingsStore settingsStore, string keyRegex)
		{
			if (settingsStore == null) throw new ArgumentNullException("settingsStore");

			bool anyRemoved = false;
			foreach (string key in settingsStore.GetKeys())
			{
				if (Regex.IsMatch(key, keyRegex))
				{
					anyRemoved |= settingsStore.Remove(key);
				}
			}
			return anyRemoved;
		}

		#endregion ISettingsStore extension methods
	}

	#region Window state settings interface

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

	#endregion Window state settings interface
}
