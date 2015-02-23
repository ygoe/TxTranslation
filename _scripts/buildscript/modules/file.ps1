# PowerShell build framework
# Copyright (c) 2015, Yves Goergen, http://unclassified.software/source/psbuild
#
# Copying and distribution of this file, with or without modification, are permitted provided the
# copyright notice and this notice are preserved. This file is offered as-is, without any warranty.

# The file module provides file management and execution functions.

# Creates an archive from a list of files.
#
# $archive = The file name of the archive to create.
# $listFile = The file that defines the archive contents.
#
# The list file defines one archive item per line. Each line contains the source file name,
# followed by a greater-than character ('>'), followed by the item name in the archive. If the
# second name ends with a backslash, the file name of the source file is appended. All source
# files are interpreted relative to the $rootDir directory. Wildcards are allowed. Empty lines
# or lines beginning with a number character ('#') are ignored.
#
# Requires 7-Zip to be installed.
#
function Create-Archive($archive, $listFile, $time)
{
	$global:actions += @{ action = "Do-Create-Archive"; archive = $archive; listFile = $listFile; time = $time }
}

# Copies a file.
#
# $src = The name of the source file.
# $dest = The name of the destination file. Can be a directory.
#
function Copy-File($src, $dest, $time)
{
	$global:actions += @{ action = "Do-Copy-File"; src = $src; dest = $dest; time = $time }
}

# Deletes a file.
#
# $file = The name of the file to delete.
#
function Delete-File($file, $time)
{
	$global:actions += @{ action = "Do-Delete-File"; file = $file; time = $time }
}

# Executes a Windows application and waits for it to finish.
#
# $file = The file name of the application to execute.
# $params = The command line parameters.
#
# This function halts the build process if the process returns an error return code.
#
# This function also supports console applications, but the standard streams will be redirected
# which is not the case with the Exec-Console function.
#
function Exec-File($file, $params, $time)
{
	$global:actions += @{ action = "Do-Exec-File"; file = $file; params = $params; time = $time }
}

# Executes a console application.
#
# $file = The file name of the application to execute.
# $params = The command line parameters.
#
# This function halts the build process if the process returns an error return code.
#
function Exec-Console($file, $params, $time)
{
	$global:actions += @{ action = "Do-Exec-Console"; file = $file; params = $params; time = $time }
}

# Selects a file in the Windows Explorer.
#
# $file = The name of the file to select.
#
function Explorer-Select($file, $time)
{
	$global:actions += @{ action = "Do-Explorer-Select"; file = $file; time = $time }
}

# ==============================  FUNCTION IMPLEMENTATIONS  ==============================

function Do-Create-Archive($action)
{
	$archive = $action.archive
	$listFile = $action.listFile
	
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Creating archive $archive..."

	# Find the 7-Zip binary
	$sevenZipBin = Check-RegFilename "hklm:\SOFTWARE\7-Zip" "Path"
	$sevenZipBin = Check-Filename "$sevenZipBin\7z.exe"
	if ($sevenZipBin -eq $null)
	{
		WaitError "7-Zip binary not found"
		exit 1
	}

	try
	{
		# Delete previous export if it exists
		if (Test-Path "$rootDir\.tmp.archive")
		{
			Remove-Item "$rootDir\.tmp.archive" -Recurse -ErrorAction Stop
		}

		# Create temp directory
		New-Item -ItemType Directory "$rootDir\.tmp.archive" -ErrorAction Stop | Out-Null

		# Prepare all files in a temporary directory
		ForEach ($line in Get-Content (MakeRootedPath $listFile) -ErrorAction Stop)
		{
			if (-not $line.Trim()) { continue }
			if ($line.StartsWith("#")) { continue }
			
			# Parse input line
			$parts = $line.Split(">")
			$src = $parts[0].Trim()
			$dest = $parts[1].Trim()

			# Copy file to temp directory
			if ($dest.EndsWith("\"))
			{
				New-Item -ItemType Directory -Path "$rootDir\.tmp.archive\$dest" -Force -ErrorAction Stop | Out-Null
			}
			else
			{
				New-Item -ItemType File -Path "$rootDir\.tmp.archive\$dest" -Force -ErrorAction Stop | Out-Null
			}
			Copy-Item -Recurse -Force (MakeRootedPath $src) "$rootDir\.tmp.archive\$dest" -ErrorAction Stop
		}

		# Delete previous archive if it exists
		if (Test-Path (MakeRootedPath $archive))
		{
			Remove-Item (MakeRootedPath $archive) -ErrorAction Stop
		}
	}
	catch
	{
		WaitError $_
		exit 1
	}

	Push-Location "$rootDir\.tmp.archive"
	& $sevenZipBin a (MakeRootedPath $archive) -mx=9 * | where {
		$_ -notmatch "^7-Zip " -and `
		$_ -notmatch "^Scanning$" -and `
		$_ -notmatch "^Creating archive " -and `
		$_ -notmatch "^\s*$" -and `
		$_ -notmatch "^Compressing "
	}
	if (-not $?)
	{
		Pop-Location
		WaitError "Creating archive failed"
		exit 1
	}
	Pop-Location

	# Clean up
	Remove-Item "$rootDir\.tmp.archive" -Recurse
}

function Do-Copy-File($action)
{
	$src = $action.src
	$dest = $action.dest
	
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Copying $src to $dest..."

	Copy-Item (MakeRootedPath $src) (MakeRootedPath $dest)
	if (-not $?)
	{
		WaitError "Copy failed"
		exit 1
	}
}

function Do-Delete-File($action)
{
	$file = $action.file
	
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Deleting $file..."

	Remove-Item (MakeRootedPath $file)
	if (-not $?)
	{
		WaitError "Deletion failed"
		exit 1
	}
}

function Do-Exec-File($action)
{
	$file = $action.file
	$params = $action.params
	
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Executing $file $params..."

	# Wait until the started process has finished
	Invoke-Expression ((MakeRootedPath $file) + " " + $params + " | Out-Host")
	if ($LASTEXITCODE -ne 0)
	{
		WaitError "Execution failed"
		exit 1
	}
}

function Do-Exec-Console($action)
{
	$file = $action.file
	$params = $action.params
	
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Executing $file $params..."

	Invoke-Expression ((MakeRootedPath $file) + " " + $params)
	if ($LASTEXITCODE -ne 0)
	{
		WaitError "Execution failed"
		exit 1
	}
}

function Do-Explorer-Select($action)
{
	$file = $action.file
	
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Selecting $file in Explorer..."

	$file = MakeRootedPath $file
	Start-Process "$env:SystemRoot\explorer.exe" "/select,`"$file`""
}
