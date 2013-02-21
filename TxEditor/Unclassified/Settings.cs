using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Globalization;

namespace Unclassified
{
	public class Settings : IDisposable
	{
		public delegate void SettingChangedDelegate(string key, object oldValue, object newValue);

		#region Private Datenfelder
		private string filename;
		private Dictionary<string, object> store;
		private DelayedCall delayedSave = null;
		private Dictionary<string, List<SettingChangedDelegate>> handlers;
		private bool readOnly = false;
		#endregion Private Datenfelder

		#region Konstruktoren
		public Settings(string filename) : this(filename, false) { }

		public Settings(string filename, bool readOnly)
		{
			store = new Dictionary<string, object>();
			handlers = new Dictionary<string, List<SettingChangedDelegate>>();
			this.readOnly = readOnly;
			Load(filename);
		}
		#endregion Konstruktoren

		#region IDisposable Member
		public void Dispose()
		{
			SaveNow();
			store.Clear();
			handlers.Clear();
			if (delayedSave != null) delayedSave.Dispose();

		}
		#endregion IDisposable Member

		#region Ändernder Datenzugriff
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
				newValue is bool[]))
			{
				throw new ArgumentException("The data type is not supported: " + newValue.GetType().ToString());
			}
		}
		
		public void Set(string key, object newValue)
		{
			lock (this)
			{
				if (readOnly) throw new InvalidOperationException("This Settings instance is created in read-only mode.");

				if (newValue == null)
				{
					Remove(key);
				}
				else
				{
					CheckType(newValue);

					object oldValue = null;
					if (store.ContainsKey(key)) oldValue = store[key];
					if (oldValue == newValue) return;
					store[key] = newValue;
					CallHandlers(key, oldValue, newValue);

					if (delayedSave != null && delayedSave.IsWaiting) delayedSave.Cancel();
					if (delayedSave != null) delayedSave.Dispose();
					delayedSave = DelayedCall.Start(Save, 1000);
				}
			}
		}

		public void SetDefault(string key, object newValue)
		{
			lock (this)
			{
				SetDefault(key, newValue, true);
			}
		}

		public void SetDefault(string key, object newValue, bool notifyNow)
		{
			lock (this)
			{
				if (readOnly) throw new InvalidOperationException("This Settings instance is created in read-only mode.");

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
					if (notifyNow) CallHandlers(key, oldValue, newValue);

					if (delayedSave != null && delayedSave.IsWaiting) delayedSave.Cancel();
					if (delayedSave != null) delayedSave.Dispose();
					delayedSave = DelayedCall.Start(Save, 1000);
				}
			}
		}

		public void Remove(string key)
		{
			lock (this)
			{
				if (store.ContainsKey(key))
				{
					if (readOnly) throw new InvalidOperationException("This Settings instance is created in read-only mode.");

					store.Remove(key);
					// Remove all handlers attached to this key
					RemoveHandler(key, null);
					if (delayedSave != null && delayedSave.IsWaiting) delayedSave.Cancel();
					if (delayedSave != null) delayedSave.Dispose();
					delayedSave = DelayedCall.Start(Save, 1000);
				}
			}
		}
		#endregion Ändernder Datenzugriff

		#region Lesender Datenzugriff
		public string[] GetKeys()
		{
			lock (this)
			{
				string[] keys = new string[store.Keys.Count];
				store.Keys.CopyTo(keys, 0);
				return keys;
			}
		}
		
		public string GetType(string key)
		{
			lock (this)
			{
				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data == null) return "";
				if (data is string) return "string";
				if (data is string[]) return "string-array";
				if (data is int) return "int";
				if (data is int[]) return "int-array";
				if (data is long) return "long";
				if (data is long[]) return "long-array";
				if (data is double) return "double";
				if (data is double[]) return "double-array";
				if (data is bool) return "bool";
				if (data is bool[]) return "bool-array";
				return "?";
			}
		}

		public object Get(string key)
		{
			lock (this)
			{
				object data = null;
				if (store.ContainsKey(key)) data = store[key];
				return data;
			}
		}

		public string GetString(string key, string defaultValue)
		{
			lock (this)
			{
				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data == null) return defaultValue;
				return Convert.ToString(data, CultureInfo.InvariantCulture);
			}
		}

		public string GetString(string key)
		{
			lock (this)
			{
				return GetString(key, "");
			}
		}

		public string[] GetStringArray(string key)
		{
			lock (this)
			{
				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data is string[]) return data as string[];
				return new string[] { };
			}
		}

		public int GetInt(string key, int defaultValue)
		{
			lock (this)
			{
				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data == null) return defaultValue;
				try
				{
					return Convert.ToInt32(data, CultureInfo.InvariantCulture);
				}
				catch (FormatException)
				{
					return defaultValue;
				}
			}
		}

		public int GetInt(string key)
		{
			lock (this)
			{
				return GetInt(key, 0);
			}
		}

		public int[] GetIntArray(string key)
		{
			lock (this)
			{
				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data is int[]) return data as int[];
				return new int[] { };
			}
		}

		public long GetLong(string key, long defaultValue)
		{
			lock (this)
			{
				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data == null) return defaultValue;
				try
				{
					return Convert.ToInt64(data, CultureInfo.InvariantCulture);
				}
				catch (FormatException)
				{
					return defaultValue;
				}
			}
		}

		public long GetLong(string key)
		{
			lock (this)
			{
				return GetLong(key, 0);
			}
		}

		public long[] GetLongArray(string key)
		{
			lock (this)
			{
				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data is long[]) return data as long[];
				return new long[] { };
			}
		}

		public double GetDouble(string key, double defaultValue)
		{
			lock (this)
			{
				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data == null) return defaultValue;
				try
				{
					return Convert.ToDouble(data, CultureInfo.InvariantCulture);
				}
				catch (FormatException)
				{
					return defaultValue;
				}
			}
		}

		public double GetDouble(string key)
		{
			lock (this)
			{
				return GetDouble(key, 0);
			}
		}

		public double[] GetDoubleArray(string key)
		{
			lock (this)
			{
				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data is double[]) return data as double[];
				return new double[] { };
			}
		}

		public bool GetBool(string key, bool defaultValue)
		{
			lock (this)
			{
				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data == null) return defaultValue;
				if (data.ToString().Trim() == "1" ||
					data.ToString().Trim().ToLower() == "true") return true;
				if (data.ToString().Trim() == "0" ||
					data.ToString().Trim().ToLower() == "false") return false;
				return defaultValue;
			}
		}

		public bool GetBool(string key)
		{
			lock (this)
			{
				return GetBool(key, false);
			}
		}

		public bool[] GetBoolArray(string key)
		{
			lock (this)
			{
				object data = null;
				if (store.ContainsKey(key)) data = store[key];

				if (data is bool[]) return data as bool[];
				return new bool[] { };
			}
		}
		#endregion Lesender Datenzugriff

		#region Ereignisbenachrichtigung
		public void AddHandler(string key, SettingChangedDelegate handler)
		{
			lock (this)
			{
				AddHandler(key, handler, false);
			}
		}

		public void AddHandler(string key, SettingChangedDelegate handler, bool notifyNow)
		{
			lock (this)
			{
				if (handlers.ContainsKey(key))
				{
					if (!handlers[key].Contains(handler)) handlers[key].Add(handler);
				}
				else
				{
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

		public void RemoveHandler(string key, SettingChangedDelegate handler)
		{
			lock (this)
			{
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

		public void CallHandlers(string key, object oldValue, object newValue)
		{
			lock (this)
			{
				// Call handlers for this key only
				if (handlers.ContainsKey(key))
				{
					foreach (SettingChangedDelegate handler in handlers[key])
					{
						if (handler != null) handler(key, oldValue, newValue);
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
		#endregion Ereignisbenachrichtigung

		#region Laden und Speichern der Daten
		private void Load(string filename)
		{
			lock (this)
			{
				this.filename = filename;
				try
				{
					store.Clear();
					XmlDocument xdoc = new XmlDocument();
					xdoc.Load(filename);
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
							else if (xn.Attributes["type"].Value == "string-array")
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
							else if (xn.Attributes["type"].Value == "int-array")
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
							else if (xn.Attributes["type"].Value == "long-array")
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
							else if (xn.Attributes["type"].Value == "double-array")
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
							else if (xn.Attributes["type"].Value == "bool-array")
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
							else
							{
								throw new XmlException("Invalid type value");
							}
						}
					}
				}
				catch (DirectoryNotFoundException)
				{
				}
				catch (FileNotFoundException)
				{
				}
				//catch (FormatException ex)
				//{
				//    throw ex;
				//}
				//catch (XmlException ex)
				//{
				//    throw ex;
				//}
			}
		}

		public void SaveNow()
		{
			lock (this)
			{
				if (delayedSave != null && delayedSave.IsWaiting)
				{
					delayedSave.Cancel();
					Save();
				}
			}
		}

		private void Save()
		{
			lock (this)
			{
				if (readOnly) throw new InvalidOperationException("This Settings instance is created in read-only mode.");

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
						xa.Value = "string-array";
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
						xa.Value = "int-array";
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
						xa.Value = "long-array";
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
						xa.Value = "double-array";
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
						xa.Value = "bool-array";
						xn.Attributes.Append(xa);
						bool[] ba = (bool[]) store[key];
						foreach (bool b in ba)
						{
							XmlNode n = xdoc.CreateElement("item");
							n.InnerText = b ? "true" : "false";
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

				if (!Directory.Exists(Path.GetDirectoryName(filename)))
					Directory.CreateDirectory(Path.GetDirectoryName(filename));
				
				XmlWriterSettings xws = new XmlWriterSettings();
				xws.Encoding = Encoding.UTF8;
				xws.Indent = true;
				xws.IndentChars = "\t";
				xws.OmitXmlDeclaration = false;
				XmlWriter writer = XmlWriter.Create(filename, xws);
				xdoc.Save(writer);
				writer.Close();
			}
		}
		#endregion Laden und Speichern der Daten
	}
}
