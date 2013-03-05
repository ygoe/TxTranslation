using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Threading;

/*
TODO: Post the following comment at http://stackoverflow.com/a/8325206/143684 after publishing this code (include a link)
Based on this suggestion, I've implemented my own ViewCommandManager that handles invoking commands in connected views. It's basically the other direction of regular Commands, for these cases when a ViewModel needs to do some action in its View. It uses reflection like data-bound commands and WeakReferences to avoid memory leaks.
*/

namespace Unclassified.UI
{
	/// <summary>
	/// Manages view connections to invoke commands on them.
	/// </summary>
	public class ViewCommandManager
	{
		#region Static property metadata setup

		/// <summary>
		/// Sets up the overridden property metadata to handle changes to the DataSource property.
		/// </summary>
		/// <typeparam name="TView">Type of the view to override the metadata for.</typeparam>
		public static void SetupMetadata<TView>()
			where TView : FrameworkElement
		{
			FrameworkElement.DataContextProperty.OverrideMetadata(
				typeof(TView),
				new FrameworkPropertyMetadata(ViewChangedHandler));
		}

		/// <summary>
		/// DataContext property changed handler to use in overridden property metadata.
		/// </summary>
		/// <param name="d"></param>
		/// <param name="e"></param>
		private static void ViewChangedHandler(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var oldValue = e.OldValue as IViewCommandSource;
			if (oldValue != null)
				oldValue.ViewCommandManager.DeregisterView(d);
			var newValue = e.NewValue as IViewCommandSource;
			if (newValue != null)
				newValue.ViewCommandManager.RegisterView(d);
		}

		#endregion Static property metadata setup

		#region Private data

		private List<WeakReference> weakViews = new List<WeakReference>();

		#endregion Private data

		#region View registration

		/// <summary>
		/// Registers a view instance for this ViewCommandManager.
		/// </summary>
		/// <param name="d">View instance to register.</param>
		public void RegisterView(DependencyObject d)
		{
			if (!weakViews.Any(wr => wr.Target == d))
				weakViews.Add(new WeakReference(d));
		}

		/// <summary>
		/// Deregisters a view instance from this ViewCommandManager.
		/// </summary>
		/// <param name="d">View instance to deregister.</param>
		public void DeregisterView(DependencyObject d)
		{
			weakViews.RemoveAll(wr => wr.Target == null || wr.Target == d);
		}

		#endregion View registration

		#region Command invokation

		/// <summary>
		/// Invokes a command on all registered views.
		/// </summary>
		/// <param name="commandName">Name of the command to invoke. This is the name of a public method with the ViewCommand attribute.</param>
		/// <param name="args">Arguments for the command, as expected by the command method.</param>
		public void Invoke(string commandName, params object[] args)
		{
			foreach (var viewRef in weakViews)
			{
				var view = viewRef.Target;
				if (view != null)
				{
					var method = view.GetType().GetMethod(commandName, BindingFlags.Instance | BindingFlags.Public);
					if (method != null)
					{
						var attr = method.GetCustomAttributes(typeof(ViewCommandAttribute), false);
						if (attr != null)
						{
							method.Invoke(view, args);
						}
					}
				}
			}
		}

		/// <summary>
		/// Invokes a command asynchronously on the current dispatcher.
		/// </summary>
		/// <param name="commandName">Name of the command to invoke. This is the name of a public method with the ViewCommand attribute.</param>
		/// <param name="priority">Priority to invoke the command with.</param>
		/// <param name="args">Arguments for the command, as expected by the command method.</param>
		public void BeginInvoke(string commandName, DispatcherPriority priority, params object[] args)
		{
			Dispatcher.CurrentDispatcher.BeginInvoke(
				(Action) delegate { Invoke(commandName, args); },
				priority);
		}

		/// <summary>
		/// Invokes a command after all Load events have completed and all views are loaded and registered.
		/// </summary>
		/// <param name="commandName">Name of the command to invoke. This is the name of a public method with the ViewCommand attribute.</param>
		/// <param name="args">Arguments for the command, as expected by the command method.</param>
		public void InvokeLoaded(string commandName, params object[] args)
		{
			BeginInvoke(commandName, DispatcherPriority.Loaded, args);
		}

		#endregion Command invokation
	}

	/// <summary>
	/// Defines a property to access the ViewCommandManager instance of an object.
	/// </summary>
	public interface IViewCommandSource
	{
		/// <summary>
		/// Gets the ViewCommandManager instance of the object.
		/// </summary>
		ViewCommandManager ViewCommandManager { get; }
	}

	/// <summary>
	/// Indicates that a method is a ViewCommand handler.
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	public class ViewCommandAttribute : Attribute
	{
	}
}
