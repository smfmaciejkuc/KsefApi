@echo off
echo ===============================================
echo   KSeF CertificateData E2E Test Runner
echo ===============================================
echo.
echo Ten skrypt uruchomi nowy test E2E, ktory demonstruje
echo zaawansowana funkcjonalnosc CertificateData z inteligentna
echo detekcja granic PEM i generowaniem kodow QR.
echo.

cd /d "%~dp0"

echo Budowanie projektu...
dotnet build --configuration Release --verbosity quiet

if %ERRORLEVEL% neq 0 (
    echo.
    echo ? Budowanie nie powiodlo sie! Sprawdz komunikaty bledow powyzej.
    echo.
    pause
    exit /b %ERRORLEVEL%
)

echo.
echo ? Budowanie pomyslne! Uruchamianie testu E2E...
echo.

REM Uruchomienie konkretnego testu CertificateData
dotnet test --configuration Release --logger "console;verbosity=detailed" --filter "FullyQualifiedName~GenerateVerificationLinks_WithCertificateData_EndToEnd_Succeeds"

if %ERRORLEVEL% neq 0 (
    echo.
    echo ? Test nie powiodl sie lub nie zostal znaleziony!
    echo.
) else (
    echo.
    echo ? Test pomyslnie zakonczony!
    echo.
    echo Wygenerowane pliki w katalogu Data:
    echo - combined_certificate_data.pem (polaczone dane certyfikatu)
    echo - certificatedata_invoice_qr.png (kod QR dla faktury)
    echo - certificatedata_certificate_qr.png (kod QR dla certyfikatu)
    echo.
)

echo.
echo Nacisnij dowolny klawisz aby zakonczyc...
pause > nul