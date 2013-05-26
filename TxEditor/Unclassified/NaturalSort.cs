// Character comparison modes: (choose exactly one!)
#define NATSORT_CMP_STRING_NOCASE
//#define NATSORT_CMP_STRING
//#define NATSORT_CMP_CHAR_NOCASE
//#define NATSORT_CMP_CHAR

// Additional features:
#define NATSORT_WITH_SPECIAL
#define NATSORT_WITH_NEGATIVE

using System;

namespace Unclassified
{
	static class NaturalSort
	{
		/// <summary>
		/// Compares two strings in natural order.
		/// </summary>
		/// <param name="s1"></param>
		/// <param name="s2"></param>
		/// <returns>Comparison result. 0: both strings are equal, negative: s1 &lt; s2, positive: s1 &gt; s2</returns>
		public static int NatCompare(string s1, string s2)
		{
			// Implementation notes:
			// * Sorting support:
			//   This function can sort strings with numbers in the order of their numeric value,
			//   not just each character's value in their strict order. Negative numbers are
			//   supported only at the beginning of each string. Leading zeros and special
			//   characters are ignored for sorting but are given a preference if both strings
			//   would be equal otherwise (to produce deterministic results).
			// * Safe use of indices:
			//   Only access a character at a string position, when it is clear that the index is
			//   within the allowed range.
			// * Produce deterministic results:
			//   Whenever the order of processing s1 and s2 is not irrelevant, i.e. exchanging them,
			//   the resulting sort order may not be deterministic and depend on the order of the
			//   unsorted data.

			// Special cases
			if (s1 == s2) return 0;
			//if (s1 == null && s2 == null) return 0;   // Now covered by the line before
			if (s1 == null) return -1;
			if (s2 == null) return 1;

			int pos1 = 0, pos2 = 0;   // Current string positions
			char c1, c2;   // Characters at current string positions
			int len1 = s1.Length;
			int len2 = s2.Length;

			// More special cases
			if (len1 == 0 && len2 == 0) return 0;
			if (len1 == 0) return -1;
			if (len2 == 0) return 1;

			// Negative number flags
#if NATSORT_WITH_NEGATIVE
			bool neg1 = len1 > 1 && s1[0] == '-' && Char.IsDigit(s1[1]);
			bool neg2 = len2 > 1 && s2[0] == '-' && Char.IsDigit(s2[1]);
#else
			bool neg1 = false, neg2 = false;
#endif
			int preference = 0;   // String preference value: -1 prefers s1, 1 prefers s2 (see function return value)

			bool digit1, digit2;
			int endnum1, endnum2;   // Where the numbers end in the string
			int numpos1, numpos2;   // Current comparison position in a number
			int lz1, lz2;   // Number of leading zeros we've seen until then

#if NATSORT_WITH_SPECIAL
			// These characters will be skipped in comparison
			const string special = " '\"";
#endif

			// Main loop:
			do
			{
#if NATSORT_WITH_SPECIAL
				// Check for non-letter characters and skip them
				int skipped1 = 0;
				int skipped2 = 0;
				while (special.IndexOf(s1[pos1 + skipped1]) != -1 && pos1 + skipped1 < len1 - 1)
				{
					skipped1++;
				}
				while (special.IndexOf(s2[pos2 + skipped2]) != -1 && pos2 + skipped2 < len2 - 1)
				{
					skipped2++;
				}
				if (preference == 0) preference = skipped1 - skipped2;
				if (preference == 0 && skipped1 == skipped2)
				{
					// Skipped none or equal number of special characters: compare them for a preference
					for (int i = 0; i < skipped1; i++)
					{
						int c = s1[pos1 + i].CompareTo(s2[pos2 + i]);
						if (c != 0)
						{
							preference = c;
							break;
						}
					}
				}
				pos1 += skipped1;
				pos2 += skipped2;
#endif

				c1 = s1[pos1];
				c2 = s2[pos2];

				// Check if we have digits in both strings; also accept '-' at the beginning
				digit1 = Char.IsDigit(c1) || (pos1 == 0 && neg1);
				digit2 = Char.IsDigit(c2) || (pos2 == 0 && neg2);

				if (!digit1 || !digit2)
				{
					// At least one of them is no digit: compare by character

					// If one of them is a negative number, replace the '-' by a digit to keep negative
					// and positive numbers together, not separated by other symbols
					if (pos1 == 0 && neg1) c1 = '0';   // c1 was '-'
					if (pos2 == 0 && neg2) c2 = '0';   // c2 was '-'

#if NATSORT_CMP_STRING_NOCASE
					int cmp = string.Compare(c1.ToString(), c2.ToString(), StringComparison.CurrentCultureIgnoreCase);
#elif NATSORT_CMP_STRING
					int cmp = string.Compare(c1.ToString(), c2.ToString(), StringComparison.CurrentCulture);
#elif NATSORT_CMP_CHAR_NOCASE
					int cmp = char.ToLower(c1).CompareTo(char.ToLower(c2));
#elif NATSORT_CMP_CHAR
					int cmp = c1.CompareTo(c2);
#endif
					if (cmp != 0) return cmp;   // We have a string difference at this point

#if NATSORT_CMP_STRING_NOCASE || NATSORT_CMP_STRING
					// Both characters are the same, compare them by value again, to make a preference
					if (preference == 0 && c1 < c2) preference = -1;
					if (preference == 0 && c1 > c2) preference = 1;
#endif
				}
				else
				{
					// Both characters are digits: find the whole number value
					endnum1 = pos1 + 1;
					while (endnum1 < len1 && Char.IsDigit(s1[endnum1])) endnum1++;
					endnum1--;

					endnum2 = pos2 + 1;
					while (endnum2 < len2 && Char.IsDigit(s2[endnum2])) endnum2++;
					endnum2--;

					// Compare both strings from the end of their number leftwards
					numpos1 = endnum1;
					numpos2 = endnum2;
					lz1 = lz2 = 0;

#if NATSORT_WITH_NEGATIVE
					int negfactor = (pos1 == 0 && (neg1 || neg2)) ? -1 : 1;
					// This implies (pos2 == 0)
					// Negative number factor, only valid at the beginning of the strings
#endif

					int numcmp = 0;
					do
					{
						c1 = numpos1 >= 0 ? s1[numpos1] : '0';
						c2 = numpos2 >= 0 ? s2[numpos2] : '0';

						// See if we're still in the number
						if (numpos1 < pos1 || c1 == '-')
						{
							// We left the number in s1: assume '0'
							c1 = '0';
						}
						if (numpos2 < pos2 || c2 == '-')
						{
							// We left the number in s2: assume '0'
							c2 = '0';
						}

						int cmp = c1.CompareTo(c2);
						if (cmp != 0)
						{
							numcmp = cmp;   // We have a numeric difference at this point, keep it for later
#if NATSORT_WITH_NEGATIVE
							numcmp *= negfactor;
#endif
						}
						// Add '0' to the number of leading zeros, or reset the counter
						if (c1 == '0') lz1++; else lz1 = 0;
						if (c2 == '0') lz2++; else lz2 = 0;

						// Now both digits are the same, try with the next to the left
						numpos1--;
						numpos2--;
					}
					while (numpos1 >= pos1 || numpos2 >= pos2);
					// Loop as long as one of the strings is still in the number

#if NATSORT_WITH_NEGATIVE
					if (negfactor == -1 && neg1 ^ neg2)
					{
						// Only one of both numbers is negative: make a decision
						if (neg1) return -1;
						return 1;
					}
#endif

					// Traversed both numbers, did we encounter a numeric difference?
					if (numcmp != 0) return numcmp;

					// We're still here, so both numbers have the same value
					// Is one of them longer? Then prefer that one (if not already prefering something)
					if (preference == 0 && lz1 > lz2) preference = -1;
					if (preference == 0 && lz1 < lz2) preference = 1;

					// Continue character-wise right after the last digit of the number:
					// Set the current pointer to the last digit position, it's increased later
					pos1 = endnum1;
					pos2 = endnum2;
				}

				// No decision yet, go to the next character
				pos1++;
				pos2++;
			}
			while (pos1 < len1 && pos2 < len2);
			// Out of the main loop:
			// Which of both strings (if any) would continue?

			if (pos1 < len1) return 1;   // s1 is longer than this
			if (pos2 < len2) return -1;   // s2 is longer than this

			// Both strings end here: Do we have a preference? If not, they're equal.
			return preference;
		}
	}
}
