using System;
using CertificateManager.Examples;
using CertificateManager.Models;

namespace TestKsefFeatures
{
    /// <summary>
    /// Console application launcher for demonstrating CertificateData functionality
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("=== KSeF Certificate Manager Demo Launcher ===");
            Console.WriteLine("This demo showcases the improved CertificateData functionality");
            Console.WriteLine("with intelligent PEM boundary detection.\n");

            // Check for command line arguments for automated execution
            if (args.Length > 0 && args[0].Equals("--auto", StringComparison.OrdinalIgnoreCase))
            {
                RunFullDemo();
                return;
            }

            // Interactive menu
            ShowMenu();
        }

        private static void ShowMenu()
        {
            bool continueRunning = true;

            while (continueRunning)
            {
                Console.Clear();
                Console.WriteLine("=== KSeF Certificate Manager Demo Menu ===\n");
                Console.WriteLine("Choose an option:");
                Console.WriteLine("1. Run Full Demo (Recommended)");
                Console.WriteLine("2. Test PEM Boundary Detection");
                Console.WriteLine("3. Test Different Key Formats");
                Console.WriteLine("4. Test Messy Data Handling");
                Console.WriteLine("5. Run CertificateData E2E Test");
                Console.WriteLine("6. Show Help");
                Console.WriteLine("0. Exit");
                Console.Write("\nEnter your choice (0-6): ");

                string input = Console.ReadLine();

                try
                {
                    switch (input?.Trim())
                    {
                        case "1":
                            RunFullDemo();
                            break;
                        case "2":
                            TestPemBoundaryDetection();
                            break;
                        case "3":
                            TestKeyFormats();
                            break;
                        case "4":
                            TestMessyDataHandling();
                            break;
                        case "5":
                            RunCertificateDataE2ETest();
                            break;
                        case "6":
                            ShowHelp();
                            break;
                        case "0":
                            continueRunning = false;
                            continue;
                        default:
                            Console.WriteLine("\n? Invalid option. Please try again.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n? Error: {ex.Message}");
                }

                if (continueRunning)
                {
                    Console.WriteLine("\nPress any key to return to menu...");
                    Console.ReadKey();
                }
            }

            Console.WriteLine("\nThank you for using KSeF Certificate Manager Demo!");
        }

        private static void RunFullDemo()
        {
            Console.Clear();
            Console.WriteLine("=== Running Full Demo ===\n");
            
            try
            {
                // Run the main CertificateData demonstration
                CertificateDataDemo.RunDemo();
                
                Console.WriteLine("\n=== Additional Interactive Demo ===");
                RunInteractiveDemo();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n? Error during demo execution: {ex.Message}");
            }
        }

        private static void RunCertificateDataE2ETest()
        {
            Console.Clear();
            Console.WriteLine("=== Running CertificateData E2E Test ===\n");
            
            Console.WriteLine("This will run the comprehensive E2E test that:");
            Console.WriteLine("• Loads certificate and key files");
            Console.WriteLine("• Creates CertificateData object");
            Console.WriteLine("• Saves combined data to file");
            Console.WriteLine("• Generates verification links");
            Console.WriteLine("• Creates QR codes for invoice and certificate");
            Console.WriteLine("");
            
            Console.Write("Do you want to continue? (y/n): ");
            string response = Console.ReadLine();
            
            if (response?.ToLower().StartsWith("y") == true)
            {
                Console.WriteLine("\nStarting E2E test execution...\n");
                
                try
                {
                    // Run the E2E test using dotnet test command
                    var processStartInfo = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = "test --configuration Release --logger \"console;verbosity=detailed\" --filter \"FullyQualifiedName~GenerateVerificationLinks_WithCertificateData_EndToEnd_Succeeds\"",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true
                    };

                    using (var process = System.Diagnostics.Process.Start(processStartInfo))
                    {
                        string output = process.StandardOutput.ReadToEnd();
                        string error = process.StandardError.ReadToEnd();
                        
                        process.WaitForExit();
                        
                        Console.WriteLine(output);
                        if (!string.IsNullOrWhiteSpace(error))
                        {
                            Console.WriteLine("Errors:");
                            Console.WriteLine(error);
                        }
                        
                        if (process.ExitCode == 0)
                        {
                            Console.WriteLine("\n? E2E test completed successfully!");
                            Console.WriteLine("\nGenerated files in Data directory:");
                            Console.WriteLine("• combined_certificate_data.pem");
                            Console.WriteLine("• certificatedata_invoice_qr.png");
                            Console.WriteLine("• certificatedata_certificate_qr.png");
                        }
                        else
                        {
                            Console.WriteLine($"\n? E2E test failed with exit code: {process.ExitCode}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"\n? Error running E2E test: {ex.Message}");
                    Console.WriteLine("\nAlternatively, you can run the test manually with:");
                    Console.WriteLine("dotnet test --filter \"FullyQualifiedName~GenerateVerificationLinks_WithCertificateData_EndToEnd_Succeeds\"");
                }
            }
            else
            {
                Console.WriteLine("E2E test cancelled.");
            }
        }

        private static void TestPemBoundaryDetection()
        {
            Console.Clear();
            Console.WriteLine("=== Testing PEM Boundary Detection ===\n");

            const string cert = @"-----BEGIN CERTIFICATE-----
MIIDXTCCAkWgAwIBAgIJAJC1HiIAZAiIMA0GCSqGSIb3DQEBBQUAMEU
-----END CERTIFICATE-----";

            const string key = @"-----BEGIN PRIVATE KEY-----
MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC7VJT
-----END PRIVATE KEY-----";

            Console.WriteLine("Testing PEM validation methods:");
            Console.WriteLine($"? Certificate detected: {CertificateData.ContainsCertificate(cert)}");
            Console.WriteLine($"? Private key detected: {CertificateData.ContainsPrivateKey(key)}");
            Console.WriteLine($"? Invalid data rejected: {!CertificateData.ContainsCertificate("invalid")}");
            
            var certData = new CertificateData(cert, key);
            string combined = certData.ToCombinedString();
            var separated = CertificateData.FromCombinedString(combined);
            
            Console.WriteLine($"\n? Round-trip successful: {separated.PublicCertificate == cert && separated.PrivateKey == key}");
            Console.WriteLine($"? Combined data length: {combined.Length} characters");
        }

        private static void TestKeyFormats()
        {
            Console.Clear();
            Console.WriteLine("=== Testing Different Key Formats ===\n");

            const string cert = @"-----BEGIN CERTIFICATE-----
MIIDXTCCAkWgAwIBAgIJAJC1HiIAZAiIMA0GCSqGSIb3DQEBBQUAMEU
-----END CERTIFICATE-----";

            const string pkcs8Key = @"-----BEGIN PRIVATE KEY-----
MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC7VJT
-----END PRIVATE KEY-----";

            const string rsaKey = @"-----BEGIN RSA PRIVATE KEY-----
MIIEowIBAAKCAQEAuVSU95EZwcwxNbOTrm5h7ntF7LP4rqGhqt4n
-----END RSA PRIVATE KEY-----";

            const string ecKey = @"-----BEGIN EC PRIVATE KEY-----
MHcCAQEEII3S7RGRzTaHBAAyQQo5TXmKdVCQsd3N0WLr8Hqr3F4D
-----END EC PRIVATE KEY-----";

            var keyFormats = new[]
            {
                ("PKCS#8", pkcs8Key),
                ("PKCS#1 RSA", rsaKey),
                ("SEC1 EC", ecKey)
            };

            foreach (var (format, key) in keyFormats)
            {
                Console.WriteLine($"Testing {format} format:");
                try
                {
                    var certData = new CertificateData(cert, key);
                    var combined = certData.ToCombinedString();
                    var restored = CertificateData.FromCombinedString(combined);
                    
                    Console.WriteLine($"  ? Successfully processed {format}");
                    Console.WriteLine($"  ? Data integrity verified: {restored.PrivateKey == key}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ? Failed: {ex.Message}");
                }
                Console.WriteLine();
            }
        }

        private static void TestMessyDataHandling()
        {
            Console.Clear();
            Console.WriteLine("=== Testing Messy Data Handling ===\n");

            const string cert = @"-----BEGIN CERTIFICATE-----
MIIDXTCCAkWgAwIBAgIJAJC1HiIAZAiIMA0GCSqGSIb3DQEBBQUAMEU
-----END CERTIFICATE-----";

            const string key = @"-----BEGIN PRIVATE KEY-----
MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC7VJT
-----END PRIVATE KEY-----";

            string messyData = $@"
# This is a configuration file
# Generated on {DateTime.Now}

{cert}

# Configuration continues here
# Private key section below:

{key}

# End of file
";

            Console.WriteLine("Testing extraction from messy data:");
            Console.WriteLine("Input contains extra comments and formatting...\n");

            try
            {
                var extracted = CertificateData.FromCombinedString(messyData);
                
                Console.WriteLine("? Successfully extracted PEM blocks");
                Console.WriteLine($"? Certificate integrity: {extracted.PublicCertificate == cert}");
                Console.WriteLine($"? Private key integrity: {extracted.PrivateKey == key}");
                Console.WriteLine($"? Extra content ignored: {!extracted.PublicCertificate.Contains("configuration")}");
                Console.WriteLine($"\nExtracted certificate length: {extracted.PublicCertificate.Length} chars");
                Console.WriteLine($"Extracted key length: {extracted.PrivateKey.Length} chars");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Extraction failed: {ex.Message}");
            }
        }

        private static void ShowHelp()
        {
            Console.Clear();
            Console.WriteLine("=== Help Information ===\n");
            
            Console.WriteLine("This demo showcases the improved CertificateData class with:");
            Console.WriteLine("• Intelligent PEM boundary detection");
            Console.WriteLine("• Support for multiple private key formats");
            Console.WriteLine("• Robust handling of messy input data");
            Console.WriteLine("• No artificial separators needed\n");
            
            Console.WriteLine("Key Features:");
            Console.WriteLine("1. PEM Boundary Detection - Uses standard markers instead of custom separators");
            Console.WriteLine("2. Multiple Key Formats - Supports PKCS#8, PKCS#1 RSA, SEC1 EC");
            Console.WriteLine("3. Data Validation - Built-in methods to validate PEM content");
            Console.WriteLine("4. Messy Data Handling - Extracts PEM blocks from complex input");
            Console.WriteLine("5. Round-trip Safety - Guarantees data integrity through combine/separate cycles");
            Console.WriteLine("6. E2E Integration - Full workflow with QR code generation\n");
            
            Console.WriteLine("Command Line Options:");
            Console.WriteLine("• Run with --auto flag for automated full demo");
            Console.WriteLine("• dotnet run --project TestKsefFeatures.csproj -- --auto\n");
            
            Console.WriteLine("E2E Test:");
            Console.WriteLine("• Option 5 runs a comprehensive test with real certificate files");
            Console.WriteLine("• Generates combined data file and QR codes");
            Console.WriteLine("• Demonstrates complete KSeF verification workflow\n");
            
            Console.WriteLine("For more information, see:");
            Console.WriteLine("• CertificateManager/README_NewFeatures.md");
            Console.WriteLine("• TestKsefFeatures/README-Demo.md");
            Console.WriteLine("• TestKsefFeatures/CertificateData-E2E-README.md");
        }

        private static void RunInteractiveDemo()
        {
            Console.WriteLine("\n6. Interactive Certificate Data Testing...");
            
            const string sampleCert = @"-----BEGIN CERTIFICATE-----
MIIDXTCCAkWgAwIBAgIJAJC1HiIAZAiIMA0GCSqGSIb3DQEBBQUAMEU
-----END CERTIFICATE-----";

            const string sampleRsaKey = @"-----BEGIN RSA PRIVATE KEY-----
MIIEowIBAAKCAQEAuVSU95EZwcwxNbOTrm5h7ntF7LP4rqGhqt4n
-----END RSA PRIVATE KEY-----";

            const string sampleEcKey = @"-----BEGIN EC PRIVATE KEY-----
MHcCAQEEII3S7RGRzTaHBAAyQQo5TXmKdVCQsd3N0WLr8Hqr3F4D
-----END EC PRIVATE KEY-----";

            Console.WriteLine("Testing different private key formats:");
            
            // Test RSA key
            Console.WriteLine("\n? Testing with RSA private key...");
            TestKeyFormat(sampleCert, sampleRsaKey, "RSA PRIVATE KEY");
            
            // Test EC key  
            Console.WriteLine("\n? Testing with EC private key...");
            TestKeyFormat(sampleCert, sampleEcKey, "EC PRIVATE KEY");
            
            // Test combined messy data
            Console.WriteLine("\n? Testing with messy combined data...");
            TestMessyData(sampleCert, sampleRsaKey);
        }

        private static void TestKeyFormat(string cert, string key, string keyType)
        {
            try
            {
                var certData = new CertificateData(cert, key);
                string combined = certData.ToCombinedString();
                var restored = CertificateData.FromCombinedString(combined);
                
                Console.WriteLine($"  ? {keyType} format: Successfully combined and separated");
                Console.WriteLine($"  ? Data integrity: {restored.PublicCertificate == cert && restored.PrivateKey == key}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ? {keyType} format failed: {ex.Message}");
            }
        }

        private static void TestMessyData(string cert, string key)
        {
            string messyData = $@"
# Configuration file header
# Generated on {DateTime.Now}

{cert}

# Some intermediate comments
# Private key follows below

{key}

# End of configuration
";
            
            try
            {
                var restored = CertificateData.FromCombinedString(messyData);
                Console.WriteLine("  ? Messy data: Successfully extracted PEM blocks");
                Console.WriteLine($"  ? Extracted certificate length: {restored.PublicCertificate.Length} chars");
                Console.WriteLine($"  ? Extracted key length: {restored.PrivateKey.Length} chars");
                Console.WriteLine("  ? Extra text properly ignored");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ? Messy data test failed: {ex.Message}");
            }
        }
    }
}