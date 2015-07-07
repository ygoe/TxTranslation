using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Unclassified.UI
{
	/// <summary>
	/// Provides an <see cref="ICommand"/> implementation which relays the Execute and CanExecute
	/// method to the specified delegates.
	/// </summary>
	public class DelegateCommand : ICommand
	{
		#region Static disabled command

		private static DelegateCommand disabledCommand;

		/// <summary>
		/// Gets a DelegateCommand instance that can never execute.
		/// </summary>
		public static DelegateCommand Disabled
		{
			get
			{
				if (disabledCommand == null)
				{
					disabledCommand = new DelegateCommand(() => { }) { IsEnabled = false };
				}
				return disabledCommand;
			}
		}

		#endregion Static disabled command

		#region Private data

		private readonly Action<object> execute;
		private readonly Func<object, bool> canExecute;
		private List<WeakReference> weakHandlers;
		private bool isEnabled = true;

		#endregion Private data

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="DelegateCommand"/> class.
		/// </summary>
		/// <param name="execute">Delegate to execute when Execute is called on the command.</param>
		/// <exception cref="ArgumentNullException">The execute argument must not be null.</exception>
		public DelegateCommand(Action execute)
			: this(execute, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DelegateCommand"/> class.
		/// </summary>
		/// <param name="execute">Delegate to execute when Execute is called on the command.</param>
		/// <exception cref="ArgumentNullException">The execute argument must not be null.</exception>
		public DelegateCommand(Action<object> execute)
			: this(execute, null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DelegateCommand"/> class.
		/// </summary>
		/// <param name="execute">Delegate to execute when Execute is called on the command.</param>
		/// <param name="canExecute">Delegate to execute when CanExecute is called on the command.</param>
		/// <exception cref="ArgumentNullException">The execute argument must not be null.</exception>
		public DelegateCommand(Action execute, Func<bool> canExecute)
			: this(execute != null ? p => execute() : (Action<object>) null, canExecute != null ? p => canExecute() : (Func<object, bool>) null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DelegateCommand"/> class.
		/// </summary>
		/// <param name="execute">Delegate to execute when Execute is called on the command.</param>
		/// <param name="canExecute">Delegate to execute when CanExecute is called on the command.</param>
		/// <exception cref="ArgumentNullException">The execute argument must not be null.</exception>
		public DelegateCommand(Action<object> execute, Func<object, bool> canExecute)
		{
			if (execute == null)
				throw new ArgumentNullException("execute");

			this.execute = execute;
			this.canExecute = canExecute;
		}

		#endregion Constructors

		#region CanExecuteChanged event

		/// <summary>
		/// Occurs when changes occur that affect whether or not the command should execute.
		/// </summary>
		public event EventHandler CanExecuteChanged
		{
			add
			{
				if (weakHandlers == null)
				{
					weakHandlers = new List<WeakReference>(new[] { new WeakReference(value) });
				}
				else
				{
					weakHandlers.Add(new WeakReference(value));
				}
			}
			remove
			{
				if (weakHandlers == null) return;

				for (int i = weakHandlers.Count - 1; i >= 0; i--)
				{
					WeakReference weakReference = weakHandlers[i];
					EventHandler handler = weakReference.Target as EventHandler;
					if (handler == null || handler == value)
					{
						weakHandlers.RemoveAt(i);
					}
				}
			}
		}

		/// <summary>
		/// Raises the <see cref="E:CanExecuteChanged"/> event.
		/// </summary>
		[DebuggerStepThrough]
		public void RaiseCanExecuteChanged()
		{
			OnCanExecuteChanged(EventArgs.Empty);
		}

		private bool raiseCanExecuteChangedPending;

		/// <summary>
		/// Raises the <see cref="E:CanExecuteChanged"/> event after all other processing has
		/// finished. Multiple calls to this function before the asynchronous action has been
		/// started are ignored.
		/// </summary>
		[DebuggerStepThrough]
		public void RaiseCanExecuteChangedAsync()
		{
			if (!raiseCanExecuteChangedPending)
			{
				// Don't do anything if not on the UI thread. The dispatcher event will never be
				// fired there and probably there's nobody interested in changed properties
				// anyway on that thread.
				if (Dispatcher.CurrentDispatcher == Application.Current.Dispatcher)
				{
					Dispatcher.CurrentDispatcher.BeginInvoke(
						(Action<EventArgs>) OnCanExecuteChanged,
						DispatcherPriority.Loaded,
						EventArgs.Empty);
					raiseCanExecuteChangedPending = true;
				}
			}
		}

		/// <summary>
		/// Raises the <see cref="E:CanExecuteChanged"/> event.
		/// </summary>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[DebuggerStepThrough]
		protected virtual void OnCanExecuteChanged(EventArgs e)
		{
			raiseCanExecuteChangedPending = false;
			PurgeWeakHandlers();
			if (weakHandlers == null) return;

			WeakReference[] handlers = weakHandlers.ToArray();
			foreach (WeakReference reference in handlers)
			{
				EventHandler handler = reference.Target as EventHandler;
				if (handler != null)
				{
					handler(this, e);
				}
			}
		}

		[DebuggerStepThrough]
		private void PurgeWeakHandlers()
		{
			if (weakHandlers == null) return;

			for (int i = weakHandlers.Count - 1; i >= 0; i--)
			{
				if (!weakHandlers[i].IsAlive)
				{
					weakHandlers.RemoveAt(i);
				}
			}

			if (weakHandlers.Count == 0)
				weakHandlers = null;
		}

		#endregion CanExecuteChanged event

		#region ICommand methods

		/// <summary>
		/// Defines the method that determines whether the command can execute in its current state.
		/// </summary>
		/// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to null.</param>
		/// <returns>true if this command can be executed; otherwise, false.</returns>
		[DebuggerStepThrough]
		public bool CanExecute(object parameter)
		{
			return canExecute != null ? canExecute(parameter) : isEnabled;
		}

		/// <summary>
		/// Convenience method that invokes CanExecute without parameters.
		/// </summary>
		/// <returns>true if this command can be executed; otherwise, false.</returns>
		public bool CanExecute()
		{
			return CanExecute(null);
		}

		/// <summary>
		/// Defines the method to be called when the command is invoked.
		/// </summary>
		/// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to null.</param>
		/// <exception cref="InvalidOperationException">The <see cref="CanExecute(object)"/> method returns false.</exception>
		[DebuggerStepThrough]
		public void Execute(object parameter)
		{
			if (!CanExecute(parameter))
			{
				throw new InvalidOperationException("The command cannot be executed because CanExecute returned false.");
			}

			execute(parameter);
		}

		/// <summary>
		/// Convenience method that invokes the command without parameters.
		/// </summary>
		/// <exception cref="InvalidOperationException">The <see cref="CanExecute(object)"/> method returns false.</exception>
		public void Execute()
		{
			Execute(null);
		}

		/// <summary>
		/// Invokes the command if the <see cref="CanExecute(object)"/> method returns true.
		/// </summary>
		/// <param name="parameter">Data used by the command. If the command does not require data to be passed, this object can be set to null.</param>
		/// <returns>true if this command was executed; otherwise, false.</returns>
		public bool TryExecute(object parameter)
		{
			if (CanExecute(parameter))
			{
				Execute(parameter);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Convenience method that invokes the command without parameters if the
		/// <see cref="CanExecute(object)"/> method returns true.
		/// </summary>
		/// <returns>true if this command was executed; otherwise, false.</returns>
		public bool TryExecute()
		{
			return TryExecute(null);
		}

		#endregion ICommand methods

		#region Enabled state

		/// <summary>
		/// Gets or sets a value indicating whether the current DelegateCommand is enabled. This
		/// property is only effective if no canExecute function was passed in the constructor.
		/// </summary>
		public bool IsEnabled
		{
			get
			{
				return isEnabled;
			}
			set
			{
				if (value != isEnabled)
				{
					isEnabled = value;
					RaiseCanExecuteChanged();
				}
			}
		}

		#endregion Enabled state
	}
}
