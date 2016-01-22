# PowerShell build framework
# Copyright (c) 2016, Yves Goergen, http://unclassified.software/source/psbuild
#
# Copying and distribution of this file, with or without modification, are permitted provided the
# copyright notice and this notice are preserved. This file is offered as-is, without any warranty.

# The pdbconvert module provides FieldLog PdbConvert functions.

# Converts the .pdb debug symbols to an archive XML format for use with FieldLogViewer.
#
# $binary = The binary file name of the assembly (or assemblies) to read.
# $outFile = The file name of the converted output file. (Optional, can be empty)
# $options = Additional command-line options to PdbConvert. (Optional, can be empty)
#
# Requires $toolsPath\PdbConvert.exe.
#
function Pdb-Convert($binary, $outFile, $options, $time = 1)
{
	$action = @{ action = "Do-Pdb-Convert"; binary = $binary; outFile = $outFile; options = $options; time = $time }
	$global:actions += $action
}

# ==============================  FUNCTION IMPLEMENTATIONS  ==============================

function Do-Pdb-Convert($action)
{
	$binary = $action.binary
	$outFile = $action.outFile
	$options = $action.options
	
	Show-ActionHeader "Converting debug symbols"

	$binPath = (MakeRootedPath $binary)
	$params = "`"$binPath`" /srcbase `"$rootDir`" $options"
	if ($outFile)
	{
		$outPath = (MakeRootedPath $outFile)
		$params += " /outfile `"$outPath`""
	}

	Invoke-Expression ((Join-Path $absToolsPath "PdbConvert") + " " + $params)
	if ($LASTEXITCODE -ne 0)
	{
		WaitError "PdbConvert failed"
		exit 1
	}
}
