using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace Unclassified.Util
{
	public class UnicodeInfo
	{
		private static Dictionary<int, UnicodeCharacter> characters = new Dictionary<int, UnicodeCharacter>();

		static UnicodeInfo()
		{
			// First load the XML file into an XmlDocument for further processing
			Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream("Unclassified.TxEditor.UnicodeTable.deflate");
			if (stream == null)
			{
				throw new ArgumentException("The embedded resource was not found in this assembly.");
			}

			characters.Clear();
			List<string> categoryNames = new List<string>();

			using (var ds = new DeflateStream(stream, CompressionMode.Decompress))
			using (var sr = new StreamReader(ds))
			{
				while (!sr.EndOfStream)
				{
					string line = sr.ReadLine();
					if (line.Length > 4 && line[4] == '\t')
					{
						// Character definition
						string[] parts = line.Split('\t');
						int codePoint = int.Parse(parts[0], System.Globalization.NumberStyles.HexNumber);
						int catIndex = int.Parse(parts[2]);
						characters[codePoint] = new UnicodeCharacter() { CodePoint = codePoint, Name = parts[1], Category = categoryNames[catIndex] };
					}
					else
					{
						// Category name
						categoryNames.Add(line);
					}
				}
			}
		}

		public static UnicodeCharacter GetChar(int codePoint)
		{
			UnicodeCharacter uc;
			characters.TryGetValue(codePoint, out uc);
			return uc;
		}
	}

	public struct UnicodeCharacter
	{
		public int CodePoint;
		public string Name;
		public string Category;
	}
}
