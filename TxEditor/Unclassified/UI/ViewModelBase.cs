// Copyright (c) 2015, Yves Goergen, http://unclassified.software/source/viewmodelbase
//
// Copying and distribution of this file, with or without modification, are permitted provided the
// copyright notice and this notice are preserved. This file is offered as-is, without any warranty.

// Define the following symbol if this class is used in a C# 5.0 or newer project.
//#define CSHARP50

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Threading;
using Unclassified.Util;
#if CSHARP50
using System.Runtime.CompilerServices;
#endif

namespace Unclassified.UI
{
	/// <summary>
	/// Provides common properties and methods supporting view model classes.
	/// </summary>
	internal abstract class ViewModelBase : INotifyPropertyChanged
	{
#if !CSHARP50
		/// <summary>
		/// Compatibility dummy attribute for C# before 5. This attribute does not backport the
		/// functionality from later C# versions.
		/// </summary>
		[AttributeUsage(AttributeTargets.Parameter)]
		public class CallerMemberNameAttribute : Attribute
		{
		}
#endif

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

		/// <summary>
		/// Gets or sets the display name of this object. Derived classes can set this property to a
		/// new value, or override it to determine the value on-demand.
		/// </summary>
		public virtual string DisplayName
		{
			get { return GetValue<string>("DisplayName"); }
			set { SetValue(value, "DisplayName"); }
		}

		/// <summary>
		/// Called when the <see cref="DisplayName"/> property on this object has a new value.
		/// </summary>
		[PropertyChangedHandler("DisplayName")]
		protected virtual void OnDisplayNameChanged()
		{
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			string displayName = DisplayName;
			if (displayName != null)
			{
				return GetType().Name + ": " + displayName;
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

		#region Property access methods

		// Ideas based on concept of Steve Cadwallader:
		// http://www.codecadwallader.com/2013/04/05/inotifypropertychanged-1-of-3-without-the-strings/
		// http://www.codecadwallader.com/2013/04/06/inotifypropertychanged-2-of-3-without-the-backing-fields/
		// http://www.codecadwallader.com/2013/04/08/inotifypropertychanged-3-of-3-without-the-reversed-notifications/

		/// <summary>
		/// Stores the values for each property in the current object.
		/// </summary>
		private Dictionary<string, object> backingFields = new Dictionary<string, object>();

		/// <summary>
		/// Gets the current value of a property.
		/// </summary>
		/// <typeparam name="T">The property type.</typeparam>
		/// <param name="propertyName">The property name.</param>
		/// <returns>The current value.</returns>
		protected T GetValue<T>([CallerMemberName] string propertyName = null)
		{
			if (propertyName == null) throw new ArgumentNullException("propertyName");

			object value;
			if (backingFields.TryGetValue(propertyName, out value))
			{
				return (T) value;
			}
			return default(T);
		}

		/// <summary>
		/// Sets a new value for a property and notifies about the change.
		/// </summary>
		/// <typeparam name="T">The property type.</typeparam>
		/// <param name="newValue">The new value for the property.</param>
		/// <param name="propertyName">The property name.</param>
		/// <returns>true if the value was changed, otherwise false.</returns>
		/// <remarks>
		/// The order of actions is defined as the following:
		/// <list type="number">
		///   <item>Change property value, accessible through <see cref="GetValue"/></item>
		///   <item>Call On…Changed method, if available</item>
		///   <item>Raise <see cref="PropertyChanged"/> event for the property</item>
		///   <item>Call On…Changed method and raise <see cref="PropertyChanged"/> event for
		///     dependent properties in no particular order</item>
		/// </list>
		/// If code must be executed before the first event is raised, the On…Changed method is the
		/// recommended place for that. This keeps the property setter clean and allows using the
		/// default notification method.
		/// </remarks>
		protected bool SetValue<T>(T newValue, [CallerMemberName] string propertyName = null)
		{
			if (propertyName == null) throw new ArgumentNullException("propertyName");

			if (EqualityComparer<T>.Default.Equals(newValue, GetValue<T>(propertyName)))
				return false;

			backingFields[propertyName] = newValue;
			OnPropertyChanged(propertyName);
			return true;
		}

		/// <summary>
		/// Sets a new value for a property and notifies about the change.
		/// </summary>
		/// <typeparam name="T">The property type.</typeparam>
		/// <param name="newValue">The new value for the property.</param>
		/// <param name="propertyName">The property name.</param>
		/// <param name="additionalPropertyNames">Names of additional properties that must be notified when the value has changed.</param>
		/// <returns>true if the value was changed, otherwise false.</returns>
		/// <remarks>
		/// The order of actions is defined as the following:
		/// <list type="number">
		///   <item>Change property value, accessible through <see cref="GetValue"/></item>
		///   <item>Call On…Changed method, if available</item>
		///   <item>Raise <see cref="PropertyChanged"/> event for the property</item>
		///   <item>Call On…Changed method and raise <see cref="PropertyChanged"/> event for
		///     dependent properties in no particular order</item>
		/// </list>
		/// If code must be executed before the first event is raised, the On…Changed method is the
		/// recommended place for that. This keeps the property setter clean and allows using the
		/// default notification method.
		/// </remarks>
		protected bool SetValue<T>(T newValue, [CallerMemberName] string propertyName = null, params string[] additionalPropertyNames)
		{
			if (SetValue(newValue, propertyName))
			{
				OnPropertyChanged(additionalPropertyNames);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Sets a new value for a property, but does not notify about the change.
		/// </summary>
		/// <typeparam name="T">The property type.</typeparam>
		/// <param name="newValue">The new value for the property.</param>
		/// <param name="propertyName">The property name.</param>
		/// <returns>true if the value was changed, otherwise false.</returns>
		/// <remarks>
		/// <para>
		/// This method does not call On…Changed methods and does not raise the
		/// <see cref="PropertyChanged"/> event for the indicated or any dependent properties.
		/// </para>
		/// <para>
		/// If you need to use this method to execute other code before the
		/// <see cref="PropertyChanged"/> event may be raised for the property, and then call
		/// <see cref="OnPropertyChanged"/> manually afterwards, you should consider moving this
		/// pre-processing code out of the property setter into a separate On…Changed method
		/// marked with the <see cref="PropertyChangedHandlerAttribute"/> and call the regular
		/// <see cref="SetValue"/> method instead.
		/// </para>
		/// </remarks>
		protected bool SetValueSuppressNotify<T>(T newValue, [CallerMemberName] string propertyName = null)
		{
			if (propertyName == null) throw new ArgumentNullException("propertyName");

			if (EqualityComparer<T>.Default.Equals(newValue, GetValue<T>(propertyName)))
				return false;

			backingFields[propertyName] = newValue;
			return true;
		}

		/// <summary>
		/// Checks whether the new property value has changed and updates the backing field.
		/// </summary>
		/// <typeparam name="T">Value type of the property.</typeparam>
		/// <param name="value">New property value.</param>
		/// <param name="field">Backing field.</param>
		/// <param name="propertyNames">Names of the properties to notify updated.</param>
		/// <returns>true if the value has changed, false otherwise.</returns>
		[Obsolete("Use the SetValue method instead.")]
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

		#endregion Property access methods

		#region View state

		private ExpandoObject viewState = new ExpandoObject();

		/// <summary>
		/// Gets a dynamic object that can be used by the view to save its view state.
		/// </summary>
		public dynamic ViewState
		{
			get { return viewState; }
		}

		/// <summary>
		/// Gets a value indicating whether the view state is not empty.
		/// </summary>
		protected bool HasViewState
		{
			get { return ((IDictionary<string, object>) ViewState).Count > 0; }
		}

		/// <summary>
		/// Clears all data from the view state.
		/// </summary>
		protected void ClearViewState()
		{
			((IDictionary<string, object>) ViewState).Clear();
		}

		#endregion

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
			if (str == null) return null;
			str = str.Trim();
			if (str.Length == 0) return "";
			try
			{
				long l = Convert.ToInt64(str);
				return Convert.ToString(l);
			}
			catch
			{
				return str;
			}
		}

		/// <summary>
		/// Sanitizes a user input string for a double type property.
		/// </summary>
		/// <param name="str">The text entered by the user.</param>
		/// <returns>The sanitized string value.</returns>
		protected string SanitizeDouble(string str)
		{
			if (str == null) return null;
			str = str.Trim();
			if (str.Length == 0) return "";
			try
			{
				double d = Convert.ToDouble(str);
				return Convert.ToString(d);
			}
			catch
			{
				return str;
			}
		}

		/// <summary>
		/// Sanitizes a user input string for a local date value.
		/// </summary>
		/// <param name="str">The text entered by the user.</param>
		/// <returns>The sanitized string value.</returns>
		protected string SanitizeDate(string str)
		{
			if (str == null) return null;
			str = str.Trim();
			if (str.Length == 0) return "";
			try
			{
				DateTime d = Convert.ToDateTime(str);
				return d.ToShortDateString();
			}
			catch
			{
				return str;
			}
		}

		/// <summary>
		/// Sanitizes a user input string for an ISO date value.
		/// </summary>
		/// <param name="str">The text entered by the user.</param>
		/// <returns>The sanitized string value.</returns>
		protected string SanitizeIsoDate(string str)
		{
			if (str == null) return null;
			str = str.Trim();
			if (str.Length == 0) return "";
			try
			{
				DateTime d = Convert.ToDateTime(str);
				return d.ToString("yyyy-MM-dd");
			}
			catch
			{
				return str;
			}
		}

		#endregion Data input cleanup

		#region Data validation

		/// <summary>
		/// The message text that indicates how many more error messages were not returned.
		/// </summary>
		public static string MoreErrorsMessage = "({0} more messages are not displayed)";
		//public static string MoreErrorsMessage = "({0} weitere Meldungen werden nicht angezeigt)";

		/// <summary>
		/// Gets a value indicating whether the view model instance has validation errors.
		/// </summary>
		public bool HasErrors
		{
			get
			{
				return GetErrors(1).Count > 0;
			}
		}

		/// <summary>
		/// Gets the validation errors in this view model instance.
		/// </summary>
		public Dictionary<string, string> Errors
		{
			get
			{
				return GetErrors();
			}
		}

		private bool validationPending;

		/// <summary>
		/// Raises PropertyChanged events for <see cref="Errors"/> and <see cref="HasErrors"/> with
		/// Loaded dispatcher priority. Multiple calls to this function before the asynchronous
		/// action has been started are ignored.
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
					Dispatcher.CurrentDispatcher.BeginInvoke(
						(Action) delegate
						{
							// Reset flag first (there's no locking)
							validationPending = false;
							OnPropertyChanged("Errors");
							OnPropertyChanged("HasErrors");
						},
						DispatcherPriority.Loaded);
				}
			}
		}

		/// <summary>
		/// Determines all validation errors in this view model instance.
		/// </summary>
		/// <param name="maxCount">The maximum number of errors to return.</param>
		/// <returns>A dictionary that holds the error message for each property name.</returns>
		protected Dictionary<string, string> GetErrors(int maxCount = 5)
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
						if (msg == null)
						{
							// Also try locally stored error messages
							msg = GetPropertyError(prop.Name);
						}
						if (msg != null)
						{
							if (dict.Count < maxCount)
							{
								dict[prop.Name] = msg;
								if (maxCount == 1)
								{
									return dict;
								}
							}
							else
							{
								// Remember the last key and message in case it's the only one.
								// (We don't need to waste space with a "more" note when we can show
								// the actual single message instead.)
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
				// There's only one more message, so show it
				dict[lastKey] = lastValue;
			}
			else if (more > 0)
			{
				// Add an item at the end, about how many more messages were not returned
				dict["zzz"] = string.Format(MoreErrorsMessage, more);
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

		private Dictionary<string, string> propertyErrorMessages = new Dictionary<string, string>();

		/// <summary>
		/// Sets a validation error message for a property.
		/// </summary>
		/// <param name="propertyName">The name of the property to validate.</param>
		/// <param name="message">The error message.</param>
		protected void SetPropertyError(string propertyName, string message)
		{
			propertyErrorMessages[propertyName] = message;
			RaiseValidationUpdated();
		}

		/// <summary>
		/// Clears the validation error message for a property.
		/// </summary>
		/// <param name="propertyName">The name of the property to validate.</param>
		protected void ClearPropertyError(string propertyName)
		{
			propertyErrorMessages.Remove(propertyName);
			RaiseValidationUpdated();
		}

		/// <summary>
		/// Returns the validation error message for a property.
		/// </summary>
		/// <param name="propertyName">The name of the property to validate.</param>
		/// <returns>The error message if set; otherwise, null.</returns>
		protected string GetPropertyError(string propertyName)
		{
			string msg;
			propertyErrorMessages.TryGetValue(propertyName, out msg);
			return msg;
		}

		#endregion Data validation

		#region INotifyPropertyChanged members

		/// <summary>
		/// Raised when a property on this object has a new value.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Raises this object's PropertyChanged event for one property and all its dependent
		/// properties.
		/// </summary>
		/// <param name="propertyName">The name of the property that has a new value.</param>
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			// Verify that the notified property actually exists in the current object. This can
			// reveal misspelled properties and missing notifications. It's only checked in Debug
			// builds for performance and stability reasons.
#if DEBUG
			if (!TypeDescriptor.GetProperties(this).OfType<PropertyDescriptor>().Any(d => d.Name == propertyName))
			{
				throw new ArgumentException("Notifying a change of non-existing property " + this.GetType().Name + "." + propertyName);
			}
#endif

			// Call On…Changed methods that are marked with the [PropertyChangedHandler] attribute
			foreach (var changeHandler in PropertyChangedHandlers.GetValuesOrEmpty(propertyName))
			{
				changeHandler();
			}

			// Raise PropertyChanged event, if there is a handler listening to it
			var handler = PropertyChanged;
			if (handler != null)
			{
				handler(this, new PropertyChangedEventArgs(propertyName));
			}

			// Also notify changes for dependent properties
			// (This could be moved inside the (handler != null) check for improved performance, but
			// then it could miss out On…Changed method calls for dependent properties, which might
			// be a nice feature.)
			foreach (var dependentPropertyName in DependentNotifications.GetValuesOrEmpty(propertyName))
			{
				OnPropertyChanged(dependentPropertyName);
			}
		}

		/// <summary>
		/// Raises this object's PropertyChanged event for multiple properties and all their
		/// dependent properties.
		/// </summary>
		/// <param name="propertyNames">The names of the properties that have a new value.</param>
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
		/// <typeparam name="TProperty">Value type of the property.</typeparam>
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

		#endregion INotifyPropertyChanged members

		#region Dependent notifications

		/// <summary>
		/// Contains dependent property relationships for all types. This is used to share the
		/// reflection knowledge of a certain type among all its instances. Access is locked through
		/// the field itself.
		/// </summary>
		private static Dictionary<Type, CollectionDictionary<string, string>> allDependentNotifications = new Dictionary<Type, CollectionDictionary<string, string>>();

		/// <summary>
		/// Contains all dependent property relationships of the current type. This is used for
		/// lookup to have a local, lock-free copy for performance reasons.
		/// </summary>
		private CollectionDictionary<string, string> dependentNotifications;

		/// <summary>
		/// Determines and caches all dependent properties with reflection.
		/// </summary>
		private CollectionDictionary<string, string> DependentNotifications
		{
			get
			{
				if (dependentNotifications == null)
				{
					lock (allDependentNotifications)
					{
						if (!allDependentNotifications.TryGetValue(GetType(), out dependentNotifications))
						{
							dependentNotifications = new CollectionDictionary<string, string>();
							foreach (var p in GetType().GetProperties())
							{
								foreach (NotifiesOnAttribute a in p.GetCustomAttributes(typeof(NotifiesOnAttribute), false))
								{
									// Verify that the notified property actually exists in the current object. This can
									// reveal misspelled properties and missing notifications. It's only checked in Debug
									// builds for performance and stability reasons.
#if DEBUG
									if (!TypeDescriptor.GetProperties(this).OfType<PropertyDescriptor>().Any(d => d.Name == a.Name))
									{
										throw new ArgumentException("Specified property " + this.GetType().Name + "." + p.Name +
											" to notify on non-existing property " + a.Name);
									}
#endif
									dependentNotifications.Add(a.Name, p.Name);
								}
							}
							allDependentNotifications[GetType()] = dependentNotifications;
						}
					}
				}
				return dependentNotifications;
			}
		}

		#endregion Dependent notifications

		#region Property changed handler methods

		/// <summary>
		/// Contains property changed handlers for all types. This is used to share the reflection
		/// knowledge of a certain type among all its instances. Access is locked through the field
		/// itself.
		/// </summary>
		private static Dictionary<Type, CollectionDictionary<string, Action>> allPropertyChangedHandlers = new Dictionary<Type, CollectionDictionary<string, Action>>();

		/// <summary>
		/// Contains all property changed handlers of the current type. This is used for lookup to
		/// have a local, lock-free copy for performance reasons.
		/// </summary>
		private CollectionDictionary<string, Action> propertyChangedHandlers;

		/// <summary>
		/// Determines and caches all property changed handler methods with reflection.
		/// </summary>
		private CollectionDictionary<string, Action> PropertyChangedHandlers
		{
			get
			{
				if (propertyChangedHandlers == null)
				{
					lock (allPropertyChangedHandlers)
					{
						if (!allPropertyChangedHandlers.TryGetValue(GetType(), out propertyChangedHandlers))
						{
							propertyChangedHandlers = new CollectionDictionary<string, Action>();
							foreach (var method in GetType().GetMethods(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance))
							{
								var m = method;
								while (true)
								{
									foreach (PropertyChangedHandlerAttribute a in m.GetCustomAttributes(typeof(PropertyChangedHandlerAttribute), false))
									{
										// Verify that the notified property actually exists in the current object. This can
										// reveal misspelled properties and missing notifications. It's only checked in Debug
										// builds for performance and stability reasons.
#if DEBUG
										if (!TypeDescriptor.GetProperties(this).OfType<PropertyDescriptor>().Any(d => d.Name == a.Name))
										{
											throw new ArgumentException("Specified method " + this.GetType().Name + "." + m.Name +
												" to handle changes of non-existing property " + a.Name);
										}
#endif
										Action action = (Action) Delegate.CreateDelegate(typeof(Action), this, m);
										propertyChangedHandlers.Add(a.Name, action);
									}

									// Check base methods for the attribute
									if (!m.IsVirtual) break;
									var baseMethod = m.GetBaseDefinition();
									if (baseMethod == m) break;
									m = baseMethod;
								}
							}
							allPropertyChangedHandlers[GetType()] = propertyChangedHandlers;
						}
					}
				}
				return propertyChangedHandlers;
			}
		}

		#endregion Property changed handler methods
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

	#region Attributes

	/// <summary>
	/// Declares that a property that depends on another property should raise a notification when
	/// the independent property is raising a notification.
	/// </summary>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class NotifiesOnAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="NotifiesOnAttribute"/> class.
		/// </summary>
		/// <param name="propertyName">The name of the independent property.</param>
		/// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is null.</exception>
		public NotifiesOnAttribute(string propertyName)
		{
			if (propertyName == null) throw new ArgumentNullException("propertyName");
			Name = propertyName;
		}

		/// <summary>
		/// Gets the name of the independent property.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// A unique identifier for this attribute.
		/// </summary>
		public override object TypeId
		{
			get { return this; }
		}
	}

	/// <summary>
	/// Declares that a method is invoked when the specified property is changed, before the
	/// notification event.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class PropertyChangedHandlerAttribute : Attribute
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PropertyChangedHandlerAttribute"/> class.
		/// </summary>
		/// <param name="propertyName">The name of the property.</param>
		/// <exception cref="ArgumentNullException"><paramref name="propertyName"/> is null.</exception>
		public PropertyChangedHandlerAttribute(string propertyName)
		{
			if (propertyName == null) throw new ArgumentNullException("propertyName");
			Name = propertyName;
		}

		/// <summary>
		/// Gets the name of the independent property.
		/// </summary>
		public string Name { get; private set; }
	}

	#endregion Attributes
}
