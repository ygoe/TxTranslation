using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Unclassified.Util
{
	/// <summary>
	/// Provides options and arguments parsing from command line arguments or a single string.
	/// </summary>
	internal class CommandLineHelper
	{
		#region Private data

		private string[] args;
		private List<Option> options = new List<Option>();
		private List<Argument> parsedArguments = new List<Argument>();

		#endregion Private data

		#region Constructor

		/// <summary>
		/// Initialises a new instance of the <see cref="CommandLineHelper"/> class.
		/// </summary>
		public CommandLineHelper()
		{
			AutoCompleteOptions = true;
		}

		#endregion Constructor

		#region Configuration properties

		/// <summary>
		/// Gets or sets a value indicating whether the option names are case-sensitive. (Default:
		/// false)
		/// </summary>
		public bool IsCaseSensitive { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether incomplete options can be automatically
		/// completed if there is only a single matching option. (Default: true)
		/// </summary>
		public bool AutoCompleteOptions { get; set; }

		#endregion Configuration properties

		#region Custom arguments line parsing

		/// <summary>
		/// Reads the command line arguments from a single string.
		/// </summary>
		/// <param name="argsString">The string that contains the entire command line.</param>
		public void ReadArgs(string argsString)
		{
			// Also posted here: http://stackoverflow.com/a/23961658/143684

			// Collects the split argument strings
			List<string> args = new List<string>();
			// Builds the current argument
			var currentArg = new StringBuilder();
			// Indicates whether the last character was a backslash escape character
			bool escape = false;
			// Indicates whether we're in a quoted range
			bool inQuote = false;
			// Indicates whether there were quotes in the current arguments
			bool hadQuote = false;
			// Remembers the previous character
			char prevCh = '\0';
			// Iterate all characters from the input string
			for (int i = 0; i < argsString.Length; i++)
			{
				char ch = argsString[i];
				if (ch == '\\' && !escape)
				{
					// Beginning of a backslash-escape sequence
					escape = true;
				}
				else if (ch == '\\' && escape)
				{
					// Double backslash, keep one
					currentArg.Append(ch);
					escape = false;
				}
				else if (ch == '"' && !escape)
				{
					// Toggle quoted range
					inQuote = !inQuote;
					hadQuote = true;
					if (inQuote && prevCh == '"')
					{
						// Doubled quote within a quoted range is like escaping
						currentArg.Append(ch);
					}
				}
				else if (ch == '"' && escape)
				{
					// Backslash-escaped quote, keep it
					currentArg.Append(ch);
					escape = false;
				}
				else if (char.IsWhiteSpace(ch) && !inQuote)
				{
					if (escape)
					{
						// Add pending escape char
						currentArg.Append('\\');
						escape = false;
					}
					// Accept empty arguments only if they are quoted
					if (currentArg.Length > 0 || hadQuote)
					{
						args.Add(currentArg.ToString());
					}
					// Reset for next argument
					currentArg.Clear();
					hadQuote = false;
				}
				else
				{
					if (escape)
					{
						// Add pending escape char
						currentArg.Append('\\');
						escape = false;
					}
					// Copy character from input, no special meaning
					currentArg.Append(ch);
				}
				prevCh = ch;
			}
			// Save last argument
			if (currentArg.Length > 0 || hadQuote)
			{
				args.Add(currentArg.ToString());
			}
		}

		#endregion Custom arguments line parsing

		#region Options management

		/// <summary>
		/// Registers a named option without additional parameters.
		/// </summary>
		/// <param name="name">The option name.</param>
		/// <returns>The option instance.</returns>
		public Option RegisterOption(string name)
		{
			return RegisterOption(name, 0);
		}

		/// <summary>
		/// Registers a named option.
		/// </summary>
		/// <param name="name">The option name.</param>
		/// <param name="parameterCount">The number of additional parameters for this option.</param>
		/// <returns>The option instance.</returns>
		public Option RegisterOption(string name, int parameterCount)
		{
			Option option = new Option(name, parameterCount);
			options.Add(option);
			return option;
		}

		#endregion Options management

		#region Parsing method

		/// <summary>
		/// Parses all command line arguments.
		/// </summary>
		public void Parse()
		{
			// Use args of the current process if no other source was given
			if (args == null)
			{
				args = Environment.GetCommandLineArgs();
				if (args.Length > 0)
				{
					// Skip myself (args[0])
					args = args.Skip(1).ToArray();
				}
			}

			// Clear/reset data
			parsedArguments.Clear();
			foreach (var option in options)
			{
				option.IsSet = false;
				option.SetCount = 0;
				option.Argument = null;
			}

			StringComparison strComp = IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
			var aw = new EnumerableWalker<string>(args);
			bool optMode = true;
			foreach (string arg in aw)
			{
				if (arg == "--")
				{
					optMode = false;
				}
				else if (optMode && (arg.StartsWith("/") || arg.StartsWith("-")))
				{
					string optName = arg.Substring(arg.StartsWith("--") ? 2 : 1);

					// Split option value if separated with : or = instead of whitespace
					int separatorIndex = optName.IndexOfAny(new[] { ':', '=' });
					string optValue = null;
					if (separatorIndex != -1)
					{
						optValue = optName.Substring(separatorIndex + 1);
						optName = optName.Substring(0, separatorIndex);
					}

					// Find the option with complete name match
					var option = options.FirstOrDefault(o => o.Names.Any(n => n.Equals(optName, strComp)));
					if (option == null)
					{
						// Try to complete the name to a unique registered option
						var matchingOptions = options.Where(o => o.Names.Any(n => n.StartsWith(optName, strComp))).ToList();
						if (AutoCompleteOptions && matchingOptions.Count > 1)
						{
							throw new Exception("Invalid option, completion is not unique: " + arg);
						}
						if (!AutoCompleteOptions || matchingOptions.Count == 0)
						{
							throw new Exception("Unknown option: " + arg);
						}
						// Accept the single auto-completed option
						option = matchingOptions[0];
					}

					// Check for single usage
					if (option.IsSingle && option.IsSet)
					{
						throw new Exception("Option cannot be set multiple times: " + arg);
					}

					// Collect option values from next argument strings
					string[] values = new string[option.ParameterCount];
					for (int i = 0; i < option.ParameterCount; i++)
					{
						if (optValue != null)
						{
							// The first value was included in this argument string
							values[i] = optValue;
							optValue = null;
						}
						else
						{
							// Fetch another argument string
							values[i] = aw.GetNext();
						}
						if (values[i] == null)
						{
							throw new Exception("Missing argument " + (i + 1) + " for option: " + arg);
						}
					}
					var argument = new Argument(option, values);

					// Set usage data on the option instance for quick access
					option.IsSet = true;
					option.SetCount++;
					option.Argument = argument;

					if (option.Action != null)
					{
						option.Action(argument);
					}
					else
					{
						parsedArguments.Add(argument);
					}
				}
				else
				{
					parsedArguments.Add(new Argument(null, new[] { arg }));
				}
			}

			var missingOption = options.FirstOrDefault(o => o.IsRequired && !o.IsSet);
			if (missingOption != null)
			{
				throw new Exception("Missing required option: /" + missingOption.Names[0]);
			}
		}

		#endregion Parsing method

		#region Parsed data properties

		/// <summary>
		/// Gets the parsed arguments.
		/// </summary>
		/// <remarks>
		/// To avoid exceptions thrown, call the <see cref="Parse"/> method in advance for
		/// exception handling.
		/// </remarks>
		public Argument[] Arguments
		{
			get
			{
				if (parsedArguments == null)
				{
					Parse();
				}
				return parsedArguments.ToArray();
			}
		}

		/// <summary>
		/// Gets the options that are set in the command line, including their value.
		/// </summary>
		/// <remarks>
		/// To avoid exceptions thrown, call the <see cref="Parse"/> method in advance for
		/// exception handling.
		/// </remarks>
		public Option[] SetOptions
		{
			get
			{
				if (parsedArguments == null)
				{
					Parse();
				}
				return parsedArguments.Where(a => a.Option != null).Select(a => a.Option).ToArray();
			}
		}

		/// <summary>
		/// Gets the free arguments that are set in the command line and don't belong to an option.
		/// </summary>
		/// <remarks>
		/// To avoid exceptions thrown, call the <see cref="Parse"/> method in advance for
		/// exception handling.
		/// </remarks>
		public string[] FreeArguments
		{
			get
			{
				if (parsedArguments == null)
				{
					Parse();
				}
				return parsedArguments.Where(a => a.Option == null).Select(a => a.Value).ToArray();
			}
		}

		#endregion Parsed data properties

		#region Nested classes for options and arguments

		/// <summary>
		/// Represents a named option.
		/// </summary>
		public class Option
		{
			/// <summary>
			/// Initialises a new instance of the <see cref="Option"/> class.
			/// </summary>
			/// <param name="name">The primary name of the option.</param>
			/// <param name="parameterCount">The number of additional parameters for this option.</param>
			internal Option(string name, int parameterCount)
			{
				this.Names = new List<string>() { name };
				this.ParameterCount = parameterCount;
			}

			/// <summary>
			/// Gets the names of this option.
			/// </summary>
			public List<string> Names { get; private set; }

			/// <summary>
			/// Gets the number of additional parameters for this option.
			/// </summary>
			public int ParameterCount { get; private set; }

			/// <summary>
			/// Gets a value indicating whether this option is required.
			/// </summary>
			public bool IsRequired { get; private set; }

			/// <summary>
			/// Gets a value indicating whether this option can only be specified once.
			/// </summary>
			public bool IsSingle { get; private set; }

			/// <summary>
			/// Gets the action to invoke when the option is set.
			/// </summary>
			public Action<Argument> Action { get; private set; }

			/// <summary>
			/// Gets a value indicating whether this option is set in the command line.
			/// </summary>
			public bool IsSet { get; internal set; }

			/// <summary>
			/// Gets the number of times that this option is set in the command line.
			/// </summary>
			public int SetCount { get; internal set; }

			/// <summary>
			/// Gets the <see cref="Argument"/> instance that contains additional parameters set
			/// for this option.
			/// </summary>
			public Argument Argument { get; internal set; }

			/// <summary>
			/// Gets the value of the <see cref="Argument"/> instance for this option.
			/// </summary>
			public string Value { get { return Argument != null ? Argument.Value : null; } }

			/// <summary>
			/// Sets alias names for this option.
			/// </summary>
			/// <param name="names">The alias names for this option.</param>
			/// <returns>The current <see cref="Option"/> instance.</returns>
			public Option Alias(params string[] names)
			{
				this.Names.AddRange(names);
				return this;
			}

			/// <summary>
			/// Marks this option as required. If a required option is not set in the command line,
			/// an exception is thrown on parsing.
			/// </summary>
			/// <returns>The current <see cref="Option"/> instance.</returns>
			public Option Required()
			{
				this.IsRequired = true;
				return this;
			}

			/// <summary>
			/// Marks this option as single. If a single option is set multiple times in the
			/// command line, an exception is thrown on parsing.
			/// </summary>
			/// <returns>The current <see cref="Option"/> instance.</returns>
			public Option Single()
			{
				this.IsSingle = true;
				return this;
			}

			/// <summary>
			/// Sets the action to invoke when the option is set.
			/// </summary>
			/// <param name="action">The action to invoke when the option is set.</param>
			/// <returns>The current <see cref="Option"/> instance.</returns>
			public Option Do(Action<Argument> action)
			{
				this.Action = action;
				return this;
			}
		}

		/// <summary>
		/// Represents a logical argument in the command line. Options with their additional
		/// parameters are combined in one argument.
		/// </summary>
		public class Argument
		{
			/// <summary>
			/// Initialises a new instance of the <see cref="Argument"/> class.
			/// </summary>
			/// <param name="option">The <see cref="Option"/> that is set in this argument; or null.</param>
			/// <param name="values">The additional parameter values for the option; or the argument value.</param>
			internal Argument(Option option, string[] values)
			{
				this.Option = option;
				this.Values = values;
			}

			/// <summary>
			/// Gets the <see cref="Option"/> that is set in this argument; or null.
			/// </summary>
			public Option Option { get; private set; }

			/// <summary>
			/// Gets the additional parameter values for the option; or the argument value.
			/// </summary>
			public string[] Values { get; private set; }

			/// <summary>
			/// Gets the first item of <see cref="Values"/>; or null.
			/// </summary>
			public string Value { get { return Values.Length > 0 ? Values[0] : null; } }
		}

		#endregion Nested classes for options and arguments
	}
}
