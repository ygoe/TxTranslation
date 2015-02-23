[Code]
procedure RegRenameStringValue(const rootKey: Integer; const subKeyName, valueName, newValueName: String);
var
	value: String;
begin
	if RegQueryStringValue(rootKey, subKeyName, valueName, value) then
	begin
		RegWriteStringValue(rootKey, subKeyName, newValueName, value);
		RegDeleteValue(rootKey, subKeyName, valueName);
	end;
end;

procedure RegRenameDWordValue(const rootKey: Integer; const subKeyName, valueName, newValueName: String);
var
	value: Cardinal;
begin
	if RegQueryDWordValue(rootKey, subKeyName, valueName, value) then
	begin
		RegWriteDWordValue(rootKey, subKeyName, newValueName, value);
		RegDeleteValue(rootKey, subKeyName, valueName);
	end;
end;

procedure UnregisterVSTool(const vsVersion, toolName: String);
var
	regKey: String;
	toolNumKeys: Cardinal;
	i, j: Cardinal;
	str, str2: String;
begin
	regKey := 'Software\Microsoft\VisualStudio\' + vsVersion + '\External Tools';

	if RegQueryDWordValue(HKEY_CURRENT_USER, regKey, 'ToolNumKeys', toolNumKeys) then
	begin
		// Visual Studio is installed
		for i := 0 to toolNumKeys - 1 do
		begin
			if RegQueryStringValue(HKEY_CURRENT_USER, regKey, 'ToolTitle' + IntToStr(i), str) then
			begin
				if str = toolName then
				begin
					// Found tool at index i. Remove it and move all others one position up.
					str := IntToStr(i);
					RegDeleteValue(HKEY_CURRENT_USER, regKey, 'ToolArg' + str);
					RegDeleteValue(HKEY_CURRENT_USER, regKey, 'ToolCmd' + str);
					RegDeleteValue(HKEY_CURRENT_USER, regKey, 'ToolDir' + str);
					RegDeleteValue(HKEY_CURRENT_USER, regKey, 'ToolOpt' + str);
					RegDeleteValue(HKEY_CURRENT_USER, regKey, 'ToolSourceKey' + str);
					RegDeleteValue(HKEY_CURRENT_USER, regKey, 'ToolTitle' + str);

					for j := i + 1 to ToolNumKeys - 1 do
					begin
						str := IntToStr(j);
						str2 := IntToStr(j - 1);
						RegRenameStringValue(HKEY_CURRENT_USER, regKey, 'ToolArg' + str, 'ToolArg' + str2);
						RegRenameStringValue(HKEY_CURRENT_USER, regKey, 'ToolCmd' + str, 'ToolCmd' + str2);
						RegRenameStringValue(HKEY_CURRENT_USER, regKey, 'ToolDir' + str, 'ToolDir' + str2);
						RegRenameDWordValue(HKEY_CURRENT_USER, regKey, 'ToolOpt' + str, 'ToolOpt' + str2);
						RegRenameStringValue(HKEY_CURRENT_USER, regKey, 'ToolSourceKey' + str, 'ToolSourceKey' + str2);
						RegRenameStringValue(HKEY_CURRENT_USER, regKey, 'ToolTitle' + str, 'ToolTitle' + str2);
						RegRenameStringValue(HKEY_CURRENT_USER, regKey, 'ToolTitlePkg' + str, 'ToolTitlePkg' + str2);
						RegRenameDWordValue(HKEY_CURRENT_USER, regKey, 'ToolTitleResID' + str, 'ToolTitleResID' + str2);
					end;
					RegWriteDWordValue(HKEY_CURRENT_USER, regKey, 'ToolNumKeys', toolNumKeys - 1);
				end;
			end;
		end;
	end;
end;

procedure RegisterVSTool(const vsVersion, toolName, toolCommand, toolArgs: String);
var
	regKey: String;
	toolNumKeys: Cardinal;
	str: String;
begin
	regKey := 'Software\Microsoft\VisualStudio\' + vsVersion + '\External Tools';

	// Clean up existing entry before adding it
	UnregisterVSTool(vsVersion, toolName);

	if RegQueryDWordValue(HKEY_CURRENT_USER, regKey, 'ToolNumKeys', toolNumKeys) then
	begin
		// Visual Studio is installed. Append the tool at the end of the list.
		str := IntToStr(toolNumKeys);
		RegWriteStringValue(HKEY_CURRENT_USER, regKey, 'ToolArg' + str, toolArgs);
		RegWriteStringValue(HKEY_CURRENT_USER, regKey, 'ToolCmd' + str, ExpandConstant(toolCommand));
		RegWriteStringValue(HKEY_CURRENT_USER, regKey, 'ToolDir' + str, '');
		RegWriteDWordValue(HKEY_CURRENT_USER, regKey, 'ToolOpt' + str, 17);
		RegWriteStringValue(HKEY_CURRENT_USER, regKey, 'ToolSourceKey' + str, '');
		RegWriteStringValue(HKEY_CURRENT_USER, regKey, 'ToolTitle' + str, toolName);
		RegWriteDWordValue(HKEY_CURRENT_USER, regKey, 'ToolNumKeys', toolNumKeys + 1);
	end;
end;

[Setup]
