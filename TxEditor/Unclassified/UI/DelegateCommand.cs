using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Input;
using System.Windows.Threading;

namespace Unclassified.UI
{
	/// <summary>
	/// Provides an <see cref="ICommand"/> implementation which relays the <see cref="Execute"/> and <see cref="CanExecute"/>
	/// method to the specified delegates.
	/// </summary>
	public class DelegateCommand : ICommand
	{
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
					disabledCommand = new DelegateCommand(() => { }, () => false);
				}
				return disabledCommand;
			}
		}

		private readonly Action<object> execute;
		private readonly Func<object, bool> canExecute;
		private List<WeakReference> weakHandlers;


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
			if (execute == null) { throw new ArgumentNullException("execute"); }

			this.execute = execute;
			this.canExecute = canExecute;
		}


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
				if (weakHandlers == null) { return; }

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
		/// Defines the method that determines whether the command can execute in its current state.
		/// </summary>
		/// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
		/// <returns>
		/// true if this command can be executed; otherwise, false.
		/// </returns>
		[DebuggerStepThrough]
		public bool CanExecute(object parameter)
		{
			return canExecute != null ? canExecute(parameter) : true;
		}

		/// <summary>
		/// Defines the method to be called when the command is invoked.
		/// </summary>
		/// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
		/// <exception cref="InvalidOperationException">The <see cref="CanExecute"/> method returns <c>false.</c></exception>
		[DebuggerStepThrough]
		public void Execute(object parameter)
		{
			if (!CanExecute(parameter))
			{
				throw new InvalidOperationException("The command cannot be executed because the canExecute action returned false.");
			}

			execute(parameter);
		}

		public void Execute()
		{
			Execute(null);
		}

		public bool TryExecute(object parameter)
		{
			if (CanExecute(parameter))
			{
				Execute(parameter);
				return true;
			}
			return false;
		}

		public bool TryExecute()
		{
			return TryExecute(null);
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
				Dispatcher.CurrentDispatcher.BeginInvoke(
					(Action<EventArgs>) OnCanExecuteChanged,
					DispatcherPriority.Loaded,
					EventArgs.Empty);
				raiseCanExecuteChangedPending = true;
			}
		}

		/// <summary>
		/// Raises the <see cref="E:CanExecuteChanged"/> event.
		/// </summary>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		[DebuggerStepThrough]
		protected virtual void OnCanExecuteChanged(EventArgs e)
		{
			//var field = execute.Target.GetType().GetField("execute");
			//Action a = null;
			//if (field != null)
			//{
			//    a = field.GetValue(execute.Target) as Action;
			//}
			//if (a != null)
			//    PerfLog.Enter("DelegateCommand.OnCanExecuteChanged()", a.Method.Name + "() on " + a.Target.ToString());
			//else
			//    PerfLog.Enter("DelegateCommand.OnCanExecuteChanged()", "(unknown)");

			raiseCanExecuteChangedPending = false;
			PurgeWeakHandlers();
			if (weakHandlers == null)
			{
				//PerfLog.Leave("DelegateCommand.OnCanExecuteChanged()", "no active handlers");
				return;
			}

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
			if (weakHandlers == null) { return; }

			for (int i = weakHandlers.Count - 1; i >= 0; i--)
			{
				if (!weakHandlers[i].IsAlive)
				{
					weakHandlers.RemoveAt(i);
				}
			}

			if (weakHandlers.Count == 0) { weakHandlers = null; }
		}
	}
}
