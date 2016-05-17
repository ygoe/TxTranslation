namespace Unclassified.TxLib.Fluent
{
	/// <summary>
	/// Provides a fluent syntax builder for the Tx API.
	/// </summary>
	public static class TxExtensions
	{
		/// <summary>
		/// Creates a <see cref="TxFluentBuilder"/> from the text key string.
		/// </summary>
		/// <param name="key">The text key to search.</param>
		/// <returns>A new <see cref="TxFluentBuilder"/> instance.</returns>
		public static TxFluentBuilder Tx(this string key)
		{
			return new TxFluentBuilder(key);
		}
	}
}
