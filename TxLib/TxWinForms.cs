// TxLib – Tx Translation & Localisation for .NET and WPF
// © Yves Goergen, Made in Germany
// Website: http://unclassified.software/source/txtranslation
//
// This library is free software: you can redistribute it and/or modify it under the terms of
// the GNU Lesser General Public License as published by the Free Software Foundation, version 3.
//
// This library is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY;
// without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.
// See the GNU Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public License along with this
// library. If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System.Windows.Forms;

namespace Unclassified.TxLib
{
	/// <summary>
	/// Provides data binding of Windows Forms controls to translated texts.
	/// </summary>
	public class TxDictionaryBinding : IDisposable, INotifyPropertyChanged
	{
		#region Static helpers

		/// <summary>
		/// Contains all object instances that are assigned with a binding of a control and its
		/// property name.
		/// </summary>
		private struct BindingData
		{
			public TxDictionaryBinding TxDictionaryBinding;
			public Binding Binding;

			public BindingData(TxDictionaryBinding txDictionaryBinding, Binding binding)
			{
				TxDictionaryBinding = txDictionaryBinding;
				Binding = binding;
			}
		}

		/// <summary>
		/// Contains all current control bindings.
		/// </summary>
		/// <remarks>
		/// Access to this object is synchronised through controlBindings.
		/// </remarks>
		private static Dictionary<Control, Dictionary<string, BindingData>> controlBindings =
			new Dictionary<Control, Dictionary<string, BindingData>>();

		/// <summary>
		/// Adds or replaces text dictionary bindings for the Text property of a control and all
		/// its child controls.
		/// </summary>
		/// <param name="control">Control or Form to start with.</param>
		public static void AddTextBindings(Control control)
		{
			// Check whether the control's Text property has a value that looks like a text key,
			// then add the binding for it
			if (control.Text.Length > 2 && control.Text.StartsWith("[") && control.Text.EndsWith("]"))
			{
				string key = control.Text.Substring(1, control.Text.Length - 2);
				AddBinding(control, "Text", key);
			}

			// Recurse to all child controls
			foreach (Control child in control.Controls)
			{
				AddTextBindings(child);
			}
		}

		/// <summary>
		/// Adds or replaces a text dictionary binding to a control's property.
		/// </summary>
		/// <param name="control">Control to add the binding to.</param>
		/// <param name="propertyName">Name of the control's property to bind to the dictionary.</param>
		/// <param name="key">Text key to use for the binding.</param>
		public static void AddBinding(Control control, string propertyName, string key)
		{
			lock (controlBindings)
			{
				Dictionary<string, BindingData> propertyBindings;
				if (!controlBindings.TryGetValue(control, out propertyBindings))
				{
					// This is the first bound property for the control
					propertyBindings = new Dictionary<string, BindingData>();
					controlBindings.Add(control, propertyBindings);

					// Be notified when the control is disposed
					control.Disposed += Control_Disposed;
				}

				BindingData bindingData;
				if (propertyBindings.TryGetValue(propertyName, out bindingData))
				{
					// This property of the control is already bound, remove that first
					// Remove the binding from the control
					control.DataBindings.Remove(bindingData.Binding);

					// Dispose of the Tx binding instance
					bindingData.TxDictionaryBinding.Dispose();
				}

				// Add the new binding to the control and keep track of it
				TxDictionaryBinding db = new TxDictionaryBinding(key);
				bindingData = new BindingData(
					db,
					control.DataBindings.Add(propertyName, db, "Text"));
				propertyBindings[propertyName] = bindingData;
			}
		}

		/// <summary>
		/// Handles the Disposed event of a bound control. Cleans up internal data structures.
		/// </summary>
		/// <param name="sender">The control that has been disposed.</param>
		/// <param name="args">Unused.</param>
		private static void Control_Disposed(object sender, EventArgs args)
		{
			Control control = sender as Control;
			if (control != null)
			{
				lock (controlBindings)
				{
					Dictionary<string, BindingData> propertyBindings;
					if (controlBindings.TryGetValue(control, out propertyBindings))
					{
						foreach (BindingData bindingData in propertyBindings.Values)
						{
							// Dispose of the Tx binding instance
							bindingData.TxDictionaryBinding.Dispose();
						}

						// Clean up internal structures
						controlBindings.Remove(control);
					}
				}
			}
		}

		/// <summary>
		/// Removes a text dictionary binding from a control's property. Does nothing if the
		/// property was not bound.
		/// </summary>
		/// <param name="control">Control to remove the binding from.</param>
		/// <param name="propertyName">Name of the control's property to unbind.</param>
		public static void RemoveBinding(Control control, string propertyName)
		{
			lock (controlBindings)
			{
				Dictionary<string, BindingData> propertyBindings;
				if (controlBindings.TryGetValue(control, out propertyBindings))
				{
					BindingData bindingData;
					if (propertyBindings.TryGetValue(propertyName, out bindingData))
					{
						// Remove the binding from the control
						control.DataBindings.Remove(bindingData.Binding);

						// Dispose of the Tx binding instance
						bindingData.TxDictionaryBinding.Dispose();

						// Clean up internal structures
						propertyBindings.Remove(propertyName);
						if (controlBindings[control].Count == 0)
						{
							controlBindings.Remove(control);

							// Also remove the control Disposed handler with the last binding of this control
							control.Disposed -= Control_Disposed;
						}
					}
				}
			}
		}

		#endregion Static helpers

		#region Private data

		private string key;
		private SynchronizationContext context;

		#endregion Private data

		#region INotifyPropertyChanged members

		/// <summary>
		/// Occurs when a property value changes.
		/// </summary>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>
		/// Invoked whenever the value of any property on this instance has been updated.
		/// </summary>
		/// <param name="propertyName">The name of the specific property that changed.</param>
		protected void OnPropertyChanged(string propertyName)
		{
			PropertyChangedEventHandler handler = PropertyChanged;
			if (handler != null)
			{
				// The event must be posted to the UI thread manually, the data binding engine of
				// Windows Forms will not do that for us and instead silently cancel updating the
				// data in the bound controls.
				context.Post(delegate
				{
					handler(this, new PropertyChangedEventArgs(propertyName));
				}, null);
			}
		}

		#endregion INotifyPropertyChanged members

		#region Constructors

		/// <summary>
		/// Initialises a new instance of the TxDictionaryBinding class.
		/// </summary>
		/// <param name="key">Text key to use for this binding.</param>
		public TxDictionaryBinding(string key)
		{
			this.key = key;
			context = SynchronizationContext.Current;
			if (context == null)
				throw new InvalidOperationException("SynchronizationContext is not available. The DictionaryBinding instance must be created on the UI thread.");

			// Be notified about changes to the dictionary to pass them on to bound controls
			Tx.DictionaryChanged += Tx_DictionaryChanged;
		}

		#endregion Constructors

		#region Public data properties

		/// <summary>
		/// Gets the text with the configured key from the global dictionary.
		/// </summary>
		public string Text { get { return Tx.T(key); } }

		#endregion Public data properties

		#region IDisposable members

		/// <summary>
		/// Releases the resources that are used by the TxDictionaryBinding object.
		/// </summary>
		public void Dispose()
		{
			// Deregister the event to avoid memory leaks
			Tx.DictionaryChanged -= Tx_DictionaryChanged;
		}

		#endregion IDisposable members

		#region Dictionary changed handler

		private void Tx_DictionaryChanged(object sender, EventArgs args)
		{
			OnPropertyChanged("Text");
		}

		#endregion Dictionary changed handler
	}
}
