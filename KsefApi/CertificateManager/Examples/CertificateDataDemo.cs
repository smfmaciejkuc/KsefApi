using System;
using CertificateManager.Models;

namespace CertificateManager.Examples
{
    /// <summary>
    /// Demonstrates the improved CertificateData with PEM boundary detection
    /// </summary>
    public static class CertificateDataDemo
    {
        public static void RunDemo()
        {
            Console.WriteLine("=== Certificate Data Management Demo ===\n");

            // Sample certificate and key data
            string certificate = @"-----BEGIN CERTIFICATE-----
MIIDXTCCAkWgAwIBAgIJAJC1HiIAZAiIMA0GCSqGSIb3DQEBBQUAMEU
xCzAJBgNVBAYTAlVTMRUwEwYDVQQIDAxTYW1wbGUgU3RhdGUx
-----END CERTIFICATE-----";

            string privateKey = @"-----BEGIN PRIVATE KEY-----
MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC7VJT
NdZJL+cCKu1M8sQi9JJI8+3FlJ9x8HJ+6sWxFsNe7C2Nz4sFqGhx
-----END PRIVATE KEY-----";

            // 1. Create CertificateData object
            Console.WriteLine("1. Creating CertificateData object...");
            var certData = new CertificateData(certificate, privateKey);
            Console.WriteLine("? Created successfully\n");

            // 2. Combine into single string - no artificial separator!
            Console.WriteLine("2. Combining certificate and key...");
            string combined = certData.ToCombinedString();
            Console.WriteLine($"Combined length: {combined.Length} characters");
            Console.WriteLine($"Contains certificate marker: {combined.Contains("-----BEGIN CERTIFICATE-----")}");
            Console.WriteLine($"Contains private key marker: {combined.Contains("-----BEGIN PRIVATE KEY-----")}\n");

            // 3. Separate back using PEM boundary detection
            Console.WriteLine("3. Separating combined data using PEM boundary detection...");
            var separated = CertificateData.FromCombinedString(combined);
            Console.WriteLine($"? Certificate extracted: {separated.PublicCertificate.Length} chars");
            Console.WriteLine($"? Private key extracted: {separated.PrivateKey.Length} chars");
            Console.WriteLine($"? Data integrity: {separated.PublicCertificate == certificate && separated.PrivateKey == privateKey}\n");

            // 4. Test validation methods
            Console.WriteLine("4. Testing PEM validation methods...");
            Console.WriteLine($"Certificate validation: {CertificateData.ContainsCertificate(certificate)}");
            Console.WriteLine($"Private key validation: {CertificateData.ContainsPrivateKey(privateKey)}");
            Console.WriteLine($"Invalid data validation: {!CertificateData.ContainsCertificate("invalid")}\n");

            // 5. Test with messy data containing extra content
            Console.WriteLine("5. Testing with messy data containing extra content...");
            string messyData = $@"
# This is a comment
Some random text here

{certificate}

More random text in between

{privateKey}

Footer text that should be ignored
# End of file
";
            
            try 
            {
                var fromMessy = CertificateData.FromCombinedString(messyData);
                Console.WriteLine("? Successfully extracted PEM blocks from messy data");
                Console.WriteLine($"? Certificate matches: {fromMessy.PublicCertificate == certificate}");
                Console.WriteLine($"? Private key matches: {fromMessy.PrivateKey == privateKey}");
                Console.WriteLine($"? No extra content in certificate: {!fromMessy.PublicCertificate.Contains("random text")}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"? Error with messy data: {ex.Message}");
            }

            Console.WriteLine("\n=== Demo completed successfully! ===");
        }
    }
}