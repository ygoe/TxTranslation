using System;

namespace Unclassified.Util
{
	/// <summary>
	/// Operation lock. This is a lightweight, non-threadsafe locking mechanism to mutually lock
	/// methods from executing the same operation. An <see cref="OpFlag"/> is created for each
	/// lockable operation and an OpLock is held in a using block around the critical code.
	/// </summary>
	internal class OpLock : IDisposable
	{
		private OpFlag opFlag;

		/// <summary>
		/// Acquires an operation lock.
		/// </summary>
		/// <param name="opFlag">The operation lock to acquire. You first need to check access manually, using the IsSet property.</param>
		public OpLock(OpFlag opFlag)
		{
			this.opFlag = opFlag;
			opFlag.Enter();
		}

		/// <summary>
		/// Frees the operation lock.
		/// </summary>
		public void Dispose()
		{
			opFlag.Leave();
		}
	}

	/// <summary>
	/// Operation flag. This is basically a boolean reference that indicates whether a certain
	/// operation is currently being locked, i.e. executing. It needs to be a reference value for
	/// the <see cref="OpLock"/> methods to write to its real instance.
	/// </summary>
	internal class OpFlag
	{
		private bool flag;

		/// <summary>
		/// Fired when the operation flag value has changed.
		/// </summary>
		public event EventHandler ValueChanged;

		/// <summary>
		/// Gets a value indicating whether the operation is currently executing.
		/// </summary>
		public bool IsSet { get { return flag; } }

		/// <summary>
		/// Sets the operation flag. This is only allowed if the flag was previously unset.
		/// </summary>
		public void Enter()
		{
			if (flag) throw new InvalidOperationException("The operation is already locked.");
			flag = true;
			OnValueChanged();
		}

		/// <summary>
		/// Unsets the operation flag.
		/// </summary>
		public void Leave()
		{
#if DEBUG
			if (!flag) throw new InvalidOperationException("The operation is already been unlocked.");
#endif
			flag = false;
			OnValueChanged();
		}

		protected void OnValueChanged()
		{
			var handler = ValueChanged;
			if (handler != null)
			{
				handler(this, EventArgs.Empty);
			}
		}
	}
}
