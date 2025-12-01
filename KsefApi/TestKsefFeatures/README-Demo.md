# Certificate Data Demo Launcher

This directory contains a console application launcher for demonstrating the improved CertificateData functionality with intelligent PEM boundary detection.

## Quick Start

### Option 1: Using Batch File (Windows)
```bash
run-demo.bat
```

### Option 2: Using PowerShell (Cross-platform)
```powershell
.\run-demo.ps1
```

### Option 3: Direct .NET CLI
```bash
# Build and run
dotnet run --project TestKsefFeatures.csproj

# Or run tests
dotnet test
```

## What the Demo Shows

The launcher runs `CertificateDataDemo.RunDemo()` which demonstrates:

1. **Certificate Data Creation** - Creating `CertificateData` objects from PEM strings
2. **Combination Without Separators** - Combining certificate and private key using PEM boundaries
3. **Intelligent Separation** - Separating combined data using regex-based PEM detection
4. **Validation Methods** - Testing `ContainsCertificate()` and `ContainsPrivateKey()` methods
5. **Messy Data Handling** - Extracting PEM blocks from data with extra content
6. **Multiple Key Formats** - Testing with different private key formats:
   - PKCS#8 (`-----BEGIN PRIVATE KEY-----`)
   - PKCS#1 RSA (`-----BEGIN RSA PRIVATE KEY-----`)
   - SEC1 EC (`-----BEGIN EC PRIVATE KEY-----`)

## Interactive Features

The launcher includes additional interactive demonstrations:
- **Format Testing** - Tests different private key formats
- **Data Integrity Checks** - Verifies round-trip data preservation
- **Error Handling** - Shows graceful handling of invalid data

## Project Configuration

The `TestKsefFeatures.csproj` has been configured to support both modes:
- **Console Application**: `OutputType=Exe` allows running as executable
- **Test Project**: Still supports `dotnet test` for running xUnit tests
- **Dual Mode**: `GenerateProgramFile=false` uses custom Program.cs

## Requirements

- .NET 9 SDK
- Windows, macOS, or Linux
- PowerShell (for .ps1 script)

## Example Output

```
=== KSeF Certificate Manager Demo Launcher ===
This demo showcases the improved CertificateData functionality
with intelligent PEM boundary detection.

=== Certificate Data Management Demo ===

1. Creating CertificateData object...
? Created successfully

2. Combining certificate and key...
Combined length: 256 characters
Contains certificate marker: True
Contains private key marker: True

3. Separating combined data using PEM boundary detection...
? Certificate extracted: 128 chars
? Private key extracted: 128 chars
? Data integrity: True

4. Testing PEM validation methods...
Certificate validation: True
Private key validation: True
Invalid data validation: True

5. Testing with messy data containing extra content...
? Successfully extracted PEM blocks from messy data
? Certificate matches: True
? Private key matches: True
? No extra content in certificate: True

=== Demo completed successfully! ===

=== Additional Interactive Demo ===

6. Interactive Certificate Data Testing...
Testing different private key formats:

? Testing with RSA private key...
  ? RSA PRIVATE KEY format: Successfully combined and separated
  ? Data integrity: True

? Testing with EC private key...
  ? EC PRIVATE KEY format: Successfully combined and separated
  ? Data integrity: True

? Testing with messy combined data...
  ? Messy data: Successfully extracted PEM blocks
  ? Extracted certificate length: 128 chars
  ? Extracted key length: 128 chars
  ? Extra text properly ignored

Press any key to exit...
```

## Related Files

- `Program.cs` - Main console application entry point
- `Examples/CertificateDataExamples.cs` - xUnit test examples
- `CertificateManager/Examples/CertificateDataDemo.cs` - Core demo implementation
- `CertificateManager/Models/CertificateData.cs` - Improved implementation with PEM detection