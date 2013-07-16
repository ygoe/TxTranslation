using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Mime;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace Unclassified
{
	// Managed RegisterHotKey wrapper for WPF applications
	// Source: http://stackoverflow.com/a/9330358/143684 (modified)
	class HotKey : IDisposable
	{
		private static Dictionary<int, HotKey> hotKeyCallbacks;

		[DllImport("user32.dll")]
		private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vlc);

		[DllImport("user32.dll")]
		private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

		public const int WM_HOTKEY = 0x0312;

		private bool disposed = false;

		public Key Key { get; private set; }
		public HotKeyModifier Modifiers { get; private set; }
		public Action<HotKey> Action { get; private set; }
		public int Id { get; set; }

		public HotKey(Key k, HotKeyModifier keyModifiers, Action<HotKey> action, bool register = true)
		{
			Key = k;
			Modifiers = keyModifiers;
			Action = action;
			if (register)
			{
				Register();
			}
		}

		public bool Register()
		{
			int virtualKeyCode = KeyInterop.VirtualKeyFromKey(Key);
			Id = virtualKeyCode + ((int) Modifiers * 0x10000);
			bool result = RegisterHotKey(IntPtr.Zero, Id, (uint) Modifiers, (uint) virtualKeyCode);

			if (hotKeyCallbacks == null)
			{
				hotKeyCallbacks = new Dictionary<int, HotKey>();
				ComponentDispatcher.ThreadFilterMessage += new ThreadMessageEventHandler(ComponentDispatcherThreadFilterMessage);
			}

			hotKeyCallbacks.Add(Id, this);

			//Debug.Print(result.ToString() + ", " + Id + ", " + virtualKeyCode);
			return result;
		}

		public void Unregister()
		{
			HotKey hotKey;
			if (hotKeyCallbacks.TryGetValue(Id, out hotKey))
			{
				UnregisterHotKey(IntPtr.Zero, Id);
			}
		}

		private static void ComponentDispatcherThreadFilterMessage(ref MSG msg, ref bool handled)
		{
			if (!handled)
			{
				if (msg.message == WM_HOTKEY)
				{
					HotKey hotKey;

					if (hotKeyCallbacks.TryGetValue((int) msg.wParam, out hotKey))
					{
						if (hotKey.Action != null)
						{
							hotKey.Action.Invoke(hotKey);
						}
						handled = true;
					}
				}
			}
		}

		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		// Dispose(bool disposing) executes in two distinct scenarios.
		// If disposing equals true, the method has been called directly
		// or indirectly by a user's code. Managed and unmanaged resources
		// can be disposed.
		// If disposing equals false, the method has been called by the
		// runtime from inside the finalizer and you should not reference
		// other objects. Only unmanaged resources can be disposed.
		protected virtual void Dispose(bool disposing)
		{
			// Check to see if Dispose has already been called.
			if (!this.disposed)
			{
				// If disposing equals true, dispose all managed
				// and unmanaged resources.
				if (disposing)
				{
					// Dispose managed resources.
					Unregister();
				}

				// Note disposing has been done.
				disposed = true;
			}
		}
	}

	[Flags]
	public enum HotKeyModifier
	{
		None = 0x0000,
		Alt = 0x0001,
		Ctrl = 0x0002,
		Shift = 0x0004,
		Win = 0x0008,
		NoRepeat = 0x4000
	}
}
