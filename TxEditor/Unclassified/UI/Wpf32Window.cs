using System;
using System.Windows;
using System.Windows.Interop;

namespace Unclassified.UI
{
	/// <summary>
	/// Provides a Windows Forms IWin32Window wrapper for a WPF Window instance.
	/// </summary>
	public class Wpf32Window : System.Windows.Forms.IWin32Window
	{
		public IntPtr Handle { get; private set; }

		/// <summary>
		/// Initialises a new instance of the Wpf32Window class for the specified WPF Window.
		/// </summary>
		/// <param name="wpfWindow">The WPF Window instance to wrap.</param>
		public Wpf32Window(Window wpfWindow)
		{
			Handle = new WindowInteropHelper(wpfWindow).Handle;
		}
	}
}
