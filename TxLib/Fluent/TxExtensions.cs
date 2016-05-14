namespace Unclassified.TxLib.Fluent
{
    /// <summary>
    ///     Provides fluent builder for Tx API
    /// </summary>
    public static class TxExtensions
    {
        #region Static members

        /// <summary>
        ///     Extension method for strings to create <see cref="TxFluentBuilder" />
        /// </summary>
        /// <param name="key">Text key to search.</param>
        /// <returns>New TxFluentBuilder instance</returns>
        public static TxFluentBuilder Tx(this string key)
        {
            return new TxFluentBuilder(key ?? string.Empty);
        }

        /// <summary>
        ///     Extension method for strings to create <see cref="TxFluentBuilder" />. Ignores key case.
        /// </summary>
        /// <param name="key">Text key to search.</param>
        /// <returns>New TxFluentBuilder instance</returns>
        public static TxFluentBuilder Txi(this string key)
        {
            return Tx(key?.ToLower());
        }

        #endregion
    }
}