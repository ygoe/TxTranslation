using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Unclassified.UI
{
	/// <summary>
	/// Provides extension methods for WPF Windows.
	/// </summary>
	public static class WindowExtensions
	{
		// Based on: http://stackoverflow.com/a/6024229/143684

		#region Native interop

		[DllImport("user32.dll")]
		private static extern uint GetWindowLong(IntPtr hwnd, int index);

		[DllImport("user32.dll")]
		private static extern uint SetWindowLong(IntPtr hwnd, int index, uint newStyle);

		[DllImport("user32.dll")]
		private static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int width, int height, uint flags);

		[DllImport("user32.dll")]
		private static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

		[StructLayout(LayoutKind.Sequential)]
		private struct FLASHWINFO
		{
			public UInt32 cbSize;
			public IntPtr hwnd;
			public UInt32 dwFlags;
			public UInt32 uCount;
			public UInt32 dwTimeout;
		}

		private const int GWL_EXSTYLE = -20;
		private const int WS_EX_DLGMODALFRAME = 0x0001;
		private const int SWP_NOSIZE = 0x0001;
		private const int SWP_NOMOVE = 0x0002;
		private const int SWP_NOZORDER = 0x0004;
		private const int SWP_FRAMECHANGED = 0x0020;
		private const int GWL_STYLE = -16;
		private const uint WS_MAXIMIZEBOX = 0x00010000;
		private const uint WS_MINIMIZEBOX = 0x00020000;
		private const uint WS_SYSMENU = 0x00080000;
		private const uint WS_POPUP = 0x80000000;
		private const uint DS_3DLOOK = 0x0004;
		private const uint DS_SETFONT = 0x40;
		private const uint DS_MODALFRAME = 0x80;
		private const uint WM_SETICON = 0x0080;

		// Stop flashing. The system restores the window to its original state.
		private const UInt32 FLASHW_STOP = 0;
		// Flash the window caption.
		private const UInt32 FLASHW_CAPTION = 1;
		// Flash the taskbar button.
		private const UInt32 FLASHW_TRAY = 2;
		// Flash both the window caption and taskbar button.
		// This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags.
		private const UInt32 FLASHW_ALL = 3;
		// Flash continuously, until the FLASHW_STOP flag is set.
		private const UInt32 FLASHW_TIMER = 4;
		// Flash continuously until the window comes to the foreground.
		private const UInt32 FLASHW_TIMERNOFG = 12;

		#endregion Native interop

		/// <summary>
		/// Hides the icon in the window title bar.
		/// </summary>
		/// <param name="w">The Window instance.</param>
		/// <remarks>
		/// This method must be called in the <see cref="Window.OnSourceInitialized"/> override or
		/// at a later point to have the desired effect.
		/// </remarks>
		public static void HideIcon(this Window w)
		{
			IntPtr hwnd = new WindowInteropHelper(w).Handle;
			uint exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
			SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_DLGMODALFRAME);
			SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
			SendMessage(hwnd, WM_SETICON, IntPtr.Zero, IntPtr.Zero);
		}

		/// <summary>
		/// Hides the system menu in the window title bar.
		/// </summary>
		/// <param name="w">The Window instance.</param>
		/// <remarks>
		/// This method must be called in the <see cref="Window.OnSourceInitialized"/> override or
		/// at a later point to have the desired effect.
		/// </remarks>
		public static void HideSysMenu(this Window w)
		{
			IntPtr hwnd = new WindowInteropHelper(w).Handle;
			uint extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
			SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_DLGMODALFRAME);
			SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
		}

		/// <summary>
		/// Hides the minimize button in the window title bar.
		/// </summary>
		/// <param name="w">The Window instance.</param>
		/// <remarks>
		/// This method must be called in the <see cref="Window.OnSourceInitialized"/> override or
		/// at a later point to have the desired effect.
		/// </remarks>
		public static void HideMinimizeBox(this Window w)
		{
			IntPtr hwnd = new WindowInteropHelper(w).Handle;
			SetWindowLong(hwnd, GWL_STYLE,
				GetWindowLong(hwnd, GWL_STYLE) & ~(WS_MINIMIZEBOX));
		}

		/// <summary>
		/// Hides the maximize button in the window title bar.
		/// </summary>
		/// <param name="w">The Window instance.</param>
		/// <remarks>
		/// This method must be called in the <see cref="Window.OnSourceInitialized"/> override or
		/// at a later point to have the desired effect.
		/// </remarks>
		public static void HideMaximizeBox(this Window w)
		{
			IntPtr hwnd = new WindowInteropHelper(w).Handle;
			SetWindowLong(hwnd, GWL_STYLE,
				GetWindowLong(hwnd, GWL_STYLE) & ~(WS_MAXIMIZEBOX));
		}

		/// <summary>
		/// Hides the minimize and maximize buttons in the window title bar.
		/// </summary>
		/// <param name="w">The Window instance.</param>
		/// <remarks>
		/// This method must be called in the <see cref="Window.OnSourceInitialized"/> override or
		/// at a later point to have the desired effect.
		/// </remarks>
		public static void HideMinimizeAndMaximizeBoxes(this Window w)
		{
			IntPtr hwnd = new WindowInteropHelper(w).Handle;
			SetWindowLong(hwnd, GWL_STYLE,
				GetWindowLong(hwnd, GWL_STYLE) & ~(WS_MAXIMIZEBOX | WS_MINIMIZEBOX));
		}

		/// <summary>
		/// Flashes the window in the task bar.
		/// </summary>
		/// <param name="w">The Window instance.</param>
		/// <returns></returns>
		public static bool Flash(this Window w)
		{
			IntPtr hWnd = new WindowInteropHelper(w).Handle;
			FLASHWINFO fInfo = new FLASHWINFO();
			fInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(fInfo));
			fInfo.hwnd = hWnd;
			fInfo.dwFlags = FLASHW_TIMER | FLASHW_TRAY;
			fInfo.uCount = 3;
			fInfo.dwTimeout = 0;
			return FlashWindowEx(ref fInfo);
		}

		/// <summary>
		/// Stops flashing the window in the task bar.
		/// </summary>
		/// <param name="w">The Window instance.</param>
		/// <returns></returns>
		public static bool StopFlashing(this Window w)
		{
			IntPtr hWnd = new WindowInteropHelper(w).Handle;
			FLASHWINFO fInfo = new FLASHWINFO();
			fInfo.cbSize = Convert.ToUInt32(Marshal.SizeOf(fInfo));
			fInfo.hwnd = hWnd;
			fInfo.dwFlags = FLASHW_STOP;
			fInfo.uCount = 0;
			fInfo.dwTimeout = 0;
			return FlashWindowEx(ref fInfo);
		}
	}
}
