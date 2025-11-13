@echo off
echo =========================================
echo Pobieranie schematów i szablonów KSEF z serwera gov.pl
echo =========================================
echo.

echo 1. Pobieranie StrukturyDanych_v10-0E.xsd...
curl -o "Ksef\StrukturyDanych_v10-0E.xsd" "http://crd.gov.pl/xml/schematy/dziedzinowe/mf/2022/01/05/eD/DefinicjeTypy/StrukturyDanych_v10-0E.xsd"

if %ERRORLEVEL% EQU 0 (
    echo ? StrukturyDanych_v10-0E.xsd pobrany pomyœlnie
) else (
    echo ? B³¹d podczas pobierania StrukturyDanych_v10-0E.xsd
)

echo.
echo 2. Pobieranie ElementarneTypyDanych_v10-0E.xsd...
curl -o "Ksef\ElementarneTypyDanych_v10-0E.xsd" "http://crd.gov.pl/xml/schematy/dziedzinowe/mf/2022/01/05/eD/DefinicjeTypy/ElementarneTypyDanych_v10-0E.xsd"

if %ERRORLEVEL% EQU 0 (
    echo ? ElementarneTypyDanych_v10-0E.xsd pobrany pomyœlnie
) else (
    echo ? B³¹d podczas pobierania ElementarneTypyDanych_v10-0E.xsd
)

echo.
echo 3. Pobieranie WspolneSzablonyWizualizacji_v12-0E.xsl...
curl -o "Ksef\WspolneSzablonyWizualizacji_v12-0E.xsl" "http://crd.gov.pl/xml/schematy/dziedzinowe/mf/2022/01/07/eD/DefinicjeSzablony/WspolneSzablonyWizualizacji_v12-0E.xsl"

if %ERRORLEVEL% EQU 0 (
    echo ? WspolneSzablonyWizualizacji_v12-0E.xsl pobrany pomyœlnie
) else (
    echo ? B³¹d podczas pobierania WspolneSzablonyWizualizacji_v12-0E.xsl
    echo   Zostanie u¿yty minimalny szablon zastêpczy
)

echo.
echo 4. Pobieranie dodatkowych schematów (opcjonalne)...

echo   4a. Pobieranie KodyKrajow_v10-0E.xsd...
curl -o "Ksef\KodyKrajow_v10-0E.xsd" "http://crd.gov.pl/xml/schematy/dziedzinowe/mf/2022/01/05/eD/DefinicjeTypy/KodyKrajow_v10-0E.xsd" 2>nul

echo   4b. Pobieranie KodyUrzedowSkarbowych_v8-0E.xsd...
curl -o "Ksef\KodyUrzedowSkarbowych_v8-0E.xsd" "http://crd.gov.pl/xml/schematy/dziedzinowe/mf/2022/01/05/eD/KodyUrzedowSkarbowych/KodyUrzedowSkarbowych_v8-0E.xsd" 2>nul

echo   4c. Pobieranie KodyWalut_v1-0E.xsd...
curl -o "Ksef\KodyWalut_v1-0E.xsd" "http://crd.gov.pl/xml/schematy/dziedzinowe/mf/2021/12/23/eD/KodyWalut/KodyWalut_v1-0E.xsd" 2>nul

echo   ? Pobieranie dodatkowych schematów zakoñczone

echo.
echo =========================================
echo Status plików schematów i szablonów:
echo =========================================

if exist "Ksef\schemat.xsd" (
    echo ? schemat.xsd - g³ówny schemat KSEF
) else (
    echo ? schemat.xsd - BRAK
)

if exist "Ksef\StrukturyDanych_v10-0E.xsd" (
    echo ? StrukturyDanych_v10-0E.xsd - struktury danych gov.pl
) else (
    echo ? StrukturyDanych_v10-0E.xsd - BRAK
)

if exist "Ksef\ElementarneTypyDanych_v10-0E.xsd" (
    echo ? ElementarneTypyDanych_v10-0E.xsd - podstawowe typy gov.pl
) else (
    echo ? ElementarneTypyDanych_v10-0E.xsd - BRAK
)

if exist "Ksef\styl.xsl" (
    echo ? styl.xsl - szablon HTML faktury
) else (
    echo ? styl.xsl - BRAK
)

if exist "Ksef\WspolneSzablonyWizualizacji_v12-0E.xsl" (
    echo ? WspolneSzablonyWizualizacji_v12-0E.xsl - wspólne szablony gov.pl
) else (
    echo ? WspolneSzablonyWizualizacji_v12-0E.xsl - BRAK (bêdzie utworzony automatycznie)
)

echo.
echo Dodatkowe schematy (opcjonalne):

if exist "Ksef\KodyKrajow_v10-0E.xsd" (
    echo ? KodyKrajow_v10-0E.xsd - kody krajów
) else (
    echo ? KodyKrajow_v10-0E.xsd - brak (nazwy krajów nie bêd¹ wyœwietlane)
)

if exist "Ksef\KodyUrzedowSkarbowych_v8-0E.xsd" (
    echo ? KodyUrzedowSkarbowych_v8-0E.xsd - kody urzêdów skarbowych
) else (
    echo ? KodyUrzedowSkarbowych_v8-0E.xsd - brak (nazwy urzêdów nie bêd¹ wyœwietlane)
)

if exist "Ksef\KodyWalut_v1-0E.xsd" (
    echo ? KodyWalut_v1-0E.xsd - kody walut
) else (
    echo ? KodyWalut_v1-0E.xsd - brak (nazwy walut nie bêd¹ wyœwietlane)
)

echo.

if exist "Ksef\schemat.xsd" if exist "Ksef\StrukturyDanych_v10-0E.xsd" if exist "Ksef\ElementarneTypyDanych_v10-0E.xsd" if exist "Ksef\styl.xsl" (
    echo ? Wszystkie podstawowe pliki s¹ dostêpne!
    echo   API KSEF powinno teraz poprawnie walidowaæ XML i generowaæ HTML.
    echo.
    if exist "Ksef\WspolneSzablonyWizualizacji_v12-0E.xsl" (
        echo ? Pe³ne wsparcie dla transformacji HTML z oficjalnymi szablonami.
    ) else (
        echo ? Transformacja HTML bêdzie u¿ywa³a uproszczonych szablonów.
    )
) else (
    echo ? Niektóre podstawowe pliki s¹ niedostêpne.
    echo   Walidacja XML lub transformacja HTML mo¿e byæ niepe³na.
)

echo.
echo =========================================
echo Informacje dodatkowe:
echo =========================================
echo.
echo Jeœli pobieranie automatyczne nie powiod³o siê, mo¿esz pobraæ pliki rêcznie:
echo.
echo 1. StrukturyDanych_v10-0E.xsd:
echo    http://crd.gov.pl/xml/schematy/dziedzinowe/mf/2022/01/05/eD/DefinicjeTypy/StrukturyDanych_v10-0E.xsd
echo.
echo 2. ElementarneTypyDanych_v10-0E.xsd:
echo    http://crd.gov.pl/xml/schematy/dziedzinowe/mf/2022/01/05/eD/DefinicjeTypy/ElementarneTypyDanych_v10-0E.xsd
echo.
echo 3. WspolneSzablonyWizualizacji_v12-0E.xsl:
echo    http://crd.gov.pl/xml/schematy/dziedzinowe/mf/2022/01/07/eD/DefinicjeSzablony/WspolneSzablonyWizualizacji_v12-0E.xsl
echo.
echo 4. Dodatkowe schematy (opcjonalne, poprawiaj¹ czytelnoœæ HTML):
echo    - KodyKrajow_v10-0E.xsd
echo    - KodyUrzedowSkarbowych_v8-0E.xsd  
echo    - KodyWalut_v1-0E.xsd
echo.

pause