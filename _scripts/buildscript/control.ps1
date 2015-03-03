# PowerShell build framework
# Project-specific control file

Begin-BuildScript "TxTranslation"

# Find revision format from the source code, require Git checkout
Set-VcsVersion "" "/require git"

Restore-NuGetTool
Restore-NuGetPackages "TxTranslation.sln"

# Debug builds
if (IsSelected build-debug)
{
	Build-Solution "TxTranslation.sln" "Debug" "Mixed Platforms" 6

	if (IsSelected sign-lib)
	{
		Sign-File "TxLib\bin\Debug\Unclassified.TxLib.dll" "$signKeyFile" "$signPassword"
	}
	if (IsSelected sign-app)
	{
		Sign-File "TxEditor\bin\Debug\TxEditor.exe" "$signKeyFile" "$signPassword"
	}
}

# Release builds
if (IsAnySelected build-release commit publish)
{
	Build-Solution "TxTranslation.sln" "Release" "Mixed Platforms" 6
	
	# Archive debug symbols for later source lookup
	EnsureDirExists ".local"
	Exec-Console "_scripts\bin\PdbConvert.exe" "$rootDir\TxEditor\bin\Release\* /srcbase $rootDir /optimize /outfile $rootDir\.local\TxTranslation-$revId.pdbx"

	if (IsAnySelected sign-lib publish)
	{
		Sign-File "TxLib\bin\Release\Unclassified.TxLib.dll" "$signKeyFile" "$signPassword"
	}
	if (IsAnySelected sign-app publish)
	{
		Sign-File "TxEditor\bin\Release\TxEditor.exe" "$signKeyFile" "$signPassword"
	}

	Create-NuGetPackage "TxLib\Unclassified.TxLib.nuspec" "TxLib\bin"
}

# Release setups
if (IsAnySelected setup-release commit publish)
{
	Create-Setup "Setup\Tx.iss" "Release"

	if (IsAnySelected sign-setup publish)
	{
		Sign-File "Setup\bin\TxSetup-$revId.exe" "$signKeyFile" "$signPassword"
	}
}

# Install release setup
if (IsSelected install)
{
	Exec-File "Setup\bin\TxSetup-$revId.exe" "/norestart /verysilent"
}

# Commit to repository
if (IsSelected commit)
{
	# Clean up test build files
	Delete-File "Setup\bin\TxSetup-$revId.exe"
	Delete-File ".local\TxTranslation-$revId.pdbx"

	Git-Commit
}

# Prepare publishing a release
if (IsSelected publish)
{
	Git-Log ".local\TxChanges.txt"
}

# Copy to website (local)
if (IsSelected transfer-web)
{
	Copy-File "Setup\bin\TxSetup-$revId.exe" "$webDir\files\source\txtranslation\"
	Copy-File ".local\TxChanges.txt" "$webDir\files\source\txtranslation\"
}

# Upload to NuGet
if (IsSelected transfer-nuget)
{
	Push-NuGetPackage "TxLib\bin\Unclassified.TxLib" $nuGetApiKey 45
}

End-BuildScript
