@echo off
rem ============================================================================
rem  Fetches the things that cannot live in this repository.
rem
rem    download_deps.bat                  find and unpack what it can
rem    download_deps.bat <archive>        use this FMOD archive
rem
rem  Right now that is FMOD's Android native libraries. They are not committed
rem  because FMOD's licence does not allow redistributing them, and they cannot
rem  be downloaded unattended because fmod.com puts every download behind an
rem  account. So this finds an archive you have already downloaded and unpacks
rem  the right .so for each ABI; if there isn't one, it tells you what to get
rem  and opens the page.
rem
rem  Everything else the build needs comes from NuGet, and the Android SDK is
rem  installed by build_all.bat.
rem ============================================================================

setlocal
cd /d "%~dp0"

echo.
echo [FMOD] Android native libraries
if "%~1"=="" (
  powershell -NoProfile -ExecutionPolicy Bypass -File "tools\fetch_fmod.ps1"
) else (
  powershell -NoProfile -ExecutionPolicy Bypass -File "tools\fetch_fmod.ps1" -Archive "%~1"
)
set FMODRESULT=%errorlevel%

echo.
if "%FMODRESULT%"=="0" (
  echo   Dependencies are ready. Run build_all.bat next.
) else (
  echo   FMOD is still missing - see above.
  echo   The build works without it; only the sound does not.
)

endlocal
exit /b 0
