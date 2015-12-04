# PowerShell build framework
# Copyright (c) 2015, Yves Goergen, http://unclassified.software/source/psbuild
#
# Copying and distribution of this file, with or without modification, are permitted provided the
# copyright notice and this notice are preserved. This file is offered as-is, without any warranty.

# The git module provides Git source control functions.

# Commits the working directory modifications to the current branch.
#
# Requires TortoiseGit to be installed (which requires Git for Windows).
#
function Git-Commit($time = 5)
{
	$action = @{ action = "Do-Git-Commit"; time = $time }
	$global:actions += $action
}

# Exports the current repository revision to an archive file.
#
# $archive = The file name of the archive to create.
#
# Requires Git for Windows and 7-Zip to be installed.
#
function Git-Export($archive, $time = 5)
{
	$action = @{ action = "Do-Git-Export"; archive = $archive; time = $time }
	$global:actions += $action
}

# Collects the recent commit messages and adds them to a log file, with the current time and
# revision ID in the header. This file will be opened in an editor to let the user edit it. The
# purpose of this file is to give it to the end users as a change log or release notes summary.
#
# $logFile = The file name of the log file to update and open.
#
# Requires Git for Windows to be installed.
#
function Git-Log($logFile, $time = 1)
{
	$action = @{ action = "Do-Git-Log"; logFile = $logFile; time = $time }
	$global:actions += $action
}

# ==============================  FUNCTION IMPLEMENTATIONS  ==============================

function Do-Git-Commit($action)
{
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Git commit..."

	# Find the TortoiseGitProc binary
	$tgitBin = Check-RegFilename "hklm:\SOFTWARE\TortoiseGit" "ProcPath"
	if ($tgitBin -eq $null)
	{
		WaitError "TortoiseGitProc binary not found"
		exit 1
	}
	
	# Wait until the started process has finished
	& $tgitBin /command:commit /path:"$rootDir" | Out-Host
	if (-not $?)
	{
		WaitError "Git commit failed"
		exit 1
	}
}

function Do-Git-Export($action)
{
	$archive = $action.archive
	
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Git export to $archive..."

	# Warn on modified working directory
	# (Set a dummy format so that it won't go search an AssemblyInfo file somewhere. We don't provide a suitable path for that.)
	$consoleEncoding = [System.Console]::OutputEncoding
	[System.Console]::OutputEncoding = [System.Text.Encoding]::UTF8
	$revId = Invoke-Expression ((Join-Path $absToolsPath "NetRevisionTool") + " /format dummy /rejectmod `"$rootDir`"")
	if ($LASTEXITCODE -ne 0)
	{
		[System.Console]::OutputEncoding = $consoleEncoding
		Write-Host -ForegroundColor Yellow "Warning: The local working copy is modified! Uncommitted changes are NOT exported."
	}
	[System.Console]::OutputEncoding = $consoleEncoding

	# Find the Git binary
	$gitBin = Check-RegFilename "hklm:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1" "InstallLocation"
	$gitBin = Check-Filename "$gitBin\bin\git.exe"
	if ($gitBin -eq $null)
	{
		$gitBin = Check-RegFilename "hklm:\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1" "InstallLocation"
		$gitBin = Check-Filename "$gitBin\bin\git.exe"
	}
	if ($gitBin -eq $null)
	{
		$gitBin = Check-RegFilename "hkcu:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1" "InstallLocation"
		$gitBin = Check-Filename "$gitBin\bin\git.exe"
	}
	if ($gitBin -eq $null)
	{
		WaitError "Git binary not found"
		exit 1
	}

	# Find the 7-Zip binary
	$sevenZipBin = Check-RegFilename "hklm:\SOFTWARE\7-Zip" "Path"
	$sevenZipBin = Check-Filename "$sevenZipBin\7z.exe"
	if ($sevenZipBin -eq $null)
	{
		WaitError "7-Zip binary not found"
		exit 1
	}

	# Delete previous export if it exists
	if (Test-Path "$rootDir\.tmp.export")
	{
		Remove-Item "$rootDir\.tmp.export" -Recurse -ErrorAction Stop
	}

	# Create temp directory
	New-Item -ItemType Directory "$rootDir\.tmp.export" -ErrorAction Stop | Out-Null

	Push-Location "$rootDir"
	& $gitBin checkout-index -a --prefix ".tmp.export\"
	if (-not $?)
	{
		WaitError "Git export failed"
		exit 1
	}
	Pop-Location

	# Delete previous archive if it exists
	if (Test-Path (MakeRootedPath $archive))
	{
		Remove-Item (MakeRootedPath $archive) -ErrorAction Stop
	}

	Push-Location "$rootDir\.tmp.export"
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
		WaitError "Creating Git export archive failed"
		exit 1
	}
	Pop-Location

	# Clean up
	Remove-Item "$rootDir\.tmp.export" -Recurse
}

function Do-Git-Log($action)
{
	$logFile = $action.logFile
	
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Git log dump..."
	
	# Stop on modified working directory
	# (Set a dummy format so that it won't go search an AssemblyInfo file somewhere. We don't provide a suitable path for that.)
	$consoleEncoding = [System.Console]::OutputEncoding
	[System.Console]::OutputEncoding = [System.Text.Encoding]::UTF8
	$revId = Invoke-Expression ((Join-Path $absToolsPath "NetRevisionTool") + " /format dummy /rejectmod `"$rootDir`"")
	if ($LASTEXITCODE -ne 0)
	{
		[System.Console]::OutputEncoding = $consoleEncoding
		WaitError "The local working copy is modified"
		exit 1
	}
	[System.Console]::OutputEncoding = $consoleEncoding

	# Find the Git binary
	$gitBin = Check-RegFilename "hklm:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1" "InstallLocation"
	$gitBin = Check-Filename "$gitBin\bin\git.exe"
	if ($gitBin -eq $null)
	{
		$gitBin = Check-RegFilename "hklm:\Software\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1" "InstallLocation"
		$gitBin = Check-Filename "$gitBin\bin\git.exe"
	}
	if ($gitBin -eq $null)
	{
		$gitBin = Check-RegFilename "hkcu:\Software\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1" "InstallLocation"
		$gitBin = Check-Filename "$gitBin\bin\git.exe"
	}
	if ($gitBin -eq $null)
	{
		WaitError "Git binary not found"
		exit 1
	}

	# Read the output log file and determine the last added revision
	$data = ""
	$lastRev = ""
	if (Test-Path (MakeRootedPath $logFile))
	{
		$data = [System.IO.File]::ReadAllText((MakeRootedPath $logFile))
		if ($data -Match " - .+ \((.+)\)")
		{
			$lastRev = ([regex]::Match($data, " - .+ \((.+)\)")).Groups[1].Value
		}
	}
	
	if ($lastRev)
	{
		Write-Host "Adding log messages since commit $lastRev"
	}
	else
	{
		Write-Host "Adding all log messages since the first commit (new log file)"
	}
	
	# Get last commit date
	$consoleEncoding = [System.Console]::OutputEncoding
	[System.Console]::OutputEncoding = [System.Text.Encoding]::UTF8
	Push-Location "$rootDir"
	$commitDate = (& $gitBin log -1 --pretty=format:"%ai" 2>&1)
	if (-not $?)
	{
		Pop-Location
		[System.Console]::OutputEncoding = $consoleEncoding
		WaitError "Git log failed for current commit date"
		exit 1
	}
	Pop-Location
	[System.Console]::OutputEncoding = $consoleEncoding
	if (-not [string]$commitDate)
	{
		Write-Host "No commit yet"
		return
	}
	# DEBUG: Write-Host -ForegroundColor Yellow $commitDate
	$commitDate = $commitDate.Substring(0, 10)

	# Get last commit hash
	$consoleEncoding = [System.Console]::OutputEncoding
	[System.Console]::OutputEncoding = [System.Text.Encoding]::UTF8
	Push-Location "$rootDir"
	$commitHash = (& $gitBin log -1 --pretty=format:"%h" 2>&1)
	if (-not $?)
	{
		Pop-Location
		[System.Console]::OutputEncoding = $consoleEncoding
		WaitError "Git log failed for current commit hash"
		exit 1
	}
	Pop-Location
	[System.Console]::OutputEncoding = $consoleEncoding
	if (-not [string]$commitHash)
	{
		Write-Host "No commit yet"
		return
	}

	# Get log messages for the new revisions
	$consoleEncoding = [System.Console]::OutputEncoding
	[System.Console]::OutputEncoding = [System.Text.Encoding]::UTF8
	Push-Location "$rootDir"
	if ($lastRev)
	{
		$logText = (& $gitBin log --pretty=format:"%B" --reverse "${lastRev}..HEAD" 2>&1)
	}
	else
	{
		$logText = (& $gitBin log --pretty=format:"%B" --reverse 2>&1)
	}
	if (-not $?)
	{
		Pop-Location
		[System.Console]::OutputEncoding = $consoleEncoding
		WaitError "Git log failed for commit messages"
		exit 1
	}
	Pop-Location
	[System.Console]::OutputEncoding = $consoleEncoding
	if (-not [string]$logText)
	{
		Write-Host "No new messages"
		return
	}
	# DEBUG: Write-Host -ForegroundColor Yellow $logText

	# Extract non-empty lines from all returned messages
	$msgs = $logText | Foreach { $_.Trim() } | Where { $_ }
	# DEBUG: Write-Host -ForegroundColor Yellow ([String]::Join("`n", $msgs))

	# Format current date and revision and new messages
	$caption = $commitDate + " - " + $shortRevId + " (" + $commitHash + ")"
	$newMsgs = $caption + "`r`n" + `
		("—" * $caption.Length) + "`r`n" + `
		[string]::Join("`r`n", $msgs).Replace("`r`r", "`r") + "`r`n`r`n"

	# Write back the complete file
	$data = ($newMsgs + $data).Trim() + "`r`n"
	[System.IO.File]::WriteAllText((MakeRootedPath $logFile), $data, [System.Text.Encoding]::UTF8)

	# Open file in editor for manual edits of the raw changes
	Start-Process (MakeRootedPath $logFile)
}
