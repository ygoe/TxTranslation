@echo off
set file=buildscript\psbuild.ps1
set config="build"

cd /d "%~dp0"
%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe -ExecutionPolicy unrestricted -File %file% %config%
exit /b %errorlevel%
