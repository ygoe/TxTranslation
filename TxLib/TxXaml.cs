using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace TxLib
{
	#region Text translation markup extensions

	public class TExtension : MarkupExtension
	{
		#region Constructors

		/// <summary>
		/// Initialises a new instance of the TExtension class.
		/// </summary>
		/// <param name="key">Text key to translate.</param>
		public TExtension(string key)
		{
			Key = key;
			Count = -1;
		}

		/// <summary>
		/// Initialises a new instance of the TExtension class.
		/// </summary>
		/// <param name="key">Text key to translate.</param>
		/// <param name="count">Count value to consider when selecting the text value.</param>
		public TExtension(string key, int count)
		{
			Key = key;
			Count = count;
		}

		/// <summary>
		/// Initialises a new instance of the TExtension class.
		/// </summary>
		/// <param name="key">Text key to translate.</param>
		/// <param name="countBinding">Binding that provides the count value to consider when selecting the text value.</param>
		public TExtension(string key, Binding countBinding)
		{
			Key = key;
			CountBinding = countBinding;
			Count = -1;
		}

		#endregion Constructors

		#region Properties

		/// <summary>
		/// Gets or sets the text key to translate.
		/// </summary>
		public string Key { get; set; }
		
		/// <summary>
		/// Gets or sets the count value to consider when selecting the text value.
		/// </summary>
		public int Count { get; set; }

		/// <summary>
		/// Gets or sets the binding that provides the count value to consider when selecting the text value.
		/// </summary>
		public BindingBase CountBinding { get; set; }

		#endregion Properties

		#region Converter action

		protected virtual Func<string, int, string> GetTFunc()
		{
			return Tx.T;
		}

		#endregion Converter action

		#region MarkupExtension overrides

		public override object ProvideValue(IServiceProvider serviceProvider)
		{
			// Always create a dummy binding to be notified when the translation dictionary changes.
			Binding binding = new Binding("Dummy");
			binding.Source = DictionaryWatcher.Instance;
			binding.Mode = BindingMode.OneWay;

			if (CountBinding != null)
			{
				// A CountBinding has been set, so multiple bindings need to be combined.
				MultiBinding multiBinding = new MultiBinding();
				multiBinding.Mode = BindingMode.TwoWay;

				// Add the dummy binding as well as the binding for the count value.
				multiBinding.Bindings.Add(binding);
				multiBinding.Bindings.Add(CountBinding);

				// The converter will invoke the actual translation of the key and additional data.
				multiBinding.Converter = new TConverter(GetTFunc(), Key);
				return multiBinding.ProvideValue(serviceProvider);
			}
			else
			{
				// No CountBinding, so a simple binding will do.

				// The converter will invoke the actual translation of the key and additional data.
				binding.Converter = new TConverter(GetTFunc(), Key, Count);
				return binding.ProvideValue(serviceProvider);
			}
		}

		#endregion MarkupExtension overrides
	}

	public class UTExtension : TExtension
	{
		#region Constructors

		public UTExtension(string key)
			: base(key)
		{
		}

		public UTExtension(string key, int count)
			: base(key, count)
		{
		}

		public UTExtension(string key, Binding countBinding)
			: base(key, countBinding)
		{
		}

		#endregion Constructors

		#region Converter action

		protected override Func<string, int, string> GetTFunc()
		{
			return Tx.UT;
		}

		#endregion Converter action
	}

	public class TCExtension : TExtension
	{
		#region Constructors

		public TCExtension(string key)
			: base(key)
		{
		}

		public TCExtension(string key, int count)
			: base(key, count)
		{
		}

		public TCExtension(string key, Binding countBinding)
			: base(key, countBinding)
		{
		}

		#endregion Constructors

		#region Converter action

		protected override Func<string, int, string> GetTFunc()
		{
			return Tx.TC;
		}

		#endregion Converter action
	}

	public class UTCExtension : TExtension
	{
		#region Constructors

		public UTCExtension(string key)
			: base(key)
		{
		}

		public UTCExtension(string key, int count)
			: base(key, count)
		{
		}

		public UTCExtension(string key, Binding countBinding)
			: base(key, countBinding)
		{
		}

		#endregion Constructors

		#region Converter action

		protected override Func<string, int, string> GetTFunc()
		{
			return Tx.UTC;
		}

		#endregion Converter action
	}

	public class QTExtension : TExtension
	{
		#region Constructors

		public QTExtension(string key)
			: base(key)
		{
		}

		public QTExtension(string key, int count)
			: base(key, count)
		{
		}

		public QTExtension(string key, Binding countBinding)
			: base(key, countBinding)
		{
		}

		#endregion Constructors

		#region Converter action

		protected override Func<string, int, string> GetTFunc()
		{
			return Tx.QT;
		}

		#endregion Converter action
	}

	public class QTCExtension : TExtension
	{
		#region Constructors

		public QTCExtension(string key)
			: base(key)
		{
		}

		public QTCExtension(string key, int count)
			: base(key, count)
		{
		}

		public QTCExtension(string key, Binding countBinding)
			: base(key, countBinding)
		{
		}

		#endregion Constructors

		#region Converter action

		protected override Func<string, int, string> GetTFunc()
		{
			return Tx.QTC;
		}

		#endregion Converter action
	}

	public class QUTExtension : TExtension
	{
		#region Constructors

		public QUTExtension(string key)
			: base(key)
		{
		}

		public QUTExtension(string key, int count)
			: base(key, count)
		{
		}

		public QUTExtension(string key, Binding countBinding)
			: base(key, countBinding)
		{
		}

		#endregion Constructors

		#region Converter action

		protected override Func<string, int, string> GetTFunc()
		{
			return Tx.QUT;
		}

		#endregion Converter action
	}

	public class QUTCExtension : TExtension
	{
		#region Constructors

		public QUTCExtension(string key)
			: base(key)
		{
		}

		public QUTCExtension(string key, int count)
			: base(key, count)
		{
		}

		public QUTCExtension(string key, Binding countBinding)
			: base(key, countBinding)
		{
		}

		#endregion Constructors

		#region Converter action

		protected override Func<string, int, string> GetTFunc()
		{
			return Tx.QUTC;
		}

		#endregion Converter action
	}

	#endregion Text translation markup extensions

	#region Helper classes

	/// <summary>
	/// Dummy class to provide notifications when the dictionary has changed.
	/// </summary>
	class DictionaryWatcher : INotifyPropertyChanged
	{
		#region Static singleton access

		private static DictionaryWatcher instance;
		internal static DictionaryWatcher Instance
		{
			get
			{
				if (instance == null)
				{
					instance = new DictionaryWatcher();
				}
				return instance;
			}
		}

		#endregion Static singleton access

		#region Constructors

		private DictionaryWatcher()
		{
			Tx.DictionaryChanged += delegate(object sender, EventArgs e)
			{
				OnPropertyChanged("Dummy");
			};
		}

		#endregion Constructors

		#region Events

		public event PropertyChangedEventHandler PropertyChanged;

		private void OnPropertyChanged(string propertyName)
		{
			var handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		#endregion Events

		#region Properties

		public int Dummy
		{
			get { return 0; }
		}

		#endregion Properties
	}

	/// <summary>
	/// Converts the various binding values to a translated text string.
	/// </summary>
	class TConverter : IValueConverter, IMultiValueConverter
	{
		#region Private fields

		private string key;
		private int count;
		private Func<string, int, string> tFunc;

		#endregion Private fields

		#region Constructors

		/// <summary>
		/// Initialises a new instance of the TConverter class.
		/// </summary>
		/// <param name="key">Text key to translate.</param>
		public TConverter(Func<string, int, string> tFunc, string key)
		{
			this.tFunc = tFunc;
			this.key = key;
			this.count = -1;
		}

		/// <summary>
		/// Initialises a new instance of the TConverter class.
		/// </summary>
		/// <param name="key">Text key to translate.</param>
		/// <param name="count">Count value to consider when selecting the text value.</param>
		public TConverter(Func<string, int, string> tFunc, string key, int count)
		{
			this.tFunc = tFunc;
			this.key = key;
			this.count = count;
		}

		#endregion Constructors

		#region IValueConverter members

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			// Return the text key only in design mode. Nothing better to do for now.
			if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
			{
				return key;
			}

			// value is the Dummy binding, don't use it

			// Now translate the text and return the result.
			return tFunc(key, count);
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			return Binding.DoNothing;
		}

		#endregion IValueConverter members

		#region IMultiValueConverter members

		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			// Return the text key only in design mode. Nothing better to do for now.
			if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
			{
				return key;
			}

			// values[0] is the Dummy binding, don't use it

			// Read the count value from binding number two.
			int count = -1;
			if (values.Length > 1)
			{
				count = System.Convert.ToInt32(values[1]);
			}

			// Now translate the text and return the result.
			return tFunc(key, count);
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
		{
			return new object[0];
		}

		#endregion IMultiValueConverter members
	}

	#endregion Helper classes
}
