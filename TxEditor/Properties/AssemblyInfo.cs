﻿using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

[assembly: AssemblyProduct("TxEditor")]
[assembly: AssemblyTitle("TxEditor")]
[assembly: AssemblyDescription("TxTranslation Editor")]

[assembly: AssemblyCompany("")]
[assembly: AssemblyCopyright("© Yves Goergen")]

// Assembly version, also used for Win32 file version resource.
// Must be a plain numeric version definition:
// 1. Major version number, should be increased with major new versions or rewrites of the application
// 2. Minor version number, should ne increased with minor feature changes or new features
// 3. Bugfix number, should be set or increased for bugfix releases of a previous version
// 4. Unused
[assembly: AssemblyVersion("1.0.0")]
// Informational version string, used for the About dialog, error reports and the setup script.
// Can be any freely formatted string containing punctuation, letters and revision codes.
// Should be set to the same value as AssemblyVersion if only the basic numbering scheme is applied.
[assembly: AssemblyInformationalVersion("1.0")]

[assembly: ComVisible(false)]
[assembly: ThemeInfo(
	ResourceDictionaryLocation.None, //Speicherort der designspezifischen Ressourcenwörterbücher
	//(wird verwendet, wenn eine Ressource auf der Seite
	// oder in den Anwendungsressourcen-Wörterbüchern nicht gefunden werden kann.)
	ResourceDictionaryLocation.SourceAssembly //Speicherort des generischen Ressourcenwörterbuchs
	//(wird verwendet, wenn eine Ressource auf der Seite, in der Anwendung oder einem 
	// designspezifischen Ressourcenwörterbuch nicht gefunden werden kann.)
)]