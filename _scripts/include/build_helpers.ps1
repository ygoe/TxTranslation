# Build script helper functions

# Configuration defaults
$toolsPath = "../_tools/"
$gitRevisionFormat = "{commit:8}{!:+}"
$svnRevisionFormat = "{commit}{!:+}"

# Disable FASTBUILD mode to always include a full version number in the assembly version info.
$env:FASTBUILD = ""

# ==============================  HELPER FUNCTIONS  ==============================

function Check-FileName($fn)
{
	$fn = [System.Environment]::ExpandEnvironmentVariables($fn)
	if (test-path $fn)
	{
		return $fn
	}
}

function Check-RegFilename($key, $value)
{
	$regKey = Get-ItemProperty -Path $key -Name $value -ErrorAction SilentlyContinue
	if ($regKey -ne $null)
	{
		return Check-FileName $regKey.$value
	}
}

function MakeRootedPath($path)
{
	if (![System.IO.Path]::IsPathRooted($path))
	{
		return "$sourcePath\$path"
	}
	return $path
}

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

function Clear-KeyBuffer()
{
	while ($Host.UI.RawUI.KeyAvailable)
	{
		[void]$Host.UI.RawUI.ReadKey("IncludeKeyUp,IncludeKeyDown,NoEcho")
	}
}

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
			while (!(IsInputKey($Host.UI.RawUI.ReadKey("IncludeKeyDown,NoEcho"))))
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
			while (!($Host.UI.RawUI.KeyAvailable -and (IsInputKey($Host.UI.RawUI.ReadKey("IncludeKeyDown,NoEcho")))) -and ($counter -lt $timeout))
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

function WaitError($msg)
{
	Write-Host ""
	& ($toolsPath + "FlashConsoleWindow") -error
	& ($toolsPath + "FlashConsoleWindow")
	Write-Host -ForegroundColor Red ("ERROR: " + $msg)
	Wait-Key
	& ($toolsPath + "FlashConsoleWindow") -noprogress
}

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

function Get-GitRevision()
{
	# Determine current repository revision
	$revId = & ($toolsPath + "GitRevisionTool") --format "$global:gitRevisionFormat" "$sourcePath"
	if ($revId -eq $null)
	{
		WaitError "Repository revision could not be determined"
		exit 1
	}
	$global:gitUsed = $true
	return $revId
}

function Get-SvnRevision()
{
	# Determine current repository revision
	$revId = & ($toolsPath + "SvnRevisionTool") --format "$global:svnRevisionFormat" "$sourcePath"
	if ($revId -eq $null)
	{
		WaitError "Repository revision could not be determined"
		exit 1
	}
	$global:svnUsed = $true
	return $revId
}

function Get-AssemblyInfoVersion($sourceFile, $attributeName)
{
	$sourceFile = Check-FileName (MakeRootedPath($sourceFile))
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

function IsSelected($part)
{
	#if ($global:configParts -eq "all" -or $global:configParts.Contains($part))
	if ($global:configParts.Contains($part))
	{
		return $true
	}
	return $false
}

# ==============================  ACTION SPECIFICATIONS  ==============================

$actions = @()

function Build-Solution($solution, $configuration, $buildPlatform, $time)
{
	$action = @{ action = "build"; solution = $solution; configuration = $configuration; buildPlatform = $buildPlatform; time = $time }
	$global:actions += $action
}

function Run-Obfuscate($configFile, $time)
{
	$action = @{ action = "obfuscate"; configFile = $configFile; time = $time }
	$global:actions += $action
}

function Run-Dotfuscate($configFile, $time)
{
	$action = @{ action = "dotfuscate"; configFile = $configFile; time = $time }
	$global:actions += $action
}

function Run-MSTest($metadataFile, $runConfig, $testList, $resultFile, $time)
{
	$action = @{ action = "mstest"; metadataFile = $metadataFile; runConfig = $runConfig; testList = $testList; resultFile = $resultFile; time = $time }
	$global:actions += $action
}

function Create-Setup($configFile, $configuration, $time)
{
	$action = @{ action = "setup"; configFile = $configFile; configuration = $configuration; time = $time }
	$global:actions += $action
}

function Create-Archive($archive, $listFile, $time)
{
	$action = @{ action = "archive"; archive = $archive; listFile = $listFile; time = $time }
	$global:actions += $action
}

function Sign-File($file, $keyFile, $password, $time)
{
	$action = @{ action = "sign"; file = $file; keyFile = $keyFile; password = $password; time = $time }
	$global:actions += $action
}

function Exec-File($file, $params, $time)
{
	$action = @{ action = "exec"; file = $file; params = $params; time = $time }
	$global:actions += $action
}

function Copy-File($src, $dest, $time)
{
	$action = @{ action = "copy"; src = $src; dest = $dest; time = $time }
	$global:actions += $action
}

function Delete-File($file, $time)
{
	$action = @{ action = "del"; file = $file; time = $time }
	$global:actions += $action
}

function Git-Commit($time)
{
	$action = @{ action = "gitcommit"; time = $time }
	$global:actions += $action
}

function Svn-Commit($time)
{
	$action = @{ action = "svncommit"; time = $time }
	$global:actions += $action
}

function Git-Export($archive, $time)
{
	$action = @{ action = "gitexport"; archive = $archive; time = $time }
	$global:actions += $action
}

function Svn-Export($archive, $time)
{
	$action = @{ action = "svnexport"; archive = $archive; time = $time }
	$global:actions += $action
}

function Git-Log($logFile, $time)
{
	$action = @{ action = "gitlog"; logFile = $logFile; time = $time }
	$global:actions += $action
}

function Svn-Log($logFile, $time)
{
	$action = @{ action = "svnlog"; logFile = $logFile; time = $time }
	$global:actions += $action
}

# ==============================  ACTION IMPLEMENTATIONS  ==============================

function Do-Build-Solution($solution, $configuration, $buildPlatform, $progressAfter)
{
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Building $solution for $configuration|$buildPlatform..." -NoNewLine
	Write-Host -ForegroundColor DarkGray " (Do not press Ctrl+C now)"

	# Find the MSBuild binary
	if ((Get-Platform) -eq "x64")
	{
		if ($buildPlatform -eq "x86")
		{
			$msbuildBin = Check-FileName "%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
		}
		else
		{
			$msbuildBin = Check-FileName "%windir%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
		}
	}
	if ((Get-Platform) -eq "x86")
	{
		$msbuildBin = Check-FileName "%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
	}
	if ($msbuildBin -eq $null)
	{
		WaitError "MSBuild binary not found"
		exit 1
	}

	# Set fastbuild and perform GitRevisionTool/SvnRevisionTool call once for the entire solution
	if ($global:gitUsed)
	{
		$env:FASTBUILD = "1"
		& ($toolsPath + "GitRevisionTool") --multi-project --assembly-info (MakeRootedPath($solution))
		if (-not $?)
		{
			WaitError "GitRevisionTool multi-project patch failed"
			exit 1
		}
	}
	if ($global:svnUsed)
	{
		$env:FASTBUILD = "1"
		& ($toolsPath + "SvnRevisionTool") --multi-project --assembly-info (MakeRootedPath($solution))
		if (-not $?)
		{
			WaitError "SvnRevisionTool multi-project patch failed"
			exit 1
		}
	}
	
	$mParam = "/m"
	if ($noParallelBuild)
	{
		# Parallel build must be disabled for overlapping projects in a solution
		$mParam = ""
	}
	
	# Other MSBuild options:
	#   /v:quiet
	#   /clp:ErrorsOnly
	#   /p:NoWarn=1591;0618;0659;0108
	# CS warning numbers:
	#   0108: Missing "new" keyword
	#   0618: Obsolete stuff used
	#   0659: Overwriting Equals but not GetHashCode
	#   1591: Missing XML documentation for public type or member

	$buildError = $false
	& $msbuildBin /nologo (MakeRootedPath($solution)) /t:Rebuild /p:Configuration="$configuration" /p:Platform="$buildPlatform" /v:minimal /p:WarningLevel=1 $mParam
	if (-not $?)
	{
		$buildError = $true
	}
	& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter

	# Reset fastbuild and restore all version files
	if ($global:gitUsed)
	{
		$env:FASTBUILD = ""
		& ($toolsPath + "GitRevisionTool") --multi-project --restore (MakeRootedPath($solution))
		if (-not $?)
		{
			if ($buildError)
			{
				WaitError "Build and GitRevisionTool multi-project restore failed"
			}
			else
			{
				WaitError "GitRevisionTool multi-project restore failed"
			}
			exit 1
		}
	}
	if ($global:svnUsed)
	{
		$env:FASTBUILD = ""
		& ($toolsPath + "SvnRevisionTool") --multi-project --restore (MakeRootedPath($solution))
		if (-not $?)
		{
			if ($buildError)
			{
				WaitError "Build and SvnRevisionTool multi-project restore failed"
			}
			else
			{
				WaitError "SvnRevisionTool multi-project restore failed"
			}
			exit 1
		}
	}
	if ($buildError)
	{
		WaitError "Build failed"
		exit 1
	}
}

function Do-Run-Obfuscate($configFile, $progressAfter)
{
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Obfuscating $configFile..."

	# Find the Obfuscator binary
	# TODO

	# Rename new map file with current build version
	# TODO
	if ($mapFile.EndsWith(".xml"))
	{
		#Move-Item -Force "$mapFile" $mapFile.Replace(".xml", ".$revId.xml")
	}

	& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter
}

function Do-Run-Dotfuscate($configFile, $progressAfter)
{
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Dotfuscating $configFile..."

	# Find the Dotfuscator CLI binary
	if ((Get-Platform) -eq "x64")
	{
		$dotfuscatorBin = Check-FileName "%ProgramFiles(x86)%\Microsoft Visual Studio 10.0\PreEmptive Solutions\Dotfuscator Community Edition\dotfuscatorCLI.exe"
	}
	if ((Get-Platform) -eq "x86")
	{
		$dotfuscatorBin = Check-FileName "%ProgramFiles%\Microsoft Visual Studio 10.0\PreEmptive Solutions\Dotfuscator Community Edition\dotfuscatorCLI.exe"
	}
	if ($dotfuscatorBin -eq $null)
	{
		WaitError "Dotfuscator binary not found"
		exit 1
	}

	# Read Dotfuscator configuration
	$config = [xml](Get-Content (MakeRootedPath($configFile)))
	$mapDir = $config.SelectSingleNode("/dotfuscator/renaming/mapping/mapoutput/file/@dir").'#text'
	$mapFile = $mapDir + "\" + $config.SelectSingleNode("/dotfuscator/renaming/mapping/mapoutput/file/@name").'#text'
	$mapFile = $mapFile.Replace("`${configdir}", $sourcePath)
	if ($mapFile.EndsWith(".xml"))
	{
		# Find and delete previous map file
		$prevMapFile = $mapFile.Replace(".xml", ".0.xml")
		if (Test-Path "$prevMapFile")
		{
			Remove-Item "$prevMapFile"
		}
	}
	
	& $dotfuscatorBin /q "$sourcePath\$configFile" | where {
		$_ -notmatch "^Dotfuscator Community Edition Version " -and `
		$_ -notmatch "^Copyright .* PreEmptive Solutions, " -and `
		$_ -notmatch "^Mit dem Verwenden dieser Software stimmen Sie dem " -and `
		$_ -notmatch "^LIZENZIERT FÜR: " -and `
		$_ -notmatch "^SERIENNUMMER: " -and `
		$_ -notmatch "^\[Intelligente Verbergung\] "
	}
	if (-not $?)
	{
		WaitError "Dotfuscation failed"
		exit 1
	}

	# Rename new map file with current build version
	if ($mapFile.EndsWith(".xml"))
	{
		Move-Item -Force "$mapFile" $mapFile.Replace(".xml", ".$revId.xml")
	}

	& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter
}

function Do-Run-MSTest($metadataFile, $runConfig, $testList, $resultFile, $progressAfter)
{
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Running test $metadataFile, $runConfig, $testList..."

	# Find the MSTest binary
	if ((Get-Platform) -eq "x64")
	{
		$mstestBin = Check-FileName "%ProgramFiles(x86)%\Microsoft Visual Studio 10.0\Common7\IDE\MSTest.exe"
	}
	if ((Get-Platform) -eq "x86")
	{
		$mstestBin = Check-FileName "%ProgramFiles%\Microsoft Visual Studio 10.0\Common7\IDE\MSTest.exe"
	}
	if ($mstestBin -eq $null)
	{
		WaitError "MSTest binary not found"
		exit 1
	}

	$md = (MakeRootedPath($metadataFile))
	$rc = (MakeRootedPath($runConfig))
	$rf = (MakeRootedPath($resultFile))

	# Create result directory if it doesn't exist (MSTest requires it)
	$resultDir = [System.IO.Path]::GetDirectoryName("$rf")
	[void][System.IO.Directory]::CreateDirectory($resultDir)
	
	& $mstestBin /nologo /testmetadata:"$md" /runconfig:"$rc" /testlist:"$testList" /resultsfile:"$rf"
	if (-not $?)
	{
		WaitError "Test failed"
		exit 1
	}
	& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter
}

function Do-Create-Setup($configFile, $configuration, $progressAfter)
{
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
	& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter
}

function Do-Create-Archive($archive, $listFile, $progressAfter)
{
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
		if (Test-Path "$sourcePath\.tmp.archive")
		{
			Remove-Item "$sourcePath\.tmp.archive" -Recurse -ErrorAction Stop
		}

		# Create temp directory
		New-Item -ItemType Directory "$sourcePath\.tmp.archive" -ErrorAction Stop | Out-Null

		# Prepare all files in a temporary directory
		ForEach ($line in Get-Content (MakeRootedPath($listFile)) -ErrorAction Stop)
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
				New-Item -ItemType Directory -Path "$sourcePath\.tmp.archive\$dest" -Force -ErrorAction Stop | Out-Null
			}
			else
			{
				New-Item -ItemType File -Path "$sourcePath\.tmp.archive\$dest" -Force -ErrorAction Stop | Out-Null
			}
			Copy-Item -Recurse -Force (MakeRootedPath($src)) "$sourcePath\.tmp.archive\$dest" -ErrorAction Stop
		}

		# Delete previous archive if it exists
		if (Test-Path (MakeRootedPath($archive)))
		{
			Remove-Item (MakeRootedPath($archive)) -ErrorAction Stop
		}
	}
	catch
	{
		WaitError $_
		exit 1
	}

	Push-Location "$sourcePath\.tmp.archive"
	& $sevenZipBin a (MakeRootedPath($archive)) -mx=9 * | where {
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
	Remove-Item "$sourcePath\.tmp.archive" -Recurse

	& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter
}

function Do-Sign-File($file, $keyFile, $password, $progressAfter)
{
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Digitally signing file $file..."

	# Find the signtool binary
	$signtoolBin = Check-RegFilename "hklm:\SOFTWARE\Microsoft\Microsoft SDKs\Windows\v7.1A" "InstallationFolder"
	$signtoolBin = Check-Filename "$signtoolBin\Bin\signtool.exe"
	if ($signtoolBin -eq $null)
	{
		$signtoolBin = Check-RegFilename "hklm:\SOFTWARE\Microsoft\Microsoft SDKs\Windows\v7.0A" "InstallationFolder"
		$signtoolBin = Check-Filename "$signtoolBin\Bin\signtool.exe"
		if ($signtoolBin -eq $null)
		{
			WaitError "signtool binary not found"
			exit 1
		}
	}
	
	# Check if the password is to be found in a separate file
	if ($password.StartsWith("@"))
	{
		$password = gc (MakeRootedPath($password.SubString(1)))
	}

	$timestampServers = @(
		"http://timestamp.verisign.com/scripts/timstamp.dll",
		"http://timestamp.comodoca.com/authenticode",
		"http://timestamp.globalsign.com/scripts/timstamp.dll"
	)
	
	$didFail = $false
	:outer for ($retry = 3; $retry -gt 0; $retry--)
	{
		foreach ($timestampServer in $timestampServers)
		{
			$output = & $signtoolBin sign /f (MakeRootedPath($keyFile)) /p "$password" /t $timestampServer (MakeRootedPath($file)) 2>&1
			if ($?)
			{
				# Sucessful, leave this function
				if ($didFail)
				{
					Write-Host Success using timestamp server $timestampServer
				}
				break outer
			}
			# Show output
			$output = ($output -Join "`n").Replace("`n`n", "`n").TrimEnd()
			Write-Host $output
			$didFail = $true
			if (-not $output.Contains("specified timestamp server"))
			{
				# Unsuccessful, output does not indicate timestamping issue, treat as permanent error and exit
				WaitError "Digitally signing failed"
				exit 1
			}
			# Should be a timestamping issue, try something else
			Write-Host -ForegroundColor Yellow "Timestamp server $timestampServer failed, trying another one..."
		}
		if ($retry -gt 1)
		{
			Write-Host -ForegroundColor Yellow "All servers $timestampServer failed, retrying in a moment..."
			Start-Sleep -s 5
		}
	}
	if ($retry -eq 0)
	{
		WaitError "Digitally signing failed"
		exit 1
	}
	& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter
}

function Do-Exec-File($file, $params, $progressAfter)
{
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Executing $file $params..."

	# Wait until the started process has finished
	Invoke-Expression ((MakeRootedPath($file)) + " " + $params + " | Out-Host")
	if (-not $?)
	{
		WaitError "Execution failed"
		exit 1
	}
	& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter
}

function Do-Copy-File($src, $dest, $progressAfter)
{
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Copying $src to $dest..."

	Copy-Item (MakeRootedPath($src)) (MakeRootedPath($dest))
	if (-not $?)
	{
		WaitError "Copy failed"
		exit 1
	}
	& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter
}

function Do-Delete-File($file, $progressAfter)
{
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Deleting $file..."

	Remove-Item (MakeRootedPath($file))
	if (-not $?)
	{
		WaitError "Deletion failed"
		exit 1
	}
	& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter
}

function Do-Git-Commit($progressAfter)
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
	& $tgitBin /command:commit /path:"$sourcePath" | Out-Host
	if (-not $?)
	{
		WaitError "Git commit failed"
		exit 1
	}
	& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter
}

function Do-Svn-Commit($progressAfter)
{
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Subversion commit and update..."

	# Find the TortoiseProc binary
	$tsvnBin = Check-RegFilename "hklm:\SOFTWARE\TortoiseSVN" "ProcPath"
	if ($tsvnBin -eq $null)
	{
		WaitError "TortoiseProc binary not found"
		exit 1
	}
	
	# Wait until the started process has finished
	& $tsvnBin /command:commit /path:"$sourcePath" | Out-Host
	if (-not $?)
	{
		WaitError "Subversion commit failed"
		exit 1
	}

	& $tsvnBin /command:update /path:"$sourcePath" | Out-Host
	if (-not $?)
	{
		WaitError "Subversion update failed"
		exit 1
	}

	& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter
}

function Do-Git-Export($archive, $progressAfter)
{
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Git export to $archive..."

	if ($revId.Contains("+"))
	{
		Write-Host -ForegroundColor Yellow "Warning: The local working copy is modified! Uncommitted changes are NOT exported."
	}

	# Find the Git binary
	$gitBin = Check-RegFilename "hklm:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1" "InstallLocation"
	$gitBin = Check-Filename "$gitBin\bin\git.exe"
	if ($gitBin -eq $null)
	{
		$gitBin = Check-RegFilename "hklm:\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1" "InstallLocation"
		$gitBin = Check-Filename "$gitBin\bin\git.exe"
		if ($gitBin -eq $null)
		{
			WaitError "Git binary not found"
			exit 1
		}
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
	if (Test-Path "$sourcePath\.tmp.export")
	{
		Remove-Item "$sourcePath\.tmp.export" -Recurse -ErrorAction Stop
	}

	# Create temp directory
	New-Item -ItemType Directory "$sourcePath\.tmp.export" -ErrorAction Stop | Out-Null

	Push-Location "$sourcePath"
	& $gitBin checkout-index -a --prefix ".tmp.export\"
	if (-not $?)
	{
		WaitError "Git export failed"
		exit 1
	}
	Pop-Location

	# Delete previous archive if it exists
	if (Test-Path (MakeRootedPath($archive)))
	{
		Remove-Item (MakeRootedPath($archive)) -ErrorAction Stop
	}

	Push-Location "$sourcePath\.tmp.export"
	& $sevenZipBin a (MakeRootedPath($archive)) -mx=9 * | where {
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
	Remove-Item "$sourcePath\.tmp.export" -Recurse

	& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter
}

function Do-Svn-Export($archive, $progressAfter)
{
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Subversion export to $archive..."

	if ($revId.Contains("+"))
	{
		Write-Host -ForegroundColor Yellow "Warning: The local working copy is modified! Uncommitted changes are exported."
	}

	# Find the SVN binary
	$svnBin = Check-RegFilename "hklm:\SOFTWARE\TortoiseSVN" "Directory"
	$svnBin = Check-Filename "$svnBin\bin\svn.exe"
	if ($svnBin -eq $null)
	{
		WaitError "Tortoise SVN CLI binary not found"
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
	if (Test-Path "$sourcePath\.tmp.export")
	{
		Remove-Item "$sourcePath\.tmp.export" -Recurse -ErrorAction Stop
	}

	& $svnBin export -q "$sourcePath" "$sourcePath\.tmp.export"
	if (-not $?)
	{
		WaitError "Subversion export failed"
		exit 1
	}

	# Delete previous archive if it exists
	if (Test-Path (MakeRootedPath($archive)))
	{
		Remove-Item (MakeRootedPath($archive)) -ErrorAction Stop
	}

	Push-Location "$sourcePath\.tmp.export"
	& $sevenZipBin a (MakeRootedPath($archive)) -mx=9 * | where {
		$_ -notmatch "^7-Zip " -and `
		$_ -notmatch "^Scanning$" -and `
		$_ -notmatch "^Creating archive " -and `
		$_ -notmatch "^\s*$" -and `
		$_ -notmatch "^Compressing "
	}
	if (-not $?)
	{
		Pop-Location
		WaitError "Creating SVN export archive failed"
		exit 1
	}
	Pop-Location

	# Clean up
	Remove-Item "$sourcePath\.tmp.export" -Recurse

	& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter
}

function Do-Git-Log($logFile, $progressAfter)
{
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Git log dump..."
	
	if ($revId.Contains("+"))
	{
		Write-Host -ForegroundColor Yellow "Warning: The local working copy is modified!"
	}

	# Find the Git binary
	$gitBin = Check-RegFilename "hklm:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1" "InstallLocation"
	$gitBin = Check-Filename "$gitBin\bin\git.exe"
	if ($gitBin -eq $null)
	{
		$gitBin = Check-RegFilename "hklm:\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1" "InstallLocation"
		$gitBin = Check-Filename "$gitBin\bin\git.exe"
		if ($gitBin -eq $null)
		{
			WaitError "Git binary not found"
			exit 1
		}
	}

	# Read the output log file and determine the last added revision
	$data = ""
	$lastRev = ""
	if (Test-Path (MakeRootedPath($logFile)))
	{
		$data = [System.IO.File]::ReadAllText((MakeRootedPath($logFile)))
		if ($data -Match " - .+ \((.+)\)")
		{
			$lastRev = ([regex]::Match($data, " - .+ \((.+)\)")).Groups[1].Value
		}
	}
	
	Write-Host Adding log messages since commit $lastRev
	
	# Get last commit date
	$consoleEncoding = [System.Console]::OutputEncoding
	[System.Console]::OutputEncoding = [System.Text.Encoding]::UTF8
	Push-Location "$sourcePath"
	$commitDate = (& $gitBin log -1 --pretty=format:"%ai" 2>&1)
	if (-not $?)
	{
		Pop-Location
		WaitError "Git log failed for current commit date"
		exit 1
	}
	Pop-Location
	[System.Console]::OutputEncoding = $consoleEncoding
	if (-not [string]$commitDate)
	{
		Write-Host "No commit yet"
		& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter
		return
	}
	# DEBUG: Write-Host -ForegroundColor Yellow $commitDate
	$commitDate = $commitDate.Substring(0, 10)

	# Get last commit hash
	$consoleEncoding = [System.Console]::OutputEncoding
	[System.Console]::OutputEncoding = [System.Text.Encoding]::UTF8
	Push-Location "$sourcePath"
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
		& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter
		return
	}

	# Get log messages for the new revisions
	$consoleEncoding = [System.Console]::OutputEncoding
	[System.Console]::OutputEncoding = [System.Text.Encoding]::UTF8
	Push-Location "$sourcePath"
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
		& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter
		return
	}
	# DEBUG: Write-Host -ForegroundColor Yellow $logText

	# Extract non-empty lines from all returned messages
	$msgs = $logText | Foreach { $_.Trim() } | Where { $_ }
	# DEBUG: Write-Host -ForegroundColor Yellow ([String]::Join("`n", $msgs))

	# Format current date and revision and new messages
	$caption = $commitDate + " - " + $revId + " (" + $commitHash + ")"
	$newMsgs = $caption + "`r`n" + `
		("—" * $caption.Length) + "`r`n" + `
		[string]::Join("`r`n", $msgs).Replace("`r`r", "`r") + "`r`n`r`n"

	# Write back the complete file
	$data = ($newMsgs + $data).Trim() + "`r`n"
	[System.IO.File]::WriteAllText((MakeRootedPath($logFile)), $data)

	# Open file in editor for manual edits of the raw changes
	Invoke-Expression (MakeRootedPath($logFile))

	& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter
}

function Do-Svn-Log($logFile, $progressAfter)
{
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Subversion log dump..."
	
	if ($revId.Contains("+"))
	{
		Write-Host -ForegroundColor Yellow "Warning: The local working copy is modified!"
	}

	# Find the SVN binary
	$svnBin = Check-RegFilename "hklm:\SOFTWARE\TortoiseSVN" "Directory"
	$svnBin = Check-Filename "$svnBin\bin\svn.exe"
	if ($svnBin -eq $null)
	{
		WaitError "Tortoise SVN CLI binary not found"
		exit 1
	}
	
	# Read the output log file and determine the last added revision
	$data = ""
	$startRev = 1;
	if (Test-Path (MakeRootedPath($logFile)))
	{
		$data = [System.IO.File]::ReadAllText((MakeRootedPath($logFile)))
		if ($data -Match " - r([0-9]+)")
		{
			$startRev = [int]([regex]::Match($data, " - r([0-9]+)")).Groups[1].Value + 1
		}
	}

	Write-Host Adding log messages since revision $startRev
	
	# Get log messages for the new revisions
	$consoleEncoding = [System.Console]::OutputEncoding
	[System.Console]::OutputEncoding = [System.Text.Encoding]::UTF8
	Push-Location "$sourcePath"
	$xmlText = (& $svnBin log --xml -r ${startRev}:HEAD 2>&1)
	if (-not $?)
	{
		Pop-Location
		[System.Console]::OutputEncoding = $consoleEncoding
		WaitError "Subversion log failed"
		exit 1
	}
	Pop-Location
	[System.Console]::OutputEncoding = $consoleEncoding
	if (([string]$xmlText).Contains(": No such revision $startRev"))
	{
		Write-Host "No new messages"
		& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter
		return
	}
	# DEBUG: Write-Host -ForegroundColor Yellow $xmlText
	$xml = [xml]$xmlText

	# Extract non-empty lines from all returned messages
	$msgs = $xml.log.logentry.msg -split "`n" | Foreach { $_.Trim() } | Where { $_ }

	# Format current date and revision and new messages
	$date = $xml.SelectSingleNode("(/log/logentry)[last()]/date").InnerText
	$currentRev = $xml.SelectSingleNode("(/log/logentry)[last()]/@revision").Value
	$caption = $date.Substring(0, 10) + " - r" + $currentRev
	$newMsgs = $caption + "`r`n" + `
		("—" * $caption.Length) + "`r`n" + `
		[string]::Join("`r`n", $msgs).Replace("`r`r", "`r") + "`r`n`r`n"

	# Write back the complete file
	$data = ($newMsgs + $data).Trim() + "`r`n"
	[System.IO.File]::WriteAllText((MakeRootedPath($logFile)), $data)

	# Open file in editor for manual edits of the raw changes
	Invoke-Expression (MakeRootedPath($logFile))

	& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter
}

# ==============================  BUILD SCRIPT FUNCTIONS  ==============================

function Begin-BuildScript($projectTitle, $configParts, $batchMode = $false)
{
	$global:configParts = $configParts
	$global:batchMode = $batchMode

	#cmd /c color f0
	$Host.UI.RawUI.WindowTitle = "$projectTitle build"
	Clear-Host
	Write-Host -ForegroundColor White "$projectTitle build script"
	Write-Host ""

	$global:startTime = Get-Date
}

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
		$timeSum += $action.time
		$progressAfter = [int] ($timeSum / $totalTime * 100)
	
		switch ($action.action)
		{
			"build"
			{
				Do-Build-Solution $action.solution $action.configuration $action.buildPlatform $progressAfter
			}
			"obfuscate"
			{
				Do-Run-Obfuscate $action.configFile $progressAfter
			}
			"dotfuscate"
			{
				Do-Run-Dotfuscate $action.configFile $progressAfter
			}
			"mstest"
			{
				Do-Run-MSTest $action.metadataFile $action.runConfig $action.testList $action.resultFile $progressAfter
			}
			"setup"
			{
				Do-Create-Setup $action.configFile $action.configuration $progressAfter
			}
			"archive"
			{
				Do-Create-Archive $action.archive $action.listFile $progressAfter
			}
			"sign"
			{
				Do-Sign-File $action.file $action.keyFile $action.password $progressAfter
			}
			"exec"
			{
				Do-Exec-File $action.file $action.params $progressAfter
			}
			"copy"
			{
				Do-Copy-File $action.src $action.dest $progressAfter
			}
			"del"
			{
				Do-Delete-File $action.file $progressAfter
			}
			"gitcommit"
			{
				Do-Git-Commit $progressAfter
			}
			"svncommit"
			{
				Do-Svn-Commit $progressAfter
			}
			"gitexport"
			{
				Do-Git-Export $action.archive $progressAfter
			}
			"svnexport"
			{
				Do-Svn-Export $action.archive $progressAfter
			}
			"gitlog"
			{
				Do-Git-Log $action.logFile $progressAfter
			}
			"svnlog"
			{
				Do-Svn-Log $action.logFile $progressAfter
			}
		}
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
		& ($toolsPath + "FlashConsoleWindow") -progress 100
		Write-Host "Press any key to exit" -NoNewLine
		Wait-Key $false 10000 $true
		Write-Host ""
	}
	& ($toolsPath + "FlashConsoleWindow") -noprogress
}
