# CertificateData E2E Test Documentation

## Nowy test E2E: `GenerateVerificationLinks_WithCertificateData_EndToEnd_Succeeds`

Ten test demonstruje kompletny workflow nowej funkcjonalnoœci `CertificateData` z inteligentn¹ detekcj¹ granic PEM.

## Co robi test

### Krok po kroku:

1. **Wczytanie plików** - Wczytuje `test_5323452439.crt` i `test_5323452439.key`
2. **Walidacja PEM** - Weryfikuje, ¿e pliki zawieraj¹ poprawne bloki PEM
3. **Utworzenie CertificateData** - £¹czy certyfikat i klucz w obiekt
4. **£¹czenie danych** - Scala dane bez sztucznych separatorów (tylko granice PEM!)
5. **Zapis do pliku** - Zapisuje po³¹czone dane do `Data/combined_certificate_data.pem`
6. **Weryfikacja integralnoœci** - Wczytuje plik z powrotem i weryfikuje integralnoœæ danych
7. **Ekstrakcja informacji** - Pobiera metadane o certyfikacie
8. **Utworzenie X509Certificate2** - Tworzy w pe³ni funkcjonalny certyfikat z kluczem prywatnym
9. **Obliczenie hash faktury** - Oblicza hash z pliku XML faktury
10. **Generowanie URL faktury** - Tworzy link do weryfikacji faktury
11. **QR kod faktury** - Generuje i zapisuje QR kod do `certificatedata_invoice_qr.png`
12. **Generowanie URL certyfikatu** - Tworzy podpisany link do weryfikacji certyfikatu
13. **QR kod certyfikatu** - Generuje i zapisuje QR kod do `certificatedata_certificate_qr.png`
14. **Test z zewnêtrznym kluczem** - Demonstruje u¿ycie zewnêtrznego klucza prywatnego

## Uruchomienie testu

### Opcja 1: Bezpoœrednio przy pomocy .NET CLI
```bash
cd TestKsefFeatures
dotnet test --filter "FullyQualifiedName~GenerateVerificationLinks_WithCertificateData_EndToEnd_Succeeds"
```

### Opcja 2: Skrypt batch (Windows)
```bash
run-certificatedata-e2e.bat
```

### Opcja 3: PowerShell (Wieloplatformowy)
```powershell
.\run-certificatedata-e2e.ps1
```

### Opcja 4: Wszystkie testy E2E
```bash
dotnet test --filter "Category=E2E"
```

## Oczekiwane wyjœcie

```
[CertificateData E2E] Krok 1: Wczytywanie plików certyfikatu i klucza...
[CertificateData E2E] Krok 2: Tworzenie obiektu CertificateData...
[CertificateData E2E] Krok 3: £¹czenie certyfikatu i klucza...
[CertificateData E2E] Po³¹czone dane maj¹ 3247 znaków
[CertificateData E2E] Krok 4: Zapisywanie po³¹czonych danych do pliku...
[CertificateData E2E] Krok 5: Weryfikacja integralnoœci danych...
[CertificateData E2E] ? Integralnoœæ danych potwierdzona
[CertificateData E2E] Krok 6: Tworzenie X509Certificate2 z publicznego certyfikatu...
[CertificateData E2E] Certyfikat dla: test_5323452439
[CertificateData E2E] Numer seryjny: ABC123...
[CertificateData E2E] Wa¿ny do: 2025-12-31
[CertificateData E2E] Krok 7: Tworzenie certyfikatu z kluczem prywatnym...
[CertificateData E2E] ? Certyfikat utworzony z kluczem prywatnym
[CertificateData E2E] Krok 8: Obliczanie hash faktury...
[CertificateData E2E] Hash faktury: xyz789...
[CertificateData E2E] Krok 9: Generowanie linku weryfikacji faktury...
[CertificateData E2E] URL faktury: https://ksef-test.mf.gov.pl/invoice/...
[CertificateData E2E] Krok 10: Generowanie QR kodu dla faktury...
[CertificateData E2E] ? QR kod faktury zapisany: Data/certificatedata_invoice_qr.png
[CertificateData E2E] Krok 11: Generowanie linku weryfikacji certyfikatu...
[CertificateData E2E] URL certyfikatu: https://ksef-test.mf.gov.pl/certificate/...
[CertificateData E2E] Krok 12: Generowanie QR kodu dla certyfikatu...
[CertificateData E2E] ? QR kod certyfikatu zapisany: Data/certificatedata_certificate_qr.png
[CertificateData E2E] Krok 13: Test z zewnêtrznym kluczem prywatnym...
[CertificateData E2E] ? Alternatywny URL z zewnêtrznym kluczem utworzony

[CertificateData E2E] === PODSUMOWANIE TESTU ===
? Po³¹czone dane zapisane: Data/combined_certificate_data.pem
? QR kod faktury: Data/certificatedata_invoice_qr.png
? QR kod certyfikatu: Data/certificatedata_certificate_qr.png
? Informacje o certyfikacie: test_5323452439 (wa¿ny do 2025-12-31)
? D³ugoœæ po³¹czonych danych: 3247 znaków
? Integralnoœæ danych zachowana: TAK
? Detekcja PEM granic dzia³a: TAK
? Podpis z zewnêtrznym kluczem: TAK
[CertificateData E2E] Test pomyœlnie zakoñczony!
```

## Generowane pliki

Po pomyœlnym uruchomieniu testu znajdziesz w katalogu `Data/`:

1. **`combined_certificate_data.pem`** - Po³¹czone dane certyfikatu i klucza prywatnego
   - U¿ywa tylko granic PEM do rozdzielenia
   - ¯adnych sztucznych separatorów!
   - Mo¿na wczytaæ z powrotem i podzieliæ u¿ywaj¹c `CertificateData.FromCombinedString()`

2. **`certificatedata_invoice_qr.png`** - QR kod dla linku weryfikacji faktury
   - Rozdzielczoœæ 300x300 pikseli
   - 16 pikseli na modu³
   - Zawiera URL do weryfikacji faktury w systemie KSeF

3. **`certificatedata_certificate_qr.png`** - QR kod dla linku weryfikacji certyfikatu
   - Te same parametry co QR kod faktury
   - Zawiera podpisany URL do weryfikacji certyfikatu
   - Podpis utworzony przy pomocy klucza prywatnego z po³¹czonych danych

## Kluczowe funkcje demonstrowane przez test

- ? **Inteligentna detekcja PEM** - ¯adnych sztucznych separatorów
- ? **£¹czenie i dzielenie** - Integralnoœæ pe³nego cyklu
- ? **Zapis do pliku** - Trwa³e przechowywanie po³¹czonych danych
- ? **Ekstrakcja metadanych** - Pozyskiwanie informacji o certyfikacie
- ? **Certyfikat z kluczem prywatnym** - W pe³ni funkcjonalny X509Certificate2
- ? **Zewnêtrzny klucz prywatny** - Podpisywanie z zewnêtrznym kluczem
- ? **Kody QR** - Generowanie dla faktury i certyfikatu
- ? **Integracja KSeF** - Funkcjonalne linki do systemu KSeF

## Wymagania

- Pliki `test_5323452439.crt` i `test_5323452439.key` w katalogu `Data/`
- Plik `5323452439-50351e5a-ddec-4aee-a1d1-2166954e5a43-fa3.xml` w katalogu `Data/`
- .NET 9 SDK
- Projekt CertificateManager z zaimplementowan¹ klas¹ `CertificateData`

## Rozwi¹zywanie problemów

Jeœli test nie powiedzie siê:

1. **SprawdŸ istnienie plików** - Upewnij siê, ¿e pliki testowe istniej¹ w `Data/`
2. **SprawdŸ uprawnienia** - Upewnij siê, ¿e masz uprawnienia do zapisu w katalogu `Data/`
3. **B³êdy budowania** - Uruchom `dotnet build` aby sprawdziæ b³êdy kompilacji
4. **Szczegó³owe wyjœcie** - U¿yj `--logger "console;verbosity=detailed"` dla wiêcej szczegó³ów