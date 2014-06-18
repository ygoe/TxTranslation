using System;

namespace Unclassified.Util
{
	/// <summary>
	/// Provides extension methods for strings.
	/// </summary>
	public static class StringExtensions
	{
		#region String conversions

		/// <summary>
		/// Converts a string value into a safe file name. Invalid characters are each replaced
		/// by an underline.
		/// </summary>
		/// <param name="str">Source string to convert.</param>
		/// <returns></returns>
		public static string ToFileName(this string str)
		{
			// TODO: Consider more restrictions
			// See http://msdn.microsoft.com/en-us/library/aa365247%28v=vs.85%29.aspx
			// * Maximum length (minus a bit for automatic numbering), maybe 100 chars?
			// * Reserved names (NUL, CON, ., .., etc.)
			// * No space at the beginning
			// * No space or . at the end
			return str
				.Replace('"', '_')
				.Replace('*', '_')
				.Replace('/', '_')
				.Replace(':', '_')
				.Replace('<', '_')
				.Replace('>', '_')
				.Replace('?', '_')
				.Replace('\\', '_')
				.Replace('|', '_');
		}

		/// <summary>
		/// Returns a new string in which a specified string is replaced with another specified
		/// string if its occurence is at the beginning of the current string.
		/// </summary>
		/// <param name="str">The source string.</param>
		/// <param name="search">The string to be replaced.</param>
		/// <param name="replacement">The string to replace the occurrence of <paramref name="search"/>.</param>
		/// <returns></returns>
		public static string ReplaceStart(this string str, string search, string replacement)
		{
			if (str.StartsWith(search))
			{
				return replacement + str.Substring(search.Length);
			}
			return str;
		}

		/// <summary>
		/// Retrieves a substring from this instance. The substring starts at a specified character
		/// position and has a specified length. If <paramref name="startIndex"/> is behind the
		/// end of the string an empty string is returned. If <paramref name="startIndex"/> is
		/// negative, it is counted backwards from the end of the string.
		/// </summary>
		/// <param name="str">The source string.</param>
		/// <param name="startIndex">The zero-based starting character position of a substring in this instance.</param>
		/// <returns></returns>
		public static string SafeSubstring(this string str, int startIndex)
		{
			if (str == null) return null;

			if (startIndex < 0)
			{
				startIndex = str.Length + startIndex;
				if (startIndex < 0)
				{
					startIndex = 0;
				}
			}
			if (startIndex >= str.Length)
			{
				return "";
			}
			return str.Substring(startIndex);
		}

		/// <summary>
		/// Retrieves a substring from this instance. The substring starts at a specified character
		/// position and has a specified length. If <paramref name="startIndex"/> is behind the
		/// end of the string, or if <paramref name="length"/> is longer than the string, an empty
		/// string is returned. If <paramref name="startIndex"/> or <paramref name="length"/> are
		/// negative, they are counted backwards from the end of the string.
		/// </summary>
		/// <param name="str">The source string.</param>
		/// <param name="startIndex">The zero-based starting character position of a substring in this instance.</param>
		/// <param name="length">The number of characters in the substring.</param>
		/// <returns></returns>
		public static string SafeSubstring(this string str, int startIndex, int length)
		{
			if (str == null) return null;

			if (startIndex < 0)
			{
				startIndex = str.Length + startIndex;
				if (startIndex < 0)
				{
					startIndex = 0;
				}
			}
			if (length < 0)
			{
				length = str.Length + length - startIndex;
				if (length <= 0)
				{
					return "";
				}
			}
			if (startIndex >= str.Length)
			{
				return "";
			}
			if (startIndex + length >= str.Length)
			{
				length = str.Length - startIndex;
			}
			return str.Substring(startIndex, length);
		}

		#endregion String conversions

		#region Tabular number formatting

		/// <summary>
		/// Formats a number for readable right-aligned Unicode text.
		/// </summary>
		/// <param name="value">The number value to format.</param>
		/// <param name="decimals">The number of decimals to show. Trailing zeros are replaced with equally wide spaces.</param>
		/// <returns>Formatted number string.</returns>
		public static string FormatTabular(this double value, int decimals)
		{
			string str = value.ToString("N" + decimals);
			if (decimals > 0)
			{
				// Has decimal places and a decimal separator
				int trailingZeros = 0;
				while (str.EndsWith("0"))
				{
					// One more trailing zero
					trailingZeros++;
					str = str.Substring(0, str.Length - 1);
				}
				if (trailingZeros == decimals)
				{
					// All decimal places removed, also replace the decimal separator
					str = str.Substring(0, str.Length - 1);
					str += "\u00a0";   // No-break Space
				}
				while (trailingZeros-- > 0)
				{
					// Fill up with spaces
					str += "\u2007";   // Figure Space, no-break
				}
			}
			return str;
		}

		#endregion Tabular number formatting
	}
}
