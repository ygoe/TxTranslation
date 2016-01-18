using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

[assembly: AssemblyProduct("TxEditor")]
[assembly: AssemblyTitle("TxEditor")]
[assembly: AssemblyDescription("TxTranslation Editor")]
[assembly: AssemblyCopyright("© Yves Goergen, GNU GPL v3")]
[assembly: AssemblyCompany("unclassified software development")]

// Assembly identity version. Must be a dotted-numeric version.
// NOTE: Do not edit. This value is automatically set during the build process.
[assembly: AssemblyVersion("1.0")]

// Repeat for Win32 file version resource because the assembly version is expanded to 4 parts.
// NOTE: Do not edit. This value is automatically set during the build process.
[assembly: AssemblyFileVersion("1.0")]

// Informational version string, used for the About dialog, error reports and the setup script.
// Can be any freely formatted string containing punctuation, letters and revision codes.
[assembly: AssemblyInformationalVersion("1.{dmin:2015}_{chash:6}{!:+}")]

// Indicate the build configuration
#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

// Other attributes
[assembly: ComVisible(false)]
[assembly: ThemeInfo(
	// Where theme specific resource dictionaries are located
	// (used if a resource is not found in the page, or application resource dictionaries)
	ResourceDictionaryLocation.None,
	// Where the generic resource dictionary is located
	// (used if a resource is not found in the page, app, or any theme specific resource dictionaries)
	ResourceDictionaryLocation.SourceAssembly
)]
