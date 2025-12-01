# Certificate Data Management - New Features

This document describes the new functionality added to the CertificateManager for handling certificate data combination, separation, and metadata extraction.

## New Features

### 1. Certificate and Key Data Combination

#### Methods Added to ICertificateService:
- `string CombineCertificateAndKey(string publicCertificate, string privateKey)` - Combines certificate and private key into a single string
- `CertificateData SeparateCertificateAndKey(string combinedData)` - Separates combined data back into certificate and key using PEM boundary detection
- `CertificateInfo ExtractCertificateInfo(X509Certificate2 certificate)` - Extracts metadata from X509Certificate2
- `CertificateInfo ExtractCertificateInfoFromPem(string certificatePem)` - Extracts metadata from PEM string

### 2. Value Objects

#### CertificateData
Represents combined certificate and private key data with intelligent PEM boundary detection:
```csharp
public class Certificate