using System;
using System.Collections.Generic;
using System.Threading;

namespace Unclassified.Util
{
	/// <summary>
	/// Implements a timer that invokes a method after a user-defined timeout.
	/// </summary>
	/// <example>
	/// <code lang="C#"><![CDATA[
	/// // Variable to keep track of the instance. This is not required for normal operation.
	/// private DelayedCall dc = null;
	///
	/// private void button1_Click(object sender, EventArgs args)
	/// {
	///     // Change button text in 2 seconds
	///     dc = DelayedCall<string>.Start(SetButtonText, "I was clicked", 2000);
	/// }
	///
	/// private void SetButtonText(string newText)
	/// {
	///     button1.Text = newText;
	/// }
	///
	/// private void HurryUpButton_Click(object sender, EventArgs args)
	/// {
	///     // Invoke SetButtonText now, if the timeout isn't elapsed yet
	///     if (dc != null) dc.Fire();
	///
	///     // Prepare a DelayedCall object for later use
	///     DelayedCall exitDelay = DelayedCall.Create(Application.Exit, 0);
	///     // Now start the timeout with 0.5 seconds
	///     exitDelay.Reset(500);
	/// }
	/// ]]></code>
	/// </example>
	public class DelayedCall : IDisposable
	{
		/// <summary>
		/// Represents a callback method.
		/// </summary>
		public delegate void Callback();

		/// <summary>
		/// List to keep track of all created <see cref="DelayedCall"/> instances.
		/// </summary>
		protected static List<DelayedCall> instances = new List<DelayedCall>();

		/// <summary>
		/// Indicates whether the instance has been disposed.
		/// </summary>
		private bool isDisposed;

		/// <summary>
		/// Timer instance.
		/// </summary>
		protected System.Timers.Timer timer;

		/// <summary>
		/// Lock object for the timer instance.
		/// </summary>
		protected object timerLock;

		/// <summary>
		/// Callback method.
		/// </summary>
		private Callback callback;

		/// <summary>
		/// Indicates whether the delayed call has been cancelled.
		/// </summary>
		protected bool isCancelled;

		/// <summary>
		/// Synchronisation context to invoke the callback method in.
		/// </summary>
		protected SynchronizationContext context;

		// More information on the SynchronizationContext thing: http://www.codeproject.com/cs/threads/SyncContextTutorial.asp

		/// <summary>
		/// Initialises a new instance of the <see cref="DelayedCall"/> class.
		/// </summary>
		protected DelayedCall()
		{
			timerLock = new object();
		}

		/// <summary>
		/// Frees all resources of this instance.
		/// </summary>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Frees all resources of this instance.
		/// </summary>
		/// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
		protected virtual void Dispose(bool disposing)
		{
			if (!isDisposed)
			{
				if (disposing)
				{
					Cancel();
					timer.Dispose();
				}
				isDisposed = true;
			}
		}

		/// <summary>
		/// Finalizer.
		/// </summary>
		~DelayedCall()
		{
			Dispose(false);
		}

		/// <summary>
		/// Gets a value indicating whether the instance has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return isDisposed; }
		}

		/// <summary>
		/// Creates a new <see cref="DelayedCall"/> instance.
		/// </summary>
		/// <param name="cb">The callback function.</param>
		/// <param name="milliseconds">Time to callback invocation in milliseconds.</param>
		/// <returns>The created <see cref="DelayedCall"/> instance that can be used for later controlling of the invocation process.</returns>
		public static DelayedCall Create(Callback cb, int milliseconds)
		{
			DelayedCall dc = new DelayedCall();
			PrepareDCObject(dc, milliseconds, false);
			dc.callback = cb;
			return dc;
		}

		/// <summary>
		/// Creates a new asynchronous <see cref="DelayedCall"/> instance. The callback function
		/// will be invoked on a ThreadPool thread.
		/// </summary>
		/// <param name="cb">The callback function.</param>
		/// <param name="milliseconds">Time to callback invocation in milliseconds.</param>
		/// <returns>The created <see cref="DelayedCall"/> instance that can be used for later controlling of the invocation process.</returns>
		public static DelayedCall CreateAsync(Callback cb, int milliseconds)
		{
			DelayedCall dc = new DelayedCall();
			PrepareDCObject(dc, milliseconds, true);
			dc.callback = cb;
			return dc;
		}

		/// <summary>
		/// Creates and starts a new <see cref="DelayedCall"/> instance.
		/// </summary>
		/// <param name="cb">The callback function.</param>
		/// <param name="milliseconds">Time to callback invocation in milliseconds.</param>
		/// <returns>The created <see cref="DelayedCall"/> instance that can be used for later controlling of the invocation process.</returns>
		public static DelayedCall Start(Callback cb, int milliseconds)
		{
			DelayedCall dc = Create(cb, milliseconds);
			if (milliseconds > 0) dc.Start();
			else if (milliseconds == 0) dc.FireNow();
			return dc;
		}

		/// <summary>
		/// Creates and starts a new asynchronous <see cref="DelayedCall"/> instance. The callback
		/// function will be invoked on a ThreadPool thread.
		/// </summary>
		/// <param name="cb">The callback function.</param>
		/// <param name="milliseconds">Time to callback invocation in milliseconds.</param>
		/// <returns>The created <see cref="DelayedCall"/> instance that can be used for later controlling of the invocation process.</returns>
		public static DelayedCall StartAsync(Callback cb, int milliseconds)
		{
			DelayedCall dc = CreateAsync(cb, milliseconds);
			if (milliseconds > 0) dc.Start();
			else if (milliseconds == 0) dc.FireNow();
			return dc;
		}

		/// <summary>
		/// Gets a value indicating whether the synchronous <see cref="DelayedCall"/> methods can be
		/// used in the current thread. If this property is false, the *Async methods must be used
		/// instead.
		/// </summary>
		public static bool SupportsSynchronization
		{
			get
			{
				return SynchronizationContext.Current != null;
			}
		}

		/// <summary>
		/// Prepares a <see cref="DelayedCall"/> instance.
		/// </summary>
		/// <param name="dc">The <see cref="DelayedCall"/> instance to prepare.</param>
		/// <param name="milliseconds">Time to callback invocation in milliseconds.</param>
		/// <param name="async">true to call the function asynchronously; false to call it in the current synchronisation context.</param>
		protected static void PrepareDCObject(DelayedCall dc, int milliseconds, bool async)
		{
			if (milliseconds < 0)
				throw new ArgumentOutOfRangeException("milliseconds", "The new timeout must be 0 or greater.");

			// Get the current synchronization context if required, with dummy fallback
			dc.context = null;
			if (!async)
			{
				dc.context = SynchronizationContext.Current;
				if (dc.context == null)
					throw new InvalidOperationException("Cannot delay calls synchronously on a non-UI thread. Use the *Async methods instead.");
			}
			if (dc.context == null) dc.context = new SynchronizationContext();   // Run asynchronously silently

			dc.timer = new System.Timers.Timer();
			if (milliseconds > 0) dc.timer.Interval = milliseconds;
			dc.timer.AutoReset = false;
			dc.timer.Elapsed += dc.Timer_Elapsed;

			Register(dc);
		}

		/// <summary>
		/// Registers a <see cref="DelayedCall"/> instance in the static list.
		/// </summary>
		/// <param name="dc">The <see cref="DelayedCall"/> instance to register.</param>
		protected static void Register(DelayedCall dc)
		{
			lock (instances)
			{
				// Keep a reference on this object to prevent it from being caught by the Garbage Collector
				if (!instances.Contains(dc))
					instances.Add(dc);
			}
		}

		/// <summary>
		/// Unregisters a <see cref="DelayedCall"/> instance from the static list.
		/// </summary>
		/// <param name="dc">The <see cref="DelayedCall"/> instance to unregister.</param>
		protected static void Unregister(DelayedCall dc)
		{
			lock (instances)
			{
				// Free a reference on this object to allow the Garbage Collector to dispose it
				// if no other reference is kept to it
				instances.Remove(dc);
			}
		}

		/// <summary>
		/// Gets the number of currently registered <see cref="DelayedCall"/> instances.
		/// </summary>
		public static int RegisteredCount
		{
			get
			{
				lock (instances)
				{
					return instances.Count;
				}
			}
		}

		/// <summary>
		/// Gets a value indicating whether any registered <see cref="DelayedCall"/> instance is
		/// still waiting to fire.
		/// </summary>
		public static bool IsAnyWaiting
		{
			get
			{
				lock (instances)
				{
					foreach (DelayedCall dc in instances)
					{
						if (dc.IsWaiting) return true;
					}
				}
				return false;
			}
		}

		/// <summary>
		/// Cancels all waiting <see cref="DelayedCall"/> instances.
		/// </summary>
		public static void CancelAll()
		{
			lock (instances)
			{
				foreach (DelayedCall dc in instances)
				{
					dc.Cancel();
				}
			}
		}

		/// <summary>
		/// Fires all waiting <see cref="DelayedCall"/> instances.
		/// </summary>
		public static void FireAll()
		{
			lock (instances)
			{
				foreach (DelayedCall dc in instances)
				{
					dc.Fire();
				}
			}
		}

		/// <summary>
		/// Disposes all registered <see cref="DelayedCall"/> instances. This will empty the list of
		/// registered <see cref="DelayedCall"/> instances.
		/// </summary>
		public static void DisposeAll()
		{
			lock (instances)
			{
				// Disposing an object removes it from the list, so foreach doesn't work
				while (instances.Count > 0)
				{
					instances[0].Dispose();
				}
			}
		}

		/// <summary>
		/// Called when the Timer has elapsed.
		/// </summary>
		/// <param name="sender">Unused.</param>
		/// <param name="args">Unused.</param>
		protected virtual void Timer_Elapsed(object sender, System.Timers.ElapsedEventArgs args)
		{
			// We're in the timer thread now.
			OnFire();
			Unregister(this);
		}

		/// <summary>
		/// Starts or restarts the timer.
		/// </summary>
		public void Start()
		{
			lock (timerLock)
			{
				isCancelled = false;
				timer.Start();
				Register(this);
			}
		}

		/// <summary>
		/// Stops the timer to prevent the callback function to be invoked.
		/// </summary>
		public void Cancel()
		{
			lock (timerLock)
			{
				isCancelled = true;
				Unregister(this);
				timer.Stop();
			}
		}

		/// <summary>
		/// Gets a value indicating whether the <see cref="DelayedCall"/> instance is currently
		/// waiting to fire.
		/// </summary>
		public bool IsWaiting
		{
			get
			{
				lock (timerLock)
				{
					return timer.Enabled && !isCancelled;
				}
			}
		}

		/// <summary>
		/// Fires the callback event regardless of whether the timeout has already finished.
		/// </summary>
		/// <remarks>
		/// The event will not be fired when the timeout has finished before, so when calling
		/// <see cref="Fire"/> near the time when the timer would elapse, you won't risk of calling
		/// it twice. If you need to invoke the callback function now and the timer has already
		/// elapsed, use <see cref="Reset(int)"/> with 0 milliseconds.
		/// </remarks>
		public void Fire()
		{
			lock (timerLock)
			{
				if (!IsWaiting) return;
				timer.Stop();
			}
			FireNow();
		}

		/// <summary>
		/// Fires the callback event regardless of whether the timeout has already finished or was
		/// started at all.
		/// </summary>
		/// <remarks>
		/// The event will be fired even when the timeout has finished before. This is the method
		/// used by <see cref="Fire"/> and it takes care of always unregistering the
		/// <see cref="DelayedCall"/> object from the internal list.
		/// </remarks>
		public void FireNow()
		{
			isCancelled = false;
			OnFire();
			Unregister(this);
		}

		/// <summary>
		/// Invokes the callback method.
		/// </summary>
		protected virtual void OnFire()
		{
			// We're in the timer thread now, if invoked through the timer.
			context.Post(
				delegate
				{
					// We're in the target thread now.
					lock (timerLock)
					{
						// Only fire the callback if the timer wasn't cancelled in this very moment.
						// This check needs to be done inside the lock and in the target thread because
						// the invocation in the target thread can be delayed and Cancel() - setting
						// cancelled to true - may be called after this delegate has been posted to the
						// target thread. So, checking the cancelled variable here leads to a more
						// current and definitive decision (the target thread cannot call Cancel()
						// between this check and the real invocation of the callback method.)
						if (isCancelled) return;

						// Remember that we're now executing this event. If the caller calls
						// Application.DoEvents inside this callback function, this timer event is
						// triggered another time although it's currently being processed already. By
						// setting cancelled to true, we won't pass the above test the second time we
						// arrive here.
						isCancelled = true;
					}

					if (callback != null) callback();
				},
				null);
		}

		/// <summary>
		/// Resets the timeout and restarts the timer.
		/// </summary>
		public void Reset()
		{
			lock (timerLock)
			{
				Cancel();
				Start();
				// ODOT: This sets and unsets the cancelled flag, so immediate execution of the
				//       previous timer right after this method call cannot be eliminated.
			}
		}

		/// <summary>
		/// Resets the timeout to a new value and restarts the timer.
		/// </summary>
		/// <param name="milliseconds">New timeout value in milliseconds.</param>
		public void Reset(int milliseconds)
		{
			lock (timerLock)
			{
				Cancel();
				Milliseconds = milliseconds;
				Start();
				// ODOT: This sets and unsets the cancelled flag, so immediate execution of the
				//       previous timer right after this method call cannot be eliminated.
				//       (Derived classes are also affected.)
			}
		}

		/// <summary>
		/// Gets or sets the timeout in milliseconds currently assigned to this
		/// <see cref="DelayedCall"/> instance.
		/// </summary>
		/// <remarks>
		/// Changing the timeout before it elapsed will restart the timer with the new value.
		/// </remarks>
		public int Milliseconds
		{
			get
			{
				lock (timerLock)
				{
					return (int) timer.Interval;
				}
			}
			set
			{
				lock (timerLock)
				{
					if (value < 0)
					{
						throw new ArgumentOutOfRangeException("value", "The new timeout must be 0 or greater.");
					}
					else if (value == 0)
					{
						Cancel();
						FireNow();
						Unregister(this);
					}
					else
					{
						timer.Interval = value;
						// ODOT: Is this untested?
					}
				}
			}
		}
	}

	/// <summary>
	/// Implements a timer that invokes a method with one argument after a user-defined timeout.
	/// </summary>
	/// <typeparam name="T">The type of the callback method parameter.</typeparam>
	public class DelayedCall<T> : DelayedCall
	{
		/// <summary>
		/// Represents a callback method that accepts one argument.
		/// </summary>
		/// <param name="data">Argument for the callback function.</param>
		public new delegate void Callback(T data);

		private Callback callback;
		private T data;

		/// <summary>
		/// Creates a new <see cref="DelayedCall"/> instance with one data argument.
		/// </summary>
		/// <param name="cb">The callback function.</param>
		/// <param name="data">Argument for the callback function.</param>
		/// <param name="milliseconds">Time to callback invocation in milliseconds.</param>
		/// <returns>The created <see cref="DelayedCall"/> instance that can be used for later controlling of the invocation process.</returns>
		public static DelayedCall<T> Create(Callback cb, T data, int milliseconds)
		{
			DelayedCall<T> dc = new DelayedCall<T>();
			PrepareDCObject(dc, milliseconds, false);
			dc.callback = cb;
			dc.data = data;
			return dc;
		}

		/// <summary>
		/// Creates a new asynchronous <see cref="DelayedCall"/> instance with one data argument.
		/// The callback function will be invoked on a ThreadPool thread.
		/// </summary>
		/// <param name="cb">The callback function.</param>
		/// <param name="data">Argument for the callback function.</param>
		/// <param name="milliseconds">Time to callback invocation in milliseconds.</param>
		/// <returns>The created <see cref="DelayedCall"/> instance that can be used for later controlling of the invocation process.</returns>
		public static DelayedCall<T> CreateAsync(Callback cb, T data, int milliseconds)
		{
			DelayedCall<T> dc = new DelayedCall<T>();
			PrepareDCObject(dc, milliseconds, true);
			dc.callback = cb;
			dc.data = data;
			return dc;
		}

		/// <summary>
		/// Creates and starts a new <see cref="DelayedCall"/> instance with one data argument.
		/// </summary>
		/// <param name="cb">The callback function.</param>
		/// <param name="data">Argument for the callback function.</param>
		/// <param name="milliseconds">Time to callback invocation in milliseconds.</param>
		/// <returns>The created <see cref="DelayedCall"/> instance that can be used for later controlling of the invocation process.</returns>
		public static DelayedCall<T> Start(Callback cb, T data, int milliseconds)
		{
			DelayedCall<T> dc = Create(cb, data, milliseconds);
			dc.Start();
			return dc;
		}

		/// <summary>
		/// Creates and starts a new asynchronous <see cref="DelayedCall"/> instance with one data
		/// argument. The callback function will be invoked on a ThreadPool thread.
		/// </summary>
		/// <param name="cb">The callback function.</param>
		/// <param name="data">Argument for the callback function.</param>
		/// <param name="milliseconds">Time to callback invocation in milliseconds.</param>
		/// <returns>The created <see cref="DelayedCall"/> instance that can be used for later controlling of the invocation process.</returns>
		public static DelayedCall<T> StartAsync(Callback cb, T data, int milliseconds)
		{
			DelayedCall<T> dc = CreateAsync(cb, data, milliseconds);
			dc.Start();
			return dc;
		}

		/// <summary>
		/// Invokes the callback method.
		/// </summary>
		protected override void OnFire()
		{
			context.Post(
				delegate
				{
					lock (timerLock)
					{
						// Only fire the callback if the timer wasn't cancelled in this very moment
						if (isCancelled) return;
					}

					if (callback != null) callback(data);
				},
				null);
		}

		/// <summary>
		/// Resets the timeout and callback function argument to a new value and restarts the timer.
		/// </summary>
		/// <param name="data">New argument for the callback function.</param>
		/// <param name="milliseconds">New timeout value in milliseconds.</param>
		public void Reset(T data, int milliseconds)
		{
			lock (timerLock)
			{
				Cancel();
				this.data = data;
				Milliseconds = milliseconds;
				Start();
			}
		}
	}

	/// <summary>
	/// Implements a timer that invokes a method with two arguments after a user-defined timeout.
	/// </summary>
	/// <typeparam name="T1">The type of the first callback method parameter.</typeparam>
	/// <typeparam name="T2">The type of the second callback method parameter.</typeparam>
	public class DelayedCall<T1, T2> : DelayedCall
	{
		/// <summary>
		/// Represents a callback method that accepts two arguments.
		/// </summary>
		/// <param name="data1">First argument for the callback function.</param>
		/// <param name="data2">Second argument for the callback function.</param>
		public new delegate void Callback(T1 data1, T2 data2);

		private Callback callback;
		private T1 data1;
		private T2 data2;

		/// <summary>
		/// Creates a new <see cref="DelayedCall"/> instance with two data arguments.
		/// </summary>
		/// <param name="cb">The callback function.</param>
		/// <param name="data1">First argument for the callback function.</param>
		/// <param name="data2">Second argument for the callback function.</param>
		/// <param name="milliseconds">Time to callback invocation in milliseconds.</param>
		/// <returns>The created <see cref="DelayedCall"/> instance that can be used for later controlling of the invocation process.</returns>
		public static DelayedCall<T1, T2> Create(Callback cb, T1 data1, T2 data2, int milliseconds)
		{
			DelayedCall<T1, T2> dc = new DelayedCall<T1, T2>();
			PrepareDCObject(dc, milliseconds, false);
			dc.callback = cb;
			dc.data1 = data1;
			dc.data2 = data2;
			return dc;
		}

		/// <summary>
		/// Creates a new asynchronous <see cref="DelayedCall"/> instance with two data arguments.
		/// The callback function will be invoked on a ThreadPool thread.
		/// </summary>
		/// <param name="cb">The callback function.</param>
		/// <param name="data1">First argument for the callback function.</param>
		/// <param name="data2">Second argument for the callback function.</param>
		/// <param name="milliseconds">Time to callback invocation in milliseconds.</param>
		/// <returns>The created <see cref="DelayedCall"/> instance that can be used for later controlling of the invocation process.</returns>
		public static DelayedCall<T1, T2> CreateAsync(Callback cb, T1 data1, T2 data2, int milliseconds)
		{
			DelayedCall<T1, T2> dc = new DelayedCall<T1, T2>();
			PrepareDCObject(dc, milliseconds, true);
			dc.callback = cb;
			dc.data1 = data1;
			dc.data2 = data2;
			return dc;
		}

		/// <summary>
		/// Creates and starts a new <see cref="DelayedCall"/> instance with two data arguments.
		/// </summary>
		/// <param name="cb">The callback function.</param>
		/// <param name="data1">First argument for the callback function.</param>
		/// <param name="data2">Second argument for the callback function.</param>
		/// <param name="milliseconds">Time to callback invocation in milliseconds.</param>
		/// <returns>The created <see cref="DelayedCall"/> instance that can be used for later controlling of the invocation process.</returns>
		public static DelayedCall<T1, T2> Start(Callback cb, T1 data1, T2 data2, int milliseconds)
		{
			DelayedCall<T1, T2> dc = Create(cb, data1, data2, milliseconds);
			dc.Start();
			return dc;
		}

		/// <summary>
		/// Creates and starts a new asynchronous <see cref="DelayedCall"/> instance with two data
		/// arguments. The callback function will be invoked on a ThreadPool thread.
		/// </summary>
		/// <param name="cb">The callback function.</param>
		/// <param name="data1">First argument for the callback function.</param>
		/// <param name="data2">Second argument for the callback function.</param>
		/// <param name="milliseconds">Time to callback invocation in milliseconds.</param>
		/// <returns>The created <see cref="DelayedCall"/> instance that can be used for later controlling of the invocation process.</returns>
		public static DelayedCall<T1, T2> StartAsync(Callback cb, T1 data1, T2 data2, int milliseconds)
		{
			DelayedCall<T1, T2> dc = CreateAsync(cb, data1, data2, milliseconds);
			dc.Start();
			return dc;
		}

		/// <summary>
		/// Invokes the callback method.
		/// </summary>
		protected override void OnFire()
		{
			context.Post(
				delegate
				{
					lock (timerLock)
					{
						// Only fire the callback if the timer wasn't cancelled in this very moment
						if (isCancelled) return;
					}

					if (callback != null) callback(data1, data2);
				},
				null);
		}

		/// <summary>
		/// Resets the timeout and callback function arguments to new values and restarts the timer.
		/// </summary>
		/// <param name="data1">New first argument for the callback function.</param>
		/// <param name="data2">New second argument for the callback function.</param>
		/// <param name="milliseconds">New timeout value in milliseconds.</param>
		public void Reset(T1 data1, T2 data2, int milliseconds)
		{
			lock (timerLock)
			{
				Cancel();
				this.data1 = data1;
				this.data2 = data2;
				Milliseconds = milliseconds;
				Start();
			}
		}
	}

	/// <summary>
	/// Implements a timer that invokes a method with three arguments after a user-defined timeout.
	/// </summary>
	/// <typeparam name="T1">The type of the first callback method parameter.</typeparam>
	/// <typeparam name="T2">The type of the second callback method parameter.</typeparam>
	/// <typeparam name="T3">The type of the third callback method parameter.</typeparam>
	public class DelayedCall<T1, T2, T3> : DelayedCall
	{
		/// <summary>
		/// Represents a callback method that accepts three arguments.
		/// </summary>
		/// <param name="data1">First argument for the callback function.</param>
		/// <param name="data2">Second argument for the callback function.</param>
		/// <param name="data3">Third argument for the callback function.</param>
		public new delegate void Callback(T1 data1, T2 data2, T3 data3);

		private Callback callback;
		private T1 data1;
		private T2 data2;
		private T3 data3;

		/// <summary>
		/// Creates a new <see cref="DelayedCall"/> instance with three data arguments.
		/// </summary>
		/// <param name="cb">The callback function.</param>
		/// <param name="data1">First argument for the callback function.</param>
		/// <param name="data2">Second argument for the callback function.</param>
		/// <param name="data3">Third argument for the callback function.</param>
		/// <param name="milliseconds">Time to callback invocation in milliseconds.</param>
		/// <returns>The created <see cref="DelayedCall"/> instance that can be used for later controlling of the invocation process.</returns>
		public static DelayedCall<T1, T2, T3> Create(Callback cb, T1 data1, T2 data2, T3 data3, int milliseconds)
		{
			DelayedCall<T1, T2, T3> dc = new DelayedCall<T1, T2, T3>();
			PrepareDCObject(dc, milliseconds, false);
			dc.callback = cb;
			dc.data1 = data1;
			dc.data2 = data2;
			dc.data3 = data3;
			return dc;
		}

		/// <summary>
		/// Creates a new asynchronous <see cref="DelayedCall"/> instance with three data arguments.
		/// The callback function will be invoked on a ThreadPool thread.
		/// </summary>
		/// <param name="cb">The callback function.</param>
		/// <param name="data1">First argument for the callback function.</param>
		/// <param name="data2">Second argument for the callback function.</param>
		/// <param name="data3">Third argument for the callback function.</param>
		/// <param name="milliseconds">Time to callback invocation in milliseconds.</param>
		/// <returns>The created <see cref="DelayedCall"/> instance that can be used for later controlling of the invocation process.</returns>
		public static DelayedCall<T1, T2, T3> CreateAsync(Callback cb, T1 data1, T2 data2, T3 data3, int milliseconds)
		{
			DelayedCall<T1, T2, T3> dc = new DelayedCall<T1, T2, T3>();
			PrepareDCObject(dc, milliseconds, true);
			dc.callback = cb;
			dc.data1 = data1;
			dc.data2 = data2;
			dc.data3 = data3;
			return dc;
		}

		/// <summary>
		/// Creates and starts a new <see cref="DelayedCall"/> instance with three data arguments.
		/// </summary>
		/// <param name="cb">The callback function.</param>
		/// <param name="data1">First argument for the callback function.</param>
		/// <param name="data2">Second argument for the callback function.</param>
		/// <param name="data3">Third argument for the callback function.</param>
		/// <param name="milliseconds">Time to callback invocation in milliseconds.</param>
		/// <returns>The created <see cref="DelayedCall"/> instance that can be used for later controlling of the invocation process.</returns>
		public static DelayedCall<T1, T2, T3> Start(Callback cb, T1 data1, T2 data2, T3 data3, int milliseconds)
		{
			DelayedCall<T1, T2, T3> dc = Create(cb, data1, data2, data3, milliseconds);
			dc.Start();
			return dc;
		}

		/// <summary>
		/// Creates and starts a new asynchronous <see cref="DelayedCall"/> instance with three data
		/// arguments. The callback function will be invoked on a ThreadPool thread.
		/// </summary>
		/// <param name="cb">The callback function.</param>
		/// <param name="data1">First argument for the callback function.</param>
		/// <param name="data2">Second argument for the callback function.</param>
		/// <param name="data3">Third argument for the callback function.</param>
		/// <param name="milliseconds">Time to callback invocation in milliseconds.</param>
		/// <returns>The created <see cref="DelayedCall"/> instance that can be used for later controlling of the invocation process.</returns>
		public static DelayedCall<T1, T2, T3> StartAsync(Callback cb, T1 data1, T2 data2, T3 data3, int milliseconds)
		{
			DelayedCall<T1, T2, T3> dc = CreateAsync(cb, data1, data2, data3, milliseconds);
			dc.Start();
			return dc;
		}

		/// <summary>
		/// Invokes the callback method.
		/// </summary>
		protected override void OnFire()
		{
			context.Post(
				delegate
				{
					lock (timerLock)
					{
						// Only fire the callback if the timer wasn't cancelled in this very moment
						if (isCancelled) return;
					}

					if (callback != null) callback(data1, data2, data3);
				},
				null);
		}

		/// <summary>
		/// Resets the timeout and callback function arguments to new values and restarts the timer.
		/// </summary>
		/// <param name="data1">New first argument for the callback function.</param>
		/// <param name="data2">New second argument for the callback function.</param>
		/// <param name="data3">New third argument for the callback function.</param>
		/// <param name="milliseconds">New timeout value in milliseconds.</param>
		public void Reset(T1 data1, T2 data2, T3 data3, int milliseconds)
		{
			lock (timerLock)
			{
				Cancel();
				this.data1 = data1;
				this.data2 = data2;
				this.data3 = data3;
				Milliseconds = milliseconds;
				Start();
			}
		}
	}
}
