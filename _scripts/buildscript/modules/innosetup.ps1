# The innosetup module provides Inno Setup functions.

# Creates a setup package with Inno Setup.
#
# $configFile = The file name of the Inno Setup script file.
# $configuration = This value is defined as "BuildConfig" in the setup script. It can be empty if unused.
#
# Requires Inno Setup 5 to be installed.
#
function Create-Setup($configFile, $configuration, $time)
{
	$action = @{ action = "Do-Create-Setup"; configFile = $configFile; configuration = $configuration; time = $time }
	$global:actions += $action
}

# ==============================  FUNCTION IMPLEMENTATIONS  ==============================

function Do-Create-Setup($action)
{
	$configFile = $action.configFile
	$configuration = $action.configuration
	
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Creating setup $configFile..."

	# Find the InnoSetup binary
	if ((Get-Platform) -eq "x64")
	{
		$innosetupBin = Check-RegFilename "hklm:\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 5_is1" "InstallLocation"
		$innosetupBin = Check-Filename "$innosetupBin\ISCC.exe"
	}
	if ((Get-Platform) -eq "x86")
	{
		$innosetupBin = Check-RegFilename "hklm:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 5_is1" "InstallLocation"
		$innosetupBin = Check-Filename "$innosetupBin\ISCC.exe"
	}
	if ($innosetupBin -eq $null)
	{
		WaitError "InnoSetup binary not found"
		exit 1
	}

	& $innosetupBin /q (MakeRootedPath($configFile)) /dBuildConfig=$configuration /dRevId=$revId
	if (-not $?)
	{
		WaitError "Creating setup failed"
		exit 1
	}
}
