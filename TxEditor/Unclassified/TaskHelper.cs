using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace Unclassified
{
	/// <summary>
	/// Provides static helper methods for easier execution of asynchronous operations.
	/// </summary>
	public static class TaskHelper
	{
		/// <summary>
		/// Starts an Action in a worker thread.
		/// </summary>
		/// <param name="action">Action to start in a worker thread.</param>
		/// <param name="endAction">Action to post back to the Dispatcher when the background work has finished.</param>
		/// <returns>A CancellationTokenSource instance to control cancelling of the background task.</returns>
		public static CancellationTokenSource Start(Action<CancellationToken> action, Action<Task> endAction)
		{
			var dispatcher = Dispatcher.CurrentDispatcher;
			var cts = new CancellationTokenSource();
			var task = new Task(() => action(cts.Token), cts.Token);
			task.ContinueWith((t) => dispatcher.BeginInvoke(endAction, t));
			task.Start();
			return cts;
		}

		/// <summary>
		/// Posts an Action to the Dispatcher queue for execution when all loading has been done.
		/// </summary>
		/// <param name="action">Action to post to the queue.</param>
		public static void WhenLoaded(Action action)
		{
			Dispatcher.CurrentDispatcher.BeginInvoke(action, DispatcherPriority.Loaded);
		}

		/// <summary>
		/// Posts an Action to the Dispatcher queue for execution with Background priority.
		/// </summary>
		/// <param name="action">Action to post to the queue.</param>
		public static void Background(Action action)
		{
			Dispatcher.CurrentDispatcher.BeginInvoke(action, DispatcherPriority.Background);
		}

		// Source: http://msdn.microsoft.com/de-de/library/system.windows.threading.dispatcher.pushframe.aspx
		/// <summary>
		/// Enters the message loop to process all pending messages down to the specified priority.
		/// This method returns after all messages have been processed.
		/// </summary>
		/// <param name="priority">Minimum priority of the messages to process.</param>
		public static void DoEvents(DispatcherPriority priority = DispatcherPriority.Background)
		{
			DispatcherFrame frame = new DispatcherFrame();
			Dispatcher.CurrentDispatcher.BeginInvoke(
				priority,
				new DispatcherOperationCallback(ExitFrame), frame);
			Dispatcher.PushFrame(frame);
		}

		private static object ExitFrame(object f)
		{
			((DispatcherFrame) f).Continue = false;
			return null;
		}
	}
}
