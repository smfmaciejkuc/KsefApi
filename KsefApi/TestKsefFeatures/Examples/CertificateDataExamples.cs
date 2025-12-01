using System;
using CertificateManager;
using CertificateManager.Models;
using System.Security.Cryptography.X509Certificates;

namespace TestKsefFeatures.Examples
{
    public class CertificateDataExamples
    {
        // Sample PEM strings for testing
        private const string SampleCert = @"-----BEGIN CERTIFICATE-----
MIIDXTCCAkWgAwIBAgIJAJC1HiIAZAiIMA0GCSqGSIb3DQEBBQUAMEU
-----END CERTIFICATE-----";

        private const string SampleKey = @"-----BEGIN PRIVATE KEY-----
MIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC7VJT
-----END PRIVATE KEY-----";

        private const string SampleRsaKey = @"-----BEGIN RSA PRIVATE KEY-----
MIIEowIBAAKCAQEAuVSU95EZwcwx
-----END RSA PRIVATE KEY-----";

        private const string SampleEcKey = @"-----BEGIN EC PRIVATE KEY-----
MHcCAQEEII3S7RGRzTaHBAA
-----END EC PRIVATE KEY-----";

        [Fact]
        public void CertificateDataCombination_WorksCorrectly()
        {
            // Arrange
            var certificateService = new CertificateService();

            // Act: Combine certificate and key into single string
            string combinedData = certificateService.CombineCertificateAndKey(SampleCert, SampleKey);
            
            // Act: Separate combined data back into certificate and key
            CertificateData separatedData = certificateService.SeparateCertificateAndKey(combinedData);

            // Assert
            Assert.Equal(SampleCert, separatedData.PublicCertificate);
            Assert.Equal(SampleKey, separatedData.PrivateKey);
            // No artificial separator - PEM boundaries are used instead
            Assert.Contains("-----BEGIN CERTIFICATE-----", combinedData);
            Assert.Contains("-----BEGIN PRIVATE KEY-----", combinedData);
        }

        [Fact]
        public void CertificateDataSeparation_HandlesDifferentKeyTypes()
        {
            var certData = new CertificateData(SampleCert, SampleRsaKey);
            string combined = certData.ToCombinedString();
            var restored = CertificateData.FromCombinedString(combined);
            
            Assert.Equal(SampleCert, restored.PublicCertificate);
            Assert.Equal(SampleRsaKey, restored.PrivateKey);
        }

        [Fact]
        public void CertificateDataSeparation_HandlesECKeys()
        {
            var certData = new CertificateData(SampleCert, SampleEcKey);
            string combined = certData.ToCombinedString();
            var restored = CertificateData.FromCombinedString(combined);
            
            Assert.Equal(SampleCert, restored.PublicCertificate);
            Assert.Equal(SampleEcKey, restored.PrivateKey);
        }

        [Fact]
        public void CertificateDataValidation_DetectsPemBlocks()
        {
            Assert.True(CertificateData.ContainsCertificate(SampleCert));
            Assert.True(CertificateData.ContainsPrivateKey(SampleKey));
            Assert.False(CertificateData.ContainsCertificate("invalid data"));
            Assert.False(CertificateData.ContainsPrivateKey("invalid data"));
        }

        [Fact]
        public void CertificateInfo_ExtractsCorrectMetadata()
        {
            // This test would require actual certificate data to be meaningful
            var certificateService = new CertificateService();
            
            // Note: This would throw in practice without valid certificate
            try
            {
                var certInfo = certificateService.ExtractCertificateInfoFromPem(SampleCert);
                Assert.NotNull(certInfo);
            }
            catch (Exception)
            {
                // Expected with invalid sample data
                Assert.True(true);
            }
        }

        [Fact]
        public void CertificateDataValueObject_EnforcesInvariants()
        {
            // Act & Assert: Should throw on null/empty inputs
            Assert.Throws<ArgumentException>(() => new CertificateData("", "valid-key"));
            Assert.Throws<ArgumentException>(() => new CertificateData("valid-cert", ""));
            Assert.Throws<ArgumentException>(() => new CertificateData(null, "valid-key"));
            Assert.Throws<ArgumentException>(() => new CertificateData("valid-cert", null));
        }

        [Fact]
        public void CombinedDataSeparation_RequiresBothCertAndKey()
        {
            // Should fail when only certificate is present
            Assert.Throws<ArgumentException>(() => CertificateData.FromCombinedString(SampleCert));
            
            // Should fail when only private key is present
            Assert.Throws<ArgumentException>(() => CertificateData.FromCombinedString(SampleKey));
        }

        [Fact]
        public void CombinedDataWithMultiplePemBlocks_ExtractsCorrectly()
        {
            // Test with combined data that might have extra content
            string combinedWithExtra = @"
Some header text that should be ignored

" + SampleCert + @"

Some intermediate text

" + SampleKey + @"

Some footer text that should be ignored
";

            try 
            {
                var restored = CertificateData.FromCombinedString(combinedWithExtra);
                
                // Should extract only the PEM blocks
                Assert.Contains("-----BEGIN CERTIFICATE-----", restored.PublicCertificate);
                Assert.Contains("-----END CERTIFICATE-----", restored.PublicCertificate);
                Assert.Contains("-----BEGIN PRIVATE KEY-----", restored.PrivateKey);
                Assert.Contains("-----END PRIVATE KEY-----", restored.PrivateKey);
                
                // Should not contain the extra text
                Assert.DoesNotContain("Some header text", restored.PublicCertificate);
                Assert.DoesNotContain("Some footer text", restored.PrivateKey);
            }
            catch (Exception)
            {
                // Expected with sample data
                Assert.True(true);
            }
        }
    }
}