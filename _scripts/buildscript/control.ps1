# PowerShell build framework
# Project-specific control file

Begin-BuildScript "TxTranslation"
Set-GitVersion "{bmin:2013:4}.{commit:6}{!:+}"

# Debug builds
if (IsSelected "build-debug")
{
	Build-Solution "TxTranslation.sln" "Debug" "Mixed Platforms" 6

	if (IsSelected "sign-lib")
	{
		. "$sourcePath\.local\sign_config.ps1"
		Sign-File "TxLib\bin\Debug\Unclassified.TxLib.dll" "$signKeyFile" "$signPassword" 1
	}
	if (IsSelected "sign-app")
	{
		. "$sourcePath\.local\sign_config.ps1"
		Sign-File "TxEditor\bin\Debug\TxEditor.exe" "$signKeyFile" "$signPassword" 1
	}
}

# Release builds
if ((IsSelected "build-release") -or (IsSelected "commit"))
{
	Build-Solution "TxTranslation.sln" "Release" "Mixed Platforms" 6
	
	# Archive debug symbols for later source lookup
	EnsureDirExists ".local"
	Exec-Console "_scripts\bin\PdbConvert.exe" "$sourcePath\TxEditor\bin\Release\* /srcbase $sourcePath /optimize /outfile $sourcePath\.local\TxTranslation-$revId.pdbx" 1

	if (IsSelected "sign-lib")
	{
		. "$sourcePath\.local\sign_config.ps1"
		Sign-File "TxLib\bin\Release\Unclassified.TxLib.dll" "$signKeyFile" "$signPassword" 1
	}
	if (IsSelected "sign-app")
	{
		. "$sourcePath\.local\sign_config.ps1"
		Sign-File "TxEditor\bin\Release\TxEditor.exe" "$signKeyFile" "$signPassword" 1
	}
}

# Release setups
if ((IsSelected "setup-release") -or (IsSelected "commit"))
{
	Create-Setup "Setup\Tx.iss" Release 1

	if (IsSelected "sign-setup")
	{
		. "$sourcePath\.local\sign_config.ps1"
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

	Git-Commit 1
}

End-BuildScript
