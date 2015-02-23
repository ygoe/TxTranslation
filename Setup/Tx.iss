; Determine product and file version from the application to be installed
#define RevFileName '..\TxEditor\bin\Release\TxEditor.exe'
#define RevId GetStringFileInfo(RevFileName, 'ProductVersion')
#define TruncRevId GetFileVersion(RevFileName)

; Include 3rd-party software check and download support
#include "include\products.iss"
#include "include\products\stringversion.iss"
#include "include\products\winversion.iss"
#include "include\products\fileversion.iss"
#include "include\products\dotnetfxversion.iss"

; Include modules for required products
#include "include\products\msi31.iss"
#include "include\products\dotnetfx40client.iss"
#include "include\products\dotnetfx40full.iss"
#include "include\products\dotnetfx45.iss"

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
VersionInfoVersion={#TruncRevId}
VersionInfoCompany=Yves Goergen

; General application information
AppId={{99B66B72-FF8D-4169-ADE6-062A9EF0EB13}
AppMutex=Global\Unclassified.TxEditor,Unclassified.TxEditor
MinVersion=0,5.01sp3

; General setup information
DefaultDirName={pf}\Unclassified\TxTranslation
AllowUNCPath=False
DefaultGroupName=TxTranslation
DisableDirPage=auto
DisableProgramGroupPage=auto
ShowLanguageDialog=no
ChangesAssociations=yes

; Setup design
; Large image max. 164x314 pixels, small image max. 55x58 pixels
WizardImageBackColor=$ffffff
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
Task_VSTool=Register as External Tool in Visual Studio (2010/2012/2013)
Task_DeleteConfig=Delete existing configuration
NgenMessage=Optimising application performance (this may take a moment)

; Add translations after messages have been defined
#include "Tx.de.iss"

[Tasks]
Name: VSTool; Description: "{cm:Task_VSTool}"
Name: DeleteConfig; Description: "{cm:Task_DeleteConfig}"; Flags: unchecked
#define Task_DeleteConfig_Index 1

[Files]
; TxEditor application files
Source: "..\TxEditor\bin\Release\TxEditor.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\TxEditor\bin\Release\MultiSelectTreeView.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\TxEditor\bin\Release\TaskDialog.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\TxEditor\bin\Release\Unclassified.FieldLog.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\Tx Documentation.pdf"; DestDir: "{app}"; Flags: ignoreversion
; This is the signed version of the DLL:
Source: "..\TxLib\bin\Release\Unclassified.TxLib.dll"; DestDir: "{app}"; Flags: ignoreversion

; TxLib assembly
Source: "..\TxLib\bin\Release\Unclassified.TxLib.dll"; DestDir: "{app}\TxLib assembly"; Flags: ignoreversion
Source: "..\TxLib\bin\Release\Unclassified.TxLib.xml"; DestDir: "{app}\TxLib assembly"; Flags: ignoreversion

; TxLib source code
Source: "..\TxLib\DateTimeInterval.cs"; DestDir: "{app}\TxLib source code"; Flags: ignoreversion
Source: "..\TxLib\Tx.cs"; DestDir: "{app}\TxLib source code"; Flags: ignoreversion
Source: "..\TxLib\TxWinForms.cs"; DestDir: "{app}\TxLib source code"; Flags: ignoreversion
Source: "..\TxLib\TxXaml.cs"; DestDir: "{app}\TxLib source code"; Flags: ignoreversion

; License files
Source: "..\LICENSE-GPL"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\LICENSE-LGPL"; DestDir: "{app}"; Flags: ignoreversion

[Dirs]
; Create user-writable log directory in the installation directory.
; FieldLog will first try to write log file there.
Name: "{app}\log"; Permissions: users-modify

[InstallDelete]
; Delete user configuration files if the task is selected
Type: files; Name: "{userappdata}\Unclassified\TxTranslation\TxEditor.conf"; Tasks: DeleteConfig

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
Filename: {win}\Microsoft.NET\Framework\v4.0.30319\ngen.exe; Parameters: uninstall {app}\TxEditor.exe; Flags: runhidden

[UninstallDelete]
; Delete user configuration files if not uninstalling for a downgrade
Type: files; Name: "{userappdata}\Unclassified\TxTranslation\TxEditor.conf"; Check: not IsDowngradeUninstall
Type: dirifempty; Name: "{userappdata}\Unclassified\TxTranslation"
Type: dirifempty; Name: "{userappdata}\Unclassified"

; Delete log files if not uninstalling for a downgrade
Type: files; Name: "{app}\log\TxEditor-*.fl"; Check: not IsDowngradeUninstall
Type: files; Name: "{app}\log\!README.txt"; Check: not IsDowngradeUninstall
; TODO: Is the following required? http://stackoverflow.com/q/28383251/143684
Type: dirifempty; Name: "{app}\log"
Type: dirifempty; Name: "{app}"

[Code]
function InitializeSetup: Boolean;
var
	cmp: Integer;
begin
	Result := InitCheckDowngrade;

	if Result then
	begin
		// Initialise 3rd-party requirements management
		initwinversion();

		msi31('3.1');

		// If no .NET 4.0 is found, install the client profile (smallest)
		if (not netfxinstalled(NetFx40Client, '') and not netfxinstalled(NetFx40Full, '')) then
			dotnetfx40client();
	end;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
	// Make upgrade install quicker
	Result := ((PageID = wpSelectTasks) or (PageID = wpReady)) and PrevInstallExists;
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

	if CurPageID = wpSelectTasks then
	begin
		if IsDowngradeSetup then
		begin
			// Pre-select task to delete existing configuration on downgrading (user can deselect it again)
			// (Use the zero-based index of all rows in the tasks list GUI)
			// Source: http://stackoverflow.com/a/10490352/143684
			WizardForm.TasksList.Checked[{#Task_DeleteConfig_Index}] := true;
		end
		else
		begin
			// Clear possibly remembered value from previous downgrade install
			WizardForm.TasksList.Checked[{#Task_DeleteConfig_Index}] := false;
		end;
	end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
	if (CurStep = ssPostInstall) and IsTaskSelected('VSTool') then
	begin
		// Register FieldLogViewer as external tool in all Visual Studio versions after setup
		RegisterVSTool('10.0', 'TxEditor', '{app}\TxEditor.exe', '/scan:"$(SolutionDir) "');   // Trailing space to prevent \" in the cmdline
		RegisterVSTool('11.0', 'TxEditor', '{app}\TxEditor.exe', '/scan:"$(SolutionDir) "');
		RegisterVSTool('12.0', 'TxEditor', '{app}\TxEditor.exe', '/scan:"$(SolutionDir) "');
		RegisterVSTool('14.0', 'TxEditor', '{app}\TxEditor.exe', '/scan:"$(SolutionDir) "');
	end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
	if CurUninstallStep = usPostUninstall then
	begin
		// Unregister FieldLogViewer as external tool in all Visual Studio versions after uninstall
		UnregisterVSTool('10.0', 'TxEditor');
		UnregisterVSTool('11.0', 'TxEditor');
		UnregisterVSTool('12.0', 'TxEditor');
		UnregisterVSTool('14.0', 'TxEditor');
	end;
end;
