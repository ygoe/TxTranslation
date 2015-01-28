# PowerShell build framework
# Copyright (c) 2015, Yves Goergen, http://unclassified.software/source/psbuild
#
# Copying and distribution of this file, with or without modification, are permitted provided the
# copyright notice and this notice are preserved. This file is offered as-is, without any warranty.

# ---------- USAGE ----------
#
# psbuild.ps1 <config-parts> [batch]
#
# config-parts: Space-separated list of selected build script parts. These parts can be tested for
#               with the IsSelected function.
# Batch mode:   Specify the parameter "batch" to run the build script non-interactively.
#               This disables user confirmations and delays.
#               Recommended for use in automatic build servers.
#
# ---------- STARTING ----------
#
# Yes, starting PowerShell scripts is a bit complicated. Here's an example batch file for use from
# the parent directory of this file. Each batch file defines the config parts to run and should be
# named accordingly. Additional parameters are passed on to PowerShell (for batch mode).
#
# @echo off
# cd /d "%~dp0"
# %SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe -ExecutionPolicy unrestricted -File buildscript\psbuild.ps1 "config-parts..." %*
# exit /b %errorlevel%
#
# ---------- REQUIREMENTS ----------
#
# Features provided by modules will likely require certain applications to be installed on the
# system. The following programs are used by the core script:
#
# * $toolsPath\FlashConsoleWindow
# * $toolsPath\GitRevisionTool or SvnRevisionTool (only if automatic Git/Subversion versioning is used)

# Initialisation code
param($configParts, $batchMode = "")
$batchMode = ($batchMode -eq "batch")

#cmd /c color f0
Clear-Host

$scriptDir = ($MyInvocation.MyCommand.Definition | Split-Path -parent)
$sourcePath = $scriptDir | Split-Path -parent | Split-Path -parent
$startTime = Get-Date

# Configuration defaults
# (Paths relative to this script file)
$toolsPath = "../bin"
$modulesPath = "modules"
$gitRevisionFormat = "{commit:8}{!:+}"
$svnRevisionFormat = "{commit}{!:+}"
$revId = "0"
$noParallelBuild = $false

# Disable FASTBUILD mode to always include a full version number in the assembly version info
$env:FASTBUILD = ""

# Contains the selected actions to be executed
$actions = @()

# Prepare often used variables
$absToolsPath = Join-Path $scriptDir $toolsPath

# ==============================  HELPER FUNCTIONS  ==============================

# Returns the file name if the file exists; otherwise, $null.
#
function Check-FileName($fn)
{
	$fn = [System.Environment]::ExpandEnvironmentVariables($fn)
	if (test-path $fn)
	{
		return $fn
	}
}

# Returns the file name from a registry value if the file exists; otherwise, $null.
#
function Check-RegFilename($key, $value)
{
	$regKey = Get-ItemProperty -Path $key -Name $value -ErrorAction SilentlyContinue
	if ($regKey -ne $null)
	{
		return Check-FileName $regKey.$value
	}
}

# Returns a rooted path. Non-rooted paths are interpreted relative to $sourcePath.
#
function MakeRootedPath($path)
{
	if (![System.IO.Path]::IsPathRooted($path))
	{
		return "$sourcePath\$path"
	}
	return $path
}

# Creates a directory if it does not exist.
#
function EnsureDirExists($path)
{
	$path = MakeRootedPath $path
	if (!(Test-Path "$path"))
	{
		New-Item -ItemType Directory "$path" -ErrorAction Stop | Out-Null
	}
}

# Moves the cursor by $count columns.
#
function Move-Cursor($count)
{
	$x = $Host.UI.RawUI.CursorPosition.X + $count
	$y = $Host.UI.RawUI.CursorPosition.Y
	if ($x -lt 0)
	{
		$x = 0
	}
	if ($x -ge $Host.UI.RawUI.BufferSize.Width)
	{
		$x = $Host.UI.RawUI.BufferSize.Width - 1
	}
	$Host.UI.RawUI.CursorPosition = New-Object System.Management.Automation.Host.Coordinates $x, $y
}

# Clears the input buffer.
#
function Clear-KeyBuffer()
{
	while ($Host.UI.RawUI.KeyAvailable)
	{
		[void]$Host.UI.RawUI.ReadKey("IncludeKeyUp,IncludeKeyDown,NoEcho")
	}
}

# Determines whether the key is an input key.
#
function IsInputKey($key)
{
	$ignore =
		16,    # Shift (left or right)
		17,    # Ctrl (left or right)
		18,    # Alt (left or right)
		20,    # Caps lock
		91,    # Windows key (left)
		92,    # Windows key (right)
		93,    # Menu key
		144,   # Num lock
		145,   # Scroll lock
		166,   # Back
		167,   # Forward
		168,   # Refresh
		169,   # Stop
		170,   # Search
		171,   # Favorites
		172,   # Start/Home
		173,   # Mute
		174,   # Volume Down
		175,   # Volume Up
		176,   # Next Track
		177,   # Previous Track
		178,   # Stop Media
		179,   # Play
		180,   # Mail
		181,   # Select Media
		182,   # Application 1
		183    # Application 2
	
	return !($key.VirtualKeyCode -eq $null -or $ignore -Contains $key.VirtualKeyCode)
}

# Waits for a keypress with a message.
#
# $msg = $true to display the default instruction message.
# $timeout = The maximum time to wait, in seconds. -1 to wait infinitely.
# $showDots = $true to show a decreasing amount of dots to indicate the remaining time until timeout.
#
# If in batch mode, this function does nothing.
#
function Wait-Key($msg = $true, $timeout = -1, $showDots = $false)
{
	if ($global:batchMode)
	{
		return
	}

	if ($psISE)
	{
		# Compatibility fallback for PowerShell ISE
		pause
	}
	else
	{
		if ($msg)
		{
			Write-Host -NoNewline "Press any key to continue"
			if (-not $showDots)
			{
				Write-Host -NoNewline "..."
			}
		}
		if ($timeout -lt 0)
		{
			Clear-KeyBuffer
			#[void][System.Console]::ReadKey($true)
			while (!(IsInputKey $Host.UI.RawUI.ReadKey("IncludeKeyDown,NoEcho")))
			{
			}
		}
		else
		{
			if ($showDots)
			{
				$counter = $timeout
				while ($counter -gt 0)
				{
					$counter -= 1000
					Write-Host "." -NoNewLine
				}
			}
			$counter = 0
			$step = 100
			$nextSecond = 1000
			Clear-KeyBuffer
			while (!($Host.UI.RawUI.KeyAvailable -and (IsInputKey $Host.UI.RawUI.ReadKey("IncludeKeyDown,NoEcho"))) -and ($counter -lt $timeout))
			{
				Start-Sleep -m $step
				$counter += $step
				if ($showDots -and $counter -ge $nextSecond)
				{
					$nextSecond += 1000
					Move-Cursor -1
					Write-Host " " -NoNewLine
					Move-Cursor -1
				}
			}
			Clear-KeyBuffer
		}
		if ($msg)
		{
			Write-Host ""
		}
	}
}

# Shows an error message and waits for a keypress.
#
function WaitError($msg)
{
	Write-Host ""
	& (Join-Path $absToolsPath "FlashConsoleWindow") -error
	& (Join-Path $absToolsPath "FlashConsoleWindow")
	Write-Host -ForegroundColor Red ("ERROR: " + $msg)
	Wait-Key
	& (Join-Path $absToolsPath "FlashConsoleWindow") -noprogress
}

# Returns the system platform (x86, x64).
#
function Get-Platform()
{
	# Determine current Windows architecture (32/64 bit)
	if ([System.Environment]::GetEnvironmentVariable("ProgramFiles(x86)") -ne $null)
	{
		return "x64"
	}
	else
	{
		return "x86"
	}
}

# Returns the Git revision of the working directory.
#
# The revision format is specified in $gitRevisionFormat.
#
function Get-GitRevision()
{
	# Determine current repository revision
	$revId = & (Join-Path $absToolsPath "GitRevisionTool") --format "$global:gitRevisionFormat" "$sourcePath"
	if ($revId -eq $null)
	{
		WaitError "Repository revision could not be determined"
		exit 1
	}
	$global:gitUsed = $true
	return $revId
}

# Returns the Subversion revision of the working directory.
#
# The revision format is specified in $svnRevisionFormat.
#
function Get-SvnRevision()
{
	# Determine current repository revision
	$revId = & (Join-Path $absToolsPath "SvnRevisionTool") --format "$global:svnRevisionFormat" "$sourcePath"
	if ($revId -eq $null)
	{
		WaitError "Repository revision could not be determined"
		exit 1
	}
	$global:svnUsed = $true
	return $revId
}

# Returns the application version from a source file defining an assembly attribute.
# (Only C# source code supported.)
#
# $sourceFile = The name of the source file to read.
# $attributeName = The name of the attribute to read.
#
function Get-AssemblyInfoVersion($sourceFile, $attributeName)
{
	$sourceFile = Check-FileName (MakeRootedPath $sourceFile)
	if ($sourceFile -eq $null)
	{
		WaitError "AssemblyInfo source file not found"
		exit 1
	}
	
	$revId = (gc $sourceFile | select-string -pattern "^\s*\[assembly:\s*$attributeName\(""(.+?)""\)\]").Matches[0].Groups[1].Value
	if ($revId -eq $null)
	{
		WaitError "AssemblyInfo version could not be determined"
		exit 1
	}
	return $revId
}

# Determines whether a build script part is selected.
#
function IsSelected($part)
{
	#if ($global:configParts -eq "all" -or $global:configParts.Contains($part))
	if ($global:configParts.Contains($part))
	{
		return $true
	}
	return $false
}

# ==============================  BUILD SCRIPT FUNCTIONS  ==============================

# Begins the build script definition.
#
# $projectTitle = The displayed title.
#
function Begin-BuildScript($projectTitle)
{
	$Host.UI.RawUI.WindowTitle = "$projectTitle build"
	Write-Host -ForegroundColor White "$projectTitle build script"
	Write-Host ""
}

# Sets the application version from the Git revision.
#
# $format = The GitRevisionTool format string.
#
function Set-GitVersion($format)
{
	$global:gitRevisionFormat = $format
	$global:revId = Get-GitRevision
}

# Sets the application version from the Subversion revision.
#
# $format = The SvnRevisionTool format string.
#
function Set-SvnVersion($format)
{
	$global:svnRevisionFormat = $format
	$global:revId = Get-SvnRevision
}

# Sets the application version from an assembly version attribute.
# (Only C# source code supported.)
#
# $sourceFile = The name of the source file to read.
# $attributeName = The name of the attribute to read.
#
function Set-AssemblyInfoVersion($sourceFile, $attributeName)
{
	$global:revId = Get-AssemblyInfoVersion $sourceFile $attributeName
}

# Disables using parallel builds with MSBuild.
#
function Disable-ParallelBuild()
{
	$global:noParallelBuild = $true
}

# Ends the build script definition and executes the configured actions.
#
function End-BuildScript()
{
	Write-Host "Application version : $revId"

	# Perform all registered actions now
	$totalTime = 0
	foreach ($action in $actions)
	{
		$totalTime += $action.time
	}
	Write-Host "Total scheduled time: $totalTime s"

	$timeSum = 0
	foreach ($action in $actions)
	{
		$functionName = $action.action
		& $functionName $action
		
		$timeSum += $action.time
		$progressAfter = [int] ($timeSum / $totalTime * 100)
		& (Join-Path $absToolsPath "FlashConsoleWindow") -progress $progressAfter
		$timeSum += $action.time
	}
	
	$endTime = Get-Date
	if ($PSVersionTable.CLRVersion.Major -ge 4)
	{
		$duration = ($endTime - $global:startTime).ToString("h\:mm\:ss")
	}
	else
	{
		$duration = ($endTime - $global:startTime).TotalSeconds.ToString("0") + " seconds"
	}

	Write-Host ""
	Write-Host -ForegroundColor DarkGreen "Build succeeded in $duration."
	if (!$global:batchMode)
	{
		& (Join-Path $absToolsPath "FlashConsoleWindow") -progress 100
		Write-Host "Press any key to exit" -NoNewLine
		Wait-Key $false 10000 $true
		Write-Host ""
	}
	& (Join-Path $absToolsPath "FlashConsoleWindow") -noprogress
}

# ==============================  MODULE SUPPORT  ==============================

# Load all modules from subdirectory
Get-ChildItem (Join-Path $scriptDir $modulesPath) `
	| Where { $_.Name -notlike '_*' -and $_.Name -like '*.ps1'} `
	| ForEach { . $_.FullName }

# ==============================  CONTROL FILE  ==============================

# Include the control file that specifies what to do
. (Join-Path $scriptDir "control.ps1")
