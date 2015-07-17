#define WITH_FIELDLOG

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
#if WITH_FIELDLOG
using Unclassified.FieldLog;
#endif

namespace Unclassified.Util
{
	/// <summary>
	/// Represents a data store that contains all setting keys and values of a specified setting
	/// file.
	/// </summary>
	/// <remarks>
	/// <para>
	///   Every time the file is written, the existing file is renamed to the extension ".bak". This
	///   backup file is permanently kept and shall handle write errors due to power or disk
	///   failures in the moment of writing the settings file.
	/// </para>
	/// <para>
	///   The file is read into a Dictionary&lt;string, object&gt; in memory in the constructor. If
	///   an entry value cannot be parsed for the indicated value type, or a
	///   <see cref="FormatException"/> or <see cref="XmlException"/> is thrown otherwise while
	///   reading the file contents, the file is renamed to the extension ".broken". If a backup
	///   file with the extension ".bak" exists, it will be copied to the regular file name once and
	///   the file will be read. If this file also cannot be read, no settings data will be
	///   available in the store initially. The static event <see cref="LoadError"/> can be used to
	///   handle read errors in the application.
	/// </para>
	/// <para>
	///   Trying to read a setting value with an incompatible type, e. g. reading the string value
	///   "abc" with the <see cref="GetInt(string)"/> method, will cause no value to be returned. If
	///   a fallback value is provided, this will be returned instead. Accessing a value with the
	///   wrong method has the same behaviour as if the key was not there at all.
	/// </para>
	/// <para>
	///   While this class is thread-safe enough to avoid data corruption, it may be blocking a bit
	///   too often and cause dead-locks. Use it with care and better avoid cross-thread access.
	/// </para>
	/// </remarks>
	public class FileSettingsStore : ISettingsStore
	{
		#region Static events

		/// <summary>
		/// Raised when an error occurs while loading a settings file.
		/// </summary>
		public static event EventHandler<SettingsFileErrorEventArgs> LoadError;

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
		private DelayedCall saveDc;

		/// <summary>
		/// Indicates whether a Save operation is still waiting for the lock. saveDc.IsWaiting will
		/// already return false because the Save method has already been called but is currently
		/// blocked.
		/// </summary>
		private bool savePending;

		/// <summary>
		/// Indicates whether the settings file was opened in read-only mode. This prevents any
		/// write access to the settings and will never save the file back.
		/// </summary>
		private bool readOnly;

		/// <summary>
		/// File encryption password. Encryption is only used if the password is not null or empty.
		/// </summary>
		private string password;

		/// <summary>
		/// Indicates whether there was a problem reading the file on loading.
		/// </summary>
		private bool hadProblem;

		/// <summary>
		/// Indicates whether the instance has already been disposed.
		/// </summary>
		private bool isDisposed;

		#endregion Private data

		#region Constructors

		/// <summary>
		/// Initialises a new instance of the <see cref="FileSettingsStore"/> class.
		/// </summary>
		/// <param name="fileName">Name of the settings file to load.</param>
		public FileSettingsStore(string fileName)
			: this(fileName, null, false)
		{
		}

		/// <summary>
		/// Initialises a new instance of the <see cref="FileSettingsStore"/> class.
		/// </summary>
		/// <param name="fileName">Name of the settings file to load.</param>
		/// <param name="password">Encryption password of the settings file.
		/// Set null to not use encryption.</param>
		public FileSettingsStore(string fileName, string password)
			: this(fileName, password, false)
		{
		}

		/// <summary>
		/// Initialises a new instance of the <see cref="FileSettingsStore"/> class.
		/// </summary>
		/// <param name="fileName">Name of the settings file to load.</param>
		/// <param name="readOnly">true to open the settings file in read-only mode. This prevents
		/// any write access to the settings and will never save the file back.</param>
		public FileSettingsStore(string fileName, bool readOnly)
			: this(fileName, null, readOnly)
		{
		}

		/// <summary>
		/// Initialises a new instance of the <see cref="FileSettingsStore"/> class.
		/// </summary>
		/// <param name="fileName">Name of the settings file to load.</param>
		/// <param name="password">Encryption password of the settings file.
		/// Set null to not use encryption.</param>
		/// <param name="readOnly">true to open the settings file in read-only mode. This prevents
		/// any write access to the settings and will never save the file back.</param>
		public FileSettingsStore(string fileName, string password, bool readOnly)
		{
			store = new Dictionary<string, object>();
			this.password = password;
			this.readOnly = readOnly;
			Load(fileName);

			if (DelayedCall.SupportsSynchronization)
			{
				saveDc = DelayedCall.Create(Save, SaveDelay);
			}
			else
			{
				saveDc = DelayedCall.CreateAsync(Save, SaveDelay);
			}
		}

		#endregion Constructors

		#region Public properties

		/// <summary>
		/// Gets the file name of the currently used setting file.
		/// </summary>
		public string FileName
		{
			get { return fileName; }
		}

		/// <summary>
		/// Gets a value indicating whether the instance is read-only.
		/// </summary>
		public bool IsReadOnly
		{
			get { return readOnly; }
		}

		/// <summary>
		/// Gets a value indicating whether the current settings file is encrypted.
		/// </summary>
		public bool IsEncrypted
		{
			get { return !string.IsNullOrEmpty(password); }
		}

		/// <summary>
		/// Gets a value indicating whether there was a problem reading the file on loading.
		/// </summary>
		public bool HadProblem
		{
			get { return hadProblem; }
		}

		/// <summary>
		/// Gets a value indicating whether the instance is disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return isDisposed; }
		}

		#endregion Public properties

		#region Write access

		/// <summary>
		/// Checks whether the passed object is of a data type that can be stored in the settings
		/// file. Throws an ArgumentException if the data type is unsupported.
		/// </summary>
		/// <param name="newValue">The value to check.</param>
		private void CheckType(object newValue)
		{
			// Unpack enum value
			// NOTE: This doesn't handle arrays of enums
			if (newValue.GetType().IsEnum)
			{
				newValue = Convert.ChangeType(newValue, newValue.GetType().GetEnumUnderlyingType());
			}

			// Check for supported type
			if (newValue is string ||
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
				newValue is TimeSpan[] ||
				newValue is NameValueCollection)
			{
				return;
			}
			throw new ArgumentException("The data type is not supported: " + newValue.GetType().Name);
		}

		/// <summary>
		/// Sets a setting key to a new value.
		/// </summary>
		/// <param name="key">The setting key to update.</param>
		/// <param name="newValue">The new value for that key. Set null to remove the key.</param>
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

					// Unpack enum value
					// TODO: This doesn't handle arrays of enums
					if (newValue.GetType().IsEnum)
					{
						newValue = Convert.ChangeType(newValue, newValue.GetType().GetEnumUnderlyingType());
					}

					store[key] = newValue;
					OnPropertyChanged(key);

					saveDc.Reset();
					savePending = true;
				}
			}
		}

		/// <summary>
		/// Removes a setting key from the settings store.
		/// </summary>
		/// <param name="key">The setting key to remove.</param>
		/// <returns>true if the key was removed, false if it did not exist.</returns>
		public bool Remove(string key)
		{
			lock (syncLock)
			{
				if (store.ContainsKey(key))
				{
					if (isDisposed) throw new ObjectDisposedException("");
					if (readOnly) throw new InvalidOperationException("This SettingsStore instance is created in read-only mode.");

					store.Remove(key);
					OnPropertyChanged(key);

					saveDc.Reset();
					savePending = true;
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Renames a setting key in the settings store.
		/// </summary>
		/// <param name="oldKey">The old setting key to rename.</param>
		/// <param name="newKey">The new setting key.</param>
		/// <returns>true if the value was renamed, false if it did not exist.</returns>
		public bool Rename(string oldKey, string newKey)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");
				if (readOnly) throw new InvalidOperationException("This SettingsStore instance is created in read-only mode.");

				if (store.ContainsKey(oldKey))
				{
					object data = store[oldKey];

					store.Remove(oldKey);
					OnPropertyChanged(oldKey);

					store[newKey] = data;
					OnPropertyChanged(newKey);

					saveDc.Reset();
					savePending = true;
					return true;
				}
				return false;
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
		/// Gets the current bool value of a setting key, or false if the key is unset or has an
		/// incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		public bool GetBool(string key)
		{
			return GetBool(key, false);
		}

		/// <summary>
		/// Gets the current bool value of a setting key, or a fallback value if the key is unset or
		/// has an incompatible data type.
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
		/// Gets the current bool[] value of a setting key, or an empty array if the key is unset or
		/// has an incompatible data type.
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
				return new bool[0];
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
			return GetInt(key, 0);
		}

		/// <summary>
		/// Gets the current int value of a setting key, or a fallback value if the key is unset or
		/// has an incompatible data type.
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
		/// Gets the current int[] value of a setting key, or an empty array if the key is unset or
		/// has an incompatible data type.
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
				return new int[0];
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
			return GetLong(key, 0);
		}

		/// <summary>
		/// Gets the current long value of a setting key, or a fallback value if the key is unset or
		/// has an incompatible data type.
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
		/// Gets the current long[] value of a setting key, or an empty array if the key is unset or
		/// has an incompatible data type.
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
				return new long[0];
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
			return GetDouble(key, double.NaN);
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
				return new double[0];
			}
		}

		/// <summary>
		/// Gets the current string value of a setting key, or "" if the key is unset.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		public string GetString(string key)
		{
			return GetString(key, "");
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
				return new string[0];
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
			return GetDateTime(key, DateTime.MinValue);
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
				return new DateTime[0];
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
			return GetTimeSpan(key, TimeSpan.Zero);
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
				return new TimeSpan[0];
			}
		}

		/// <summary>
		/// Gets the current NameValueCollection of a setting key, or an empty collection if the key
		/// is unset or has an incompatible data type.
		/// </summary>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		public NameValueCollection GetNameValueCollection(string key)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");

				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data is NameValueCollection) return data as NameValueCollection;
				return new NameValueCollection();
			}
		}

		/// <summary>
		/// Creates a list wrapper for an array-typed key. Changes to the list are written back to
		/// the settings store.
		/// </summary>
		/// <typeparam name="T">The type of list items.</typeparam>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		public IList<T> CreateList<T>(string key)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");

				return new SettingsStoreBoundList<T>(this, key);
			}
		}

		/// <summary>
		/// Creates a dictionary wrapper for a NameValueCollection-typed key. Changes to the
		/// dictionary are written back to the settings store.
		/// </summary>
		/// <typeparam name="TKey">The type of dictionary keys.</typeparam>
		/// <typeparam name="TValue">The type of dictionary values.</typeparam>
		/// <param name="key">The setting key.</param>
		/// <returns></returns>
		public IDictionary<TKey, TValue> CreateDictionary<TKey, TValue>(string key)
		{
			lock (syncLock)
			{
				if (isDisposed) throw new ObjectDisposedException("");

				return new SettingsStoreBoundDictionary<TKey, TValue>(this, key);
			}
		}

		#endregion Read access

		#region Loading and saving

		/// <summary>
		/// Loads all settings from a file.
		/// </summary>
		/// <param name="fileName">Name of the settings file to load.</param>
		private void Load(string fileName)
		{
			lock (syncLock)
			{
				if (!Path.IsPathRooted(fileName))
				{
					fileName = Path.GetFullPath(fileName);
				}
				this.fileName = fileName;

				// Run the following code at most two times.
				// First with the regular file, second with a restored backup if it exists.
				int tryCount = 2;
				while (tryCount-- > 0)
				{
					try
					{
						store.Clear();
						XmlDocument xdoc = new XmlDocument();

						if (!string.IsNullOrEmpty(this.password))
						{
#if WITH_FIELDLOG
							FL.Trace("FileSettingsStore.Load", "fileName = " + fileName + "\nWith password");
#endif
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
										// DEBUG:
										//string data = sr.ReadToEnd();
										//xdoc.LoadXml(data);
									}
								}
							}
						}
						else
						{
#if WITH_FIELDLOG
							FL.Trace("FileSettingsStore.Load", "fileName = " + fileName + "\nNo password");
#endif
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
								string type = xn.Attributes["type"].Value.Trim();

								if (key == "") throw new XmlException("Empty entry key");

								if (type == "string")
								{
									store.Add(key, xn.InnerText);
								}
								else if (type == "string[]" ||
									type == "string-array")
								{
									List<string> list = new List<string>();
									foreach (XmlNode itemNode in xn.SelectNodes("item"))
									{
										list.Add(itemNode.InnerText);
									}
									store.Add(key, list.ToArray());
								}
								else if (type == "int")
								{
									store.Add(key, int.Parse(xn.InnerText, CultureInfo.InvariantCulture));
								}
								else if (type == "int[]" ||
									type == "int-array")
								{
									List<int> list = new List<int>();
									foreach (XmlNode itemNode in xn.SelectNodes("item"))
									{
										if (itemNode.InnerText == "")
											list.Add(0);
										else
											list.Add(int.Parse(itemNode.InnerText, CultureInfo.InvariantCulture));
									}
									store.Add(key, list.ToArray());
								}
								else if (type == "long")
								{
									store.Add(key, long.Parse(xn.InnerText, CultureInfo.InvariantCulture));
								}
								else if (type == "long[]" ||
									type == "long-array")
								{
									List<long> list = new List<long>();
									foreach (XmlNode itemNode in xn.SelectNodes("item"))
									{
										if (itemNode.InnerText == "")
											list.Add(0);
										else
											list.Add(long.Parse(itemNode.InnerText, CultureInfo.InvariantCulture));
									}
									store.Add(key, list.ToArray());
								}
								else if (type == "double")
								{
									store.Add(key, double.Parse(xn.InnerText, CultureInfo.InvariantCulture));
								}
								else if (type == "double[]" ||
									type == "double-array")
								{
									List<double> list = new List<double>();
									foreach (XmlNode itemNode in xn.SelectNodes("item"))
									{
										if (itemNode.InnerText == "")
											list.Add(0);
										else
											list.Add(double.Parse(itemNode.InnerText, CultureInfo.InvariantCulture));
									}
									store.Add(key, list.ToArray());
								}
								else if (type == "bool")
								{
									if (xn.InnerText.ToString().Trim() == "1" ||
										xn.InnerText.ToString().Trim().ToLower() == "true") store.Add(key, true);
									else if (xn.InnerText.ToString().Trim() == "0" ||
										xn.InnerText.ToString().Trim().ToLower() == "false") store.Add(key, false);
									else throw new FormatException("Invalid bool value");
								}
								else if (type == "bool[]" ||
									type == "bool-array")
								{
									List<bool> list = new List<bool>();
									foreach (XmlNode itemNode in xn.SelectNodes("item"))
									{
										if (itemNode.InnerText.ToString().Trim() == "1" ||
											itemNode.InnerText.ToString().Trim().ToLower() == "true") list.Add(true);
										else if (itemNode.InnerText.ToString().Trim() == "0" ||
											itemNode.InnerText.ToString().Trim().ToLower() == "false") list.Add(false);
										else throw new FormatException("Invalid bool value");
									}
									store.Add(key, list.ToArray());
								}
								else if (type == "DateTime")
								{
									store.Add(key, DateTime.Parse(xn.InnerText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
								}
								else if (type == "DateTime[]")
								{
									List<DateTime> list = new List<DateTime>();
									foreach (XmlNode itemNode in xn.SelectNodes("item"))
									{
										long lng;
										if (itemNode.InnerText == "")
											list.Add(DateTime.MinValue);
										else if (long.TryParse(itemNode.InnerText, NumberStyles.Integer, CultureInfo.InvariantCulture, out lng))   // Old format: Ticks as long integer
											list.Add(new DateTime(lng));
										else
											list.Add(DateTime.Parse(itemNode.InnerText, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind));
									}
									store.Add(key, list.ToArray());
								}
								else if (type == "TimeSpan")
								{
									store.Add(key, new TimeSpan(long.Parse(xn.InnerText, CultureInfo.InvariantCulture)));
								}
								else if (type == "TimeSpan[]")
								{
									List<TimeSpan> list = new List<TimeSpan>();
									foreach (XmlNode itemNode in xn.SelectNodes("item"))
									{
										if (itemNode.InnerText == "")
											list.Add(TimeSpan.Zero);
										else
											list.Add(new TimeSpan(long.Parse(itemNode.InnerText, CultureInfo.InvariantCulture)));
									}
									store.Add(key, list.ToArray());
								}
								else if (type == "map")
								{
									NameValueCollection collection = new NameValueCollection();
									foreach (XmlNode itemNode in xn.SelectNodes("item"))
									{
										string itemName = itemNode.Attributes["name"].Value;
										string itemValue = itemNode.InnerText;
										collection[itemName] = itemValue;
									}
									store.Add(key, collection);
								}
								else
								{
									throw new XmlException("Invalid type value");
								}
							}
						}
						return;
					}
					catch (DirectoryNotFoundException)
					{
#if WITH_FIELDLOG
						FL.Trace("FileSettingsStore.Load: DirectoryNotFoundException, no settings loaded");
#endif
					}
					catch (FileNotFoundException)
					{
#if WITH_FIELDLOG
						FL.Trace("FileSettingsStore.Load: FileNotFoundException, no settings loaded");
#endif
					}
					catch (FormatException ex)
					{
						HandleBrokenFile(ex);
					}
					catch (XmlException ex)
					{
						HandleBrokenFile(ex);
					}
					hadProblem = true;

					// Try and use the backup file (once) if the file wasn't loaded
					string backupFileName = fileName + ".bak";
					if (!File.Exists(backupFileName)) return;
#if WITH_FIELDLOG
					FL.Info("Restoring backup settings file and retrying", "backupFileName = " + backupFileName);
#endif
					File.Copy(backupFileName, fileName, true);

					// Read the restored backup file in a second iteration.
				}
			}
		}

		/// <summary>
		/// Handles a broken settings file. Renames the file, clears the settings store and raises
		/// the LoadError event so that the application can log the error.
		/// </summary>
		/// <param name="ex">The exception instance that was thrown.</param>
		private void HandleBrokenFile(Exception ex)
		{
#if WITH_FIELDLOG
			FL.Warning(ex, "Loading settings file");
#endif
			store.Clear();
			try
			{
				File.Delete(fileName + ".broken");
				File.Move(fileName, fileName + ".broken");
#if WITH_FIELDLOG
				FL.Trace("Broken settings file renamed", "New file name: " + fileName + ".broken");
#endif
			}
#if WITH_FIELDLOG
			catch (Exception ex2)
			{
				FL.Warning(ex2, "Renaming broken settings file");
				// Best-effort. If it fails, do nothing.
			}
#else
			catch
			{
				// Best-effort. If it fails, do nothing.
			}
#endif

			var handler = LoadError;
			if (handler != null)
			{
				handler(this, new SettingsFileErrorEventArgs(fileName, ex));
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
				if (readOnly) throw new InvalidOperationException("This SettingsStore instance is created in read-only mode.");

				if (saveDc.IsDisposed || saveDc.IsWaiting || savePending)
				{
					saveDc.Cancel();
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
				savePending = false;
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
							XmlNode itemNode = xdoc.CreateElement("item");
							itemNode.InnerText = s;
							xn.AppendChild(itemNode);
						}
					}
					else if (store[key] is int)
					{
						xa = xdoc.CreateAttribute("type");
						xa.Value = "int";
						xn.Attributes.Append(xa);
						xn.InnerText = GetInt(key).ToString(CultureInfo.InvariantCulture);
					}
					else if (store[key] is int[])
					{
						xa = xdoc.CreateAttribute("type");
						xa.Value = "int[]";
						xn.Attributes.Append(xa);
						int[] ia = (int[]) store[key];
						foreach (int i in ia)
						{
							XmlNode itemNode = xdoc.CreateElement("item");
							itemNode.InnerText = i.ToString(CultureInfo.InvariantCulture);
							xn.AppendChild(itemNode);
						}
					}
					else if (store[key] is long)
					{
						xa = xdoc.CreateAttribute("type");
						xa.Value = "long";
						xn.Attributes.Append(xa);
						xn.InnerText = GetLong(key).ToString(CultureInfo.InvariantCulture);
					}
					else if (store[key] is long[])
					{
						xa = xdoc.CreateAttribute("type");
						xa.Value = "long[]";
						xn.Attributes.Append(xa);
						long[] la = (long[]) store[key];
						foreach (long l in la)
						{
							XmlNode itemNode = xdoc.CreateElement("item");
							itemNode.InnerText = l.ToString(CultureInfo.InvariantCulture);
							xn.AppendChild(itemNode);
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
							XmlNode itemNode = xdoc.CreateElement("item");
							itemNode.InnerText = d.ToString(CultureInfo.InvariantCulture);
							xn.AppendChild(itemNode);
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
							XmlNode itemNode = xdoc.CreateElement("item");
							itemNode.InnerText = b ? "true" : "false";
							xn.AppendChild(itemNode);
						}
					}
					else if (store[key] is DateTime)
					{
						xa = xdoc.CreateAttribute("type");
						xa.Value = "DateTime";
						xn.Attributes.Append(xa);
						xn.InnerText = GetDateTime(key).ToString("o", CultureInfo.InvariantCulture);
					}
					else if (store[key] is DateTime[])
					{
						xa = xdoc.CreateAttribute("type");
						xa.Value = "DateTime[]";
						xn.Attributes.Append(xa);
						DateTime[] da = (DateTime[]) store[key];
						foreach (DateTime d in da)
						{
							XmlNode itemNode = xdoc.CreateElement("item");
							itemNode.InnerText = d.ToString("o");
							xn.AppendChild(itemNode);
						}
					}
					else if (store[key] is TimeSpan)
					{
						xa = xdoc.CreateAttribute("type");
						xa.Value = "TimeSpan";
						xn.Attributes.Append(xa);
						xn.InnerText = GetTimeSpan(key).Ticks.ToString(CultureInfo.InvariantCulture);
					}
					else if (store[key] is TimeSpan[])
					{
						xa = xdoc.CreateAttribute("type");
						xa.Value = "TimeSpan[]";
						xn.Attributes.Append(xa);
						TimeSpan[] ta = (TimeSpan[]) store[key];
						foreach (TimeSpan t in ta)
						{
							XmlNode itemNode = xdoc.CreateElement("item");
							itemNode.InnerText = t.Ticks.ToString(CultureInfo.InvariantCulture);
							xn.AppendChild(itemNode);
						}
					}
					else if (store[key] is NameValueCollection)
					{
						xa = xdoc.CreateAttribute("type");
						xa.Value = "map";
						xn.Attributes.Append(xa);
						NameValueCollection collection = (NameValueCollection) store[key];
						for (int i = 0; i < collection.Count; i++)
						{
							XmlNode itemNode = xdoc.CreateElement("item");
							xa = xdoc.CreateAttribute("name");
							xa.Value = collection.GetKey(i);
							itemNode.Attributes.Append(xa);
							itemNode.InnerText = collection[i];
							xn.AppendChild(itemNode);
						}
					}
					else
					{
						// Internal error, cannot save this store entry
						continue;
					}

					root.AppendChild(xn);
				}

#if WITH_FIELDLOG
				FL.Trace("FileSettingsStore.Save", "fileName = " + fileName);
#endif
				if (!Directory.Exists(Path.GetDirectoryName(fileName)))
				{
#if WITH_FIELDLOG
					FL.Trace("FileSettingsStore.Save: Creating directory");
#endif
					Directory.CreateDirectory(Path.GetDirectoryName(fileName));
				}

				// Create a backup of the existing file. This backup will be permanently kept and
				// can be restored when a load error occurs.
				string backupFileName = fileName + ".bak";
				if (File.Exists(fileName))
				{
					File.Copy(fileName, backupFileName, true);
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

		#region Finalizer and IDisposable members

		/// <summary>
		/// Finalizer.
		/// </summary>
		~FileSettingsStore()
		{
			Dispose();
#if WITH_FIELDLOG
			if (!FL.IsShutdown)
			{
				FL.Warning("FileSettingsStore.Dispose has not been called, saving in the Finalizer", "fileName = " + fileName);
			}
#endif
		}

		/// <summary>
		/// Saves all settings to the file and frees all resources.
		/// </summary>
		public void Dispose()
		{
			lock (syncLock)
			{
				if (!isDisposed)
				{
					if (!readOnly)
						SaveNow();
					isDisposed = true;
					store.Clear();
					saveDc.Dispose();
					saveDc = null;
				}
				GC.SuppressFinalize(this);
			}
		}

		#endregion Finalizer and IDisposable members

		#region INotifyPropertyChanged members

		/// <summary>
		/// Occurs when a setting key value changes.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Raises the PropertyChanged event.
		/// </summary>
		/// <param name="key">Name of the setting key that has changed.</param>
		protected void OnPropertyChanged(string key)
		{
			var handler = this.PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(key));
			}
		}

		#endregion INotifyPropertyChanged members
	}

	#region Error EventArgs class

	/// <summary>
	/// Provides data for the LoadError event.
	/// </summary>
	public class SettingsFileErrorEventArgs : EventArgs
	{
		/// <summary>
		/// Initialises a new instance of the SettingsErrorEventArgs class.
		/// </summary>
		/// <param name="fileName">The name of the settings file that caused the error.</param>
		/// <param name="exception">The exception object that was raised.</param>
		public SettingsFileErrorEventArgs(string fileName, Exception exception)
		{
			FileName = fileName;
			Exception = exception;
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

	#endregion Error EventArgs class
}
