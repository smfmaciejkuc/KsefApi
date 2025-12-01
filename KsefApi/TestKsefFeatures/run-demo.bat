@echo off
echo ===============================================
echo   KSeF Certificate Manager Demo Launcher
echo ===============================================
echo.
echo This script will run the CertificateData demo
echo showcasing improved PEM boundary detection.
echo.
echo Press any key to start the demo...
pause > nul
echo.

cd /d "%~dp0"

echo Building project...
dotnet build --configuration Release

if %ERRORLEVEL% neq 0 (
    echo.
    echo ? Build failed! Please check the error messages above.
    echo.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ? Build successful! Starting demo...
echo.

dotnet run --project TestKsefFeatures.csproj --configuration Release

echo.
echo Demo completed. Press any key to exit...
pause > nul