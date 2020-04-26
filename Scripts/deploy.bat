REM Builds and packages the release
REM
SETLOCAL EnableDelayedExpansion

if not exist "%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" (
  echo "WARNING: You need VS 2017 version 15.2 or later (for vswhere.exe)"
)

for /f "usebackq tokens=*" %%i in (`"%ProgramFiles(x86)%\Microsoft Visual Studio\Installer\vswhere.exe" -latest -products * -requires Microsoft.Component.MSBuild -property installationPath`) do (
  set InstallDir=%%i
)

if exist "!InstallDir!\Common7\Tools\VsMSBuildCmd.bat" (
  call "!InstallDir!\Common7\Tools\VsMSBuildCmd.bat"
) else (
  echo "Could not find "!InstallDir!\Common7\Tools\VsMSBuildCmd.bat"
)

rem go to current folder
cd %~dp0

msbuild CLS_Deploy.proj /target:Deploy
pause
