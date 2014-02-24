using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace Unclassified.TxEditor.ViewModel
{
	/// <summary>
	/// Provides common properties and methods supporting view model classes.
	/// </summary>
	abstract class ViewModelBase : INotifyPropertyChanged
	{
		#region Common view properties

		private string displayName;

		/// <summary>
		/// Gets or sets the user-friendly name of this object. Derived classes can set this
		/// property to a new value, or override it to determine the value on-demand.
		/// </summary>
		public virtual string DisplayName
		{
			get { return this.displayName; }
			set
			{
				if (value != this.displayName)
				{
					this.displayName = value;
					OnPropertyChanged("DisplayName");
				}
			}
		}

		#endregion Common view properties

		#region Property update helpers

		/// <summary>
		/// Checks whether the new property value has changed and updates the backing field.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value">New property value.</param>
		/// <param name="field">Backing field.</param>
		/// <param name="propertyNames">Names of the properties to notify updated.</param>
		/// <returns>true if the value has changed, false otherwise.</returns>
		protected bool CheckUpdate<T>(T value, ref T field, params string[] propertyNames)
		{
			if ((value == null) != (field == null) ||      // Exactly one is null
				(value != null && !value.Equals(field)))   // Neither is null and they're not equal
			{
				field = value;
				OnPropertyChanged(propertyNames);
				return true;
			}
			return false;
		}

		#endregion Property update helpers

		#region Data input cleanup

		/// <summary>
		/// Sanitizes a user input string for a string type property.
		/// </summary>
		/// <param name="str">The text entered by the user.</param>
		/// <returns>The sanitized string value.</returns>
		protected string SanitizeString(string str)
		{
			if (str == null) return null;
			return str.Trim();
		}

		/// <summary>
		/// Sanitizes a user input string for an int type property.
		/// </summary>
		/// <param name="str">The text entered by the user.</param>
		/// <returns>The sanitized string value.</returns>
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

		/// <summary>
		/// Sanitizes a user input string for a double type property.
		/// </summary>
		/// <param name="str">The text entered by the user.</param>
		/// <returns>The sanitized string value.</returns>
		protected string SanitizeDouble(string str)
		{
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

		/// <summary>
		/// Gets a value indicating whether the view model instance has validation errors.
		/// </summary>
		public bool HasErrors { get { return GetErrors(true).Count > 0; } }

		/// <summary>
		/// Gets the validation errors in this view model instance.
		/// </summary>
		public Dictionary<string, string> Errors { get { return GetErrors(); } }

		private bool validationPending;

		/// <summary>
		/// Raises PropertyChanged events for Errors and HasErrors with Loaded priority. Multiple
		/// calls to this function before the asynchronous action has been started are ignored.
		/// </summary>
		protected virtual void RaiseValidationUpdated()
		{
			if (!validationPending)
			{
				// Don't do anything if not on the UI thread. The dispatcher event will never be
				// fired there and probably there's nobody interested in changed properties
				// anyway on that thread.
				if (Dispatcher.CurrentDispatcher == TxEditor.View.MainWindow.Instance.Dispatcher)
				{
					validationPending = true;
					Dispatcher.CurrentDispatcher.BeginInvoke((Action) delegate
					{
						validationPending = false;
						OnPropertyChanged("Errors");
						OnPropertyChanged("HasErrors");
					}, DispatcherPriority.Loaded);
				}
			}
		}

		/// <summary>
		/// Determines all validation errors in this view model instance. Up to 5 error messages
		/// are returned.
		/// </summary>
		/// <param name="onlyFirst">Only report the first determined error.</param>
		/// <returns>A dictionary that holds the error message for each property name.</returns>
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
				IDataErrorInfo dei = obj as IDataErrorInfo;
				if (dei != null)
				{
					foreach (PropertyDescriptor prop in TypeDescriptor.GetProperties(obj))
					{
						// Avoid recursion: These properties call the GetErrors method
						if (prop.Name == "Errors") continue;
						if (prop.Name == "HasErrors") continue;

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

		/// <summary>
		/// Adds logical children to consider for validation. Derived classes should override this
		/// method to add relevant children.
		/// </summary>
		/// <param name="objs">The list of objects to validate. Derived classes should add child objects to this list.</param>
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
		/// Raises this object's PropertyChanged event once for each property.
		/// </summary>
		/// <param name="propertyNames">The properties that has a new value.</param>
		protected void OnPropertyChanged(params string[] propertyNames)
		{
			foreach (string propertyName in propertyNames)
			{
				OnPropertyChanged(propertyName);
			}
		}

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

			var handler = this.PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		#endregion INotifyPropertyChanged Member

		#region Data binding update

		/// <summary>
		/// Updates the data binding source of the currently focused TextBox to update the model
		/// instance with the current user input.
		/// </summary>
		public void UpdateFocusedTextBox()
		{
			// Source: http://stackoverflow.com/a/5631292/143684
			TextBox focusedTextBox = Keyboard.FocusedElement as TextBox;
			if (focusedTextBox != null)
			{
				var be = focusedTextBox.GetBindingExpression(TextBox.TextProperty);
				if (be != null)
				{
					be.UpdateSource();
				}
			}
		}

		#endregion Data binding update
	}
}
