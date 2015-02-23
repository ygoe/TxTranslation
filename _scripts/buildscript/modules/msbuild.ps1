# PowerShell build framework
# Copyright (c) 2015, Yves Goergen, http://unclassified.software/source/psbuild
#
# Copying and distribution of this file, with or without modification, are permitted provided the
# copyright notice and this notice are preserved. This file is offered as-is, without any warranty.

# The msbuild module provides Microsoft MSBuild functions.

# Builds a Visual Studio solution.
#
# $solution = The file name of the solution to build.
# $configuration = The build configuration to select (e.g.: Debug, Release).
# $buildPlatform = The platform to select (e.g.: x86, x64, Any CPU).
#
# To disable parallel build, set the variable $noParallelBuild = $true.
#
# Requires MSBuild of the .NET Framework 4.0 to be installed.
#
function Build-Solution($solution, $configuration, $buildPlatform, $time)
{
	$action = @{ action = "Do-Build-Solution"; solution = $solution; configuration = $configuration; buildPlatform = $buildPlatform; time = $time }
	$global:actions += $action
}

# ==============================  FUNCTION IMPLEMENTATIONS  ==============================

function Do-Build-Solution($action)
{
	$solution = $action.solution
	$configuration = $action.configuration
	$buildPlatform = $action.buildPlatform
	
	Write-Host ""
	Write-Host -ForegroundColor DarkCyan "Building $solution for $configuration|$buildPlatform..." -NoNewLine
	if ($global:revisionToolUsed)
	{
		Write-Host -ForegroundColor DarkGray " (Do not press Ctrl+C now)" -NoNewLine
	}
	Write-Host ""

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

	# Set %SuppressNetRevisionTool% and perform NetRevisionTool call once for the entire solution
	if ($global:revisionToolUsed)
	{
		Invoke-Expression ((Join-Path $absToolsPath "NetRevisionTool") + " /multi /patch " + $global:revisionToolOptions + " `"" + (MakeRootedPath $solution) + "`"")
		if ($LASTEXITCODE -ne 0)
		{
			WaitError "NetRevisionTool multi-project patch failed"
			exit 1
		}
		$env:SuppressNetRevisionTool = "1"
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
	& $msbuildBin /nologo (MakeRootedPath $solution) /t:Rebuild /p:Configuration="$configuration" /p:Platform="$buildPlatform" /v:minimal /p:WarningLevel=1 $mParam
	if (-not $?)
	{
		$buildError = $true
	}

	# Reset %SuppressNetRevisionTool% and restore all version files
	if ($global:revisionToolUsed)
	{
		$env:SuppressNetRevisionTool = ""
		Invoke-Expression ((Join-Path $absToolsPath "NetRevisionTool") + " /multi /restore " + $global:revisionToolOptions + " `"" + (MakeRootedPath $solution) + "`"")
		if ($LASTEXITCODE -ne 0)
		{
			if ($buildError)
			{
				WaitError "Build and NetRevisionTool multi-project restore failed"
			}
			else
			{
				WaitError "NetRevisionTool multi-project restore failed"
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
