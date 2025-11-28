using CertificateManager;
using CertificateManager.Interfaces;
using CertificateManager.Models.QRCode;
using CertificateManager.Services;
using System.Security.Cryptography.X509Certificates;

namespace TestKsefFeatures.E2E
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
            var verificationLinkService = new VerificationLinkService(KsefEnvironmentUris.TEST, certificateService);
            IQrCodeService qrCodeService = new QrCodeService();

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

            // Act 3.1: Wygeneruj QR dla linku faktury i zapisz do PNG
            byte[] invoiceQrPng = qrCodeService.GenerateQrCode(invoiceVerificationUrl, pixelsPerModule: 16, qrCodeResolutionInPx: 300);
            string invoiceQrPath = Path.Combine(DataDir, "invoice_verification_qr.png");
            File.WriteAllBytes(invoiceQrPath, invoiceQrPng);
            Assert.True(File.Exists(invoiceQrPath));

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

            // Act 4.1: Wygeneruj QR dla linku certyfikatu i zapisz do PNG
            byte[] certificateQrPng = qrCodeService.GenerateQrCode(certificateVerificationUrl, pixelsPerModule: 16, qrCodeResolutionInPx: 300);
            string certificateQrPath = Path.Combine(DataDir, "certificate_verification_qr.png");
            File.WriteAllBytes(certificateQrPath, certificateQrPng);
            Assert.True(File.Exists(certificateQrPath));

            // Diagnostyka
            Console.WriteLine("Invoice verification URL:");
            Console.WriteLine(invoiceVerificationUrl);
            Console.WriteLine("Certificate verification URL:");
            Console.WriteLine(certificateVerificationUrl);
            Console.WriteLine($"Saved invoice QR: {invoiceQrPath}");
            Console.WriteLine($"Saved certificate QR: {certificateQrPath}");
        }

        [Fact]
        public void GenerateVerificationLinks_WithDelayedPassword_Succeeds()
        {
            // Arrange: us³ugi
            ICertificateService certificateService = new CertificateService();
            ICryptographyService cryptographyService = new CryptographyService();
            var verificationLinkService = new VerificationLinkService(KsefEnvironmentUris.TEST, certificateService);
            IQrCodeService qrCodeService = new QrCodeService();

            // Arrange: œcie¿ki absolutne
            string keyPath = Path.Combine(DataDir, PrivateKeyFile);
            string crtPath = Path.Combine(DataDir, PublicCertFile);
            string xmlPath = Path.Combine(DataDir, InvoiceXmlFile);

            Assert.True(File.Exists(keyPath), $"Nie znaleziono pliku klucza: {keyPath}");
            Assert.True(File.Exists(crtPath), $"Nie znaleziono pliku certyfikatu: {crtPath}");
            Assert.True(File.Exists(xmlPath), $"Nie znaleziono pliku XML: {xmlPath}");

            // Krok A: Wczytujemy tylko publiczny certyfikat (bez klucza prywatnego)
            string crtContent = File.ReadAllText(crtPath);
            string crtBase64 = new string(crtContent
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(l => !l.StartsWith("-----"))
                .SelectMany(l => l).ToArray());
            byte[] crtDer = Convert.FromBase64String(crtBase64);
            var publicOnlyCert = new X509Certificate2(crtDer);
            Assert.False(publicOnlyCert.HasPrivateKey);

            // Krok B: Liczymy hash faktury
            byte[] xmlBytes = File.ReadAllBytes(xmlPath);
            string invoiceHashBase64 = cryptographyService.GetHashData(xmlBytes);
            Assert.False(string.IsNullOrWhiteSpace(invoiceHashBase64));

            // Krok C: Generujemy link do faktury
            DateTime issueDate = DateTime.Today;
            string invoiceVerificationUrl = verificationLinkService.BuildInvoiceVerificationUrl(
                nip: SellerNip,
                issueDate: issueDate,
                invoiceHash: invoiceHashBase64
            );
            Assert.StartsWith($"{KsefEnvironmentUris.TEST}/client-app/invoice/", invoiceVerificationUrl);

            // Krok C.1: Wygeneruj QR dla linku faktury i zapisz do PNG
            byte[] invoiceQrPng = qrCodeService.GenerateQrCode(invoiceVerificationUrl, pixelsPerModule: 16, qrCodeResolutionInPx: 300);
            string invoiceQrPath = Path.Combine(DataDir, "invoice_verification_qr_delayed.png");
            File.WriteAllBytes(invoiceQrPath, invoiceQrPng);
            Assert.True(File.Exists(invoiceQrPath));

            // Krok D: PóŸniej (np. od u¿ytkownika) pobieramy has³o i podpisujemy dane, przekazuj¹c privateKey + has³o do metody
            string keyContent = File.ReadAllText(keyPath);
            string userProvidedPassword = PfxPassword; // symulacja pobrania has³a
            string certificateSerial = publicOnlyCert.SerialNumber; // serial z publicznego certyfikatu
            string certificateVerificationUrl = verificationLinkService.BuildCertificateVerificationUrl(
                sellerNip: SellerNip,
                contextIdentifierType: QRCodeContextIdentifierType.Nip,
                contextIdentifierValue: SellerNip,
                certificateSerial: certificateSerial,
                invoiceHash: invoiceHashBase64,
                signingCertificate: publicOnlyCert, // bez prywatnego klucza
                privateKey: keyContent,
                privateKeyPassword: userProvidedPassword
            );

            Assert.StartsWith($"{KsefEnvironmentUris.TEST}/client-app/certificate/", certificateVerificationUrl);
            Assert.Contains(SellerNip, certificateVerificationUrl);
            Assert.Contains(certificateSerial, certificateVerificationUrl);

            // Krok D.1: Wygeneruj QR dla linku certyfikatu i zapisz do PNG
            byte[] certificateQrPng = qrCodeService.GenerateQrCode(certificateVerificationUrl, pixelsPerModule: 16, qrCodeResolutionInPx: 300);
            string certificateQrPath = Path.Combine(DataDir, "certificate_verification_qr_delayed.png");
            File.WriteAllBytes(certificateQrPath, certificateQrPng);
            Assert.True(File.Exists(certificateQrPath));

            // Diagnostyka
            Console.WriteLine("[DelayedPassword] Invoice verification URL:");
            Console.WriteLine(invoiceVerificationUrl);
            Console.WriteLine("[DelayedPassword] Certificate verification URL:");
            Console.WriteLine(certificateVerificationUrl);
            Console.WriteLine($"[DelayedPassword] Saved invoice QR: {invoiceQrPath}");
            Console.WriteLine($"[DelayedPassword] Saved certificate QR: {certificateQrPath}");
        }
    }
}