using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using Unclassified.FieldLog;

namespace Unclassified.Util
{
	/// <summary>
	/// Represents the method that handles a changed setting value.
	/// </summary>
	/// <param name="key">They setting key that has changed.</param>
	/// <param name="oldValue">The old value of that setting.</param>
	/// <param name="newValue">The new value of that setting.</param>
	public delegate void SettingChangedDelegate(string key, object oldValue, object newValue);

	/// <summary>
	/// Provides data for the LoadError event.
	/// </summary>
	public class SettingsErrorEventArgs : EventArgs
	{
		/// <summary>
		/// Initialises a new instance of the SettingsErrorEventArgs class.
		/// </summary>
		/// <param name="fileName">The name of the settings file that caused the error.</param>
		/// <param name="exception">The exception object that was raised.</param>
		public SettingsErrorEventArgs(string fileName, Exception exception)
		{
			this.FileName = fileName;
			this.Exception = exception;
		}

		/// <summary>
		/// Gets the name of the settings file that caused the error.
		/// </summary>
		public string FileName { get; private set; }

		/// <summary>
		/// Gets the exception object that was raised.
		/// </summary>
		public Exception Exception { get; private set; }
	}

	/// <summary>
	/// Represents a data store that contains all setting keys and values of a specified setting
	/// file. Derived classes can add properties for specific settings to provide a list of
	/// supported settings and define their data type and fallback value.
	/// </summary>
	/// <remarks>
	/// While this class is thread-safe enough to avoid data corruption, it may be blocking a bit
	/// too often and cause dead-locks. Use it with care and better avoid cross-thread access.
	/// </remarks>
	public class SettingsStore : IDisposable, INotifyPropertyChanged
	{
		#region Static events

		/// <summary>
		/// Raised when an error occurs while loading a settings file.
		/// </summary>
		public static event EventHandler<SettingsErrorEventArgs> LoadError;

		#endregion Static events

		#region Constants

		/// <summary>
		/// Delayed save timeout in milliseconds. The settings are saved to the file if no other
		/// value change occurs within the specified time.
		/// </summary>
		public const int SaveDelay = 1000;

		#endregion Constants

		#region Private data

		/// <summary>
		/// Internal synchronisation object.
		/// </summary>
		private object syncLock = new object();

		/// <summary>
		/// Name of the loaded settings file.
		/// </summary>
		private string fileName;

		/// <summary>
		/// Contains all keys and values that are currently in the settings file.
		/// </summary>
		private Dictionary<string, object> store;

		/// <summary>
		/// DelayedCall to save the settings back to the file.
		/// </summary>
		private DelayedCall delayedSave = null;

		/// <summary>
		/// List of setting changed handlers.
		/// </summary>
		private Dictionary<string, List<SettingChangedDelegate>> handlers;

		/// <summary>
		/// Indicates whether the settings file was opened in read-only mode. This prevents any
		/// write access to the settings and will never save the file back.
		/// </summary>
		private bool readOnly = false;

		/// <summary>
		/// File encryption password. Encryption is only used if the password is not null or empty.
		/// </summary>
		private string password;

		/// <summary>
		/// Indicates whether the instance has already been disposed.
		/// </summary>
		private bool isDisposed;

		#endregion Private data

		#region Constructors

		/// <summary>
		/// Initialises a new instance of the SettingsStore class.
		/// </summary>
		/// <param name="fileName">Name of the settings file to load.</param>
		public SettingsStore(string fileName)
			: this(fileName, null, false)
		{
		}

		/// <summary>
		/// Initialises a new instance of the SettingsStore class.
		/// </summary>
		/// <param name="fileName">Name of the settings file to load.</param>
		/// <param name="password">Encryption password of the settings file.
		/// Set null to not use encryption.</param>
		public SettingsStore(string fileName, string password)
			: this(fileName, password, false)
		{
		}

		/// <summary>
		/// Initialises a new instance of the SettingsStore class.
		/// </summary>
		/// <param name="fileName">Name of the settings file to load.</param>
		/// <param name="readOnly">true to open the settings file in read-only mode. This prevents
		/// any write access to the settings and will never save the file back.</param>
		public SettingsStore(string fileName, bool readOnly)
			: this(fileName, null, readOnly)
		{
		}

		/// <summary>
		/// Initialises a new instance of the SettingsStore class.
		/// </summary>
		/// <param name="fileName">Name of the settings file to load.</param>
		/// <param name="password">Encryption password of the settings file.
		/// Set null to not use encryption.</param>
		/// <param name="readOnly">true to open the settings file in read-only mode. This prevents
		/// any write access to the settings and will never save the file back.</param>
		public SettingsStore(string fileName, string password, bool readOnly)
		{
			store = new Dictionary<string, object>();
			handlers = new Dictionary<string, List<SettingChangedDelegate>>();
			this.password = password;
			this.readOnly = readOnly;
			Load(fileName);
		}

		#endregion Constructors

		#region Write access

		/// <summary>
		/// Checks whether the passed object is of a data type that can be stored in the settings
		/// file. Throws an ArgumentException if the data type is unsupported.
		/// </summary>
		/// <param name="newValue">Value to check.</param>
		private void CheckType(object newValue)
		{
			// Check for supported type
			if (!(newValue is string ||
				newValue is string[] ||
				newValue is int ||
				newValue is int[] ||
				newValue is long ||
				newValue is long[] ||
				newValue is double ||
				newValue is double[] ||
				newValue is bool ||
				newValue is bool[] ||
				newValue is DateTime ||
				newValue is DateTime[] ||
				newValue is TimeSpan ||
				newValue is TimeSpan[]))
			{
				throw new ArgumentException("The data type is not supported: " + newValue.GetType().ToString());
			}
		}

		/// <summary>
		/// Sets a setting key to a new value.
		/// </summary>
		/// <param name="key">The setting key to update.</param>
		/// <param name="newValue">The new value for that key.</param>
		public void Set(string key, object newValue)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");
				if (readOnly) throw new InvalidOperationException("This SettingsStore instance is created in read-only mode.");

				if (newValue == null)
				{
					Remove(key);
				}
				else
				{
					CheckType(newValue);

					object oldValue;
					store.TryGetValue(key, out oldValue);
					if (newValue.Equals(oldValue)) return;
					store[key] = newValue;
					InvokeHandlers(key, oldValue, newValue);

					if (delayedSave != null && delayedSave.IsWaiting) delayedSave.Cancel();
					if (delayedSave != null) delayedSave.Dispose();
					if (DelayedCall.SupportsSynchronization)
					{
						delayedSave = DelayedCall.Start(Save, SaveDelay);
					}
					else
					{
						delayedSave = DelayedCall.StartAsync(Save, SaveDelay);
					}
				}
			}
		}

		/// <summary>
		/// Sets a default value for a setting key and notifies all registered key handlers. The
		/// default value is written to the settings store and will be saved normally, only if it
		/// does not yet exist.
		/// </summary>
		/// <param name="key">The setting key to set the default value for.</param>
		/// <param name="newValue">The new default value for that key.</param>
		public void SetDefault(string key, object newValue)
		{
			lock (syncLock)
			{
				SetDefault(key, newValue, true);
			}
		}

		/// <summary>
		/// Sets a default value for a setting key. The default value is written to the settings
		/// store and will be saved normally, only if it does not yet exist.
		/// </summary>
		/// <param name="key">The setting key to set the default value for.</param>
		/// <param name="newValue">The new default value for that key.</param>
		/// <param name="notifyNow">true to notify all registered handlers for that key.</param>
		public void SetDefault(string key, object newValue, bool notifyNow)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");
				if (readOnly) throw new InvalidOperationException("This SettingsStore instance is created in read-only mode.");

				if (newValue == null)
				{
					Remove(key);
				}
				else
				{
					CheckType(newValue);

					object oldValue = null;
					if (store.ContainsKey(key)) return;
					store[key] = newValue;
					if (notifyNow) InvokeHandlers(key, oldValue, newValue);

					if (delayedSave != null && delayedSave.IsWaiting) delayedSave.Cancel();
					if (delayedSave != null) delayedSave.Dispose();
					if (DelayedCall.SupportsSynchronization)
					{
						delayedSave = DelayedCall.Start(Save, SaveDelay);
					}
					else
					{
						delayedSave = DelayedCall.StartAsync(Save, SaveDelay);
					}
				}
			}
		}

		/// <summary>
		/// Removes a setting key from the settings store.
		/// </summary>
		/// <param name="key">The setting key to remove.</param>
		public void Remove(string key)
		{
			lock (syncLock)
			{
				if (store.ContainsKey(key))
				{
					if (isDisposed) throw new ObjectDisposedException("");
					if (readOnly) throw new InvalidOperationException("This SettingsStore instance is created in read-only mode.");

					store.Remove(key);
					// Remove all handlers attached to this key
					RemoveHandler(key, null);
					if (delayedSave != null && delayedSave.IsWaiting) delayedSave.Cancel();
					if (delayedSave != null) delayedSave.Dispose();
					if (DelayedCall.SupportsSynchronization)
					{
						delayedSave = DelayedCall.Start(Save, SaveDelay);
					}
					else
					{
						delayedSave = DelayedCall.StartAsync(Save, SaveDelay);
					}
				}
			}
		}

		#endregion Write access

		#region Read access

		/// <summary>
		/// Gets all setting keys that are currently set in this settings store.
		/// </summary>
		/// <returns></returns>
		public string[] GetKeys()
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");

				string[] keys = new string[store.Keys.Count];
				store.Keys.CopyTo(keys, 0);
				return keys;
			}
		}

		/// <summary>
		/// Gets the data type of the value of a setting key.
		/// </summary>
		/// <param name="key">The setting key to determine the data type of.</param>
		/// <returns>The internal data type name.</returns>
		public string GetType(string key)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");

				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data == null) return "";
				if (data is string) return "string";
				if (data is string[]) return "string[]";
				if (data is int) return "int";
				if (data is int[]) return "int[]";
				if (data is long) return "long";
				if (data is long[]) return "long[]";
				if (data is double) return "double";
				if (data is double[]) return "double[]";
				if (data is bool) return "bool";
				if (data is bool[]) return "bool[]";
				if (data is DateTime) return "DateTime";
				if (data is DateTime[]) return "DateTime[]";
				if (data is TimeSpan) return "TimeSpan";
				if (data is TimeSpan[]) return "TimeSpan[]";
				return "?";
			}
		}

		/// <summary>
		/// Gets the current value of a setting key, or null if the key is unset.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		public object Get(string key)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");

				object data = null;
				if (store.ContainsKey(key)) data = store[key];
				return data;
			}
		}

		/// <summary>
		/// Gets the current string value of a setting key, or a fallback value if the key is unset.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <param name="fallbackValue">The fallback value to return if the key is unset.</param>
		/// <returns></returns>
		public string GetString(string key, string fallbackValue)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");

				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data == null) return fallbackValue;
				return Convert.ToString(data, CultureInfo.InvariantCulture);
			}
		}

		/// <summary>
		/// Gets the current string value of a setting key, or "" if the key is unset.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		public string GetString(string key)
		{
			lock (syncLock)
			{
				return GetString(key, "");
			}
		}

		/// <summary>
		/// Gets the current string[] value of a setting key, or an empty array if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		public string[] GetStringArray(string key)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");

				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data is string[]) return data as string[];
				return new string[] { };
			}
		}

		/// <summary>
		/// Gets the current int value of a setting key, or a fallback value if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <param name="fallbackValue">The fallback value to return if the key is unset.</param>
		/// <returns></returns>
		public int GetInt(string key, int fallbackValue)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");

				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data == null) return fallbackValue;
				try
				{
					return Convert.ToInt32(data, CultureInfo.InvariantCulture);
				}
				catch (FormatException)
				{
					return fallbackValue;
				}
			}
		}

		/// <summary>
		/// Gets the current int value of a setting key, or 0 if the key is unset or has an
		/// incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		public int GetInt(string key)
		{
			lock (syncLock)
			{
				return GetInt(key, 0);
			}
		}

		/// <summary>
		/// Gets the current int[] value of a setting key, or an empty array if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		public int[] GetIntArray(string key)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");

				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data is int[]) return data as int[];
				return new int[] { };
			}
		}

		/// <summary>
		/// Gets the current long value of a setting key, or a fallback value if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <param name="fallbackValue">The fallback value to return if the key is unset.</param>
		/// <returns></returns>
		public long GetLong(string key, long fallbackValue)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");

				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data == null) return fallbackValue;
				try
				{
					return Convert.ToInt64(data, CultureInfo.InvariantCulture);
				}
				catch (FormatException)
				{
					return fallbackValue;
				}
			}
		}

		/// <summary>
		/// Gets the current long value of a setting key, or 0 if the key is unset or has an
		/// incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		public long GetLong(string key)
		{
			lock (syncLock)
			{
				return GetLong(key, 0);
			}
		}

		/// <summary>
		/// Gets the current long[] value of a setting key, or an empty array if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		public long[] GetLongArray(string key)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");

				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data is long[]) return data as long[];
				return new long[] { };
			}
		}

		/// <summary>
		/// Gets the current double value of a setting key, or a fallback value if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <param name="fallbackValue">The fallback value to return if the key is unset.</param>
		/// <returns></returns>
		public double GetDouble(string key, double fallbackValue)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");

				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data == null) return fallbackValue;
				try
				{
					return Convert.ToDouble(data, CultureInfo.InvariantCulture);
				}
				catch (FormatException)
				{
					return fallbackValue;
				}
			}
		}

		/// <summary>
		/// Gets the current double value of a setting key, or NaN if the key is unset or has an
		/// incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		public double GetDouble(string key)
		{
			lock (syncLock)
			{
				return GetDouble(key, double.NaN);
			}
		}

		/// <summary>
		/// Gets the current double[] value of a setting key, or an empty array if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		public double[] GetDoubleArray(string key)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");

				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data is double[]) return data as double[];
				return new double[] { };
			}
		}

		/// <summary>
		/// Gets the current bool value of a setting key, or a fallback value if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <param name="fallbackValue">The fallback value to return if the key is unset.</param>
		/// <returns></returns>
		public bool GetBool(string key, bool fallbackValue)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");

				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data == null) return fallbackValue;
				if (data.ToString().Trim() == "1" ||
					data.ToString().Trim().ToLower() == "true") return true;
				if (data.ToString().Trim() == "0" ||
					data.ToString().Trim().ToLower() == "false") return false;
				return fallbackValue;
			}
		}

		/// <summary>
		/// Gets the current bool value of a setting key, or false if the key is unset or has an
		/// incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		public bool GetBool(string key)
		{
			lock (syncLock)
			{
				return GetBool(key, false);
			}
		}

		/// <summary>
		/// Gets the current bool[] value of a setting key, or an empty array if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		public bool[] GetBoolArray(string key)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");

				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data is bool[]) return data as bool[];
				return new bool[] { };
			}
		}

		/// <summary>
		/// Gets the current DateTime value of a setting key, or a fallback value if the key is
		/// unset or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <param name="fallbackValue">The fallback value to return if the key is unset.</param>
		/// <returns></returns>
		public DateTime GetDateTime(string key, DateTime fallbackValue)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");

				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data == null) return fallbackValue;
				if (data is DateTime) return (DateTime) data;
				return fallbackValue;
			}
		}

		/// <summary>
		/// Gets the current DateTime value of a setting key, or DateTime.MinValue if the key is
		/// unset or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		public DateTime GetDateTime(string key)
		{
			lock (syncLock)
			{
				return GetDateTime(key, DateTime.MinValue);
			}
		}

		/// <summary>
		/// Gets the current DateTime[] value of a setting key, or an empty array if the key is
		/// unset or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		public DateTime[] GetDateTimeArray(string key)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");

				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data is DateTime[]) return data as DateTime[];
				return new DateTime[] { };
			}
		}

		/// <summary>
		/// Gets the current TimeSpan value of a setting key, or a fallback value if the key is
		/// unset or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <param name="fallbackValue">The fallback value to return if the key is unset.</param>
		/// <returns></returns>
		public TimeSpan GetTimeSpan(string key, TimeSpan fallbackValue)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");

				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data == null) return fallbackValue;
				if (data is TimeSpan) return (TimeSpan) data;
				return fallbackValue;
			}
		}

		/// <summary>
		/// Gets the current TimeSpan value of a setting key, or TimeSpan.Zero if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		public TimeSpan GetTimeSpan(string key)
		{
			lock (syncLock)
			{
				return GetTimeSpan(key, TimeSpan.Zero);
			}
		}

		/// <summary>
		/// Gets the current TimeSpan[] value of a setting key, or an empty array if the key is
		/// unset or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		public TimeSpan[] GetTimeSpanArray(string key)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");

				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data is TimeSpan[]) return data as TimeSpan[];
				return new TimeSpan[] { };
			}
		}

		#endregion Read access

		#region Event notification

		/// <summary>
		/// Adds a value changed handler for a setting key that is mapped to a property of the same
		/// name. Derived classes can call this method to register the properties they define.
		/// </summary>
		/// <param name="propertyName">The property name and setting key.</param>
		public void AddPropertyHandler(string propertyName)
		{
			AddHandler(propertyName, () => OnPropertyChanged(propertyName));
		}

		/// <summary>
		/// Adds a value changed handler for a setting key.
		/// </summary>
		/// <param name="key">The setting key to observe. Set "" to add a handler for all keys.</param>
		/// <param name="handler">The handler method to invoke on a changed value.</param>
		public void AddHandler(string key, Action handler)
		{
			lock (syncLock)
			{
				AddHandler(key, delegate(string key_, object oldValue, object newValue) { handler(); }, false);
			}
		}

		/// <summary>
		/// Adds a value changed handler for a setting key.
		/// </summary>
		/// <param name="key">The setting key to observe. Set "" to add a handler for all keys.</param>
		/// <param name="handler">The handler method to invoke on a changed value.</param>
		public void AddHandler(string key, SettingChangedDelegate handler)
		{
			lock (syncLock)
			{
				AddHandler(key, handler, false);
			}
		}

		/// <summary>
		/// Adds a value changed handler for a setting key.
		/// </summary>
		/// <param name="key">The setting key to observe. Set "" to add a handler for all keys.</param>
		/// <param name="handler">The handler method to invoke on a changed value.</param>
		/// <param name="notifyNow">true to invoke the handler method immediately (not for an all-keys handler).</param>
		public void AddHandler(string key, SettingChangedDelegate handler, bool notifyNow)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");

				if (handlers.ContainsKey(key))
				{
					// The handlers list for this key exists.
					// Add the handler to the list if it's not already in it.
					if (!handlers[key].Contains(handler)) handlers[key].Add(handler);
				}
				else
				{
					// This is the first handler for the key.
					// Create the new list and add the handler.
					List<SettingChangedDelegate> list = new List<SettingChangedDelegate>();
					list.Add(handler);
					handlers.Add(key, list);
				}
				if (notifyNow && key != "")
				{
					if (store.ContainsKey(key))
						handler(key, null, store[key]);
					else
						handler(key, null, null);
				}
			}
		}

		/// <summary>
		/// Removes a value changed handler for a setting key.
		/// </summary>
		/// <param name="key">The setting key to observe. Set "" to add a handler for all keys.</param>
		/// <param name="handler">The handler method to invoke on a changed value.</param>
		public void RemoveHandler(string key, SettingChangedDelegate handler)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");

				if (handlers.ContainsKey(key))
				{
					if (handler != null)
					{
						if (handlers[key].Contains(handler)) handlers[key].Remove(handler);
					}
					else
					{
						handlers[key].Clear();
					}
				}
			}
		}

		/// <summary>
		/// Invokes all registered value changed handlers for a setting key.
		/// </summary>
		/// <param name="key">They setting key that has changed.</param>
		/// <param name="oldValue">The old value of that setting.</param>
		/// <param name="newValue">The new value of that setting.</param>
		public void InvokeHandlers(string key, object oldValue, object newValue)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");

				// Call handlers for this key only
				if (key != "")
				{
					if (handlers.ContainsKey(key))
					{
						foreach (SettingChangedDelegate handler in handlers[key])
						{
							if (handler != null) handler(key, oldValue, newValue);
						}
					}
				}
				// Call global handlers
				if (handlers.ContainsKey(""))
				{
					foreach (SettingChangedDelegate handler in handlers[""])
					{
						if (handler != null) handler(key, oldValue, newValue);
					}
				}
			}
		}

		#endregion Event notification

		#region Loading and saving

		/// <summary>
		/// Loads all settings from a file.
		/// </summary>
		/// <param name="fileName">Name of the settings file to load.</param>
		private void Load(string fileName)
		{
			lock (syncLock)
			{
				this.fileName = fileName;
				try
				{
					store.Clear();
					XmlDocument xdoc = new XmlDocument();

					if (!string.IsNullOrEmpty(this.password))
					{
						FL.Trace("SettingsStore.Load", "fileName = " + fileName + "\nWith password");
						using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read))
						{
							byte[] salt = new byte[8];
							fs.Read(salt, 0, salt.Length);
							Rfc2898DeriveBytes keyGenerator = new Rfc2898DeriveBytes(this.password, salt);

							using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
							{
								aes.Key = keyGenerator.GetBytes(aes.KeySize / 8);
								aes.IV = keyGenerator.GetBytes(aes.BlockSize / 8);

								using (CryptoStream cs = new CryptoStream(fs, aes.CreateDecryptor(), CryptoStreamMode.Read))
								using (StreamReader sr = new StreamReader(cs))
								{
									xdoc.Load(sr);
									//string data = sr.ReadToEnd();
									//xdoc.LoadXml(data);
								}
							}
						}
					}
					else
					{
						FL.Trace("SettingsStore.Load", "fileName = " + fileName + "\nNo password");
						using (StreamReader sr = new StreamReader(fileName))
						{
							xdoc.Load(sr);
						}
					}

					if (xdoc.DocumentElement.Name != "settings") throw new XmlException("Invalid XML root element");
					foreach (XmlNode xn in xdoc.DocumentElement.ChildNodes)
					{
						if (xn.Name == "entry")
						{
							string key = xn.Attributes["key"].Value.Trim();
							if (key == "") throw new XmlException("Empty entry key");

							if (xn.Attributes["type"].Value == "string")
							{
								store.Add(key, xn.InnerText);
							}
							else if (xn.Attributes["type"].Value == "string[]" ||
								xn.Attributes["type"].Value == "string-array")
							{
								List<string> l = new List<string>();
								foreach (XmlNode n in xn.SelectNodes("item"))
								{
									l.Add(n.InnerText);
								}
								store.Add(key, l.ToArray());
							}
							else if (xn.Attributes["type"].Value == "int")
							{
								store.Add(key, int.Parse(xn.InnerText));
							}
							else if (xn.Attributes["type"].Value == "int[]" ||
								xn.Attributes["type"].Value == "int-array")
							{
								List<int> l = new List<int>();
								foreach (XmlNode n in xn.SelectNodes("item"))
								{
									if (n.InnerText == "")
										l.Add(0);
									else
										l.Add(int.Parse(n.InnerText));
								}
								store.Add(key, l.ToArray());
							}
							else if (xn.Attributes["type"].Value == "long")
							{
								store.Add(key, long.Parse(xn.InnerText));
							}
							else if (xn.Attributes["type"].Value == "long[]" ||
								xn.Attributes["type"].Value == "long-array")
							{
								List<long> l = new List<long>();
								foreach (XmlNode n in xn.SelectNodes("item"))
								{
									if (n.InnerText == "")
										l.Add(0);
									else
										l.Add(long.Parse(n.InnerText));
								}
								store.Add(key, l.ToArray());
							}
							else if (xn.Attributes["type"].Value == "double")
							{
								store.Add(key, double.Parse(xn.InnerText, CultureInfo.InvariantCulture));
							}
							else if (xn.Attributes["type"].Value == "double[]" ||
								xn.Attributes["type"].Value == "double-array")
							{
								List<double> l = new List<double>();
								foreach (XmlNode n in xn.SelectNodes("item"))
								{
									if (n.InnerText == "")
										l.Add(0);
									else
										l.Add(double.Parse(n.InnerText, CultureInfo.InvariantCulture));
								}
								store.Add(key, l.ToArray());
							}
							else if (xn.Attributes["type"].Value == "bool")
							{
								if (xn.InnerText.ToString().Trim() == "1" ||
									xn.InnerText.ToString().Trim().ToLower() == "true") store.Add(key, true);
								else if (xn.InnerText.ToString().Trim() == "0" ||
									xn.InnerText.ToString().Trim().ToLower() == "false") store.Add(key, false);
								else throw new FormatException("Invalid bool value");
							}
							else if (xn.Attributes["type"].Value == "bool[]" ||
								xn.Attributes["type"].Value == "bool-array")
							{
								List<bool> l = new List<bool>();
								foreach (XmlNode n in xn.SelectNodes("item"))
								{
									if (n.InnerText.ToString().Trim() == "1" ||
										n.InnerText.ToString().Trim().ToLower() == "true") l.Add(true);
									else if (n.InnerText.ToString().Trim() == "0" ||
										n.InnerText.ToString().Trim().ToLower() == "false") l.Add(false);
									else throw new FormatException("Invalid bool value");
								}
								store.Add(key, l.ToArray());
							}
							else if (xn.Attributes["type"].Value == "DateTime")
							{
								store.Add(key, new DateTime(long.Parse(xn.InnerText)));
							}
							else if (xn.Attributes["type"].Value == "DateTime[]")
							{
								List<DateTime> l = new List<DateTime>();
								foreach (XmlNode n in xn.SelectNodes("item"))
								{
									if (n.InnerText == "")
										l.Add(DateTime.MinValue);
									else
										l.Add(new DateTime(long.Parse(n.InnerText)));
								}
								store.Add(key, l.ToArray());
							}
							else if (xn.Attributes["type"].Value == "TimeSpan")
							{
								store.Add(key, new TimeSpan(long.Parse(xn.InnerText)));
							}
							else if (xn.Attributes["type"].Value == "TimeSpan[]")
							{
								List<TimeSpan> l = new List<TimeSpan>();
								foreach (XmlNode n in xn.SelectNodes("item"))
								{
									if (n.InnerText == "")
										l.Add(TimeSpan.Zero);
									else
										l.Add(new TimeSpan(long.Parse(n.InnerText)));
								}
								store.Add(key, l.ToArray());
							}
							else
							{
								throw new XmlException("Invalid type value");
							}
						}
					}
				}
				catch (DirectoryNotFoundException)
				{
					FL.Trace("SettingsStore.Load: DirectoryNotFoundException, no settings loaded");
				}
				catch (FileNotFoundException)
				{
					FL.Trace("SettingsStore.Load: FileNotFoundException, no settings loaded");
				}
				catch (FormatException ex)
				{
					HandleBrokenFile(ex);
				}
				catch (XmlException ex)
				{
					HandleBrokenFile(ex);
				}
			}
		}

		/// <summary>
		/// Handles a broken settings file. Renames the file, clears the settings store and raises
		/// the LoadError event so that the application can log the error.
		/// </summary>
		/// <param name="ex"></param>
		private void HandleBrokenFile(Exception ex)
		{
			FL.Warning(ex, "Loading settings file");
			store.Clear();
			try
			{
				File.Delete(this.fileName + ".broken");
				File.Move(this.fileName, this.fileName + ".broken");
				FL.Trace("Broken settings file renamed", "New file name: " + this.fileName + ".broken");
			}
			catch (Exception ex2)
			{
				FL.Warning(ex2, "Renaming broken settings file");
				// Best-effort. If it fails, do nothing.
			}

			EventHandler<SettingsErrorEventArgs> handler = LoadError;
			if (handler != null)
			{
				handler(this, new SettingsErrorEventArgs(this.fileName, ex));
			}
		}

		/// <summary>
		/// Saves all settings to the file immediately.
		/// </summary>
		public void SaveNow()
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");

				if (delayedSave != null && delayedSave.IsWaiting)
				{
					delayedSave.Cancel();
					Save();
				}
			}
		}

		/// <summary>
		/// Saves all settings to the file.
		/// </summary>
		private void Save()
		{
			lock (syncLock)
			{
				if (isDisposed) return;
				if (readOnly) throw new InvalidOperationException("This SettingsStore instance is created in read-only mode.");

				List<string> listKeys = new List<string>(store.Keys);
				listKeys.Sort();

				XmlDocument xdoc = new XmlDocument();
				XmlNode root = xdoc.CreateElement("settings");
				xdoc.AppendChild(root);
				foreach (string key in listKeys)
				{
					XmlNode xn;
					XmlAttribute xa;

					xn = xdoc.CreateElement("entry");
					xa = xdoc.CreateAttribute("key");
					xa.Value = key;
					xn.Attributes.Append(xa);

					if (store[key] is string)
					{
						xa = xdoc.CreateAttribute("type");
						xa.Value = "string";
						xn.Attributes.Append(xa);
						xn.InnerText = GetString(key);
					}
					else if (store[key] is string[])
					{
						xa = xdoc.CreateAttribute("type");
						xa.Value = "string[]";
						xn.Attributes.Append(xa);
						string[] sa = (string[]) store[key];
						foreach (string s in sa)
						{
							XmlNode n = xdoc.CreateElement("item");
							n.InnerText = s;
							xn.AppendChild(n);
						}
					}
					else if (store[key] is int)
					{
						xa = xdoc.CreateAttribute("type");
						xa.Value = "int";
						xn.Attributes.Append(xa);
						xn.InnerText = GetInt(key).ToString();
					}
					else if (store[key] is int[])
					{
						xa = xdoc.CreateAttribute("type");
						xa.Value = "int[]";
						xn.Attributes.Append(xa);
						int[] ia = (int[]) store[key];
						foreach (int i in ia)
						{
							XmlNode n = xdoc.CreateElement("item");
							n.InnerText = i.ToString();
							xn.AppendChild(n);
						}
					}
					else if (store[key] is long)
					{
						xa = xdoc.CreateAttribute("type");
						xa.Value = "long";
						xn.Attributes.Append(xa);
						xn.InnerText = GetLong(key).ToString();
					}
					else if (store[key] is long[])
					{
						xa = xdoc.CreateAttribute("type");
						xa.Value = "long[]";
						xn.Attributes.Append(xa);
						long[] la = (long[]) store[key];
						foreach (long l in la)
						{
							XmlNode n = xdoc.CreateElement("item");
							n.InnerText = l.ToString();
							xn.AppendChild(n);
						}
					}
					else if (store[key] is double)
					{
						xa = xdoc.CreateAttribute("type");
						xa.Value = "double";
						xn.Attributes.Append(xa);
						xn.InnerText = GetDouble(key).ToString(CultureInfo.InvariantCulture);
					}
					else if (store[key] is double[])
					{
						xa = xdoc.CreateAttribute("type");
						xa.Value = "double[]";
						xn.Attributes.Append(xa);
						double[] da = (double[]) store[key];
						foreach (double d in da)
						{
							XmlNode n = xdoc.CreateElement("item");
							n.InnerText = d.ToString(CultureInfo.InvariantCulture);
							xn.AppendChild(n);
						}
					}
					else if (store[key] is bool)
					{
						xa = xdoc.CreateAttribute("type");
						xa.Value = "bool";
						xn.Attributes.Append(xa);
						xn.InnerText = GetBool(key) ? "true" : "false";
					}
					else if (store[key] is bool[])
					{
						xa = xdoc.CreateAttribute("type");
						xa.Value = "bool[]";
						xn.Attributes.Append(xa);
						bool[] ba = (bool[]) store[key];
						foreach (bool b in ba)
						{
							XmlNode n = xdoc.CreateElement("item");
							n.InnerText = b ? "true" : "false";
							xn.AppendChild(n);
						}
					}
					else if (store[key] is DateTime)
					{
						xa = xdoc.CreateAttribute("type");
						xa.Value = "DateTime";
						xn.Attributes.Append(xa);
						xn.InnerText = GetDateTime(key).Ticks.ToString();
					}
					else if (store[key] is DateTime[])
					{
						xa = xdoc.CreateAttribute("type");
						xa.Value = "DateTime[]";
						xn.Attributes.Append(xa);
						DateTime[] da = (DateTime[]) store[key];
						foreach (DateTime d in da)
						{
							XmlNode n = xdoc.CreateElement("item");
							n.InnerText = d.Ticks.ToString();
							xn.AppendChild(n);
						}
					}
					else if (store[key] is TimeSpan)
					{
						xa = xdoc.CreateAttribute("type");
						xa.Value = "TimeSpan";
						xn.Attributes.Append(xa);
						xn.InnerText = GetTimeSpan(key).Ticks.ToString();
					}
					else if (store[key] is TimeSpan[])
					{
						xa = xdoc.CreateAttribute("type");
						xa.Value = "TimeSpan[]";
						xn.Attributes.Append(xa);
						TimeSpan[] ta = (TimeSpan[]) store[key];
						foreach (TimeSpan t in ta)
						{
							XmlNode n = xdoc.CreateElement("item");
							n.InnerText = t.Ticks.ToString();
							xn.AppendChild(n);
						}
					}
					else
					{
						// Internal error, cannot save this store entry
						continue;
					}

					root.AppendChild(xn);
				}

				FL.Trace("SettingsStore.Save", "fileName = " + fileName);
				if (!Directory.Exists(Path.GetDirectoryName(fileName)))
				{
					FL.Trace("SettingsStore.Save: Creating directory");
					Directory.CreateDirectory(Path.GetDirectoryName(fileName));
				}

				XmlWriterSettings xws = new XmlWriterSettings();
				xws.Encoding = Encoding.UTF8;
				xws.Indent = true;
				xws.IndentChars = "\t";
				xws.OmitXmlDeclaration = false;

				if (!string.IsNullOrEmpty(this.password))
				{
					byte[] salt = new byte[8];
					new RNGCryptoServiceProvider().GetBytes(salt);
					Rfc2898DeriveBytes keyGenerator = new Rfc2898DeriveBytes(this.password, salt);

					using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
					{
						aes.Key = keyGenerator.GetBytes(aes.KeySize / 8);
						aes.IV = keyGenerator.GetBytes(aes.BlockSize / 8);

						using (FileStream fs = new FileStream(fileName, FileMode.Create, FileAccess.Write))
						using (CryptoStream cs = new CryptoStream(fs, aes.CreateEncryptor(), CryptoStreamMode.Write))
						using (StreamWriter sw = new StreamWriter(cs, Encoding.UTF8))
						{
							fs.Write(salt, 0, salt.Length);

							XmlWriter writer = XmlWriter.Create(sw, xws);
							xdoc.Save(writer);
							writer.Close();
						}
					}
				}
				else
				{
					XmlWriter writer = XmlWriter.Create(fileName, xws);
					xdoc.Save(writer);
					writer.Close();
				}
			}
		}

		#endregion Loading and saving

		#region IDisposable members

		/// <summary>
		/// Saves all settings to the file and frees all resources.
		/// </summary>
		public void Dispose()
		{
			lock (syncLock)
			{
				if (!isDisposed)
				{
					SaveNow();
					isDisposed = true;
					store.Clear();
					handlers.Clear();
					if (delayedSave != null) delayedSave.Dispose();
					delayedSave = null;
				}
			}
		}

		#endregion IDisposable members

		#region INotifyPropertyChanged members

		/// <summary>
		/// Occurs when a property value changes.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Raises the PropertyChanged event.
		/// </summary>
		/// <param name="propertyName">Name of the property that has changed.</param>
		protected void OnPropertyChanged(string propertyName)
		{
			var handler = this.PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		#endregion INotifyPropertyChanged members
	}

	/// <summary>
	/// Represents a view on a SettingsStore instance that defines a common key prefix. Derived
	/// classes can add properties for specific settings with this key prefix to provide a list of
	/// supported settings and define their data type and fallback value.
	/// </summary>
	public class SettingsView : INotifyPropertyChanged
	{
		#region Constants

		/// <summary>
		/// Defines the separator of view levels in the setting keys.
		/// </summary>
		public const string Separator = ".";

		#endregion Constants

		#region Private data

		/// <summary>
		/// The settings store behind this view.
		/// </summary>
		private SettingsStore settings;

		/// <summary>
		/// The full key prefix of this view, including the trailing separator.
		/// </summary>
		private string fullPrefix;

		#endregion Private data

		#region Constructors

		/// <summary>
		/// Initialises a new instance of the SettingsView class.
		/// </summary>
		/// <param name="settings">The settings store behind this view.</param>
		/// <param name="prefix">The partial prefix for this view level.</param>
		public SettingsView(SettingsStore settings, string prefix)
		{
			this.settings = settings;
			this.fullPrefix = prefix + Separator;
		}

		/// <summary>
		/// Initialises a new instance of the SettingsView class.
		/// </summary>
		/// <param name="view">The settings view that this view is based on.</param>
		/// <param name="prefix">The partial prefix for this view level.</param>
		public SettingsView(SettingsView view, string prefix)
		{
			this.settings = view.settings;
			this.fullPrefix = view.fullPrefix + prefix + Separator;
		}

		#endregion Constructors

		#region Write access

		/// <summary>
		/// Sets a setting key to a new value.
		/// </summary>
		/// <param name="subkey">The setting subkey to update.</param>
		/// <param name="newValue">The new value for that key.</param>
		public void Set(string subkey, object newValue)
		{
			settings.Set(this.fullPrefix + subkey, newValue);
		}

		/// <summary>
		/// Sets a default value for a setting key and notifies all registered key handlers. The
		/// default value is written to the settings store and will be saved normally, only if it
		/// does not yet exist.
		/// </summary>
		/// <param name="subkey">The setting subkey to set the default value for.</param>
		/// <param name="newValue">The new default value for that key.</param>
		public void SetDefault(string subkey, object newValue)
		{
			settings.SetDefault(this.fullPrefix + subkey, newValue);
		}

		/// <summary>
		/// Sets a default value for a setting key. The default value is written to the settings
		/// store and will be saved normally, only if it does not yet exist.
		/// </summary>
		/// <param name="subkey">The setting subkey to set the default value for.</param>
		/// <param name="newValue">The new default value for that key.</param>
		/// <param name="notifyNow">true to notify all registered handlers for that key.</param>
		public void SetDefault(string subkey, object newValue, bool notifyNow)
		{
			settings.SetDefault(this.fullPrefix + subkey, newValue, notifyNow);
		}

		/// <summary>
		/// Removes a setting key from the settings store.
		/// </summary>
		/// <param name="subkey">The setting subkey to remove.</param>
		public void Remove(string subkey)
		{
			settings.Remove(this.fullPrefix + subkey);
		}

		#endregion Write access

		#region Read access

		/// <summary>
		/// Gets the data type of the value of a setting key.
		/// </summary>
		/// <param name="subkey">The setting subkey to determine the data type of.</param>
		/// <returns>The internal data type name.</returns>
		public string GetType(string subkey)
		{
			return settings.GetType(this.fullPrefix + subkey);
		}

		/// <summary>
		/// Gets the current value of a setting key, or null if the key is unset.
		/// </summary>
		/// <param name="subkey">The setting subkey.</param>
		/// <returns></returns>
		public object Get(string subkey)
		{
			return settings.Get(this.fullPrefix + subkey);
		}

		/// <summary>
		/// Gets the current string value of a setting key, or a fallback value if the key is unset.
		/// </summary>
		/// <param name="subkey">The setting subkey.</param>
		/// <param name="fallbackValue">The fallback value to return if the key is unset.</param>
		/// <returns></returns>
		public string GetString(string subkey, string fallbackValue)
		{
			return settings.GetString(this.fullPrefix + subkey, fallbackValue);
		}

		/// <summary>
		/// Gets the current string value of a setting key, or "" if the key is unset.
		/// </summary>
		/// <param name="subkey">The setting subkey.</param>
		/// <returns></returns>
		public string GetString(string subkey)
		{
			return settings.GetString(this.fullPrefix + subkey);
		}

		/// <summary>
		/// Gets the current string[] value of a setting key, or an empty array if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="subkey">The setting subkey.</param>
		/// <returns></returns>
		public string[] GetStringArray(string subkey)
		{
			return settings.GetStringArray(this.fullPrefix + subkey);
		}

		/// <summary>
		/// Gets the current int value of a setting key, or a fallback value if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="subkey">The setting subkey.</param>
		/// <param name="fallbackValue">The fallback value to return if the key is unset.</param>
		/// <returns></returns>
		public int GetInt(string subkey, int fallbackValue)
		{
			return settings.GetInt(this.fullPrefix + subkey, fallbackValue);
		}

		/// <summary>
		/// Gets the current int value of a setting key, or 0 if the key is unset or has an
		/// incompatible data type.
		/// </summary>
		/// <param name="subkey">The setting subkey.</param>
		/// <returns></returns>
		public int GetInt(string subkey)
		{
			return settings.GetInt(this.fullPrefix + subkey);
		}

		/// <summary>
		/// Gets the current int[] value of a setting key, or an empty array if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="subkey">The setting subkey.</param>
		/// <returns></returns>
		public int[] GetIntArray(string subkey)
		{
			return settings.GetIntArray(this.fullPrefix + subkey);
		}

		/// <summary>
		/// Gets the current long value of a setting key, or a fallback value if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="subkey">The setting subkey.</param>
		/// <param name="fallbackValue">The fallback value to return if the key is unset.</param>
		/// <returns></returns>
		public long GetLong(string subkey, long fallbackValue)
		{
			return settings.GetLong(this.fullPrefix + subkey, fallbackValue);
		}

		/// <summary>
		/// Gets the current long value of a setting key, or 0 if the key is unset or has an
		/// incompatible data type.
		/// </summary>
		/// <param name="subkey">The setting subkey.</param>
		/// <returns></returns>
		public long GetLong(string subkey)
		{
			return settings.GetLong(this.fullPrefix + subkey);
		}

		/// <summary>
		/// Gets the current long[] value of a setting key, or an empty array if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="subkey">The setting subkey.</param>
		/// <returns></returns>
		public long[] GetLongArray(string subkey)
		{
			return settings.GetLongArray(this.fullPrefix + subkey);
		}

		/// <summary>
		/// Gets the current double value of a setting key, or a fallback value if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="subkey">The setting subkey.</param>
		/// <param name="fallbackValue">The fallback value to return if the key is unset.</param>
		/// <returns></returns>
		public double GetDouble(string subkey, double fallbackValue)
		{
			return settings.GetDouble(this.fullPrefix + subkey, fallbackValue);
		}

		/// <summary>
		/// Gets the current double value of a setting key, or NaN if the key is unset or has an
		/// incompatible data type.
		/// </summary>
		/// <param name="subkey">The setting subkey.</param>
		/// <returns></returns>
		public double GetDouble(string subkey)
		{
			return settings.GetDouble(this.fullPrefix + subkey);
		}

		/// <summary>
		/// Gets the current double[] value of a setting key, or an empty array if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="subkey">The setting subkey.</param>
		/// <returns></returns>
		public double[] GetDoubleArray(string subkey)
		{
			return settings.GetDoubleArray(this.fullPrefix + subkey);
		}

		/// <summary>
		/// Gets the current bool value of a setting key, or a fallback value if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="subkey">The setting subkey.</param>
		/// <param name="fallbackValue">The fallback value to return if the key is unset.</param>
		/// <returns></returns>
		public bool GetBool(string subkey, bool fallbackValue)
		{
			return settings.GetBool(this.fullPrefix + subkey, fallbackValue);
		}

		/// <summary>
		/// Gets the current bool value of a setting key, or false if the key is unset or has an
		/// incompatible data type.
		/// </summary>
		/// <param name="subkey">The setting subkey.</param>
		/// <returns></returns>
		public bool GetBool(string subkey)
		{
			return settings.GetBool(this.fullPrefix + subkey);
		}

		/// <summary>
		/// Gets the current bool[] value of a setting key, or an empty array if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="subkey">The setting subkey.</param>
		/// <returns></returns>
		public bool[] GetBoolArray(string subkey)
		{
			return settings.GetBoolArray(this.fullPrefix + subkey);
		}

		/// <summary>
		/// Gets the current DateTime value of a setting key, or a fallback value if the key is
		/// unset or has an incompatible data type.
		/// </summary>
		/// <param name="subkey">The setting subkey.</param>
		/// <param name="fallbackValue">The fallback value to return if the key is unset.</param>
		/// <returns></returns>
		public DateTime GetDateTime(string subkey, DateTime fallbackValue)
		{
			return settings.GetDateTime(this.fullPrefix + subkey, fallbackValue);
		}

		/// <summary>
		/// Gets the current DateTime value of a setting key, or DateTime.MinValue if the key is
		/// unset or has an incompatible data type.
		/// </summary>
		/// <param name="subkey">The setting subkey.</param>
		/// <returns></returns>
		public DateTime GetDateTime(string subkey)
		{
			return settings.GetDateTime(this.fullPrefix + subkey);
		}

		/// <summary>
		/// Gets the current DateTime[] value of a setting key, or an empty array if the key is
		/// unset or has an incompatible data type.
		/// </summary>
		/// <param name="subkey">The setting subkey.</param>
		/// <returns></returns>
		public DateTime[] GetDateTimeArray(string subkey)
		{
			return settings.GetDateTimeArray(this.fullPrefix + subkey);
		}

		/// <summary>
		/// Gets the current TimeSpan value of a setting key, or a fallback value if the key is
		/// unset or has an incompatible data type.
		/// </summary>
		/// <param name="subkey">The setting subkey.</param>
		/// <param name="fallbackValue">The fallback value to return if the key is unset.</param>
		/// <returns></returns>
		public TimeSpan GetTimeSpan(string subkey, TimeSpan fallbackValue)
		{
			return settings.GetTimeSpan(this.fullPrefix + subkey, fallbackValue);
		}

		/// <summary>
		/// Gets the current TimeSpan value of a setting key, or TimeSpan.Zero if the key is unset
		/// or has an incompatible data type.
		/// </summary>
		/// <param name="subkey">The setting subkey.</param>
		/// <returns></returns>
		public TimeSpan GetTimeSpan(string subkey)
		{
			return settings.GetTimeSpan(this.fullPrefix + subkey);
		}

		/// <summary>
		/// Gets the current TimeSpan[] value of a setting key, or an empty array if the key is
		/// unset or has an incompatible data type.
		/// </summary>
		/// <param name="subkey">The setting subkey.</param>
		/// <returns></returns>
		public TimeSpan[] GetTimeSpanArray(string subkey)
		{
			return settings.GetTimeSpanArray(this.fullPrefix + subkey);
		}

		#endregion Read access

		#region Event notification

		/// <summary>
		/// Adds a value changed handler for a setting key that is mapped to a property of the same
		/// name. Derived classes can call this method to register the properties they define.
		/// </summary>
		/// <param name="propertyName">The property name and setting key.</param>
		public void AddPropertyHandler(string propertyName)
		{
			settings.AddHandler(this.fullPrefix + propertyName, () => OnPropertyChanged(propertyName));
		}

		#endregion Event notification

		#region INotifyPropertyChanged members

		/// <summary>
		/// Occurs when a property value changes.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Raises the PropertyChanged event.
		/// </summary>
		/// <param name="propertyName">Name of the property that has changed.</param>
		protected void OnPropertyChanged(string propertyName)
		{
			var handler = this.PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		#endregion INotifyPropertyChanged members
	}
}
