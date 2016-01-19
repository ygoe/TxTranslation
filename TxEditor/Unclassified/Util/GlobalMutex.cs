using System;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;

namespace Unclassified.Util
{
	/// <summary>
	/// Provides a global mutex for system-wide synchronisation.
	/// </summary>
	public class GlobalMutex : IDisposable
	{
		#region Static members

		private static GlobalMutex instance;

		/// <summary>
		/// Creates a global mutex and keeps a static reference to it. This method can be used to
		/// create a mutex to easily synchronise with a setup.
		/// </summary>
		/// <param name="name">The name of the mutex to create in the Global namespace.</param>
		public static void Create(string name)
		{
			if (instance != null)
				throw new InvalidOperationException("The static global mutex is already created.");

			// Create a new mutex and keep a reference to it so it won't be GC'ed
			instance = new GlobalMutex(name);
			// Close the mutex when the application exits
			AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
		}

		/// <summary>
		/// Process exit handler. Closes the mutex.
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="args"></param>
		private static void CurrentDomain_ProcessExit(object sender, EventArgs args)
		{
			instance.Dispose();
		}

		/// <summary>
		/// Gets the static instance of the global mutex.
		/// </summary>
		public static GlobalMutex Instance
		{
			get { return instance; }
		}

		#endregion Static members

		#region Private data

		private Mutex mutex;
		private Thread ownerThread;
		private bool hasHandle;
		private bool isDisposed;

		#endregion Private data

		#region Constructors

		/// <summary>
		/// Creates a global mutex and allows everyone access to it.
		/// </summary>
		/// <param name="name">The name of the mutex to create in the Global namespace.</param>
		public GlobalMutex(string name)
		{
			// Allow full control of the mutex for everyone so that other users will be able to
			// create the same mutex and synchronise on it, if required.
			var allowEveryoneRule = new MutexAccessRule(
				new SecurityIdentifier(WellKnownSidType.WorldSid, null),
				MutexRights.FullControl,
				AccessControlType.Allow);
			var securitySettings = new MutexSecurity();
			securitySettings.AddAccessRule(allowEveryoneRule);

			bool createdNew;
			// Use the Global prefix to make it a system-wide object
			mutex = new Mutex(false, @"Global\" + name, out createdNew, securitySettings);
		}

		#endregion Constructors

		#region Synchronisation methods

		/// <summary>
		/// Waits infinitely for the mutex.
		/// </summary>
		public void Wait()
		{
			TryWait(Timeout.Infinite);
		}

		/// <summary>
		/// Waits for the mutex.
		/// </summary>
		/// <param name="timeout">The timeout to wait for the mutex.</param>
		/// <returns>true if the mutex is owned; otherwise, false.</returns>
		public bool TryWait(TimeSpan timeout)
		{
			return TryWait((int)timeout.TotalMilliseconds);
		}

		/// <summary>
		/// Waits for the mutex.
		/// </summary>
		/// <param name="millisecondsTimeout">The timeout to wait for the mutex.</param>
		/// <returns>true if the mutex is owned; otherwise, false.</returns>
		public bool TryWait(int millisecondsTimeout)
		{
			try
			{
				hasHandle = mutex.WaitOne(millisecondsTimeout);
			}
			catch (AbandonedMutexException)
			{
				// Another thread or process has abandoned the mutex. Now we own it.
				hasHandle = true;
			}
			if (hasHandle)
			{
				// Remember the owning thread
				ownerThread = Thread.CurrentThread;
			}
			return hasHandle;
		}

		/// <summary>
		/// Releases the mutex.
		/// </summary>
		/// <returns>true if the mutex was owned and released; otherwise, false.</returns>
		public bool Release()
		{
			if (hasHandle)
			{
				mutex.ReleaseMutex();
				ownerThread = null;
				hasHandle = false;
				return true;
			}
			return false;
		}

		/// <summary>
		/// Checks whether the current thread owns the mutex.
		/// </summary>
		/// <returns>true if the current thread owns the mutex; otherwise, false.</returns>
		/// <remarks>
		/// The mutex may only be released by the thread that owns it. Otherwise, an exception will
		/// be thrown.
		/// </remarks>
		public bool CheckThread()
		{
			return Thread.CurrentThread == ownerThread;
		}

		#endregion Synchronisation methods

		#region Dispose and finalizer

		/// <summary>
		/// Closes the mutex and frees all resources. This method also releases the mutex if it is
		/// owned by the current thread. Calling this method on a different thread will abandon the
		/// mutex.
		/// </summary>
		public void Dispose()
		{
			if (!isDisposed)
			{
				if (CheckThread())
				{
					Release();
				}
				mutex.Close();
				isDisposed = true;
				GC.SuppressFinalize(this);
			}
		}

		/// <summary>
		/// Finalizer.
		/// </summary>
		~GlobalMutex()
		{
			Dispose();
		}

		#endregion Dispose and finalizer
	}
}
