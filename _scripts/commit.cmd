@echo off
cd /d "%~dp0"
%SystemRoot%\System32\WindowsPowerShell\v1.0\powershell.exe -ExecutionPolicy unrestricted -File buildscript\psbuild.ps1 "commit" %*
exit /b %errorlevel%
