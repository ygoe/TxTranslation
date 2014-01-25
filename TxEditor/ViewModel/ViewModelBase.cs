using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Media;

namespace TxEditor.ViewModel
{
	abstract class ViewModelBase : INotifyPropertyChanged
	{
		#region Common view properties

		/// <summary>
		/// Returns the user-friendly name of this object.
		/// Child classes can set this property to a new value,
		/// or override it to determine the value on-demand.
		/// </summary>
		private string displayName;
		public virtual string DisplayName
		{
			get { return displayName; }
			set
			{
				if (value != displayName)
				{
					displayName = value;
					OnPropertyChanged("DisplayName");
				}
			}
		}

		#endregion Common view properties

		#region Data input cleanup

		protected string SanitizeString(string str)
		{
			if (str == null) return null;
			return str.Trim();
		}

		protected string SanitizeInt(string str)
		{
			if (string.IsNullOrWhiteSpace(str)) return null;
			try
			{
				long l = Convert.ToInt64(str);
				return Convert.ToString(l);
			}
			catch
			{
				return str.Trim();
			}
		}

		protected string SanitizeDouble(string str)
		{
			//return str.Trim();
			if (string.IsNullOrWhiteSpace(str)) return null;
			try
			{
				double d = Convert.ToDouble(str);
				return Convert.ToString(d);
			}
			catch
			{
				return str.Trim();
			}
		}

		#endregion Data input cleanup

		#region Data validation

		public bool HasErrors { get { return GetErrors(true).Count > 0; } }
		public Dictionary<string, string> Errors { get { return GetErrors(); } }

		protected virtual void RaiseValidationUpdated()
		{
			OnPropertyChanged("Errors");
			OnPropertyChanged("HasErrors");
		}

		protected Dictionary<string, string> GetErrors(bool onlyFirst = false)
		{
			var dict = new Dictionary<string, string>();
			int more = 0;
			string lastKey = null;
			string lastValue = null;
			List<object> objs = new List<object>();
			objs.Add(this);
			AddErrorSubObjects(objs);
			foreach (object obj in objs)
			{
				foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(obj))
				{
					if (prop.Name == "Errors") continue;
					if (prop.Name == "HasErrors") continue;

					IDataErrorInfo dei = obj as IDataErrorInfo;
					if (dei != null)
					{
						string msg = dei[prop.Name];
						if (msg != null)
						{
							if (dict.Count < 5)
							{
								dict[prop.Name] = msg;
								if (onlyFirst && dict.Count > 0)
								{
									return dict;
								}
							}
							else
							{
								lastKey = prop.Name;
								lastValue = msg;
								more++;
							}
						}
					}
				}
			}
			if (more == 1)
			{
				dict[lastKey] = lastValue;
			}
			else if (more > 0)
			{
				// I18N - TODO: Is this method used at all?
				dict["zzz"] = "(" + more + " weitere Meldungen werden nicht angezeigt)";
			}
			return dict;
		}

		protected virtual void AddErrorSubObjects(List<object> objs)
		{
		}

		#endregion Data validation

		#region INotifyPropertyChanged Member

		/// <summary>
		/// Raised when a property on this object has a new value.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Raises this object's PropertyChanged event.
		/// </summary>
		/// <param name="propertyName">The property that has a new value.</param>
		protected void OnPropertyChanged(string propertyName)
		{
#if DEBUG
			if (!TypeDescriptor.GetProperties(this).OfType<PropertyDescriptor>().Any(d => d.Name == propertyName))
			{
				throw new NotImplementedException("Notifying a change of non-existing property " + this.GetType().Name + "." + propertyName);
			}
#endif

			var handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		#endregion INotifyPropertyChanged Member
	}
}
