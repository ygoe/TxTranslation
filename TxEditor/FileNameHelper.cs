using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TaskDialogInterop;
using Unclassified.TxEditor.Views;
using Unclassified.TxLib;

namespace Unclassified.TxEditor
{
	internal static class FileNameHelper
	{
		public static string GetPrefix(string fileName)
		{
			// txd files (format v2) do not have additional parts in their file name
			if (Path.GetExtension(fileName).ToLowerInvariant() == ".txd")
			{
				return Path.GetFileNameWithoutExtension(fileName);
			}

			// xml files (format v1) have their culture encoded in the file name
			string regex = @"^(.+?)\.(([a-z]{2})([-][a-z]{2})?)\.xml$";
			Match m = Regex.Match(Path.GetFileName(fileName), regex, RegexOptions.IgnoreCase);
			if (m.Success)
			{
				return m.Groups[1].Value;
			}

			// Whatever this may be...
			return Path.GetFileNameWithoutExtension(fileName);
		}

		public static string GetCulture(string fileName)
		{
			// txd files (format v2) have all cultures in one file
			if (Path.GetExtension(fileName).ToLowerInvariant() == ".txd")
			{
				return null;
			}

			// xml files (format v1) have their culture encoded in the file name
			string regex = @"\.(([a-z]{2})([-][a-z]{2})?)\.xml$";
			Match m = Regex.Match(Path.GetFileName(fileName), regex, RegexOptions.IgnoreCase);
			if (m.Success)
			{
				return m.Groups[1].Value.ToLowerInvariant();
			}

			return null;
		}

		/// <summary>
		/// Looks for other culture files with the same prefix in the same directory and asks the
		/// user whether other files shall also be loaded.
		/// </summary>
		/// <param name="filesToLoad">The files to load. This method only does anything if the list
		/// contains a single .xml file. If the user accepts to load other files as well, they're
		/// added to this list.</param>
		/// <returns>false if the user cancelled the operation; otherwise, true.</returns>
		public static bool FindOtherCultures(List<string> filesToLoad)
		{
			if (filesToLoad.Count == 1 && Path.GetExtension(filesToLoad[0]).ToLowerInvariant() == ".xml")
			{
				// Scan for similar files and ask if not all of them are selected
				string myCulture = FileNameHelper.GetCulture(filesToLoad[0]);
				List<string> otherFiles = new List<string>();
				List<string> otherCultures = new List<string>();
				foreach (string otherFile in Directory.GetFiles(Path.GetDirectoryName(filesToLoad[0]), FileNameHelper.GetPrefix(filesToLoad[0]) + ".*.xml"))
				{
					string otherCulture = FileNameHelper.GetCulture(otherFile);
					if (otherCulture != null && otherCulture != myCulture)
					{
						otherFiles.Add(otherFile);
						otherCultures.Add(otherCulture);
					}
				}
				if (otherCultures.Count > 0)
				{
					otherCultures.Sort();
					if (App.SplashScreen != null)
					{
						App.SplashScreen.Close(TimeSpan.Zero);
					}
					var result = TaskDialog.Show(
						owner: MainWindow.Instance,
						title: "TxEditor",
						mainInstruction: Tx.T("msg.load file.other cultures for prefix"),
						content: Tx.T("msg.load file.other cultures for prefix.desc", "list", string.Join(", ", otherCultures)),
						customButtons: new string[] { Tx.T("task dialog.button.load all"), Tx.T("task dialog.button.load one"), Tx.T("task dialog.button.cancel") },
						allowDialogCancellation: true);
					if (result.CustomButtonResult == 0)
					{
						// Load all, add other files
						filesToLoad.AddRange(otherFiles);
					}
					else if (result.CustomButtonResult == 1)
					{
						// Load one, do nothing
					}
					else
					{
						// Cancel or unset
						return false;
					}
				}
			}
			return true;
		}
	}
}
