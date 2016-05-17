using System.Collections.Concurrent;
using System.Linq;

namespace Unclassified.TxLib.Fluent
{
	/// <summary>
	/// Provides a fluent syntax API for Tx.
	/// </summary>
	public class TxFluentBuilder
	{
		#region Static members

		/// <summary>
		/// Implicitly converts a <see cref="TxFluentBuilder"/> instance to a string that contains
		/// the translated text.
		/// </summary>
		/// <param name="builder">The builder instance.</param>
		/// <returns>Text value for the builder's text key.</returns>
		public static implicit operator string(TxFluentBuilder builder)
		{
			return builder.Make();
		}

		#endregion Static members

		#region Private fields

		private readonly ConcurrentDictionary<string, string> arguments = new ConcurrentDictionary<string, string>();
		private readonly string text;
		private int count = -1;
		private bool upperCase;
		private bool colon;
		private bool quoteNested;
		private bool quote;
		private bool parentheses;

		#endregion Private fields

		#region Constructors

		/// <summary>
		/// Initialises a new instance of the <see cref="TxFluentBuilder"/> class.
		/// </summary>
		/// <param name="text">The text key to search.</param>
		public TxFluentBuilder(string text)
		{
			this.text = text;
		}

		#endregion Constructors

		#region Public methods

		/// <summary>
		/// Adds a placeholder name and value.
		/// </summary>
		/// <param name="name">Placeholder name.</param>
		/// <param name="value">Placeholder value.</param>
		/// <returns>The builder for fluent configuration.</returns>
		public TxFluentBuilder Argument(string name, string value)
		{
			arguments.AddOrUpdate(name, value, (key, existing) => value);
			return this;
		}

		/// <summary>
		/// Sets the count value to consider when selecting the text value.
		/// </summary>
		/// <param name="count">Count value.</param>
		/// <returns>The builder for fluent configuration.</returns>
		public TxFluentBuilder Count(int count)
		{
			this.count = count;
			return this;
		}

		/// <summary>
		/// Transforms the first character of the text to upper case.
		/// </summary>
		/// <returns>The builder for fluent configuration.</returns>
		public TxFluentBuilder UpperCase()
		{
			upperCase = true;
			return this;
		}

		/// <summary>
		/// Adds a colon string after the text.
		/// </summary>
		/// <returns>The builder for fluent configuration.</returns>
		public TxFluentBuilder Colon()
		{
			colon = true;
			return this;
		}

		/// <summary>
		/// Encloses the text in nested quotation marks. These marks are used within normal
		/// quotation marks.
		/// </summary>
		/// <returns>The builder for fluent configuration.</returns>
		public TxFluentBuilder QuoteNested()
		{
			quoteNested = true;
			return this;
		}

		/// <summary>
		/// Encloses the text in normal quotation marks.
		/// </summary>
		/// <returns>The builder for fluent configuration.</returns>
		public TxFluentBuilder Quote()
		{
			quote = true;
			return this;
		}

		/// <summary>
		/// Encloses the text in parentheses (round brackets).
		/// </summary>
		/// <returns>The builder for fluent configuration.</returns>
		public TxFluentBuilder Parentheses()
		{
			parentheses = true;
			return this;
		}

		/// <summary>
		/// Performs the dictionary lookup and translates the text key based on the selected
		/// configuration.
		/// </summary>
		/// <returns>Text value for the builder's text key.</returns>
		public string Make()
		{
			var result = Tx.T(text, count, arguments.ToDictionary(p => p.Key, p => p.Value));
			if (upperCase) result = Tx.UpperCase(result);
			if (colon) result += Tx.Colon();
			if (quoteNested) result = Tx.QuoteNested(result);
			if (quote) result = Tx.Quote(result);
			if (parentheses) result = Tx.Parentheses(result);
			return result;
		}

		#endregion Public methods
	}
}
