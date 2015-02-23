[Code]
// ==================== STRING FUNCTIONS FROM C# ====================

// Equivalent to C# String.StartsWith
function StartsWith(source, subStr: String): Boolean;
begin
	Result := Pos(subStr, source) = 1;
end;

// Equivalent to C# String.Replace
function StringReplace(source, oldSubString, newSubString: String): String;
var
	sourceCopy : String;
begin
	sourceCopy := source;   // Prevent modification to the original string
	StringChange(sourceCopy, oldSubString, newSubString);
	Result := sourceCopy;
end;

// ==================== VERSION NUMBERS ====================

// Splits a dotted-numeric version into an array of number strings.
// Ignores any suffix other than digits or a dot.
function SplitVersionNumbers(str: String): TArrayOfString;
var
	arr: TArrayOfString;
	index: Integer;
	ch: String;
begin
	// Special handling of older {bmin} scheme: /^[0-3][^.]{3}$/ is treated as "0"
	if (Length(str) = 4) and (Pos('.', str) = 0) and
		((Copy(str, 1, 1) = '0') or (Copy(str, 1, 1) = '1') or (Copy(str, 1, 1) = '2') or (Copy(str, 1, 1) = '3')) then
	begin
		SetArrayLength(arr, 1);
		arr[0] := '0';
		Result := arr;
		Exit;
	end;

	// Start with a first array item
	SetArrayLength(arr, 1);
	for index := 1 to Length(str) do
	begin
		ch := Copy(str, index, 1);
		if (ch = '0') or (ch = '1') or (ch = '2') or (ch = '3') or (ch = '4') or
			(ch = '5') or (ch = '6') or (ch = '7') or (ch = '8') or (ch = '9') then
		begin
			// Append all digits to the last array item
			arr[GetArrayLength(arr) - 1] := arr[GetArrayLength(arr) - 1] + ch;
		end
		else if ch = '.' then
		begin
			// Increase the array size with a dot, make room for next number
			SetArrayLength(arr, GetArrayLength(arr) + 1);
		end
		else
		begin
			// Ignore other characters until the end
			Break;
		end;
	end;
	// Remove last part if it's empty
	if Length(arr[GetArrayLength(arr) - 1]) = 0 then
		SetArrayLength(arr, GetArrayLength(arr) - 1);
	Result := arr;
end;

// Compares two dotted-numeric version strings.
// Ignores any suffix other than digits or a dot.
function CompareVersionsEx(v1, v2: String): Integer;
var
	arr1, arr2: TArrayOfString;
	index, size, num1, num2: Integer;
begin
	Log('Comparing version ' + v1 + ' with ' + v2);
	// Parse the full strings into arrays of number strings
	arr1 := SplitVersionNumbers(v1);
	arr2 := SplitVersionNumbers(v2);
	// Find the longer of the two arrays
	size := GetArrayLength(arr1);
	if GetArrayLength(arr2) > size then
		size := GetArrayLength(arr2);
	// Compare each array item, treat missing as zero
	Result := 0;
	for index := 0 to size - 1 do
	begin
		num1 := 0;
		if index < GetArrayLength(arr1) then
			num1 := StrToInt(arr1[index]);
		num2 := 0;
		if index < GetArrayLength(arr2) then
			num2 := StrToInt(arr2[index]);
		// End on difference, continue otherwise
		Result := num1 - num2;
		if Result <> 0 then
			Exit;
	end;
	// No differences found, versions are equal (result is already 0)
end;

// Truncates a version string to its dotted-numeric prefix.
function TruncateVersion(version: String): String;
var
	arr: TArrayOfString;
	index: Integer;
begin
	arr := SplitVersionNumbers(version);
	for index := 0 to GetArrayLength(arr) - 1 do
	begin
		if index > 0 then
			Result := Result + '.';
		Result := Result + arr[index];
	end;
end;

// ==================== COMMAND LINE PARAMETERS ====================

// Based on: http://stackoverflow.com/a/19529712/143684
function GetCommandLineParam(inParamName: String): String;
var
	paramNameAndValue: String;
	i: Integer;
begin
	Result := '';

	if not StartsWith(inParamName, '/') then inParamName := '/' + inParamName;
	for i := 1 to ParamCount do
	begin
		paramNameAndValue := ParamStr(i);
		if StartsWith(paramNameAndValue, inParamName) then
		begin
			Result := StringReplace(paramNameAndValue, inParamName + '=', '');
			break;
		end;
	end;
end;

function IsCommandLineParamSet(inParamName: String): Boolean;
var
	paramNameAndValue: String;
	i: Integer;
begin
	Result := false;

	if not StartsWith(inParamName, '/') then inParamName := '/' + inParamName;
	for i := 1 to ParamCount do
	begin
		paramNameAndValue := ParamStr(i);
		if StartsWith(paramNameAndValue, inParamName + '=') or (paramNameAndValue = inParamName) then
		begin
			Result := true;
			break;
		end;
	end;
end;

[Setup]
