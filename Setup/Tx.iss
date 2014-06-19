#ifndef RevId
	#define RevId "0.1"
#endif
#ifndef ShortRevId
	#define ShortRevId Copy(RevId, 1, Pos(".", RevId) - 1)
#endif

#include "scripts\products.iss"
#include "scripts\products\stringversion.iss"
#include "scripts\products\winversion.iss"
#include "scripts\products\fileversion.iss"
#include "scripts\products\dotnetfxversion.iss"

#include "scripts\products\msi31.iss"

#include "scripts\products\dotnetfx40client.iss"
#include "scripts\products\dotnetfx40full.iss"

[Setup]
AppCopyright=© Yves Goergen, GNU GPL v3
AppPublisher=Yves Goergen
AppPublisherURL=http://dev.unclassified.de/source/txlib
AppName=TxEditor
AppVersion={#ShortRevId}
AppMutex=Unclassified.TxEditor
AppId={{99B66B72-FF8D-4169-ADE6-062A9EF0EB13}
MinVersion=0,5.01sp3

ShowLanguageDialog=no
ChangesAssociations=yes

DefaultDirName={pf}\Unclassified\TxTranslation
AllowUNCPath=False
DefaultGroupName=TxTranslation
DisableDirPage=auto
DisableProgramGroupPage=auto

; Large image max. 164x314 pixels, small image max. 55x58 pixels
WizardImageFile=TxFlag.bmp
WizardImageBackColor=$ffffff
WizardImageStretch=no
WizardSmallImageFile=TxFlagSmall.bmp

UninstallDisplayName=TxTranslation
UninstallDisplayIcon={app}\TxEditor.exe

OutputDir=bin
OutputBaseFilename=TxSetup-{#RevId}
SolidCompression=True
InternalCompressLevel=max
VersionInfoVersion=1.0
VersionInfoCompany=Yves Goergen
VersionInfoDescription=TxTranslation Setup

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"

[LangOptions]
;de.LanguageName=Deutsch
;de.LanguageID=$0407
DialogFontName=Segoe UI
DialogFontSize=9
WelcomeFontName=Segoe UI
WelcomeFontSize=12

[Messages]
WelcomeLabel1=%n%n%n%nWelcome to the Tx setup wizard
WelcomeLabel2=TxTranslation is a simple yet powerful translation and localisation library for .NET applications. It supports XAML binding, language fallbacks, count-specific translations, placeholders, and number and time formatting.%n%nVersion: {#RevId}
ClickNext=Click Next to continue installing TxEditor, documentation, the TxLib library and source code, or Cancel to exit the setup.
FinishedHeadingLabel=%n%n%n%nTxTranslation is now installed.
FinishedLabelNoIcons=
FinishedLabel=The application may be launched by selecting the installed start menu icon.
ClickFinish=Click Finish to exit the setup.

de.WelcomeLabel1=%n%n%n%nWillkommen zum Tx-Setup-Assistenten
de.WelcomeLabel2=TxTranslation ist eine einfache aber mächtige Bibliothek für Übersetzungen und Lokalisierung in .NET-Anwendungen. Sie unterstützt XAML-Binding, Ersatzsprachen, Anzahl-abhängige Übersetzungen, Platzhalter und Zeitformatierung.%n%nVersion: {#RevId}
de.ClickNext=Klicken Sie auf Weiter, um den TxEditor, die Dokumentation und die TxLib-Bibliothek mit Quelltext zu installieren, oder auf Abbrechen zum Beenden des Setups.
de.FinishedHeadingLabel=%n%n%n%nTxTranslation ist jetzt installiert.
de.FinishedLabelNoIcons=
de.FinishedLabel=Die Anwendung kann über die installierte Startmenü-Verknüpfung gestartet werden.
de.ClickFinish=Klicken Sie auf Fertigstellen, um das Setup zu beenden.

[CustomMessages]
Upgrade=&Upgrade
UpdatedHeadingLabel=%n%n%n%nTxTranslation was upgraded.
Task_VSTool=Register as External Tool in Visual Studio (2010/2012/2013)
NgenMessage=Optimising application performance (this may take a moment)
DowngradeUninstall=You are trying to install an older version than is currently installed on the system. The newer version must first be uninstalled. Would you like to do that now?

de.Upgrade=&Aktualisieren
de.UpdatedHeadingLabel=%n%n%n%nTxTranslation wurde aktualisiert.
de.Task_VSTool=In Visual Studio (2010/2012/2013) als Externes Tool eintragen
de.NgenMessage=Anwendungs-Performance optimieren (kann einen Moment dauern)
de.DowngradeUninstall=Sie versuchen, eine ältere Version zu installieren, als bereits im System installiert ist. Die neuere Version muss zuerst deinstalliert werden. Möchten Sie das jetzt tun?

[Tasks]
Name: VSTool; Description: "{cm:Task_VSTool}"

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
Type: files; Name: "{userappdata}\Unclassified\TxTranslation\TxEditor.conf"
Type: dirifempty; Name: "{userappdata}\Unclassified\TxTranslation"
Type: dirifempty; Name: "{userappdata}\Unclassified"

; Delete log files
Type: files; Name: "{app}\log\TxEditor-*.fl"
Type: files; Name: "{app}\log\!README.txt"

[Code]
function IsUpgrade: Boolean;
var
	Value: string;
	UninstallKey: string;
begin
	UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\' +
		ExpandConstant('{#SetupSetting("AppId")}') + '_is1';
	Result := (RegQueryStringValue(HKLM, UninstallKey, 'UninstallString', Value) or
		RegQueryStringValue(HKCU, UninstallKey, 'UninstallString', Value)) and (Value <> '');
end;

function GetQuietUninstallString: String;
var
	Value: string;
	UninstallKey: string;
begin
	UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\' +
		ExpandConstant('{#SetupSetting("AppId")}') + '_is1';
	if not RegQueryStringValue(HKLM, UninstallKey, 'QuietUninstallString', Value) then
		RegQueryStringValue(HKCU, UninstallKey, 'QuietUninstallString', Value);
	Result := Value;
end;

function GetInstalledVersion: String;
var
	Value: string;
	UninstallKey: string;
begin
	UninstallKey := 'Software\Microsoft\Windows\CurrentVersion\Uninstall\' +
		ExpandConstant('{#SetupSetting("AppId")}') + '_is1';
	if not RegQueryStringValue(HKLM, UninstallKey, 'DisplayVersion', Value) then
		RegQueryStringValue(HKCU, UninstallKey, 'DisplayVersion', Value);
	Result := Value;
end;

function InitializeSetup(): boolean;
var
	ResultCode: Integer;
begin
	Result := true;

	// Check for downgrade
	if IsUpgrade then
	begin
		if '{#ShortRevId}' < GetInstalledVersion then
		begin
			if MsgBox(ExpandConstant('{cm:DowngradeUninstall}'), mbConfirmation, MB_YESNO) = IDYES then
			begin
				Exec('>', GetQuietUninstallString, '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
			end;

			// Check again
			if '{#ShortRevId}' < GetInstalledVersion then
			begin
				Result := false;
			end;
		end;
	end;
	
	if Result then
	begin
		//init windows version
		initwinversion();

		msi31('3.1');

		// if no .netfx 4.0 is found, install the client (smallest)
		if (not netfxinstalled(NetFx40Client, '') and not netfxinstalled(NetFx40Full, '')) then
			dotnetfx40client();
	end;
end;

procedure RegRenameStringValue(const RootKey: Integer; const SubKeyName, ValueName, NewValueName: String);
var
	value: String;
begin
	if RegQueryStringValue(RootKey, SubKeyName, ValueName, value) then
	begin
		RegWriteStringValue(RootKey, SubKeyName, NewValueName, value);
		RegDeleteValue(RootKey, SubKeyName, ValueName);
	end;
end;

procedure RegRenameDWordValue(const RootKey: Integer; const SubKeyName, ValueName, NewValueName: String);
var
	value: Cardinal;
begin
	if RegQueryDWordValue(RootKey, SubKeyName, ValueName, value) then
	begin
		RegWriteDWordValue(RootKey, SubKeyName, NewValueName, value);
		RegDeleteValue(RootKey, SubKeyName, ValueName);
	end;
end;

procedure UnregisterVSTool(vsVersion: String);
var
	regKey: String;
	ToolNumKeys: Cardinal;
	i, j: Cardinal;
	num: Cardinal;
	str: String;
begin
	regKey := 'Software\Microsoft\VisualStudio\' + vsVersion + '\External Tools';

	if RegQueryDWordValue(HKEY_CURRENT_USER, regKey, 'ToolNumKeys', ToolNumKeys) then
	begin
		// Visual Studio is installed
		for i := 0 to ToolNumKeys - 1 do
		begin
			if RegQueryStringValue(HKEY_CURRENT_USER, regKey, 'ToolTitle' + IntToStr(i), str) then
			begin
				if str = 'TxEditor' then
				begin
					// Found TxEditor at index i. Remove it and move all others one position up.
					RegDeleteValue(HKEY_CURRENT_USER, regKey, 'ToolArg' + IntToStr(i));
					RegDeleteValue(HKEY_CURRENT_USER, regKey, 'ToolCmd' + IntToStr(i));
					RegDeleteValue(HKEY_CURRENT_USER, regKey, 'ToolDir' + IntToStr(i));
					RegDeleteValue(HKEY_CURRENT_USER, regKey, 'ToolOpt' + IntToStr(i));
					RegDeleteValue(HKEY_CURRENT_USER, regKey, 'ToolSourceKey' + IntToStr(i));
					RegDeleteValue(HKEY_CURRENT_USER, regKey, 'ToolTitle' + IntToStr(i));
					
					for j := i + 1 to ToolNumKeys - 1 do
					begin
						RegRenameStringValue(HKEY_CURRENT_USER, regKey, 'ToolArg' + IntToStr(j), 'ToolArg' + IntToStr(j - 1));
						RegRenameStringValue(HKEY_CURRENT_USER, regKey, 'ToolCmd' + IntToStr(j), 'ToolCmd' + IntToStr(j - 1));
						RegRenameStringValue(HKEY_CURRENT_USER, regKey, 'ToolDir' + IntToStr(j), 'ToolDir' + IntToStr(j - 1));
						RegRenameDWordValue(HKEY_CURRENT_USER, regKey, 'ToolOpt' + IntToStr(j), 'ToolOpt' + IntToStr(j - 1));
						RegRenameStringValue(HKEY_CURRENT_USER, regKey, 'ToolSourceKey' + IntToStr(j), 'ToolSourceKey' + IntToStr(j - 1));
						RegRenameStringValue(HKEY_CURRENT_USER, regKey, 'ToolTitle' + IntToStr(j), 'ToolTitle' + IntToStr(j - 1));
						RegRenameStringValue(HKEY_CURRENT_USER, regKey, 'ToolTitlePkg' + IntToStr(j), 'ToolTitlePkg' + IntToStr(j - 1));
						RegRenameDWordValue(HKEY_CURRENT_USER, regKey, 'ToolTitleResID' + IntToStr(j), 'ToolTitleResID' + IntToStr(j - 1));
					end;
					RegWriteDWordValue(HKEY_CURRENT_USER, regKey, 'ToolNumKeys', ToolNumKeys - 1);
				end;
			end;
		end;
	end;
end;

procedure RegisterVSTool(vsVersion: String);
var
	regKey: String;
	ToolNumKeys: Cardinal;
begin
	regKey := 'Software\Microsoft\VisualStudio\' + vsVersion + '\External Tools';

	// Clean up existing entry before adding it
	UnregisterVSTool(vsVersion);
	
	if RegQueryDWordValue(HKEY_CURRENT_USER, regKey, 'ToolNumKeys', ToolNumKeys) then
	begin
		// Visual Studio is installed
		RegWriteStringValue(HKEY_CURRENT_USER, regKey, 'ToolArg' + IntToStr(ToolNumKeys), '/scan:"$(SolutionDir) "');   // Trailing space to prevent \" in the cmdline
		RegWriteStringValue(HKEY_CURRENT_USER, regKey, 'ToolCmd' + IntToStr(ToolNumKeys), ExpandConstant('{app}') + '\TxEditor.exe');
		RegWriteStringValue(HKEY_CURRENT_USER, regKey, 'ToolDir' + IntToStr(ToolNumKeys), '');
		RegWriteDWordValue(HKEY_CURRENT_USER, regKey, 'ToolOpt' + IntToStr(ToolNumKeys), 17);
		RegWriteStringValue(HKEY_CURRENT_USER, regKey, 'ToolSourceKey' + IntToStr(ToolNumKeys), '');
		RegWriteStringValue(HKEY_CURRENT_USER, regKey, 'ToolTitle' + IntToStr(ToolNumKeys), 'TxEditor');
		RegWriteDWordValue(HKEY_CURRENT_USER, regKey, 'ToolNumKeys', ToolNumKeys + 1);
	end;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
	if (CurStep = ssPostInstall) and IsTaskSelected('VSTool') then
	begin
		RegisterVSTool('10.0');
		RegisterVSTool('11.0');
		RegisterVSTool('12.0');
	end;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
	if CurUninstallStep = usPostUninstall then
	begin
		UnregisterVSTool('10.0');
		UnregisterVSTool('11.0');
		UnregisterVSTool('12.0');
	end;
end;

function ShouldSkipPage(PageID: Integer): Boolean;
begin
	Result := ((PageID = wpSelectTasks) or (PageID = wpReady)) and IsUpgrade;
end;

procedure CurPageChanged(CurPageID: Integer);
begin
	if CurPageID = wpWelcome then
	begin
		if IsUpgrade then
		begin
			WizardForm.NextButton.Caption := ExpandConstant('{cm:Upgrade}');
			WizardForm.FinishedHeadingLabel.Caption := ExpandConstant('{cm:UpdatedHeadingLabel}');
		end;
	end;
end;
