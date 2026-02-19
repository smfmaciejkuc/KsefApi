using CertificateManager;
using CertificateManager.Interfaces;
using CertificateManager.Models;
using CertificateManager.Services;
using System.Security.Cryptography.X509Certificates;

namespace TestKsefFeatures
{
    public class CertificateServiceTests
    {
        [Fact]
        public void PemToDer_ExtractsBase64DataCorrectly()
        {
            // Arrange
            ICertificateService certificateService = new CertificateService();
            
            // U¿ywamy prawid³owej struktury PEM, ale z prostymi danymi testowymi
            const string pemCertificate = @"-----BEGIN CERTIFICATE-----
VGVzdCBkYXRhIGZvciBjZXJ0aWZpY2F0ZQo=
-----END CERTIFICATE-----";

            // Act
            byte[] derBytes = certificateService.PemToDer(pemCertificate, "CERTIFICATE");

            // Assert
            Assert.NotNull(derBytes);
            Assert.True(derBytes.Length > 0);
            
            // SprawdŸmy czy zosta³a wyekstraktowana poprawna zawartoœæ base64
            string expectedData = "Test data for certificate\n";
            string actualData = System.Text.Encoding.UTF8.GetString(derBytes);
            Assert.Equal(expectedData, actualData);
        }

        [Fact]
        public void PemToDer_ExtractsPrivateKeyDataCorrectly()
        {
            // Arrange
            ICertificateService certificateService = new CertificateService();
            
            const string pemPrivateKey = @"-----BEGIN PRIVATE KEY-----
VGVzdCBkYXRhIGZvciBwcml2YXRlIGtleQo=
-----END PRIVATE KEY-----";

            // Act
            byte[] derBytes = certificateService.PemToDer(pemPrivateKey, "PRIVATE KEY");

            // Assert
            Assert.NotNull(derBytes);
            Assert.True(derBytes.Length > 0);
            
            // SprawdŸmy zawartoœæ
            string expectedData = "Test data for private key\n";
            string actualData = System.Text.Encoding.UTF8.GetString(derBytes);
            Assert.Equal(expectedData, actualData);
        }

        [Fact]
        public void PemToDer_ThrowsExceptionForInvalidPem()
        {
            // Arrange
            ICertificateService certificateService = new CertificateService();
            const string invalidPem = "This is not a valid PEM format";

            // Act & Assert
            var exception = Assert.Throws<ArgumentException>(() => 
                certificateService.PemToDer(invalidPem, "CERTIFICATE"));
                
            Assert.Contains("Invalid PEM format", exception.Message);
        }

        [Fact]
        public void PemToDer_HandlesMessyPemCorrectly()
        {
            // Arrange
            ICertificateService certificateService = new CertificateService();
            
            const string messyPem = @"
            # This is a comment
            -----BEGIN CERTIFICATE-----
            VGVzdCBkYXRhIGZvciBjZXJ0aWZpY2F0ZQo=
            -----END CERTIFICATE-----
            # Final comment
            ";

            // Act
            byte[] derBytes = certificateService.PemToDer(messyPem, "CERTIFICATE");

            // Assert
            Assert.NotNull(derBytes);
            Assert.True(derBytes.Length > 0);
            
            // SprawdŸmy czy zosta³a poprawnie wyekstraktowana zawartoœæ pomimo dodatkowych znaków
            string expectedData = "Test data for certificate\n";
            string actualData = System.Text.Encoding.UTF8.GetString(derBytes);
            Assert.Equal(expectedData, actualData);
        }

        [Fact]
        public void PemToDer_HandlesMultiLineBase64()
        {
            // Arrange
            ICertificateService certificateService = new CertificateService();
            
            const string multiLinePem = @"-----BEGIN CERTIFICATE-----
VGVzdCBkYXRhIGZvciBjZXJ0aWZpY2F0ZSB3aXRoIG11bHRpcGxlIGxpbmVz
IGFuZCBsb25nZXIgY29udGVudCB0aGF0IHNwYW5zIG92ZXIgbXVsdGlwbGUg
bGluZXMgaW4gdGhlIFBFTSBmb3JtYXQNCg==
-----END CERTIFICATE-----";

            // Act
            byte[] derBytes = certificateService.PemToDer(multiLinePem, "CERTIFICATE");

            // Assert
            Assert.NotNull(derBytes);
            Assert.True(derBytes.Length > 0);
            
            // SprawdŸmy czy wszystkie linie zosta³y po³¹czone
            string actualData = System.Text.Encoding.UTF8.GetString(derBytes);
            Assert.Contains("Test data for certificate with multiple lines", actualData);
            Assert.Contains("and longer content", actualData);
        }

        [Fact]
        public void PemToDer_SupportsVariousSectionTypes()
        {
            // Arrange
            ICertificateService certificateService = new CertificateService();

            var testCases = new[]
            {
                ("CERTIFICATE", "-----BEGIN CERTIFICATE-----\nVGVzdA==\n-----END CERTIFICATE-----"),
                ("PRIVATE KEY", "-----BEGIN PRIVATE KEY-----\nVGVzdA==\n-----END PRIVATE KEY-----"),
                ("RSA PRIVATE KEY", "-----BEGIN RSA PRIVATE KEY-----\nVGVzdA==\n-----END RSA PRIVATE KEY-----"),
                ("PUBLIC KEY", "-----BEGIN PUBLIC KEY-----\nVGVzdA==\n-----END PUBLIC KEY-----"),
                ("CERTIFICATE REQUEST", "-----BEGIN CERTIFICATE REQUEST-----\nVGVzdA==\n-----END CERTIFICATE REQUEST-----")
            };

            foreach (var (section, pem) in testCases)
            {
                // Act
                byte[] derBytes = certificateService.PemToDer(pem, section);

                // Assert
                Assert.NotNull(derBytes);
                Assert.True(derBytes.Length > 0);
                
                // SprawdŸmy czy dane s¹ poprawne dla ka¿dego typu
                string actualData = System.Text.Encoding.UTF8.GetString(derBytes);
                Assert.Equal("Test", actualData);
            }
        }

        [Fact]
        public void PemToDer_WithRealTestFiles_CreatesValidCertificate()
        {
            // Arrange
            ICertificateService certificateService = new CertificateService();
            const string testCertFile = "Data/test_5323452439.crt";

            // Skip test jeœli plik nie istnieje
            if (!File.Exists(testCertFile))
            {
                return; // Test skipped gracefully
            }

            // Act
            string certContent = File.ReadAllText(testCertFile);
            byte[] derBytes = certificateService.PemToDer(certContent, "CERTIFICATE");

            // Assert
            Assert.NotNull(derBytes);
            Assert.True(derBytes.Length > 0);
            
            // SprawdŸmy czy mo¿na utworzyæ poprawny certyfikat z rzeczywistych danych
            using var cert = new X509Certificate2(derBytes);
            Assert.NotNull(cert);
            Assert.False(cert.HasPrivateKey);
            
            // SprawdŸmy podstawowe w³aœciwoœci certyfikatu
            Assert.False(string.IsNullOrWhiteSpace(cert.Subject));
            Assert.False(string.IsNullOrWhiteSpace(cert.SerialNumber));
            Assert.True(cert.NotAfter > DateTime.Now); // Certyfikat powinien byæ aktualny
            
            Console.WriteLine($"Test certificate loaded: {cert.Subject}");
            Console.WriteLine($"Serial: {cert.SerialNumber}");
            Console.WriteLine($"Valid until: {cert.NotAfter:yyyy-MM-dd}");
        }
    }
}