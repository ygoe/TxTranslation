; Determine product and file version from the application to be installed
#ifndef BuildConfig
	#define BuildConfig "Release"
#endif
#define RevFileName "..\TxEditor\bin\" + BuildConfig + "\TxEditor.exe"
#define RevId GetStringFileInfo(RevFileName, "ProductVersion")
#define ShortRevId GetFileVersion(RevFileName)

; Include 3rd-party software check and download support
#include "include\products.iss"
#include "include\products\stringversion.iss"
#include "include\products\winversion.iss"
#include "include\products\fileversion.iss"
#include "include\products\dotnetfxversion.iss"

; Include modules ONLY for required products to be installed
#include "include\products\msi31.iss"
#include "include\products\dotnetfx40client.iss"

; Include general helper functions
#include "include\util-code.iss"

; Include Visual Studio external tools management
#include "include\visualstudio-tool.iss"

[Setup]
; Names and versions for the Windows programs listing
AppName=TxEditor
AppVersion={#RevId}
AppCopyright=© Yves Goergen, GNU GPL v3
AppPublisher=Yves Goergen
AppPublisherURL=http://unclassified.software/source/txtranslation

; Setup file version
VersionInfoDescription=TxTranslation Setup
VersionInfoVersion={#ShortRevId}
VersionInfoCompany=Yves Goergen

; General application information
AppId={{99B66B72-FF8D-4169-ADE6-062A9EF0EB13}
AppMutex=Global\Unclassified.TxEditor,Unclassified.TxEditor
MinVersion=0,5.01sp3
; isxdl.dll may not be DEP compatible
DEPCompatible=no

; General setup information
DefaultDirName={pf}\Unclassified\TxTranslation
AllowUNCPath=False
DefaultGroupName=TxTranslation
DisableWelcomePage=no
DisableDirPage=auto
DisableProgramGroupPage=auto
ShowLanguageDialog=no
ChangesAssociations=yes

; Setup design
; Large image max. 164x314 pixels, small image max. 55x58 pixels
WizardImageStretch=no
WizardImageFile=TxFlag.bmp
WizardSmallImageFile=TxFlagSmall.bmp

; Uninstaller configuration
UninstallDisplayName=TxTranslation
UninstallDisplayIcon={app}\TxEditor.exe

; Setup package creation
OutputDir=bin
OutputBaseFilename=TxSetup-{#RevId}
SolidCompression=True
InternalCompressLevel=max

; This file must be included after other setup settings
#include "include\previous-installation.iss"

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"

[LangOptions]
; More setup design
DialogFontName=Segoe UI
DialogFontSize=9
WelcomeFontName=Segoe UI
WelcomeFontSize=12

[Messages]
WelcomeLabel1=%n%n%nWelcome to the Tx setup wizard
WelcomeLabel2=TxTranslation is a simple yet powerful translation and localisation library for .NET applications. It supports XAML binding, language fallbacks, count-specific translations, placeholders, and number and time formatting.%n%nVersion: {#RevId}
ClickNext=Click Next to continue installing TxEditor, documentation, the TxLib library and source code, or Cancel to exit the setup.
FinishedHeadingLabel=%n%n%n%nTxTranslation is now installed.
FinishedLabelNoIcons=
FinishedLabel=The application may be launched by selecting the installed start menu icon.
ClickFinish=Click Finish to exit the setup.

[CustomMessages]
Upgrade=&Upgrade
UpdatedHeadingLabel=%n%n%n%nTxTranslation was upgraded.
Task_VSTool=Register as External Tool in Visual Studio (2010–2015)
NgenMessage=Optimising application performance (this may take a moment)
Uninstall_DeleteConfig=Do you want to delete the configuration data incl. logs?

; Add translations after messages have been defined
#include "Tx.de.iss"

[Tasks]
Name: VSTool; Description: "{cm:Task_VSTool}"

[Files]
; TxEditor application files
Source: "..\TxEditor\bin\{#BuildConfig}\TxEditor.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\TxEditor\bin\{#BuildConfig}\MultiSelectTreeView.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\TxEditor\bin\{#BuildConfig}\TaskDialog.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\TxEditor\bin\{#BuildConfig}\Unclassified.FieldLog.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Tx Documentation.pdf"; DestDir: "{app}"
; This is the signed version of the DLL:
Source: "..\TxLib\bin\{#BuildConfig}\Unclassified.TxLib.dll"; DestDir: "{app}"; Flags: ignoreversion

; TxLib assembly
Source: "..\TxLib\bin\{#BuildConfig}\Unclassified.TxLib.dll"; DestDir: "{app}\TxLib assembly"; Flags: ignoreversion
Source: "..\TxLib\bin\{#BuildConfig}\Unclassified.TxLib.xml"; DestDir: "{app}\TxLib assembly"

; TxLib source code
Source: "..\TxLib\DateTimeInterval.cs"; DestDir: "{app}\TxLib source code"
Source: "..\TxLib\Tx.cs"; DestDir: "{app}\TxLib source code"
Source: "..\TxLib\TxWinForms.cs"; DestDir: "{app}\TxLib source code"
Source: "..\TxLib\TxXaml.cs"; DestDir: "{app}\TxLib source code"

; License files
Source: "..\LICENSE-GPL"; DestDir: "{app}"
Source: "..\LICENSE-LGPL"; DestDir: "{app}"

[Dirs]
; Create user-writable log directory in the installation directory.
; FieldLog will first try to write log file there.
Name: "{app}\log"; Permissions: users-modify

[Registry]
; Register .txd file name extension
Root: HKCR; Subkey: ".txd"; ValueType: string; ValueName: ""; ValueData: "TxDictionary"; Flags: uninsdeletevalue 
Root: HKCR; Subkey: "TxDictionary"; ValueType: string; ValueName: ""; ValueData: "Tx dictionary"; Flags: uninsdeletekey
Root: HKCR; Subkey: "TxDictionary\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\TxEditor.exe,1"
Root: HKCR; Subkey: "TxDictionary\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\TxEditor.exe"" ""%1"""

; Add to .txd "Open with" menu
Root: HKCR; Subkey: ".txd\OpenWithList\TxEditor.exe"; ValueType: string; ValueName: ""; ValueData: ""; Flags: uninsdeletekey
Root: HKCR; Subkey: ".txd\OpenWithList\notepad.exe"; ValueType: string; ValueName: ""; ValueData: ""; Flags: uninsdeletekey
Root: HKCR; Subkey: "Applications\TxEditor.exe"; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "TxEditor"; Flags: uninsdeletekey
Root: HKCR; Subkey: "Applications\TxEditor.exe\shell\open"; ValueType: string; ValueName: "FriendlyAppName"; ValueData: "TxEditor"
Root: HKCR; Subkey: "Applications\TxEditor.exe\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\TxEditor.exe"" ""%1"""

[Icons]
; Start menu
Name: "{group}\TxEditor"; Filename: "{app}\TxEditor.exe"; IconFilename: "{app}\TxEditor.exe"
Name: "{group}\Tx Documentation"; Filename: "{app}\Tx Documentation.pdf"
Name: "{group}\TxLib website"; Filename: "http://dev.unclassified.de/source/txlib"
Name: "{group}\TxLib assembly"; Filename: "{app}\TxLib assembly\"
Name: "{group}\TxLib source code"; Filename: "{app}\TxLib source code\"

[Run]
Filename: {win}\Microsoft.NET\Framework\v4.0.30319\ngen.exe; Parameters: "install ""{app}\TxEditor.exe"""; StatusMsg: "{cm:NgenMessage}"; Flags: runhidden
Filename: {app}\TxEditor.exe; WorkingDir: {app}; Flags: nowait postinstall skipifsilent

[UninstallRun]
Filename: {win}\Microsoft.NET\Framework\v4.0.30319\ngen.exe; Parameters: "uninstall ""{app}\TxEditor.exe"""; Flags: runhidden

[Code]
function InitializeSetup: Boolean;
begin
	Result := InitCheckDowngrade;

	if Result then
	begin
		// Initialise 3rd-party requirements management
		initwinversion();

		msi31('3.1');

		// If no .NET 4.0 is found, install the client profile (smallest)
		if (not netfxinstalled(NetFx40Client, '') and not netfxinstalled(NetFx40Full, '') and not netfxinstalled(NetFx4x, '')) then
			dotnetfx40client();
	end;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
	// Make upgrade install quicker
	Result := ((PageID = wpSelectTasks) or ((PageID = wpReady) and (GetArrayLength(products) = 0))) and PrevInstallExists;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
	if CurPageID = wpWelcome then
	begin
		if PrevInstallExists then
		begin
			// Change "Next" button to "Upgrade" on the first page, because it won't ask any more
			WizardForm.NextButton.Caption := ExpandConstant('{cm:Upgrade}');
			WizardForm.FinishedHeadingLabel.Caption := ExpandConstant('{cm:UpdatedHeadingLabel}');
		end;
	end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
	if (CurStep = ssPostInstall) and IsTaskSelected('VSTool') then
	begin
		// Register application as external tool in all Visual Studio versions after setup
		RegisterVSTool('10.0', 'TxEditor', '{app}\TxEditor.exe', '/scan:"$(SolutionDir) "');   // Trailing space to prevent \" in the cmdline
		RegisterVSTool('11.0', 'TxEditor', '{app}\TxEditor.exe', '/scan:"$(SolutionDir) "');
		RegisterVSTool('12.0', 'TxEditor', '{app}\TxEditor.exe', '/scan:"$(SolutionDir) "');
		RegisterVSTool('14.0', 'TxEditor', '{app}\TxEditor.exe', '/scan:"$(SolutionDir) "');
	end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
	if CurUninstallStep = usUninstall then
	begin
		if IsCommandLineParamSet('verysilent') or
			IsCommandLineParamSet('suppressmsgboxes') or
			(MsgBox(ExpandConstant('{cm:Uninstall_DeleteConfig}'), mbConfirmation, MB_YESNO) = IDYES) then
		begin
			DeleteFile(ExpandConstant('{userappdata}\Unclassified\TxTranslation\TxEditor.conf'));
			DeleteFile(ExpandConstant('{userappdata}\Unclassified\TxTranslation\TxEditor.conf.bak'));
			RemoveDir(ExpandConstant('{userappdata}\Unclassified\TxTranslation'));
			RemoveDir(ExpandConstant('{userappdata}\Unclassified'));
			
			DelTree(ExpandConstant('{app}\log'), true, true, true);
			RemoveDir(ExpandConstant('{app}'));
		end;
	end;
	if CurUninstallStep = usPostUninstall then
	begin
		// Unregister application as external tool in all Visual Studio versions after uninstall
		UnregisterVSTool('10.0', 'TxEditor');
		UnregisterVSTool('11.0', 'TxEditor');
		UnregisterVSTool('12.0', 'TxEditor');
		UnregisterVSTool('14.0', 'TxEditor');
	end;
end;
