# ?? Certificate Demo Launcher - Quick Start

## Running the Demo

### Option 1: Direct .NET CLI
```bash
cd TestKsefFeatures
dotnet run
```

### Option 2: Automated Mode
```bash
cd TestKsefFeatures
dotnet run -- --auto
```

### Option 3: Using Batch Script (Windows)
```bash
cd TestKsefFeatures
run-demo.bat
```

### Option 4: Using PowerShell (Cross-platform)
```powershell
cd TestKsefFeatures
.\run-demo.ps1
```

## Interactive Menu

The launcher provides an interactive menu with the following options:

```
=== KSeF Certificate Manager Demo Menu ===

Choose an option:
1. Run Full Demo (Recommended)
2. Test PEM Boundary Detection  
3. Test Different Key Formats
4. Test Messy Data Handling
5. Show Help
0. Exit
```

## What You'll See

### Full Demo Output Example:
```
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
```

## Key Features Demonstrated

- ? **No Artificial Separators** - Uses PEM boundary markers
- ? **Multiple Key Formats** - PKCS#8, PKCS#1 RSA, SEC1 EC
- ? **Messy Data Handling** - Extracts from complex input
- ? **Round-trip Safety** - Data integrity verification
- ? **Robust Validation** - Built-in PEM detection methods

## VS Code Integration

Use the provided launch configurations:
- **F5** to run with debugger
- **Ctrl+Shift+P** ? "Tasks: Run Task" ? "run-demo"

## Testing

The project still supports all xUnit tests:
```bash
dotnet test
```

## Requirements

- .NET 9 SDK
- Windows/macOS/Linux
- Console terminal for interactive experience