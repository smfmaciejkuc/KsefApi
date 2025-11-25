#!/usr/bin/env pwsh

# Skrypt do automatycznego wykrywania portu kontenera Docker
Write-Host "?? Wyszukiwanie kontenera KSeF API..." -ForegroundColor Cyan

# ZnajdŸ kontener z aplikacj¹
$container = docker ps --format "table {{.Names}}\t{{.Image}}\t{{.Ports}}" | Where-Object { $_ -like "*ksef*" -or $_ -like "*KsefApi*" }

if ($container) {
    Write-Host "? Znaleziono kontener:" -ForegroundColor Green
    Write-Host $container
    
    # Wyci¹gnij port z informacji o kontenerze
    $portInfo = docker ps --format "{{.Ports}}" | Where-Object { $_ -like "*8080*" } | Select-Object -First 1
    
    if ($portInfo -match "(\d+)->8080") {
        $externalPort = $matches[1]
        $newUrl = "http://localhost:$externalPort"
        
        Write-Host "?? Znaleziony port zewnêtrzny: $externalPort" -ForegroundColor Yellow
        Write-Host "?? Aktualizowanie KsefApi.http..." -ForegroundColor Cyan
        
        # Zaktualizuj plik KsefApi.http
        $httpFilePath = "KsefApi.http"
        if (Test-Path $httpFilePath) {
            $content = Get-Content $httpFilePath
            $updatedContent = $content -replace "@KsefApi_HostAddress = http://localhost:\d+", "@KsefApi_HostAddress = $newUrl"
            Set-Content $httpFilePath $updatedContent
            
            Write-Host "? Zaktualizowano adres na: $newUrl" -ForegroundColor Green
        } else {
            Write-Host "? Nie znaleziono pliku KsefApi.http" -ForegroundColor Red
        }
    } else {
        Write-Host "? Nie mo¿na wyci¹gn¹æ informacji o porcie" -ForegroundColor Red
        Write-Host "Informacja o portach: $portInfo" -ForegroundColor Yellow
    }
} else {
    Write-Host "? Nie znaleziono dzia³aj¹cego kontenera z KSeF API" -ForegroundColor Red
    Write-Host "?? Uruchom aplikacjê w kontenerze Docker najpierw" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "?? Aktualne kontenery Docker:" -ForegroundColor Cyan
docker ps --format "table {{.Names}}\t{{.Image}}\t{{.Ports}}\t{{.Status}}"