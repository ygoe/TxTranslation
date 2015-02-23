; Requires the value for "AppId" to be set before including the file.
; Requires the setup version in {#RevId}.

[CustomMessages]
DowngradeUninstall=You are trying to install an older version than is currently installed on the system. The newer version must first be uninstalled. Would you like to do that now?

de.DowngradeUninstall=Sie versuchen, eine ältere Version zu installieren, als bereits im System installiert ist. Die neuere Version muss zuerst deinstalliert werden. Möchten Sie das jetzt tun?

[Code]
var
	IsDowngradeSetup: Boolean;

function PrevInstallExists: Boolean;
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

function InitCheckDowngrade: Boolean;
var
	ResultCode: Integer;
begin
	Result := true;
	// Check for downgrade
	if PrevInstallExists then
	begin
		if CompareVersionsEx('{#RevId}', GetInstalledVersion) < 0 then
		begin
			if MsgBox(ExpandConstant('{cm:DowngradeUninstall}'), mbConfirmation, MB_YESNO) = IDYES then
			begin
				Exec('>', GetQuietUninstallString + ' /downgrade', '', SW_SHOW, ewWaitUntilTerminated, ResultCode);
			end;

			// Check again
			if CompareVersionsEx('{#RevId}', GetInstalledVersion) < 0 then
			begin
				Result := false;
			end;

			IsDowngradeSetup := true;
		end;
	end;
end;

function IsDowngradeUninstall: Boolean;
begin
	Result := IsCommandLineParamSet('downgrade');
end;

[Setup]
