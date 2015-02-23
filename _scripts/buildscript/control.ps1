# PowerShell build framework
# Project-specific control file

Begin-BuildScript "TxTranslation"

# Find revision format from the source code, require Git checkout
Set-VcsVersion "" "/require git"

# Debug builds
if (IsSelected "build-debug")
{
	Build-Solution "TxTranslation.sln" "Debug" "Mixed Platforms" 6

	if (IsSelected "sign-lib")
	{
		Sign-File "TxLib\bin\Debug\Unclassified.TxLib.dll" "$signKeyFile" "$signPassword" 1
	}
	if (IsSelected "sign-app")
	{
		Sign-File "TxEditor\bin\Debug\TxEditor.exe" "$signKeyFile" "$signPassword" 1
	}
}

# Release builds
if ((IsSelected "build-release") -or (IsSelected "commit") -or (IsSelected "publish"))
{
	Build-Solution "TxTranslation.sln" "Release" "Mixed Platforms" 6
	
	# Archive debug symbols for later source lookup
	EnsureDirExists ".local"
	Exec-Console "_scripts\bin\PdbConvert.exe" "$rootDir\TxEditor\bin\Release\* /srcbase $rootDir /optimize /outfile $rootDir\.local\TxTranslation-$revId.pdbx" 1

	if ((IsSelected "sign-lib") -or (IsSelected "publish"))
	{
		Sign-File "TxLib\bin\Release\Unclassified.TxLib.dll" "$signKeyFile" "$signPassword" 1
	}
	if ((IsSelected "sign-app") -or (IsSelected "publish"))
	{
		Sign-File "TxEditor\bin\Release\TxEditor.exe" "$signKeyFile" "$signPassword" 1
	}

	Create-NuGet "TxLib\Unclassified.TxLib.nuspec" "TxLib\bin" 2
}

# Release setups
if ((IsSelected "setup-release") -or (IsSelected "commit") -or (IsSelected "publish"))
{
	Create-Setup "Setup\Tx.iss" Release 1

	if ((IsSelected "sign-setup") -or (IsSelected "publish"))
	{
		Sign-File "Setup\bin\TxSetup-$revId.exe" "$signKeyFile" "$signPassword" 1
	}
}

# Install release setup
if (IsSelected "install")
{
	Exec-File "Setup\bin\TxSetup-$revId.exe" "/norestart /verysilent" 1
}

# Commit to repository
if (IsSelected "commit")
{
	# Clean up test build files
	Delete-File "Setup\bin\TxSetup-$revId.exe" 0
	Delete-File ".local\TxTranslation-$revId.pdbx" 0

	Git-Commit 5
}

# Prepare publishing a release
if (IsSelected "publish")
{
	Git-Log ".local\TxChanges.txt" 1
}

# Copy to website (local)
if (IsSelected "transfer-web")
{
	Copy-File "Setup\bin\TxSetup-$revId.exe" "$webDir\files\source\txtranslation\" 0
	Copy-File ".local\TxChanges.txt" "$webDir\files\source\txtranslation\" 0
}

# Upload to NuGet
if (IsSelected "transfer-nuget")
{
	Push-NuGet "TxLib\bin\Unclassified.TxLib" $nuGetApiKey 45
}

End-BuildScript
