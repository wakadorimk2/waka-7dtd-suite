@echo off
setlocal
set "PROFILE_PATH=%~1"
if "%PROFILE_PATH%"=="" set "PROFILE_PATH=C:\Modding\MO2\profiles\Default"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0deploy.ps1" "%PROFILE_PATH%" %2 %3 %4
echo.
echo Press any key to close...
pause >nul
