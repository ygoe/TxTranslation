using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Threading;

namespace Unclassified.TxEditor.ViewModels
{
	/// <summary>
	/// Provides common properties and methods supporting view model classes.
	/// </summary>
	internal abstract class ViewModelBase : INotifyPropertyChanged
	{
		#region Constructor

		/// <summary>
		/// Initializes a new instance of the ViewModelBase class.
		/// </summary>
		public ViewModelBase()
		{
			InitializeCommands();
		}

		#endregion Constructor

		#region Common view properties

		private string displayName;

		/// <summary>
		/// Gets or sets the user-friendly name of this object. Derived classes can set this
		/// property to a new value, or override it to determine the value on-demand.
		/// </summary>
		public virtual string DisplayName
		{
			get { return displayName; }
			set
			{
				if (value != displayName)
				{
					displayName = value;
					OnDisplayNameChanged();
					OnPropertyChanged("DisplayName");
				}
			}
		}

		/// <summary>
		/// Raised when the DisplayName property on this object has a new value.
		/// </summary>
		protected virtual void OnDisplayNameChanged()
		{
		}

		public override string ToString()
		{
			if (DisplayName != null)
			{
				return GetType().Name + ": " + DisplayName;
			}
			return base.ToString();
		}

		#endregion Common view properties

		#region Commands support

		/// <summary>
		/// Initializes the commands in the ViewModel class.
		/// </summary>
		protected virtual void InitializeCommands()
		{
		}

		#endregion Commands support

		#region Property update helpers

		/// <summary>
		/// Checks whether the new property value has changed and updates the backing field.
		/// </summary>
		/// <typeparam name="T">Value type of the property.</typeparam>
		/// <param name="value">New property value.</param>
		/// <param name="field">Backing field.</param>
		/// <param name="propertyNames">Names of the properties to notify updated.</param>
		/// <returns>true if the value has changed, false otherwise.</returns>
		protected bool CheckUpdate<T>(T value, ref T field, params string[] propertyNames)
		{
			if (!EqualityComparer<T>.Default.Equals(value, field))
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

		protected string SanitizeDate(string str)
		{
			if (string.IsNullOrWhiteSpace(str)) return null;
			try
			{
				DateTime d = Convert.ToDateTime(str);
				return d.ToShortDateString();
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
				if (Dispatcher.CurrentDispatcher == Application.Current.Dispatcher)
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
		/// Raises this object's PropertyChanged event.
		/// </summary>
		/// <param name="propertyName">The property that has a new value.</param>
		protected void OnPropertyChanged(string propertyName)
		{
#if DEBUG
			if (!TypeDescriptor.GetProperties(this).OfType<PropertyDescriptor>().Any(d => d.Name == propertyName))
			{
				throw new ArgumentException("Notifying a change of non-existing property " + this.GetType().Name + "." + propertyName);
			}
#endif

			var handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		/// <summary>
		/// Raises this object's PropertyChanged event for multiple properties.
		/// </summary>
		/// <param name="propertyNames">The properties that have a new value.</param>
		protected void OnPropertyChanged(params string[] propertyNames)
		{
			// Only do all this work if somebody might listen to it
			var handler = this.PropertyChanged;
			if (handler != null)
			{
				foreach (string propertyName in propertyNames)
				{
#if DEBUG
					if (!TypeDescriptor.GetProperties(this).OfType<PropertyDescriptor>().Any(d => d.Name == propertyName))
					{
						throw new ArgumentException("Notifying a change of non-existing property " + this.GetType().Name + "." + propertyName);
					}
#endif

					handler(this, new PropertyChangedEventArgs(propertyName));
				}
			}
		}

		/// <summary>
		/// Raises this object's PropertyChanged event.
		/// </summary>
		/// <typeparam name="T">Value type of the property.</typeparam>
		/// <param name="selectorExpression">A lambda expression that describes the property that has a new value.</param>
		protected void OnPropertyChanged<TProperty>(Expression<Func<TProperty>> selectorExpression)
		{
			// This type of parameter can only be used with the params keyword if all passed
			// member expressions have the same value type. When using object instead of the
			// TProperty type parameter, the expression gets wrapped with some Convert method and
			// is no longer a MemberExpression. So this only works for one property per method call.

			// Only do all this work if somebody might listen to it
			if (this.PropertyChanged != null)
			{
				if (selectorExpression == null)
					throw new ArgumentNullException("selectorExpression");
				MemberExpression body = selectorExpression.Body as MemberExpression;
				if (body == null)
					throw new ArgumentException("The body must be a member expression");
				OnPropertyChanged(body.Member.Name);
			}
		}

		#endregion INotifyPropertyChanged Member
	}

	#region Special view model classes

	/// <summary>
	/// Represents an empty view model with a display name.
	/// </summary>
	internal sealed class EmptyViewModel : ViewModelBase
	{
		/// <summary>
		/// Initialises a new instance of the EmptyViewModel class.
		/// </summary>
		/// <param name="displayName">The display name of the new instance.</param>
		public EmptyViewModel(string displayName)
		{
			DisplayName = displayName;
		}
	}

	/// <summary>
	/// Represents a view model for a value with a display name.
	/// </summary>
	/// <typeparam name="T">The type of the value.</typeparam>
	internal class ValueViewModel<T> : ViewModelBase
	{
		/// <summary>
		/// Initialises a new instance of the ValueViewModel class.
		/// </summary>
		/// <param name="displayName">The display name of the new instance.</param>
		/// <param name="value">The value represented by the new instance.</param>
		public ValueViewModel(string displayName, T value)
		{
			if (value == null)
				throw new ArgumentNullException("value");

			Value = value;   // must be set first to prevent NullReferenceException caused in the following line
			DisplayName = displayName;
		}

		/// <summary>
		/// Gets the value represented by the current instance.
		/// </summary>
		public T Value { get; private set; }

		/// <summary>
		/// Determines whether the specified ValueViewModel instance represents the same value as
		/// the current instance.
		/// </summary>
		/// <param name="obj">The ValueViewModel to compare with the current object.</param>
		/// <returns>true if the specified ValueViewModel represents the same value as the current
		/// instance; otherwise, false.</returns>
		public override bool Equals(object obj)
		{
			ValueViewModel<T> other = obj as ValueViewModel<T>;
			if (other != null)
			{
				return other.Value.Equals(this.Value);
			}
			return false;
		}

		/// <summary>
		/// Overriden.
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			return Value.GetHashCode();
		}
	}

	#endregion Special view model classes
}
