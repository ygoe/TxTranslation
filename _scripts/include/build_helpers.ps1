# Build script helper functions

$toolsPath = "../_tools/"
$gitRevisionFormat = "{bmin:2013:4}.{commit:6}{!:+}"

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
			[void]$Host.UI.RawUI.ReadKey("IncludeKeyUp,IncludeKeyDown,NoEcho")
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
			while (!$Host.UI.RawUI.KeyAvailable -and ($counter -lt $timeout))
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
	$revId = & ($toolsPath + "GitRevisionTool") --format $global:gitRevisionFormat "$sourcePath"
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
	$revId = & ($toolsPath + "SvnRevisionTool") --format "{commit}{!:+}" "$sourcePath"
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
	$sourceFile = Check-FileName "$sourcePath\$sourceFile"
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

function Create-DistArchive($archive, $listFile, $time)
{
	$action = @{ action = "distarchive"; archive = $archive; listFile = $listFile; time = $time }
	$global:actions += $action
}

function Sign-File($file, $keyFile, $password, $time)
{
	$action = @{ action = "sign"; file = $file; keyFile = $keyFile; password = $password; time = $time }
	$global:actions += $action
}

function Do-Build-Solution($solution, $configuration, $buildPlatform, $progressAfter)
{
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Building $solution for $configuration|$buildPlatform..."

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
		& ($toolsPath + "GitRevisionTool") --multi-project --assembly-info "$sourcePath\$solution"
		if (-not $?)
		{
			WaitError "GitRevisionTool multi-project patch failed"
			exit 1
		}
	}
	if ($global:svnUsed)
	{
		$env:FASTBUILD = "1"
		& ($toolsPath + "SvnRevisionTool") --multi-project --assembly-info "$sourcePath\$solution"
		if (-not $?)
		{
			WaitError "SvnRevisionTool multi-project patch failed"
			exit 1
		}
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
	& $msbuildBin /nologo "$sourcePath\$solution" /t:Rebuild /p:Configuration="$configuration" /p:Platform="$buildPlatform" /v:minimal /p:WarningLevel=1 /m
	if (-not $?)
	{
		$buildError = $true
	}
	& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter

	# Reset fastbuild and restore all version files
	if ($global:gitUsed)
	{
		$env:FASTBUILD = ""
		& ($toolsPath + "GitRevisionTool") --multi-project --restore "$sourcePath\$solution"
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
		& ($toolsPath + "SvnRevisionTool") --multi-project --restore "$sourcePath\$solution"
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

	# Create result directory if it doesn't exist (MSTest requires it)
	$resultDir = [System.IO.Path]::GetDirectoryName("$sourcePath\$resultFile")
	[void][System.IO.Directory]::CreateDirectory($resultDir)
	
	& $mstestBin /nologo /testmetadata:"$sourcePath\$metadataFile" /runconfig:"$sourcePath\$runConfig" /testlist:"$testList" /resultsfile:"$sourcePath\$resultFile"
	if (-not $?)
	{
		WaitError "Test failed"
		exit 1
	}
	& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter
}

function Do-Run-Obfuscate($configFile, $progressAfter)
{
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Obfuscating $configFile..."

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
	$config = [xml](Get-Content "$sourcePath\$configFile")
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
		WaitError "Obfuscation failed"
		exit 1
	}

	# Rename new map file with current build version
	if ($mapFile.EndsWith(".xml"))
	{
		Move-Item -Force "$mapFile" $mapFile.Replace(".xml", ".$revId.xml")
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

	& $innosetupBin /q "$sourcePath\$configFile" /dBuildConfig=$configuration /dRevId=$revId
	if (-not $?)
	{
		WaitError "Creating setup failed"
		exit 1
	}
	& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter
}

function Do-Create-DistArchive($archive, $listFile, $progressAfter)
{
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Creating distribution archive $archive..."

	# Find the 7-Zip binary
	$sevenZipBin = Check-RegFilename "hklm:\SOFTWARE\7-Zip" "Path"
	$sevenZipBin = Check-Filename "$sevenZipBin\7z.exe"
	if ($sevenZipBin -eq $null)
	{
		WaitError "7-Zip binary not found"
		exit 1
	}

	Push-Location "$sourcePath"
	& $sevenZipBin a "$sourcePath\$archive" -t7z -mx=9 "@$sourcePath\$listFile"
	if (-not $?)
	{
		Pop-Location
		WaitError "Creating distribution archive failed"
		exit 1
	}
	Pop-Location
	& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter
}

function Do-Sign-File($file, $keyFile, $password, $progressAfter)
{
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Digitally signing file $file..."

	# Find the signtool binary
	$signtoolBin = Check-RegFilename "hklm:\SOFTWARE\Microsoft\Microsoft SDKs\Windows" "CurrentInstallFolder"
	$signtoolBin = Check-Filename "$signtoolBin\Bin\signtool.exe"
	if ($signtoolBin -eq $null)
	{
		WaitError "signtool binary not found"
		exit 1
	}
	
	# Check if the password is to be found in a separate file
	if ($password.StartsWith("@"))
	{
		$password = gc ("$sourcePath\" + $password.SubString(1))
	}

	& $signtoolBin sign /f "$sourcePath\$keyFile" /p "$password" /t http://timestamp.verisign.com/scripts/timstamp.dll "$sourcePath\$file"
	if (-not $?)
	{
		WaitError "Digitally signing failed"
		exit 1
	}
	& ($toolsPath + "FlashConsoleWindow") -progress $progressAfter
}

function Begin-BuildScript($projectTitle, $batchMode = $false)
{
	$global:batchMode = $batchMode

	#cmd /c color f0
	$Host.UI.RawUI.WindowTitle = "$projectTitle build"
	Clear-Host
	Write-Host "$projectTitle build script"
	Write-Host ""

	$global:startTime = Get-Date
}

function End-BuildScript()
{
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
			"mstest"
			{
				Do-Run-MSTest $action.metadataFile $action.runConfig $action.testList $action.resultFile $progressAfter
			}
			"obfuscate"
			{
				Do-Run-Obfuscate $action.configFile $progressAfter
			}
			"setup"
			{
				Do-Create-Setup $action.configFile $action.configuration $progressAfter
			}
			"distarchive"
			{
				Do-Create-DistArchive $action.archive $action.listFile $progressAfter
			}
			"sign"
			{
				Do-Sign-File $action.file $action.keyFile $action.password $progressAfter
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
	& ($toolsPath + "FlashConsoleWindow") -progress 100
	Write-Host "Press any key to exit" -NoNewLine
	Wait-Key $false 10000 $true
	Write-Host ""
	& ($toolsPath + "FlashConsoleWindow") -noprogress
}
