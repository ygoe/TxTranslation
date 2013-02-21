#ifndef RevId
	#define RevId "0.1"
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
AppCopyright=© Yves Goergen
AppPublisher=Yves Goergen
AppPublisherURL=http://dev.unclassified.de/
AppName=TxEditor
AppVersion={#RevId}
AppMutex=Unclassified.TxEditor
AppId={{99B66B72-FF8D-4169-ADE6-062A9EF0EB13}
MinVersion=0,5.01sp3

ShowLanguageDialog=no
ChangesAssociations=yes

DefaultDirName={pf}\Unclassified\TxTranslation
AllowUNCPath=False
DefaultGroupName=TxTranslation

UninstallDisplayName=TxEditor
UninstallDisplayIcon={app}\TxEditor.exe

OutputDir=.
OutputBaseFilename=TxEditor-Setup-{#RevId}
SolidCompression=True
InternalCompressLevel=max
VersionInfoVersion={#RevId}
VersionInfoCompany=Yves Goergen
VersionInfoDescription=TxEditor installation package

[Languages]
Name: "en"; MessagesFile: "compiler:Default.isl"
Name: "de"; MessagesFile: "compiler:Languages\German.isl"

;[LangOptions]
;de.LanguageName=Deutsch
;de.LanguageID=$0407
;DialogFontName=Tahoma
;WelcomeFontName=Tahoma

[Files]
Source: "..\TxEditor\bin\Release\TxEditor.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\TxEditor\bin\Release\MultiSelectTreeView.dll"; DestDir: "{app}"; Flags: ignoreversion
;Source: "..\...\TxEditor-Dokumentation.pdf"; DestDir: "{app}"; Flags: ignoreversion

[Registry]
Root: HKCR; Subkey: ".txd"; ValueType: string; ValueName: ""; ValueData: "TxDictionary"; Flags: uninsdeletevalue 
Root: HKCR; Subkey: "TxDictionary"; ValueType: string; ValueName: ""; ValueData: "Tx dictionary"; Flags: uninsdeletekey
Root: HKCR; Subkey: "TxDictionary\DefaultIcon"; ValueType: string; ValueName: ""; ValueData: "{app}\TxEditor.exe,1"
Root: HKCR; Subkey: "TxDictionary\shell\open\command"; ValueType: string; ValueName: ""; ValueData: """{app}\TxEditor.exe"" ""%1"""

[Icons]
Name: "{group}\TxEditor"; Filename: "{app}\TxEditor.exe"; IconFilename: "{app}\TxEditor.exe"
;Name: "{group}\TxEditor-Dokumentation"; Filename: "{app}\TxEditor-Dokumentation.pdf"

[CustomMessages]
en.NgenMessage=Optimising application performance
de.NgenMessage=Anwendungs-Performance optimieren

[Run]
Filename: {app}\TxEditor.exe; WorkingDir: {app}; Flags: nowait postinstall
Filename: {win}\Microsoft.NET\Framework\v4.0.30319\ngen.exe; Parameters: "install ""{app}\TxEditor.exe"""; StatusMsg: "{cm:NgenMessage}"; Flags: runhidden

[UninstallRun]
Filename: {win}\Microsoft.NET\Framework\v4.0.30319\ngen.exe; Parameters: uninstall {app}\TxEditor.exe; Flags: runhidden

[UninstallDelete]
Type: files; Name: "{userappdata}\Unclassified\TxTranslation\TxEditor.conf"
Type: dirifempty; Name: "{userappdata}\Unclassified\TxTranslation"
Type: dirifempty; Name: "{userappdata}\Unclassified"

[Code]
function InitializeSetup(): boolean;
begin
	//init windows version
	initwinversion();

	msi31('3.1');

	// if no .netfx 4.0 is found, install the client (smallest)
	if (not netfxinstalled(NetFx40Client, '') and not netfxinstalled(NetFx40Full, '')) then
		dotnetfx40client();

	Result := true;
end;

