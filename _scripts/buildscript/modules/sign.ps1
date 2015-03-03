# PowerShell build framework
# Copyright (c) 2015, Yves Goergen, http://unclassified.software/source/psbuild
#
# Copying and distribution of this file, with or without modification, are permitted provided the
# copyright notice and this notice are preserved. This file is offered as-is, without any warranty.

# The sign module provides digital signing functions.

# Digitally signs a file.
#
# $file = The name of the file to sign.
# $keyFile = the name of the file that contains the private key for signing.
# $password = The password for the private key file.
#
# The key file and password should be passed from variables that are defined in the private
# configuration file which is excluded from the source code repository. Example:
#
#    Sign-File "My.exe" "$signKeyFile" "$signPassword" 2
#
# The file _scripts\buildscript\private.ps1 could contain this script:
#
#    $signKeyFile = (Join-Path $scriptDir "my_key.p12")
#    $signPassword = "secret"
#
# This function tries to use multiple timestamping servers. If one fails, another server is
# tried. If all fail, a second try is started after a few seconds delay. This process requires an
# internet connection.
#
# Requires Signtool from Visual Studio 2010/2012/2013 to be installed.
#
function Sign-File($file, $keyFile, $password, $time = 1)
{
	$action = @{ action = "Do-Sign-File"; file = $file; keyFile = $keyFile; password = $password; time = $time }
	$global:actions += $action
}

# ==============================  FUNCTION IMPLEMENTATIONS  ==============================

function Do-Sign-File($action)
{
	$file = $action.file
	$keyFile = $action.keyFile
	$password = $action.password
	
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
			$output = & $signtoolBin sign /f (MakeRootedPath $keyFile) /p "$password" /t $timestampServer (MakeRootedPath $file) 2>&1
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
}
