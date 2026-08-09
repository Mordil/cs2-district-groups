@echo off
rem Builds the UI bundle. Node isn't on the system PATH in all contexts
rem (MSBuild, WSL interop), so prepend it explicitly.
set "PATH=C:\Program Files\nodejs;%PATH%"
cd /d "%~dp0"
call npm run build
