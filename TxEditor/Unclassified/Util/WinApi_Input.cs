using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Unclassified.Util
{
	public partial class WinApi
	{
		// Windows messages
		public const int WM_INPUT = 0x00FF;
		public const int WM_KEYDOWN = 0x0100;
		public const int WM_KEYUP = 0x0101;
		public const int WM_HOTKEY = 0x0312;
		public const int WM_APPCOMMAND = 0x0319;

		// Modifier keys for RegisterHotKey and WM_HOTKEY
		public const int MOD_ALT = 0x0001;
		public const int MOD_CONTROL = 0x0002;
		public const int MOD_SHIFT = 0x0004;
		public const int MOD_WIN = 0x0008;

		// Key State Masks for Mouse Messages
		public const int MK_LBUTTON = 0x0001;
		public const int MK_RBUTTON = 0x0002;

		// from http://www.pinvoke.net/default.aspx/user32/SendInput.html
		public const int INPUT_MOUSE = 0;
		public const int INPUT_KEYBOARD = 1;
		public const int INPUT_HARDWARE = 2;

		public const int KEYEVENTF_KEYUP = 0x0002;

		public const int FAPPCOMMAND_MASK = 0xF000;

		[StructLayout(LayoutKind.Sequential)]
		public struct MOUSEINPUT
		{
			public int dx;
			public int dy;
			public int mouseData;
			public int dwFlags;
			public int time;
			public IntPtr dwExtraInfo;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct KEYBDINPUT
		{
			public short wVk;
			public short wScan;
			public int dwFlags;
			public int time;
			public IntPtr dwExtraInfo;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct HARDWAREINPUT
		{
			public int uMsg;
			public short wParamL;
			public short wParamH;
		}

		[StructLayout(LayoutKind.Explicit)]
		public struct INPUT
		{
			[FieldOffset(0)]
			public int type;
			[FieldOffset(4)]
			public MOUSEINPUT mi;
			[FieldOffset(4)]
			public KEYBDINPUT ki;
			[FieldOffset(4)]
			public HARDWAREINPUT hi;
		}

		public enum VK : short
		{
			SHIFT = 0x10,
			CONTROL = 0x11,
			MENU = 0x12,
			ESCAPE = 0x1B,
			BACK = 0x08,
			TAB = 0x09,
			RETURN = 0x0D,
			PRIOR = 0x21,
			NEXT = 0x22,
			END = 0x23,
			HOME = 0x24,
			LEFT = 0x25,
			UP = 0x26,
			RIGHT = 0x27,
			DOWN = 0x28,
			SELECT = 0x29,
			PRINT = 0x2A,
			EXECUTE = 0x2B,
			SNAPSHOT = 0x2C,
			INSERT = 0x2D,
			DELETE = 0x2E,
			HELP = 0x2F,
			NUMPAD0 = 0x60,
			NUMPAD1 = 0x61,
			NUMPAD2 = 0x62,
			NUMPAD3 = 0x63,
			NUMPAD4 = 0x64,
			NUMPAD5 = 0x65,
			NUMPAD6 = 0x66,
			NUMPAD7 = 0x67,
			NUMPAD8 = 0x68,
			NUMPAD9 = 0x69,
			MULTIPLY = 0x6A,
			ADD = 0x6B,
			SEPARATOR = 0x6C,
			SUBTRACT = 0x6D,
			DECIMAL = 0x6E,
			DIVIDE = 0x6F,
			F1 = 0x70,
			F2 = 0x71,
			F3 = 0x72,
			F4 = 0x73,
			F5 = 0x74,
			F6 = 0x75,
			F7 = 0x76,
			F8 = 0x77,
			F9 = 0x78,
			F10 = 0x79,
			F11 = 0x7A,
			F12 = 0x7B,
			OEM_1 = 0xBA,   // ',:' for US
			OEM_PLUS = 0xBB,   // '+' any country
			OEM_COMMA = 0xBC,   // ',' any country
			OEM_MINUS = 0xBD,   // '-' any country
			OEM_PERIOD = 0xBE,   // '.' any country
			OEM_2 = 0xBF,   // '/?' for US
			OEM_3 = 0xC0,   // '`~' for US
			MEDIA_NEXT_TRACK = 0xB0,
			MEDIA_PREV_TRACK = 0xB1,
			MEDIA_STOP = 0xB2,
			MEDIA_PLAY_PAUSE = 0xB3,
			LWIN = 0x5B,
			RWIN = 0x5C,
			VOLUME_MUTE = 0xAD,
			VOLUME_DOWN = 0xAE,
			VOLUME_UP = 0xAF
		}

		public static VK KeyToVk(Keys key)
		{
			switch (key)
			{
				case Keys.A:
					return (VK)VkKeyScan('a');
				case Keys.B:
					return (VK)VkKeyScan('b');
				case Keys.C:
					return (VK)VkKeyScan('c');
				case Keys.D:
					return (VK)VkKeyScan('d');
				case Keys.E:
					return (VK)VkKeyScan('e');
				case Keys.F:
					return (VK)VkKeyScan('f');
				case Keys.G:
					return (VK)VkKeyScan('g');
				case Keys.H:
					return (VK)VkKeyScan('h');
				case Keys.I:
					return (VK)VkKeyScan('i');
				case Keys.J:
					return (VK)VkKeyScan('j');
				case Keys.K:
					return (VK)VkKeyScan('k');
				case Keys.L:
					return (VK)VkKeyScan('l');
				case Keys.M:
					return (VK)VkKeyScan('m');
				case Keys.N:
					return (VK)VkKeyScan('n');
				case Keys.O:
					return (VK)VkKeyScan('o');
				case Keys.P:
					return (VK)VkKeyScan('p');
				case Keys.Q:
					return (VK)VkKeyScan('q');
				case Keys.R:
					return (VK)VkKeyScan('r');
				case Keys.S:
					return (VK)VkKeyScan('s');
				case Keys.T:
					return (VK)VkKeyScan('t');
				case Keys.U:
					return (VK)VkKeyScan('u');
				case Keys.V:
					return (VK)VkKeyScan('v');
				case Keys.W:
					return (VK)VkKeyScan('w');
				case Keys.X:
					return (VK)VkKeyScan('x');
				case Keys.Y:
					return (VK)VkKeyScan('y');
				case Keys.Z:
					return (VK)VkKeyScan('z');
				case Keys.D0:
					return (VK)VkKeyScan('0');
				case Keys.D1:
					return (VK)VkKeyScan('1');
				case Keys.D2:
					return (VK)VkKeyScan('2');
				case Keys.D3:
					return (VK)VkKeyScan('3');
				case Keys.D4:
					return (VK)VkKeyScan('4');
				case Keys.D5:
					return (VK)VkKeyScan('5');
				case Keys.D6:
					return (VK)VkKeyScan('6');
				case Keys.D7:
					return (VK)VkKeyScan('7');
				case Keys.D8:
					return (VK)VkKeyScan('8');
				case Keys.D9:
					return (VK)VkKeyScan('9');
				case Keys.Space:
					return (VK)VkKeyScan(' ');

				case Keys.ShiftKey:
					return VK.SHIFT;
				case Keys.ControlKey:
					return VK.CONTROL;
				case Keys.Menu:
					return VK.MENU;
				case Keys.Escape:
					return VK.ESCAPE;
				case Keys.Back:
					return VK.BACK;
				case Keys.Tab:
					return VK.TAB;
				case Keys.Return:
					return VK.RETURN;
				case Keys.End:
					return VK.END;
				case Keys.Home:
					return VK.HOME;
				case Keys.Left:
					return VK.LEFT;
				case Keys.Up:
					return VK.UP;
				case Keys.Right:
					return VK.RIGHT;
				case Keys.Down:
					return VK.DOWN;
				case Keys.Insert:
					return VK.INSERT;
				case Keys.Delete:
					return VK.DELETE;
				case Keys.NumPad0:
					return VK.NUMPAD0;
				case Keys.NumPad1:
					return VK.NUMPAD1;
				case Keys.NumPad2:
					return VK.NUMPAD2;
				case Keys.NumPad3:
					return VK.NUMPAD3;
				case Keys.NumPad4:
					return VK.NUMPAD4;
				case Keys.NumPad5:
					return VK.NUMPAD5;
				case Keys.NumPad6:
					return VK.NUMPAD6;
				case Keys.NumPad7:
					return VK.NUMPAD7;
				case Keys.NumPad8:
					return VK.NUMPAD8;
				case Keys.NumPad9:
					return VK.NUMPAD9;
				case Keys.Multiply:
					return VK.MULTIPLY;
				case Keys.Add:
					return VK.ADD;
				case Keys.Separator:
					return VK.SEPARATOR;
				case Keys.Subtract:
					return VK.SUBTRACT;
				case Keys.Decimal:
					return VK.DECIMAL;
				case Keys.Divide:
					return VK.DIVIDE;
				case Keys.F1:
					return VK.F1;
				case Keys.F2:
					return VK.F2;
				case Keys.F3:
					return VK.F3;
				case Keys.F4:
					return VK.F4;
				case Keys.F5:
					return VK.F5;
				case Keys.F6:
					return VK.F6;
				case Keys.F7:
					return VK.F7;
				case Keys.F8:
					return VK.F8;
				case Keys.F9:
					return VK.F9;
				case Keys.F10:
					return VK.F10;
				case Keys.F11:
					return VK.F11;
				case Keys.F12:
					return VK.F12;
				case Keys.LWin:
					return VK.LWIN;
				case Keys.RWin:
					return VK.RWIN;
				case Keys.MediaNextTrack:
					return VK.MEDIA_NEXT_TRACK;
				case Keys.MediaPreviousTrack:
					return VK.MEDIA_PREV_TRACK;
				case Keys.MediaPlayPause:
					return VK.MEDIA_PLAY_PAUSE;
				case Keys.MediaStop:
					return VK.MEDIA_STOP;
				default:
					return 0;
			}
		}

		public enum AppCommands : int
		{
			APPCOMMAND_BROWSER_BACKWARD = 1,
			APPCOMMAND_BROWSER_FORWARD = 2,
			APPCOMMAND_BROWSER_REFRESH = 3,
			APPCOMMAND_BROWSER_STOP = 4,
			APPCOMMAND_BROWSER_SEARCH = 5,
			APPCOMMAND_BROWSER_FAVORITES = 6,
			APPCOMMAND_BROWSER_HOME = 7,
			APPCOMMAND_VOLUME_MUTE = 8,
			APPCOMMAND_VOLUME_DOWN = 9,
			APPCOMMAND_VOLUME_UP = 10,
			APPCOMMAND_MEDIA_NEXTTRACK = 11,
			APPCOMMAND_MEDIA_PREVIOUSTRACK = 12,
			APPCOMMAND_MEDIA_STOP = 13,
			APPCOMMAND_MEDIA_PLAY_PAUSE = 14,
			APPCOMMAND_LAUNCH_MAIL = 15,
			APPCOMMAND_LAUNCH_MEDIA_SELECT = 16,
			APPCOMMAND_LAUNCH_APP1 = 17,
			APPCOMMAND_LAUNCH_APP2 = 18,
			APPCOMMAND_BASS_DOWN = 19,
			APPCOMMAND_BASS_BOOST = 20,
			APPCOMMAND_BASS_UP = 21,
			APPCOMMAND_TREBLE_DOWN = 22,
			APPCOMMAND_TREBLE_UP = 23,
			APPCOMMAND_MICROPHONE_VOLUME_MUTE = 24,
			APPCOMMAND_MICROPHONE_VOLUME_DOWN = 25,
			APPCOMMAND_MICROPHONE_VOLUME_UP = 26,
			APPCOMMAND_HELP = 27,
			APPCOMMAND_FIND = 28,
			APPCOMMAND_NEW = 29,
			APPCOMMAND_OPEN = 30,
			APPCOMMAND_CLOSE = 31,
			APPCOMMAND_SAVE = 32,
			APPCOMMAND_PRINT = 33,
			APPCOMMAND_UNDO = 34,
			APPCOMMAND_REDO = 35,
			APPCOMMAND_COPY = 36,
			APPCOMMAND_CUT = 37,
			APPCOMMAND_PASTE = 38,
			APPCOMMAND_REPLY_TO_MAIL = 39,
			APPCOMMAND_FORWARD_MAIL = 40,
			APPCOMMAND_SEND_MAIL = 41,
			APPCOMMAND_SPELL_CHECK = 42,
			APPCOMMAND_DICTATE_OR_COMMAND_CONTROL_TOGGLE = 43,
			APPCOMMAND_MIC_ON_OFF_TOGGLE = 44,
			APPCOMMAND_CORRECTION_LIST = 45,
			APPCOMMAND_MEDIA_PLAY = 46,
			APPCOMMAND_MEDIA_PAUSE = 47,
			APPCOMMAND_MEDIA_RECORD = 48,
			APPCOMMAND_MEDIA_FAST_FORWARD = 49,
			APPCOMMAND_MEDIA_REWIND = 50,
			APPCOMMAND_MEDIA_CHANNEL_UP = 51,
			APPCOMMAND_MEDIA_CHANNEL_DOWN = 52
		}

		[DllImport("kernel32")]
		public static extern int GetCurrentThreadId();

		[DllImport("user32")]
		public static extern bool AttachThreadInput(int idAttach, int idAttachTo, bool fAttach);

		[DllImport("user32.dll")]
		public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

		[DllImport("user32.dll")]
		public static extern short VkKeyScan(char ch);

		[DllImport("user32.dll")]
		public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

		[DllImport("user32.dll")]
		public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

		[DllImport("user32.dll")]
		public static extern short GetKeyState(VK nVirtKey);

		private static int GET_APPCOMMAND_LPARAM(int lParam)
		{
			return ((short)(((lParam >> 16) & 0xFFFF) & ~FAPPCOMMAND_MASK));
		}

		public static AppCommands GetAppCommand(Message m)
		{
			int cmd = GET_APPCOMMAND_LPARAM(m.LParam.ToInt32());
			return (AppCommands)cmd;
		}
	}
}
