# PowerShell build framework
# Project-specific control file

Begin-BuildScript "TxTranslation"

# Find revision format from the source code, require Git checkout
Set-VcsVersion "" "/require git"

# Release builds
if (IsAnySelected build commit publish)
{
	Restore-NuGetPackages "TxTranslation.sln"
	Build-Solution "TxTranslation.sln" "Release" "Mixed Platforms" 6
	Pdb-Convert "TxEditor\bin\Release\*" "TxEditor\bin\Release\TxTranslation.pdbx" "/optimize"
	Create-NuGetPackage "TxLib\Unclassified.TxLib.nuspec" "TxLib\bin"
	Create-Setup "Setup\Tx.iss" Release

	if (IsSelected sign)
	{
		Sign-File "TxLib\bin\Release\Unclassified.TxLib.dll" "$signKeyFile" "$signPassword"
		Sign-File "TxEditor\bin\Release\TxEditor.exe" "$signKeyFile" "$signPassword"
		Sign-File "Setup\bin\TxSetup-$revId.exe" "$signKeyFile" "$signPassword"
	}
}

# Install setup
if (IsSelected install)
{
	Exec-File "Setup\bin\TxSetup-$revId.exe" "/norestart /verysilent"
}

# Commit to repository
if (IsSelected commit)
{
	# Clean up test build files
	Delete-File "Setup\bin\TxSetup-$revId.exe"

	Git-Commit
}

# Prepare publishing a release
if (IsSelected publish)
{
	# Copy all necessary files into their own release directory
	EnsureDirExists ".local\Release"
	Copy-File "TxEditor\bin\Release\TxTranslation.pdbx" ".local\Release\TxTranslation-$revId.pdbx"

	Git-Log ".local\Release\TxChanges.txt"
}

# Copy to website (local)
if (IsSelected transfer-web)
{
	Copy-File "Setup\bin\TxSetup-$revId.exe" "$webDir\files\source\txtranslation\"
	Copy-File ".local\Release\TxChanges.txt" "$webDir\files\source\txtranslation\"
	
	$today = (Get-Date -Format "yyyy-MM-dd")
	Exec-File "_scripts\bin\AutoReplace.exe" "$webDataFile txtranslation version=$revId date=$today"
}

# Upload to NuGet
if (IsSelected transfer-nuget)
{
	Push-NuGetPackage "TxLib\bin\Unclassified.TxLib" $nuGetApiKey 11
}

End-BuildScript
