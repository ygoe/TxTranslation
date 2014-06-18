using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace Unclassified.Util
{
	public partial class WinApi
	{
		// Windows messages
		public const int WM_NULL = 0x0;
		public const int WM_CREATE = 0x1;
		public const int WM_DESTROY = 0x2;
		public const int WM_MOVE = 0x3;
		public const int WM_SIZE = 0x5;
		public const int WM_SETTEXT = 0xC;
		public const int WM_GETTEXT = 0xD;
		public const int WM_CLOSE = 0x10;
		public const int WM_QUIT = 0x12;
		public const int WM_ACTIVATEAPP = 0x1C;
		public const int WM_MOUSEACTIVATE = 0x21;
		public const int WM_NCCREATE = 0x81;
		public const int WM_NCDESTROY = 0x82;
		public const int WM_NCCALCSIZE = 0x83;
		public const int WM_NCHITTEST = 0x84;
		public const int WM_COMMAND = 0x111;
		public const int WM_USER = 0x400;
		public const int WM_MOUSEMOVE = 0x200;
		public const int WM_LBUTTONDOWN = 0x201;
		public const int WM_LBUTTONUP = 0x202;
		public const int WM_MOUSEWHEEL = 0x20A;

		// WM_MOUSEACTIVATE parameters
		public const int MA_ACTIVATE = 1;
		public const int MA_ACTIVATEANDEAT = 2;
		public const int MA_NOACTIVATE = 3;
		public const int MA_NOACTIVATEANDEAT = 4;

		// GetWindowLong constants
		public const int GWL_ID = -12;
		public const int GWL_STYLE = -16;
		public const int GWL_EXSTYLE = -20;

		// ShowWindow constants
		public const int SW_HIDE = 0;
		public const int SW_SHOW = 5;
		public const int SW_MAXIMIZE = 3;
		public const int SW_MINIMIZE = 6;	// SW_FORCEMINIMIZE = 11;
		public const int SW_RESTORE = 9;

		// Button messages
		public const int BM_CLICK = 0xF5;

		// Edit messages
		public const int EM_GETFIRSTVISIBLELINE = 0xCE;
		public const int EM_SCROLL = 0xB5;

		public const int SB_LINEUP = 0;
		public const int SB_LINEDOWN = 1;
		public const int SB_PAGEUP = 2;
		public const int SB_PAGEDOWN = 3;

		// Combobox messages
		public const int CB_GETCOUNT = 0x0146;
		public const int CB_GETCURSEL = 0x0147;
		public const int CB_GETLBTEXT = 0x0148;
		public const int CB_GETLBTEXTLEN = 0x0149;
		public const int CB_SETCURSEL = 0x014E;
		public const int CB_SHOWDROPDOWN = 0x014F;

		// Window styles
		public const uint WS_OVERLAPPED = 0x00000000;
		public const uint WS_POPUP = 0x80000000;
		public const uint WS_CAPTION = 0x00C00000;   // WS_BORDER | WS_DLGFRAME
		public const uint WS_SYSMENU = 0x00080000;

		// Extended window styles
		public const uint WS_EX_TOPMOST = 0x00000008;
		public const uint WS_EX_MDICHILD = 0x00000040;
		public const uint WS_EX_TOOLWINDOW = 0x00000080;
		public const uint WS_EX_APPWINDOW = 0x00040000;
		public const uint WS_EX_LAYERED = 0x00080000;
		public const uint WS_EX_NOACTIVATE = 0x08000000;

		[DllImport("user32")]
		public static extern bool IsWindow(IntPtr hWnd);

		[DllImport("user32")]
		public static extern bool IsWindowEnabled(IntPtr hWnd);

		[DllImport("user32")]
		public static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport("user32")]
		public static extern bool IsIconic(IntPtr hWnd);

		[DllImport("user32")]
		public static extern int GetWindow(IntPtr hWnd, uint flags);

		[DllImport("user32")]
		public static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

		[DllImport("user32")]
		public static extern int GetClassName(IntPtr hWnd, [MarshalAs(UnmanagedType.LPStr)] StringBuilder buf, int nMaxCount);

		[DllImport("user32")]
		public static extern int GetWindowText(IntPtr hWnd, [MarshalAs(UnmanagedType.LPStr)] StringBuilder buf, int nMaxCount);

		[DllImport("user32.dll")]
		public static extern bool GetWindowRect(IntPtr hWnd, out Rectangle lpRect);

		[DllImport("user32")]
		public static extern int SetActiveWindow(IntPtr hWnd);

		[DllImport("user32")]
		public static extern bool SetForegroundWindow(IntPtr hWnd);

		[DllImport("user32")]
		public static extern int SetFocus(IntPtr hWnd);

		[DllImport("user32")]
		public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

		[DllImport("user32.dll")]
		public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

		[DllImport("user32")]
		public static extern IntPtr GetForegroundWindow();

		[DllImport("user32")]
		public static extern int GetWindowThreadProcessId(IntPtr hWnd, out int lpdwProcessId);

		[DllImport("user32")]
		public static extern bool PostMessage(IntPtr hWnd, int msg, uint wParam, uint lParam);

		// ----- SendMessage -----
		// int, *
		[DllImport("user32")]
		public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);

		[DllImport("user32")]
		public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, uint lParam);

		[DllImport("user32", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, StringBuilder lParam);

		// uint, *
		[DllImport("user32")]
		public static extern IntPtr SendMessage(IntPtr hWnd, int msg, uint wParam, int lParam);

		[DllImport("user32")]
		public static extern IntPtr SendMessage(IntPtr hWnd, int msg, uint wParam, uint lParam);

		[DllImport("user32", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessage(IntPtr hWnd, int msg, uint wParam, StringBuilder lParam);

		// StringBuilder, *
		[DllImport("user32", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessage(IntPtr hWnd, int msg, StringBuilder wParam, int lParam);

		[DllImport("user32", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessage(IntPtr hWnd, int msg, StringBuilder wParam, uint lParam);

		[DllImport("user32", CharSet = CharSet.Auto)]
		public static extern IntPtr SendMessage(IntPtr hWnd, int msg, StringBuilder wParam, StringBuilder lParam);

		[DllImport("user32.dll")]
		public static extern bool LockWindowUpdate(IntPtr hWnd);

		[DllImport("user32.dll")]
		public static extern bool ClientToScreen(IntPtr hWnd, ref Point lpPoint);

		[DllImport("user32.dll")]
		public static extern bool ScreenToClient(IntPtr hWnd, ref Point lpPoint);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern ushort RegisterClass([In] ref WNDCLASS lpWndClass);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern bool UnregisterClass(string lpClassName, IntPtr hInstance);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr CreateWindowEx(
			uint dwExStyle,
			string lpClassName,
			string lpWindowName,
			uint dwStyle,
			int x,
			int y,
			int nWidth,
			int nHeight,
			IntPtr hWndParent,
			IntPtr hMenu,
			IntPtr hInstance,
			IntPtr lpParam);

		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool DestroyWindow(IntPtr hWnd);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr GetModuleHandle(string modName);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern bool SetWindowText(IntPtr hWnd, string lpString);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		public struct WNDCLASS
		{
			public uint style;
			public Delegate lpfnWndProc;
			public int cbClsExtra;
			public int cbWndExtra;
			public IntPtr hInstance;
			public IntPtr hIcon;
			public IntPtr hCursor;
			public IntPtr hbrBackground;
			public string lpszMenuName;
			public string lpszClassName;
		}

		/// <summary>
		/// Put a Form into foreground and activate the window. This is more reliable then Form.Focus().
		/// </summary>
		/// <param name="form"></param>
		public static void FocusForm(System.Windows.Forms.Form form)
		{
			SetForegroundWindow(form.Handle);
		}

		// From: http://www.pinvoke.net/default.aspx/Enums/WindowHitTestRegions.html
		/// <summary>Options available when a form is tested for mose positions.</summary>
		public enum WindowHitTestRegions
		{
			/// <summary>HTERROR: On the screen background or on a dividing line between windows
			/// (same as HTNOWHERE, except that the DefWindowProc function produces a system
			/// beep to indicate an error).</summary>
			Error = -2,
			/// <summary>HTTRANSPARENT: In a window currently covered by another window in the
			/// same thread (the message will be sent to underlying windows in the same thread
			/// until one of them returns a code that is not HTTRANSPARENT).</summary>
			TransparentOrCovered = -1,
			/// <summary>HTNOWHERE: On the screen background or on a dividing line between
			/// windows.</summary>
			NoWhere = 0,
			/// <summary>HTCLIENT: In a client area.</summary>
			ClientArea = 1,
			/// <summary>HTCAPTION: In a title bar.</summary>
			TitleBar = 2,
			/// <summary>HTSYSMENU: In a window menu or in a Close button in a child window.</summary>
			SystemMenu = 3,
			/// <summary>HTGROWBOX: In a size box (same as HTSIZE).</summary>
			GrowBox = 4,
			/// <summary>HTMENU: In a menu.</summary>
			Menu = 5,
			/// <summary>HTHSCROLL: In a horizontal scroll bar.</summary>
			HorizontalScrollBar = 6,
			/// <summary>HTVSCROLL: In the vertical scroll bar.</summary>
			VerticalScrollBar = 7,
			/// <summary>HTMINBUTTON: In a Minimize button. </summary>
			MinimizeButton = 8,
			/// <summary>HTMAXBUTTON: In a Maximize button.</summary>
			MaximizeButton = 9,
			/// <summary>HTLEFT: In the left border of a resizable window (the user can click
			/// the mouse to resize the window horizontally).</summary>
			LeftSizeableBorder = 10,
			/// <summary>HTRIGHT: In the right border of a resizable window (the user can click
			/// the mouse to resize the window horizontally).</summary>
			RightSizeableBorder = 11,
			/// <summary>HTTOP: In the upper-horizontal border of a window.</summary>
			TopSizeableBorder = 12,
			/// <summary>HTTOPLEFT: In the upper-left corner of a window border.</summary>
			TopLeftSizeableCorner = 13,
			/// <summary>HTTOPRIGHT: In the upper-right corner of a window border.</summary>
			TopRightSizeableCorner = 14,
			/// <summary>HTBOTTOM: In the lower-horizontal border of a resizable window (the
			/// user can click the mouse to resize the window vertically).</summary>
			BottomSizeableBorder = 15,
			/// <summary>HTBOTTOMLEFT: In the lower-left corner of a border of a resizable
			/// window (the user can click the mouse to resize the window diagonally).</summary>
			BottomLeftSizeableCorner = 16,
			/// <summary>HTBOTTOMRIGHT: In the lower-right corner of a border of a resizable
			/// window (the user can click the mouse to resize the window diagonally).</summary>
			BottomRightSizeableCorner = 17,
			/// <summary>HTBORDER: In the border of a window that does not have a sizing
			/// border.</summary>
			NonSizableBorder = 18,
			/// <summary>HTOBJECT: Unknown...No Documentation Found</summary>
			Object = 19,
			/// <summary>HTCLOSE: In a Close button.</summary>
			CloseButton = 20,
			/// <summary>HTHELP: In a Help button.</summary>
			HelpButton = 21,
			/// <summary>HTSIZE: In a size box (same as HTGROWBOX). (Same as GrowBox).</summary>
			SizeBox = GrowBox,
			/// <summary>HTREDUCE: In a Minimize button. (Same as MinimizeButton).</summary>
			ReduceButton = MinimizeButton,
			/// <summary>HTZOOM: In a Maximize button. (Same as MaximizeButton).</summary>
			ZoomButton = MaximizeButton
		}
	}
}
