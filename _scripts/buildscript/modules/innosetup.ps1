# PowerShell build framework
# Copyright (c) 2015, Yves Goergen, http://unclassified.software/source/psbuild
#
# Copying and distribution of this file, with or without modification, are permitted provided the
# copyright notice and this notice are preserved. This file is offered as-is, without any warranty.

# The innosetup module provides Inno Setup functions.

# Creates a setup package with Inno Setup.
#
# $configFile = The file name of the Inno Setup script file.
# $configuration = This value is defined as "BuildConfig" in the setup script. It can be empty if unused.
#
# The current build revision ID is passed to the setup script as "RevId".
#
# Requires Inno Setup 5 to be installed.
#
function Create-Setup($configFile, $configuration, $time = 1)
{
	$action = @{ action = "Do-Create-Setup"; configFile = $configFile; configuration = $configuration; time = $time }
	$global:actions += $action
}

# ==============================  FUNCTION IMPLEMENTATIONS  ==============================

function Do-Create-Setup($action)
{
	$configFile = $action.configFile
	$configuration = $action.configuration
	
	Show-ActionHeader "Creating setup $configFile"

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

	& $innosetupBin /q (MakeRootedPath $configFile) /dBuildConfig=$configuration /dRevId=$revId
	if (-not $?)
	{
		WaitError "Creating setup failed"
		exit 1
	}
}
