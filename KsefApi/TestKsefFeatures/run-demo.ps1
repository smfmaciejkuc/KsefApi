# KSeF Certificate Manager Demo Launcher
# PowerShell script for running the CertificateData demonstration

Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "   KSeF Certificate Manager Demo Launcher" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "This script will run the CertificateData demo" -ForegroundColor Green
Write-Host "showcasing improved PEM boundary detection." -ForegroundColor Green
Write-Host ""

# Change to script directory
Set-Location -Path $PSScriptRoot

Write-Host "Building project..." -ForegroundColor Yellow
$buildResult = dotnet build --configuration Release --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "? Build failed! Please check the error messages above." -ForegroundColor Red
    Write-Host ""
    Read-Host "Press Enter to exit"
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "? Build successful! Starting demo..." -ForegroundColor Green
Write-Host ""

# Run the demo
dotnet run --project TestKsefFeatures.csproj --configuration Release --verbosity quiet

Write-Host ""
Write-Host "Demo completed successfully!" -ForegroundColor Green
Read-Host "Press Enter to exit"