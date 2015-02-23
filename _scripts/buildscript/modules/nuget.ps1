# PowerShell build framework
# Copyright (c) 2015, Yves Goergen, http://unclassified.software/source/psbuild
#
# Copying and distribution of this file, with or without modification, are permitted provided the
# copyright notice and this notice are preserved. This file is offered as-is, without any warranty.

# The nuget module provides NuGet packaging functions.

# Creates a NuGet package.
#
# $specFile = The file name of the .nuspec file.
# $outDir = The output directory of the created package.
#
# Requires nuget.exe in the search path.
#
function Create-NuGet($specFile, $outDir, $time)
{
	$action = @{ action = "Do-Create-NuGet"; specFile = $specFile; outDir = $outDir; time = $time }
	$global:actions += $action
}

# Uploads a NuGet package.
#
# $packageFile = The file path and base name of the .nupkg file to upload to the NuGet gallery (excluding version and extension).
# $apiKey = Your API key. If empty, the current user configuration is used.
#
# The API key should be passed from variables that are defined in the private configuration file
# which is excluded from the source code repository. Example:
#
#    Push-NuGet "MyLib" $nuGetApiKey 30
#
# The file _scripts\buildscript\private.ps1 could contain this script:
#
#    $nuGetApiKey = "01234567-89ab-cdef-0123-456789abcdef"
#
# Requires nuget.exe in the search path.
#
function Push-NuGet($packageFile, $apiKey, $time)
{
	$action = @{ action = "Do-Push-NuGet"; packageFile = $packageFile; apiKey = $apiKey; time = $time }
	$global:actions += $action
}

# ==============================  FUNCTION IMPLEMENTATIONS  ==============================

function Do-Create-NuGet($action)
{
	$specFile = $action.specFile
	$outDir = $action.outDir

	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Creating NuGet package $specFile..."

	& nuget pack (MakeRootedPath $specFile) -OutputDirectory (MakeRootedPath $outDir) -Version $shortRevId -NonInteractive
	if (-not $?)
	{
		WaitError "Creating NuGet package failed"
		exit 1
	}
}

function Do-Push-NuGet($action)
{
	$packageFile = $action.packageFile
	$apiKey = $action.apiKey

	$packageFile = $packageFile + "." + $shortRevId + ".nupkg"
	
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Uploading NuGet package $packageFile..."

	if ($apiKey)
	{
		& nuget push (MakeRootedPath $packageFile) -ApiKey $apiKey -NonInteractive
	}
	else
	{
		& nuget push (MakeRootedPath $packageFile) -NonInteractive
	}
	if (-not $?)
	{
		WaitError "Uploading NuGet package failed"
		exit 1
	}
}
