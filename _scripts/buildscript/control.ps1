param($config, $batchMode = "")
$scriptDir = ($MyInvocation.MyCommand.Definition | Split-Path -parent)
$sourcePath = $scriptDir | Split-Path -parent | Split-Path -parent
. (Join-Path $scriptDir "psbuildlib.ps1")

Begin-BuildScript "TxTranslation" "$config" ($batchMode -eq "batch")

# Set the application version
$gitRevisionFormat = "{bmin:2013:4}.{commit:6}{!:+}"
$revId = Get-GitRevision

# Debug builds
if (IsSelected("build-debug"))
{
	Build-Solution "TxTranslation.sln" "Debug" "Mixed Platforms" 6

	if (IsSelected("sign-lib"))
	{
		Sign-File "TxLib\bin\Debug\Unclassified.TxLib.dll" "signkey.pfx" "@signkey.password" 1
	}
	if (IsSelected("sign-app"))
	{
		Sign-File "TxEditor\bin\Debug\TxEditor.exe" "signkey.pfx" "@signkey.password" 1
	}
}

# Release builds
if ((IsSelected("build-release")) -or (IsSelected("commit")))
{
	Build-Solution "TxTranslation.sln" "Release" "Mixed Platforms" 6
	Exec-Console "_tools\PdbConvert.exe" "$sourcePath\TxEditor\bin\Release\* /srcbase $sourcePath /optimize /gz /outfile $sourcePath\.local\TxTranslation-$revId.pdb.xml.gz" 1

	if (IsSelected("sign-lib"))
	{
		Sign-File "TxLib\bin\Release\Unclassified.TxLib.dll" "signkey.pfx" "@signkey.password" 1
	}
	if (IsSelected("sign-app"))
	{
		Sign-File "TxEditor\bin\Release\TxEditor.exe" "signkey.pfx" "@signkey.password" 1
	}
}

# Release setups
if ((IsSelected("setup-release")) -or (IsSelected("commit")))
{
	Create-Setup "Setup\Tx.iss" Release 1

	if (IsSelected("sign-setup"))
	{
		Sign-File "Setup\bin\TxSetup-$revId.exe" "signkey.pfx" "@signkey.password" 1
	}
}

# Install release setup
if (IsSelected("install"))
{
	Exec-File "Setup\bin\TxSetup-$revId.exe" "/norestart /verysilent" 1
}

# Commit to repository
if (IsSelected("commit"))
{
	Delete-File "Setup\bin\TxSetup-$revId.exe" 0
	Git-Commit 1
}

End-BuildScript
