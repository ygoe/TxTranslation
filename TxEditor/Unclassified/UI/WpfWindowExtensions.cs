using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace Unclassified.UI
{
	// Source: http://stackoverflow.com/a/6024229/143684
	public static class WpfWindowExtensions
	{
		[DllImport("user32.dll")]
		private static extern uint GetWindowLong(IntPtr hwnd, int index);
		[DllImport("user32.dll")]
		private static extern uint SetWindowLong(IntPtr hwnd, int index, uint newStyle);
		[DllImport("user32.dll")]
		private static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int width, int height, uint flags);
		[DllImport("user32.dll")]
		private static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

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

		public static void HideIcon(this Window w)
		{
			IntPtr hwnd = new WindowInteropHelper(w).Handle;
			uint exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
			SetWindowLong(hwnd, GWL_EXSTYLE, exStyle | WS_EX_DLGMODALFRAME);
			SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
			SendMessage(hwnd, WM_SETICON, IntPtr.Zero, IntPtr.Zero);
		}

		public static void HideSysMenu(this Window w)
		{
			IntPtr hwnd = new WindowInteropHelper(w).Handle;
			uint extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
			SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_DLGMODALFRAME);
			SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);
		}

		public static void HideMinimizeBox(this Window w)
		{
			IntPtr hwnd = new WindowInteropHelper(w).Handle;
			SetWindowLong(hwnd, GWL_STYLE,
				GetWindowLong(hwnd, GWL_STYLE) & ~(WS_MINIMIZEBOX));
		}

		public static void HideMaximizeBox(this Window w)
		{
			IntPtr hwnd = new WindowInteropHelper(w).Handle;
			SetWindowLong(hwnd, GWL_STYLE,
				GetWindowLong(hwnd, GWL_STYLE) & ~(WS_MAXIMIZEBOX));
		}

		public static void HideMinimizeAndMaximizeBoxes(this Window w)
		{
			IntPtr hwnd = new WindowInteropHelper(w).Handle;
			SetWindowLong(hwnd, GWL_STYLE,
				GetWindowLong(hwnd, GWL_STYLE) & ~(WS_MAXIMIZEBOX | WS_MINIMIZEBOX));
		}
	}
}
