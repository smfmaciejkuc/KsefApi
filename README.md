# KSeF API

## Opis projektu

KSeF API to aplikacja webowa napisana w **ASP.NET Core (.NET 9)**, która udostêpnia funkcjonalnoœci do walidacji XML oraz transformacji dokumentów elektronicznych zgodnych ze standardem **KSeF (Krajowy System e-Faktur)**. 

G³ówne funkcjonalnoœci:
- ? **Walidacja XML** - sprawdzanie poprawnoœci dokumentów XML wzglêdem schematów XSD KSeF
- ?? **Transformacja XML do HTML** - konwertowanie dokumentów XML na czyteln¹ wersjê HTML
- ?? **Upload plików** - obs³uga przesy³ania plików XML przez formularz
- ?? **Monitoring statusu** - sprawdzanie dostêpnoœci wymaganych plików schematów

## Wymagania

- **.NET 9 SDK** lub nowszy
- **Curl** (opcjonalnie, do pobierania schematów)
- Pliki schematów KSeF w katalogu `Ksef/` (mog¹ byæ pobrane automatycznie)

## Instalacja i uruchomienie

### 1. Pobranie wymaganych schematów

Przed pierwszym uruchomieniem nale¿y pobraæ oficjalne schematy i szablony z serwera gov.pl:

```bash
# Na Windows:
download-schema.bat

# Na Linux/Mac (alternatywnie):
mkdir -p Ksef
curl -o "Ksef/StrukturyDanych_v10-0E.xsd" "http://crd.gov.pl/xml/schematy/dziedzinowe/mf/2022/01/05/eD/DefinicjeTypy/StrukturyDanych_v10-0E.xsd"
curl -o "Ksef/ElementarneTypyDanych_v10-0E.xsd" "http://crd.gov.pl/xml/schematy/dziedzinowe/mf/2022/01/05/eD/DefinicjeTypy/ElementarneTypyDanych_v10-0E.xsd"
curl -o "Ksef/WspolneSzablonyWizualizacji_v12-0E.xsl" "http://crd.gov.pl/xml/schematy/dziedzinowe/mf/2022/01/07/eD/DefinicjeSzablony/WspolneSzablonyWizualizacji_v12-0E.xsl"
```

**Uwaga:** Dodatkowo wymagane s¹ pliki `schemat.xsd` i `styl.xsl` w katalogu `Ksef/` - te pliki musz¹ byæ dostarczone lokalnie.

### 2. Uruchomienie aplikacji

```bash
# PrzejdŸ do katalogu projektu
cd KsefApi

# Przywróæ pakiety NuGet
dotnet restore

# Uruchom aplikacjê
dotnet run
```

### 3. Alternatywne sposoby uruchomienia

```bash
# HTTP (port 5228)
dotnet run --launch-profile http

# HTTPS (porty 7275 i 5228)
dotnet run --launch-profile https

# Docker
docker build -t ksef-api .
docker run -p 5228:8080 -p 7275:8081 ksef-api
```

## Dostêpne endpointy

Po uruchomieniu aplikacji API jest dostêpne pod adresem:
- **HTTP**: `http://localhost:5228`
- **HTTPS**: `https://localhost:7275`

### ?? Endpointy diagnostyczne

#### `GET /api/ksef/info`
Zwraca informacje o konfiguracji API i œrodowisku.

**OdpowiedŸ:**
```json
{
  "message": "KSeF API Configuration Info",
  "timestamp": "2024-01-15T10:30:00Z",
  "connection": {
    "scheme": "http",
    "host": "localhost:5228",
    "fullUrl": "http://localhost:5228"
  },
  "suggestedHttpVariable": "@KsefApi_HostAddress = http://localhost:5228"
}
```

#### `GET /api/ksef/ping`
Test po³¹czenia z API.

**OdpowiedŸ:**
```json
{
  "message": "KSeF API is running",
  "timestamp": "2024-01-15T10:30:00Z",
  "environment": "DESKTOP-PC",
  "version": "1.0.0"
}
```

#### `GET /api/ksef/status`
Sprawdza status wymaganych plików schematów i szablonów.

**OdpowiedŸ:**
```json
{
  "status": "OK",
  "message": "Wszystkie wymagane pliki s¹ dostêpne",
  "canValidate": true,
  "canTransform": true,
  "files": {
    "schemat.xsd": {
      "exists": true,
      "required": true,
      "description": "G³ówny schemat KSEF"
    }
  },
  "summary": {
    "requiredFiles": "4/4",
    "optionalFiles": "3/5"
  }
}
```

### ? Walidacja XML

#### `POST /api/ksef/validate`
Waliduje dokument XML wzglêdem schematów KSeF.

**Content-Type**: `application/json`

**Body**: XML jako string JSON (z escapowanymi cudzys³owami)

**Przyk³ad:**
```http
POST /api/ksef/validate
Content-Type: application/json

"<?xml version=\"1.0\" encoding=\"UTF-8\"?><Faktura xmlns=\"http://crd.gov.pl/wzor/2025/06/25/13775/\">...</Faktura>"
```

**OdpowiedŸ (sukces):**
```json
{
  "valid": true,
  "errors": [],
  "warnings": [],
  "message": "XML jest poprawny zgodnie ze schematem KSEF"
}
```

**OdpowiedŸ (b³¹d):**
```json
{
  "valid": false,
  "errors": ["B³¹d walidacji: Element 'InvalidElement' nie jest dozwolony (linia: 5)"],
  "warnings": [],
  "message": "XML zawiera b³êdy walidacji"
}
```

### ?? Transformacja do HTML

#### `POST /api/ksef/html`
Transformuje dokument XML KSeF do formatu HTML.

**Content-Type**: `application/json`

**Body**: XML jako string JSON

**Przyk³ad:**
```http
POST /api/ksef/html
Content-Type: application/json

"<?xml version=\"1.0\" encoding=\"UTF-8\"?><Faktura xmlns=\"http://crd.gov.pl/wzor/2025/06/25/13775/\">...</Faktura>"
```

**OdpowiedŸ:** HTML document (Content-Type: `text/html`)

#### `POST /api/ksef/upload`
Upload pliku XML przez formularz i transformacja do HTML.

**Content-Type**: `multipart/form-data`

**Body**: Plik XML jako `IFormFile`

### ?? Test endpoint

#### `POST /api/ksef/test`
Endpoint testowy do sprawdzania komunikacji.

**Content-Type**: `application/json`

**Body**: Dowolny obiekt JSON

**OdpowiedŸ:**
```json
{
  "received": "OK",
  "dataType": "JsonElement", 
  "timestamp": "2024-01-15T10:30:00Z"
}
```

## Przyk³ady u¿ycia

### Testowanie z u¿yciem pliku KsefApi.http

W projekcie znajduje siê plik `KsefApi.http` z gotowymi przyk³adami zapytañ do API, które mo¿na wykonaæ w Visual Studio Code z rozszerzeniem REST Client lub w Visual Studio.

### Curl

```bash
# Test ping
curl -X GET http://localhost:5228/api/ksef/ping

# Status plików
curl -X GET http://localhost:5228/api/ksef/status

# Walidacja XML
curl -X POST http://localhost:5228/api/ksef/validate \
  -H "Content-Type: application/json" \
  -d '"<?xml version=\"1.0\" encoding=\"UTF-8\"?>...</Faktura>"'

# Transformacja do HTML
curl -X POST http://localhost:5228/api/ksef/html \
  -H "Content-Type: application/json" \
  -d '"<?xml version=\"1.0\" encoding=\"UTF-8\"?>...</Faktura>"'
```

## Struktura plików

```
KsefApi/
??? Controllers/
?   ??? KsefController.cs          # G³ówny kontroler API
??? Properties/
?   ??? launchSettings.json        # Konfiguracja uruchomienia
??? Ksef/                          # Pliki schematów i szablonów
?   ??? schemat.xsd                # G³ówny schemat KSEF (wymagany)
?   ??? styl.xsl                   # Szablon XSL (wymagany)
?   ??? StrukturyDanych_v10-0E.xsd # Struktury danych gov.pl
?   ??? ElementarneTypyDanych_v10-0E.xsd # Typy bazowe gov.pl  
?   ??? WspolneSzablonyWizualizacji_v12-0E.xsl # Szablony gov.pl
??? Program.cs                     # Entry point aplikacji
??? KsefApi.csproj                # Plik projektu
??? download-schema.bat           # Skrypt pobierania schematów
??? KsefApi.http                  # Przyk³ady zapytañ HTTP
??? README.md                     # Dokumentacja
```

## Rozwi¹zywanie problemów

### B³êdy walidacji
- Upewnij siê, ¿e wszystkie wymagane pliki schematów s¹ obecne w katalogu `Ksef/`
- Uruchom `download-schema.bat` aby pobraæ brakuj¹ce pliki
- SprawdŸ status plików przez endpoint `/api/ksef/status`

### B³êdy transformacji HTML
- SprawdŸ czy plik `styl.xsl` istnieje w katalogu `Ksef/`
- Jeœli brakuje `WspolneSzablonyWizualizacji_v12-0E.xsl`, zostanie automatycznie utworzony minimalny szablon zastêpczy

### Problemy z portami
- Domyœlnie aplikacja u¿ywa portów 5228 (HTTP) i 7275 (HTTPS)
- Porty mo¿na zmieniæ w pliku `Properties/launchSettings.json`

## Technologie

- **ASP.NET Core 9.0**
- **C# 13**
- **System.Xml** - do obs³ugi XML/XSD/XSLT
- **Docker** - obs³uga konteneryzacji

## Licencja

Projekt s³u¿y do obs³ugi dokumentów zgodnych ze standardem KSeF (Krajowy System e-Faktur) zgodnie z przepisami Ministerstwa Finansów RP.