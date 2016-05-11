using System.Collections.Concurrent;
using System.Linq;

namespace Unclassified.TxLib.Fluent
{
    /// <summary>
    ///     Class wich provides fluent API for Tx
    /// </summary>
    public class TxFluentBuilder
    {
        #region Static members

        /// <summary>
        ///     Implicitly converts builder to string.
        /// </summary>
        /// <param name="builder">Builder instance.</param>
        /// <returns>String replacement for builder's key</returns>
        public static implicit operator string(TxFluentBuilder builder)
        {
            return builder.Make();
        }

        #endregion

        private readonly ConcurrentDictionary<string, string> _arguments;
        private readonly string _text;
        private bool _colon;
        private int? _count;
        private bool _upper;

        #region Constructors

        /// <summary>
        ///     Key string is atomic.
        /// </summary>
        /// <param name="text">Key to search.</param>
        public TxFluentBuilder(string text)
        {
            _text = text;

            _arguments = new ConcurrentDictionary<string, string>();
        }

        #endregion

        #region Members

        /// <summary>
        ///     Adds named argument to replace in formatted string.
        /// </summary>
        /// <param name="name">Replacement name.</param>
        /// <param name="value">Replacement value.</param>
        /// <returns>Builder for fluent configuration.</returns>
        public TxFluentBuilder Argument(string name, string value)
        {
            _arguments.AddOrUpdate(name, value, (key, existing) => value);
            return this;
        }

        /// <summary>
        ///     Setup colon string to putting after text.
        /// </summary>
        /// <returns>Builder for fluent configuration.</returns>
        public TxFluentBuilder Colon()
        {
            _colon = true;
            return this;
        }

        /// <summary>
        ///     Setup count value to consider when selecting the text value.
        /// </summary>
        /// <param name="count">Count value.</param>
        /// <returns>Builder for fluent configuration.</returns>
        public TxFluentBuilder Count(int count)
        {
            _count = count;
            return this;
        }

        /// <summary>
        ///     Creates replacement based on selected configuration.
        /// </summary>
        /// <returns>String replacement for builder's key</returns>
        public string Make()
        {
            var count = _count.HasValue ? _count.Value : -1;
            var result = Tx.T(_text, count, _arguments.ToDictionary(p => p.Key, p => p.Value));
            if (_upper) result = Tx.UpperCase(result);
            if (_colon) result += Tx.Colon();
            return result;
        }

        /// <summary>
        ///     Setup first character of a text transformation to the upper case.
        /// </summary>
        /// <returns>Builder for fluent configuration.</returns>
        public TxFluentBuilder Upper()
        {
            _upper = true;
            return this;
        }

        #endregion
    }
}