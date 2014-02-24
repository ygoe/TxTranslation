using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace Unclassified
{
	/// <summary>
	/// Provides information about the execution environment like the operating system version or
	/// user context.
	/// </summary>
	public class MyEnvironment
	{
		#region OS version detection

		[StructLayout(LayoutKind.Sequential)]
		private struct OSVERSIONINFOEX
		{
			public int dwOSVersionInfoSize;
			public int dwMajorVersion;
			public int dwMinorVersion;
			public int dwBuildNumber;
			public int dwPlatformId;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string szCSDVersion;
			public short wServicePackMajor;
			public short wServicePackMinor;
			public ushort wSuiteMask;
			public byte wProductType;
			public byte wReserved;
		}

		private enum SystemMetric
		{
			SM_SERVERR2 = 89
		}

		private const ushort VER_SUITE_WH_SERVER = 0x8000;
		private const ushort VER_NT_WORKSTATION = 1;

		[DllImport("kernel32.dll")]
		private static extern short GetVersionEx(ref OSVERSIONINFOEX osvi);

		[DllImport("user32.dll")]
		private static extern int GetSystemMetrics(SystemMetric smIndex);

		private static OSVERSIONINFOEX osvi;
		private static bool haveOsvi;

		private static void EnsureVersion()
		{
			if (!haveOsvi)
			{
				osvi = new OSVERSIONINFOEX();
				osvi.dwOSVersionInfoSize = Marshal.SizeOf(typeof(OSVERSIONINFOEX));
				GetVersionEx(ref osvi);
				haveOsvi = true;
			}
		}

		/// <summary>
		/// Gets the installed operating system version value.
		/// </summary>
		public static OSVersion OSVersion
		{
			get
			{
				OperatingSystem os = Environment.OSVersion;
				switch (os.Platform)
				{
					case PlatformID.Win32Windows:
						if (os.Version.Major >= 4 && os.Version.Minor == 0)
							return OSVersion.Windows95;
						if (os.Version.Major >= 4 && os.Version.Minor > 0 && os.Version.Minor < 90)
							return OSVersion.Windows98;
						if (os.Version.Major >= 4 && os.Version.Minor >= 90)
							return OSVersion.WindowsME;
						break;

					case PlatformID.Win32NT:
						EnsureVersion();
						int r2 = GetSystemMetrics(SystemMetric.SM_SERVERR2);
						// Interpretation of the values see
						// http://msdn.microsoft.com/en-us/library/windows/desktop/ms724833.aspx
						// section Remarks
						if (os.Version.Major <= 4)
							return OSVersion.WindowsNT4;
						if (os.Version.Major == 5 && os.Version.Minor == 0)
							return OSVersion.Windows2000;
						if (os.Version.Major == 5 && os.Version.Minor == 1)
							return OSVersion.WindowsXP;
						if (os.Version.Major == 5 && os.Version.Minor == 2)
						{
							if ((osvi.wSuiteMask & VER_SUITE_WH_SERVER) != 0)
								return OSVersion.WindowsHomeServer;
							if (osvi.wProductType == VER_NT_WORKSTATION && Environment.Is64BitOperatingSystem)
								return OSVersion.WindowsXP;
							if (GetSystemMetrics(SystemMetric.SM_SERVERR2) == 0)
								return OSVersion.WindowsServer2003;
							if (GetSystemMetrics(SystemMetric.SM_SERVERR2) != 0)
								return OSVersion.WindowsServer2003R2;
						}
						if (os.Version.Major == 6 && os.Version.Minor == 0)
						{
							if (osvi.wProductType == VER_NT_WORKSTATION)
								return OSVersion.WindowsVista;
							else
								return OSVersion.WindowsServer2008;
						}
						if (os.Version.Major == 6 && os.Version.Minor == 1)
						{
							if (osvi.wProductType == VER_NT_WORKSTATION)
								return OSVersion.Windows7;
							else
								return OSVersion.WindowsServer2008R2;
						}
						if (os.Version.Major == 6 && os.Version.Minor == 2)
						{
							if (osvi.wProductType == VER_NT_WORKSTATION)
								return OSVersion.Windows8;
							else
								return OSVersion.WindowsServer2012;
						}
						if (os.Version.Major == 6 && os.Version.Minor > 2 || os.Version.Major > 6)
							return OSVersion.WindowsFuture;
						break;
				}
				return OSVersion.Unknown;
			}
		}

		/// <summary>
		/// Gets the installed operating system name.
		/// </summary>
		public static string OSName
		{
			get
			{
				switch (OSVersion)
				{
					case OSVersion.Windows95: return "Windows 95";
					case OSVersion.Windows98: return "Windows 98";
					case OSVersion.WindowsME: return "Windows ME";
					case OSVersion.WindowsNT4: return "Windows NT 4";
					case OSVersion.Windows2000: return "Windows 2000";
					case OSVersion.WindowsXP: return "Windows XP";
					case OSVersion.WindowsHomeServer: return "Windows Home Server";
					case OSVersion.WindowsServer2003: return "Windows Server 2003";
					case OSVersion.WindowsServer2003R2: return "Windows Server 2003 R2";
					case OSVersion.WindowsVista: return "Windows Vista";
					case OSVersion.WindowsServer2008: return "Windows Server 2008";
					case OSVersion.Windows7: return "Windows 7";
					case OSVersion.WindowsServer2008R2: return "Windows Server 2008 R2";
					case OSVersion.Windows8: return "Windows 8";
					case OSVersion.WindowsServer2012: return "Windows Server 2012";
					case OSVersion.WindowsFuture: return "Future Windows version";
					case OSVersion.Unknown: return "Unknown";
				}
				return "";
			}
		}

		/// <summary>
		/// Gets a value indicating whether the running process is 64 bit.
		/// </summary>
		public static bool Is64BitProcess
		{
			get
			{
				return Environment.Is64BitProcess;
				//return IntPtr.Size == 8;
			}
		}

		/// <summary>
		/// Gets a value indicating whether the installed operating system is 64 bit.
		/// </summary>
		public static bool Is64BitOS
		{
			get
			{
				return Environment.Is64BitOperatingSystem;
			}
		}

		/// <summary>
		/// Gets the installed operating system product name, including edition.
		/// </summary>
		public static string OSProductName
		{
			get
			{
				if (Environment.OSVersion.Platform == PlatformID.Win32NT)
				{
					try
					{
						using (RegistryKey key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion"))
						{
							string productName = key.GetValue("ProductName") as string;
							if (string.IsNullOrEmpty(productName))
							{
								// Missing value, use fallback
								return OSName;
							}
							return productName;
						}
					}
					catch
					{
						// Something went wrong, use fallback
						return OSName;
					}
				}
				else
				{
					// Not an NT system, use fallback
					return OSName;
				}
			}
		}

		/// <summary>
		/// Gets the major version number of the installed service pack, if any, or 0 otherwise.
		/// </summary>
		static public int OSServicePack
		{
			get
			{
				EnsureVersion();
				return osvi.wServicePackMajor;
			}
		}

		/// <summary>
		/// Gets the name of the installed service pack, if any, or an empty string otherwise.
		/// </summary>
		static public string OSServicePackString
		{
			get
			{
				EnsureVersion();
				return osvi.szCSDVersion;
			}
		}

		/// <summary>
		/// Gets the installed operating system build number.
		/// </summary>
		public static string OSBuild
		{
			get
			{
				OperatingSystem os = Environment.OSVersion;
				return os.Version.Build.ToString();
			}
		}

		#endregion OS version detection

		#region OS user detection

		/// <summary>
		/// Checks whether the logged on Windows user is member of the specified Windows group.
		/// </summary>
		/// <param name="groupName">Group name (format: "Domain\Group")</param>
		/// <returns>true, if the user is member of the group, false otherwise.</returns>
		/// <remarks>Source: http://www.mycsharp.de/wbb2/thread.php?threadid=36895 </remarks>
		public static bool IsCurrentUserInWindowsGroup(string groupName)
		{
			System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
			if (!identity.IsAuthenticated)
				throw new System.Security.SecurityException("The current Windows user is not authenticated.");

			System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
			return principal.IsInRole(groupName);
		}

		#endregion OS user detection

		#region Application information

		public static string AssemblyTitle
		{
			get
			{
				object[] customAttributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyTitleAttribute) customAttributes[0]).Title;
				}
				return null;
			}
		}

		public static string AssemblyProduct
		{
			get
			{
				object[] customAttributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyProductAttribute) customAttributes[0]).Product;
				}
				return null;
			}
		}

		public static string AssemblyFileVersion
		{
			get
			{
				object[] customAttributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyFileVersionAttribute) customAttributes[0]).Version;
				}
				return null;
			}
		}

		public static string AssemblyDescription
		{
			get
			{
				object[] customAttributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyDescriptionAttribute) customAttributes[0]).Description;
				}
				return null;
			}
		}

		public static string AssemblyCopyright
		{
			get
			{
				object[] customAttributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyCopyrightAttribute) customAttributes[0]).Copyright;
				}
				return null;
			}
		}

		public static string AssemblyInformationalVersion
		{
			get
			{
				object[] customAttributes = Assembly.GetEntryAssembly().GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false);
				if (customAttributes != null && customAttributes.Length > 0)
				{
					return ((AssemblyInformationalVersionAttribute) customAttributes[0]).InformationalVersion;
				}
				return null;
			}
		}

		#endregion Application information
	}

	#region Operating system versions enumeration

	/// <summary>
	/// Enumeration of known operating systems
	/// </summary>
	public enum OSVersion
	{
		/// <summary>
		/// Unknown
		/// </summary>
		Unknown,
		/// <summary>
		/// Microsoft Windows 95
		/// </summary>
		Windows95,
		/// <summary>
		/// Microsoft Windows 98
		/// </summary>
		Windows98,
		/// <summary>
		/// Microsoft Windows ME
		/// </summary>
		WindowsME,
		/// <summary>
		/// Microsoft Windows NT 4.0
		/// </summary>
		WindowsNT4,
		/// <summary>
		/// Microsoft Windows 2000
		/// </summary>
		Windows2000,
		/// <summary>
		/// Microsoft Windows XP
		/// </summary>
		WindowsXP,
		/// <summary>
		/// Microsoft Windows Home Server (like Windows XP)
		/// </summary>
		WindowsHomeServer,
		/// <summary>
		/// Microsoft Windows Server 2003 (like Windows XP)
		/// </summary>
		WindowsServer2003,
		/// <summary>
		/// Microsoft Windows Server 2003 R2 (like Windows XP)
		/// </summary>
		WindowsServer2003R2,
		/// <summary>
		/// Microsoft Windows Vista
		/// </summary>
		WindowsVista,
		/// <summary>
		/// Microsoft Windows Server 2008 (like Windows Vista)
		/// </summary>
		WindowsServer2008,
		/// <summary>
		/// Microsoft Windows 7
		/// </summary>
		Windows7,
		/// <summary>
		/// Microsoft Windows Server 2008 R2 (like Windows 7)
		/// </summary>
		WindowsServer2008R2,
		/// <summary>
		/// Microsoft Windows 8
		/// </summary>
		Windows8,
		/// <summary>
		/// Microsoft Windows Server 2012 (like Windows 8)
		/// </summary>
		WindowsServer2012,
		/// <summary>
		/// Future version of Microsoft Windows
		/// </summary>
		WindowsFuture
	}

	#endregion Operating system versions enumeration
}
