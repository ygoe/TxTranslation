# PowerShell build framework
# Copyright (c) 2015, Yves Goergen, http://unclassified.software/source/psbuild
#
# Copying and distribution of this file, with or without modification, are permitted provided the
# copyright notice and this notice are preserved. This file is offered as-is, without any warranty.

# The nuget module provides NuGet packaging functions.

# Restores the NuGet tool program file in $toolsPath.
#
# You should only call this function if NuGet.exe is not included in the repository and is also
# ignored from it so that the new file won't mark the working directory as modified. The file might
# stay there for the next build, but if it's gone it will just be downloaded again.
#
function Restore-NuGetTool($time = 0)
{
	$action = @{ action = "Do-Restore-NuGetTool"; time = $time }
	$global:actions += $action
}

# Restores all NuGet packages in a solution.
#
# $solutionFile = The file name of the .sln file.
#
# Requires nuget.exe in $toolsPath or the search path.
#
function Restore-NuGetPackages($solutionFile, $time = 0)
{
	$action = @{ action = "Do-Restore-NuGetPackages"; solutionFile = $solutionFile; time = $time }
	$global:actions += $action
}

# Creates a NuGet package.
#
# $specFile = The file name of the .nuspec file.
# $outDir = The output directory of the created package.
#
# Requires nuget.exe in $toolsPath or the search path.
#
function Create-NuGetPackage($specFile, $outDir, $time = 2)
{
	$action = @{ action = "Do-Create-NuGetPackage"; specFile = $specFile; outDir = $outDir; time = $time }
	$global:actions += $action
}

# Uploads a NuGet package to the official package gallery.
#
# $packageFile = The file path and base name of the .nupkg file to upload to the NuGet gallery (excluding version and extension).
# $apiKey = Your API key. If empty, the current user configuration is used.
#
# The API key should be passed from variables that are defined in the private configuration file
# which is excluded from the source code repository. Example:
#
#    Push-NuGetPackage "MyLib" $nuGetApiKey 30
#
# The file _scripts\buildscript\private.ps1 could contain this script:
#
#    $nuGetApiKey = "01234567-89ab-cdef-0123-456789abcdef"
#
# Requires nuget.exe in $toolsPath or the search path.
#
function Push-NuGetPackage($packageFile, $apiKey, $time = 30)
{
	$action = @{ action = "Do-Push-NuGetPackage"; packageFile = $packageFile; apiKey = $apiKey; time = $time }
	$global:actions += $action
}

# ==============================  FUNCTION IMPLEMENTATIONS  ==============================

function Do-Restore-NuGetTool($action)
{
	$nugetBin = (Join-Path $absToolsPath "NuGet.exe")
	if (!(Check-FileName $nugetBin))
	{
		Write-Host ""
		Write-Host -ForegroundColor DarkCyan "Downloading NuGet tool..."

		try
		{
			$webclient = New-Object System.Net.WebClient
			$webclient.DownloadFile("https://nuget.org/nuget.exe", $nugetBin)
		}
		catch
		{
			Write-Host $_
			WaitError "Downloading NuGet tool failed"
			exit 1
		}
	}
}

function Do-Restore-NuGetPackages($action)
{
	$solutionFile = $action.solutionFile
	
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Restoring NuGet packages for $solutionFile..."

	$nugetBin = Check-FileName (Join-Path $absToolsPath "NuGet.exe")
	if (!$nugetBin)
	{
		$nugetBin = "NuGet.exe"
	}

	& $nugetBin restore (MakeRootedPath $solutionFile) -NonInteractive > $null
	if (-not $?)
	{
		WaitError "Restoring NuGet packages failed"
		exit 1
	}
}

function Do-Create-NuGetPackage($action)
{
	$specFile = $action.specFile
	$outDir = $action.outDir

	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Creating NuGet package $specFile..."

	$nugetBin = Check-FileName (Join-Path $absToolsPath "NuGet.exe")
	if (!$nugetBin)
	{
		$nugetBin = "NuGet.exe"
	}

	& $nugetBin pack (MakeRootedPath $specFile) -OutputDirectory (MakeRootedPath $outDir) -Version $shortRevId -NonInteractive > $null
	if (-not $?)
	{
		WaitError "Creating NuGet package failed"
		exit 1
	}
}

function Do-Push-NuGetPackage($action)
{
	$packageFile = $action.packageFile
	$apiKey = $action.apiKey

	$packageFile = $packageFile + "." + $shortRevId + ".nupkg"
	
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Uploading NuGet package $packageFile..."

	$nugetBin = Check-FileName (Join-Path $absToolsPath "NuGet.exe")
	if (!$nugetBin)
	{
		$nugetBin = "NuGet.exe"
	}

	if ($apiKey)
	{
		& $nugetBin push (MakeRootedPath $packageFile) -ApiKey $apiKey -NonInteractive
	}
	else
	{
		& $nugetBin push (MakeRootedPath $packageFile) -NonInteractive
	}
	if (-not $?)
	{
		WaitError "Uploading NuGet package failed"
		exit 1
	}
}
