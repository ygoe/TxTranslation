using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;
using System.IO.Compression;

namespace UnicodeTableConverter
{
	class Program
	{
		static void Main(string[] args)
		{
			int categoryIndex = -1;
			int codePoint = -1;
			string charName = null;

			List<string> categoryNames = new List<string>();
			Dictionary<int, CharacterData> characters = new Dictionary<int, CharacterData>();
			
			using (StreamReader sr = new StreamReader(args[0]))
			{
				while (!sr.EndOfStream)
				{
					string line = sr.ReadLine().TrimEnd();

					if (line.StartsWith("@\t\t"))
					{
						// Category begins.
						if (codePoint == 0xFFFF)
						{
							// Last supported code point, stop here
							break;
						}
						// Remember the category name
						string catName = line.Substring(3);
						categoryIndex = categoryNames.IndexOf(catName);
						if (categoryIndex == -1)
						{
							categoryNames.Add(catName);
							categoryIndex = categoryNames.Count - 1;
						}
					}
					else if ((char.IsDigit(line[0]) || line[0] >= 'A' && line[0] <= 'F') && line[4] == '\t')
					{
						// Character definition (4 hex digits).
						// If still waiting for previous character's proper name: Output without name
						if (codePoint >= 0)
						{
							characters[codePoint] = new CharacterData() { Name = charName, Category = categoryIndex };
						}
						// If proper character name available: output
						// Otherwise: remember for next line
						codePoint = int.Parse(line.Substring(0, 4), NumberStyles.HexNumber);
						charName = line.Substring(5);
						if (charName[0] != '<')
						{
							characters[codePoint] = new CharacterData() { Name = charName, Category = categoryIndex };
							if (codePoint == 0xFFFF)
							{
								// Last supported code point, stop here
								break;
							}
							codePoint = -1;
						}
					}
					else if ((char.IsDigit(line[0]) || line[0] >= 'A' && line[0] <= 'F') && line[4] != '\t')
					{
						// Character definition (5 hex digits).
						// If still waiting for previous character's proper name: Output without name
						if (codePoint >= 0)
						{
							characters[codePoint] = new CharacterData() { Name = charName, Category = categoryIndex };
						}
						// Stop here
						break;
					}
					else if (line.StartsWith("\t= ") && codePoint >= 0)
					{
						// Alias definition, waiting for a proper character name.
						// Output
						charName = line.Substring(3);
						characters[codePoint] = new CharacterData() { Name = charName, Category = categoryIndex };
						if (codePoint == 0xFFFF)
						{
							// Last supported code point, stop here
							break;
						}
						codePoint = -1;
					}
					else if (codePoint >= 0)
					{
						// Anything else
						characters[codePoint] = new CharacterData() { Name = charName, Category = categoryIndex };
						if (codePoint == 0xFFFF)
						{
							// Last supported code point, stop here
							break;
						}
						codePoint = -1;
					}
				}
			}

			using (StreamWriter sw = new StreamWriter("UnicodeTable.txt"))
			{
				for (int i = 0; i < categoryNames.Count; i++)
				{
					sw.WriteLine(i.ToString() + "=" + categoryNames[i]);
				}
				foreach (var kvp in characters)
				{
					sw.WriteLine(kvp.Key.ToString("X4") + "\t" + kvp.Value.Name + "\t" + kvp.Value.Category);
				}
			}

			using (var s = new FileStream("UnicodeTable.deflate", FileMode.Create))
			using (var ds = new DeflateStream(s, CompressionMode.Compress))
			//using (var ds = new GZipStream(s, CompressionMode.Compress))
			using (StreamWriter sw = new StreamWriter(ds))
			{
				for (int i = 0; i < categoryNames.Count; i++)
				{
					sw.Write(categoryNames[i] + "\n");
				}
				foreach (var kvp in characters)
				{
					sw.Write(kvp.Key.ToString("X4") + "\t" + kvp.Value.Name + "\t" + kvp.Value.Category + "\n");
				}
			}
		}
	}

	struct CharacterData
	{
		public string Name;
		public int Category;
	}
}
