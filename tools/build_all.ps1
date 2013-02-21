#cmd /c color f0
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
$revId = (gc $sourcePath\TxEditor\Properties\AssemblyInfo.cs | `
    select-string -pattern "AssemblyInformationalVersion\(""(.+?)""\)").Matches[0].Groups[1].Value

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

function Wait-Key($msg = $true)
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
            Write-Host -NoNewline "Press any key to continue..."
        }
        [void][System.Console]::ReadKey($true)
        if ($msg)
        {
            Write-Host ""
        }
    }
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
	    echo ""
	    Write-Host -ForegroundColor Red "ERROR: MSBuild binary not found"
        Wait-Key
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
	    echo ""
	    Write-Host -ForegroundColor Red "ERROR: Dotfuscator binary not found"
        Wait-Key
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
	    echo ""
	    Write-Host -ForegroundColor Red "ERROR: InnoSetup binary not found"
        Wait-Key
        return
    }
}

if ($revId -eq $null)
{
    # Determine current repository revision
    #$revId = & .\GitRevisionTool --format "{bmin:2012}.{commit:8}{!:+}" "$sourcePath"
    if ($revId -eq $null)
    {
	    echo ""
	    Write-Host -ForegroundColor Red "ERROR: Repository revision could not be determined"
        Wait-Key
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
	    echo ""
	    Write-Host -ForegroundColor Red "ERROR: Build failed"
        Wait-Key
	    return
    }
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
		echo ""
        Write-Host -ForegroundColor Red "ERROR: Obfuscation failed"
        Wait-Key
		return
	}

    Move-Item -Force "$sourcePath\Dotfuscated\Map.xml" "$sourcePath\Dotfuscated\Map.$revId.xml"
}

if ($doSetup)
{
	echo ""
	Write-Host -ForegroundColor DarkCyan "Compiling setup..."

    & $innosetupBin /q "$sourcePath\Setup\TxEditor.iss" /dRevId=$revId
	if (-not $?)
    {
		echo ""
		Write-Host -ForegroundColor Red "ERROR: Creating setup failed"
        Wait-Key
		return
	}
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

& .\FlashConsoleWindow
echo ""
Write-Host -ForegroundColor Green "Build succeeded in $duration. Press any key to exit..."
Wait-Key $false
