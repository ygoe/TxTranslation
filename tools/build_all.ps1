#cmd /c color f0
$Host.UI.RawUI.WindowTitle = "TxEditor build"
Clear-Host
echo "Creating a release build for TxEditor"
echo ""

$startTime = Get-Date

# -----------------------------  SCRIPT CONFIGURATION  ----------------------------

# Set the following variable to the platform architecture you want to build for.
#
$buildPlatform = "x86"

# Set the following variables to 1 to include the respective steps, 0 to disable them.
#
$doBuild = $true
$doObfuscate = $false
$doSetup = $true

# Set the path to the source files.
#
$sourcePath = $MyInvocation.MyCommand.Definition | split-path -parent | split-path -parent

# Set the application version number. Disable for Git repository revision.
#
#$revId = "1.0"
#$revId = (gc $sourcePath\TxEditor\Properties\AssemblyInfo.cs | `
#    select-string -pattern "AssemblyInformationalVersion\(""(.+?)""\)").Matches[0].Groups[1].Value

# ---------------------------------------------------------------------------------

# -------------------------------  HELPER FUNCTIONS  ------------------------------

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
	echo ""
	.\FlashConsoleWindow -error
	.\FlashConsoleWindow
	Write-Host -ForegroundColor Red ("ERROR: " + $msg)
	Wait-Key
	.\FlashConsoleWindow -noprogress
}

# ---------------------------------------------------------------------------------

# ----------------------------  ENVIRONMENT DETECTION  ----------------------------

# Determine current Windows architecture (32/64 bit)
if ([System.Environment]::GetEnvironmentVariable("ProgramFiles(x86)") -ne $null)
{
	$arch = "x64"
}
else
{
	$arch = "x86"
}

.\FlashConsoleWindow -progress 5
if ($doBuild)
{
    # Find the MSBuild binary
    if ($arch -eq "x64")
    {
	    if ($buildPlatform -eq "x64")
        {
		    $msbuildBin = Check-FileName "%windir%\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe"
	    }
	    if ($buildPlatform -eq "x86")
        {
		    $msbuildBin = Check-FileName "%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
	    }
    }
    if ($arch -eq "x86")
    {
	    $msbuildBin = Check-FileName "%windir%\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe"
    }
    if ($msbuildBin -eq $null)
    {
	    WaitError "MSBuild binary not found"
	    return
    }
}

if ($doObfuscate)
{
    # Find the Dotfuscator CLI binary
    if ($arch -eq "x64")
    {
	    $dotfuscatorBin = Check-FileName "%ProgramFiles(x86)%\Microsoft Visual Studio 10.0\PreEmptive Solutions\Dotfuscator Community Edition\dotfuscatorCLI.exe"
    }
    if ($arch -eq "x86")
    {
	    $dotfuscatorBin = Check-FileName "%ProgramFiles%\Microsoft Visual Studio 10.0\PreEmptive Solutions\Dotfuscator Community Edition\dotfuscatorCLI.exe"
    }
    if ($dotfuscatorBin -eq $null)
    {
	    WaitError "Dotfuscator binary not found"
        return
    }
}

if ($doSetup)
{
    # Find the InnoSetup binary
    if ($arch -eq "x64")
    {
        $innosetupBin = Check-RegFilename "hklm:\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 5_is1" "InstallLocation"
        $innosetupBin = Check-Filename "$innosetupBin\ISCC.exe"
    }
    if ($arch -eq "x86")
    {
        $innosetupBin = Check-RegFilename "hklm:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Inno Setup 5_is1" "InstallLocation"
        $innosetupBin = Check-Filename "$innosetupBin\ISCC.exe"
    }
    if ($innosetupBin -eq $null)
    {
	    WaitError "InnoSetup binary not found"
        return
    }
}

if ($revId -eq $null)
{
    # Determine current repository revision
    $revId = & .\GitRevisionTool --format "{bmin:2013:4}-{commit:6}{!:+}" "$sourcePath"
    if ($revId -eq $null)
    {
	    WaitError "Repository revision could not be determined"
        return
    }
}
echo "Application version: $revId"

# ---------------------------------------------------------------------------------

# -------------------------------  PERFORM ACTIONS  -------------------------------

if ($doBuild)
{
    echo ""
    Write-Host -ForegroundColor DarkCyan "Building application..."

    & $msbuildBin /nologo "$sourcePath\TxTranslation.sln" /t:Rebuild /p:Configuration=Release /p:Platform="$buildPlatform" /v:minimal
    if (-not $?)
    {
	    WaitError "Build failed"
	    return
    }
	.\FlashConsoleWindow -progress 20
}

if ($doObfuscate)
{
	echo ""
    Write-Host -ForegroundColor DarkCyan "Obfuscating assembly..."

    if (Test-Path "$sourcePath\Dotfuscated\Map.0.xml")
    {
        Remove-Item "$sourcePath\Dotfuscated\Map.0.xml"
    }

	& $dotfuscatorBin /q "$sourcePath\Dotfuscator.xml" | where {
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
		return
	}
	.\FlashConsoleWindow -progress 80

    Move-Item -Force "$sourcePath\Dotfuscated\Map.xml" "$sourcePath\Dotfuscated\Map.$revId.xml"
}

if ($doSetup)
{
	echo ""
	Write-Host -ForegroundColor DarkCyan "Compiling setup..."

    & $innosetupBin /q "$sourcePath\Setup\Tx.iss" /dRevId=$revId
	if (-not $?)
    {
		WaitError "Creating setup failed"
		return
	}
	.\FlashConsoleWindow -progress 100
}

# ---------------------------------------------------------------------------------

$endTime = Get-Date
if ($PSVersionTable.CLRVersion.Major -ge 4)
{
    $duration = ($endTime - $startTime).ToString("h\:mm\:ss")
}
else
{
    $duration = ($endTime - $startTime).TotalSeconds.ToString("0") + " seconds"
}

echo ""
Write-Host -ForegroundColor DarkGreen "Build succeeded in $duration."
.\FlashConsoleWindow -progress 100
Write-Host "Press any key to exit" -NoNewLine
Wait-Key $false 10000 $true
Write-Host ""
.\FlashConsoleWindow -noprogress
