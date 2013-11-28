@echo off
cd /d "%~dp0"
C:\Windows\System32\WindowsPowerShell\v1.0\powershell.exe -ExecutionPolicy unrestricted -File include\build.ps1 "build-debug" %*
exit /b %errorlevel%
