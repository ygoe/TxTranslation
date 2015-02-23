@echo off
set file=buildscript\psbuild.ps1
set config="transfer-web"

cd /d "%~dp0"
%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe -ExecutionPolicy unrestricted -File %file% %config%
exit /b %errorlevel%
