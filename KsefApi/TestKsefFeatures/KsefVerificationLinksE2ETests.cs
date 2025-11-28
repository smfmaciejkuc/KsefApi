using CertificateManager;
using CertificateManager.Interfaces;
using CertificateManager.Models.QRCode;
using CertificateManager.Services;
using System.Security.Cryptography.X509Certificates;

namespace Tests.EndToEnd
{
    public class KsefVerificationLinksE2ETests
    {
        // Œcie¿ki do danych testowych
        private const string DataDir = @"Data";
        private const string PrivateKeyFile = @"test_5323452439.key";
        private const string PublicCertFile = @"test_5323452439.crt";
        private const string InvoiceXmlFile = @"5323452439-50351e5a-ddec-4aee-a1d1-2166954e5a43-fa3.xml";

        // Parametry testowe
        private const string SellerNip = "5323452439";
        private const string PfxPassword = "Certyfikat_5323452439"; // dowolne has³o testowe

        [Fact]
        public void GenerateVerificationLinks_EndToEnd_Succeeds()
        {
            // Arrange: us³ugi
            ICertificateService certificateService = new CertificateService();
            ICryptographyService cryptographyService = new CryptographyService();
            var verificationLinkService = new CertificateManager.Services.VerificationLinkService(KsefEnvironmentUris.TEST);

            // Arrange: œcie¿ki absolutne
            string keyPath = Path.Combine(DataDir, PrivateKeyFile);
            string crtPath = Path.Combine(DataDir, PublicCertFile);
            string xmlPath = Path.Combine(DataDir, InvoiceXmlFile);

            Assert.True(File.Exists(keyPath), $"Nie znaleziono pliku klucza: {keyPath}");
            Assert.True(File.Exists(crtPath), $"Nie znaleziono pliku certyfikatu: {crtPath}");
            Assert.True(File.Exists(xmlPath), $"Nie znaleziono pliku XML: {xmlPath}");

            // Act 1: Utwórz X509Certificate2 (PFX) z pary KEY + CRT (bez importu do Windows Store)
            X509Certificate2 signingCert = certificateService.CreateCertificateFromPem(
                pathKeyPem: keyPath,
                pathCertPem: crtPath,
                pfxPassword: PfxPassword,
                nonExportable: true,
                storeLocation: StoreLocation.CurrentUser
            );
            Assert.NotNull(signingCert);

            // Act 2: Za³aduj plik XML i policz SHA-256 (base64)
            byte[] xmlBytes = File.ReadAllBytes(xmlPath);
            string invoiceHashBase64 = cryptographyService.GetHashData(xmlBytes);
            Assert.False(string.IsNullOrWhiteSpace(invoiceHashBase64));

            // Act 3: Wygeneruj link do weryfikacji faktury
            DateTime issueDate = DateTime.Today;
            string invoiceVerificationUrl = verificationLinkService.BuildInvoiceVerificationUrl(
                nip: SellerNip,
                issueDate: issueDate,
                invoiceHash: invoiceHashBase64
            );
            Assert.StartsWith($"{KsefEnvironmentUris.TEST}/client-app/invoice/", invoiceVerificationUrl);
            Assert.Contains(SellerNip, invoiceVerificationUrl);

            // Act 4: Wygeneruj link do weryfikacji certyfikatu (offline)
            string certificateSerial = signingCert.SerialNumber;
            string certificateVerificationUrl = verificationLinkService.BuildCertificateVerificationUrl(
                sellerNip: SellerNip,
                contextIdentifierType: QRCodeContextIdentifierType.Nip,
                contextIdentifierValue: SellerNip,
                certificateSerial: certificateSerial,
                invoiceHash: invoiceHashBase64,
                signingCertificate: signingCert,
                privateKey: "" // opcjonalny parametr; jeœli klucz prywatny ju¿ w certyfikacie, mo¿na zostawiæ pusty
            );

            // Assert: podstawowe walidacje treœci URL
            Assert.StartsWith($"{KsefEnvironmentUris.TEST}/client-app/certificate/", certificateVerificationUrl);
            Assert.Contains(SellerNip, certificateVerificationUrl);
            Assert.Contains(certificateSerial, certificateVerificationUrl);

            // Diagnostyka
            Console.WriteLine("Invoice verification URL:");
            Console.WriteLine(invoiceVerificationUrl);
            Console.WriteLine("Certificate verification URL:");
            Console.WriteLine(certificateVerificationUrl);
        }
    }
}