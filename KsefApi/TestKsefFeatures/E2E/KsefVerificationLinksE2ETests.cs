using CertificateManager;
using CertificateManager.Interfaces;
using CertificateManager.Models;
using CertificateManager.Models.QRCode;
using CertificateManager.Services;
using System.Security.Cryptography.X509Certificates;

namespace TestKsefFeatures.E2E
{
    public class KsefVerificationLinksE2ETests
    {
        // Ścieżki do danych testowych
        private const string DataDir = @"Data";
        private const string PrivateKeyFile = @"test_5323452439.key";
        private const string PublicCertFile = @"test_5323452439.crt";
        private const string InvoiceXmlFile = @"5323452439-50351e5a-ddec-4aee-a1d1-2166954e5a43-fa3.xml";

        // Parametry testowe
        private const string SellerNip = "5323452439";
        private const string PfxPassword = "Certyfikat_5323452439"; // dowolne hasło testowe

        [Fact]
        public void GenerateVerificationLinks_EndToEnd_Succeeds()
        {
            // Arrange: usługi
            ICertificateService certificateService = new CertificateService();
            ICryptographyService cryptographyService = new CryptographyService();
            var verificationLinkService = new VerificationLinkService(null, certificateService);
            IQrCodeService qrCodeService = new QrCodeService();

            // Arrange: ścieżki absolutne
            string keyPath = Path.Combine(DataDir, PrivateKeyFile);
            string crtPath = Path.Combine(DataDir, PublicCertFile);
            string xmlPath = Path.Combine(DataDir, InvoiceXmlFile);

            Assert.True(File.Exists(keyPath), $"Nie znaleziono pliku klucza: {keyPath}");
            Assert.True(File.Exists(crtPath), $"Nie znaleziono pliku certyfikatu: {crtPath}");
            Assert.True(File.Exists(xmlPath), $"Nie znaleziono pliku XML: {xmlPath}");

            // Act 1: Utwórz X509Certificate2 (PFX) z pary KEY + CRT (bez importu do Windows Store)
            X509Certificate2 signingCert = certificateService.CreateCertificateFromFile(
                keyPath,
                crtPath,
                PfxPassword
            );
            Assert.NotNull(signingCert);

            // Act 2: Załaduj plik XML i policz SHA-256 (base64)
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
            Assert.StartsWith($"{KsefEnvironmentUris.TEST}/invoice/", invoiceVerificationUrl);
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
                privateKey: "" // opcjonalny parametr; jeśli klucz prywatny już w certyfikacie, można zostawić pusty
            );

            // Assert: podstawowe walidacje treści URL
            Assert.StartsWith($"{KsefEnvironmentUris.TEST}/certificate/", certificateVerificationUrl);
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
            // Arrange: usługi
            ICertificateService certificateService = new CertificateService();
            ICryptographyService cryptographyService = new CryptographyService();
            var verificationLinkService = new VerificationLinkService(KsefEnvironmentUris.TEST, certificateService);
            IQrCodeService qrCodeService = new QrCodeService();

            // Arrange: ścieżki absolutne
            string keyPath = Path.Combine(DataDir, PrivateKeyFile);
            string crtPath = Path.Combine(DataDir, PublicCertFile);
            string xmlPath = Path.Combine(DataDir, InvoiceXmlFile);

            Assert.True(File.Exists(keyPath), $"Nie znaleziono pliku klucza: {keyPath}");
            Assert.True(File.Exists(crtPath), $"Nie znaleziono pliku certyfikatu: {crtPath}");
            Assert.True(File.Exists(xmlPath), $"Nie znaleziono pliku XML: {xmlPath}");

            // Krok A: Wczytujemy tylko publiczny certyfikat (bez klucza prywatnego) - używamy PemToDer
            string crtContent = File.ReadAllText(crtPath);
            byte[] crtDer = certificateService.PemToDer(crtContent, "CERTIFICATE");
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
            Assert.StartsWith($"{KsefEnvironmentUris.TEST}/invoice/", invoiceVerificationUrl);

            // Krok C.1: Wygeneruj QR dla linku faktury i zapisz do PNG
            byte[] invoiceQrPng = qrCodeService.GenerateQrCode(invoiceVerificationUrl, pixelsPerModule: 16, qrCodeResolutionInPx: 300);
            string invoiceQrPath = Path.Combine(DataDir, "invoice_verification_qr_delayed.png");
            File.WriteAllBytes(invoiceQrPath, invoiceQrPng);
            Assert.True(File.Exists(invoiceQrPath));

            // Krok D: Później (np. od użytkownika) pobieramy hasło i podpisujemy dane, przekazując privateKey + hasło do metody
            string keyContent = File.ReadAllText(keyPath);
            string userProvidedPassword = PfxPassword; // symulacja pobrania hasła
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

            Assert.StartsWith($"{KsefEnvironmentUris.TEST}/certificate/", certificateVerificationUrl);
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

        /// <summary>
        /// Test E2E demonstrujący nową funkcjonalność CertificateData z inteligentną detekcją PEM granic.
        /// Ten test wczytuje pliki klucza i certyfikatu, łączy je w obiekt CertificateData,
        /// zapisuje połączone dane do pliku, a następnie demonstruje użycie zewnętrznego zaszyfrowanego 
        /// klucza prywatnego podczas podpisywania (bez tworzenia certyfikatu z kluczem prywatnym).
        /// </summary>
        [Fact]
        public void GenerateVerificationLinks_WithCertificateData_EndToEnd_Succeeds()
        {
            // Arrange: usługi
            ICertificateService certificateService = new CertificateService();
            ICryptographyService cryptographyService = new CryptographyService();
            var verificationLinkService = new VerificationLinkService(KsefEnvironmentUris.TEST, certificateService);
            IQrCodeService qrCodeService = new QrCodeService();

            // Arrange: ścieżki do plików testowych
            string keyPath = Path.Combine(DataDir, PrivateKeyFile);
            string crtPath = Path.Combine(DataDir, PublicCertFile);
            string xmlPath = Path.Combine(DataDir, InvoiceXmlFile);

            Assert.True(File.Exists(keyPath), $"Nie znaleziono pliku klucza: {keyPath}");
            Assert.True(File.Exists(crtPath), $"Nie znaleziono pliku certyfikatu: {crtPath}");
            Assert.True(File.Exists(xmlPath), $"Nie znaleziono pliku XML: {xmlPath}");

            // Krok 1: Wczytanie plików certyfikatu i klucza
            Console.WriteLine("[CertificateData E2E] Krok 1: Wczytywanie plików certyfikatu i klucza...");
            string certContent = File.ReadAllText(crtPath);
            string keyContent = File.ReadAllText(keyPath);
            
            Assert.True(CertificateData.ContainsCertificate(certContent), "Plik certyfikatu nie zawiera poprawnego PEM certyfikatu");
            Assert.True(CertificateData.ContainsPrivateKey(keyContent), "Plik klucza nie zawiera poprawnego PEM klucza prywatnego");

            // Krok 2: Utworzenie obiektu CertificateData z inteligentną detekcją PEM granic
            Console.WriteLine("[CertificateData E2E] Krok 2: Tworzenie obiektu CertificateData...");
            var certificateData = new CertificateData(certContent, keyContent);
            
            Assert.NotNull(certificateData);
            Assert.Equal(certContent.Trim(), certificateData.PublicCertificate);
            Assert.Equal(keyContent.Trim(), certificateData.PrivateKey);

            // Krok 3: Łączenie danych w jeden string (bez sztucznych separatorów!)
            Console.WriteLine("[CertificateData E2E] Krok 3: Łączenie certyfikatu i klucza...");
            string combinedData = certificateData.ToCombinedString();
            
            Assert.False(string.IsNullOrWhiteSpace(combinedData));
            Assert.Contains("-----BEGIN CERTIFICATE-----", combinedData);
            Assert.Contains("-----END CERTIFICATE-----", combinedData);
            Assert.Contains("-----BEGIN", combinedData); // Jakiś typ klucza
            Assert.Contains("-----END", combinedData);
            Console.WriteLine($"[CertificateData E2E] Połączone dane mają {combinedData.Length} znaków");

            // Krok 4: Zapisanie połączonych danych do pliku w katalogu Data
            Console.WriteLine("[CertificateData E2E] Krok 4: Zapisywanie połączonych danych do pliku...");
            string combinedDataPath = Path.Combine(DataDir, "combined_certificate_data.pem");
            File.WriteAllText(combinedDataPath, combinedData);
            Assert.True(File.Exists(combinedDataPath), $"Plik połączonych danych nie został utworzony: {combinedDataPath}");

            // Krok 5: Weryfikacja, że możemy ponownie wczytać i podzielić dane
            Console.WriteLine("[CertificateData E2E] Krok 5: Weryfikacja integralności danych...");
            string loadedCombinedData = File.ReadAllText(combinedDataPath);
            var restoredCertificateData = CertificateData.FromCombinedString(loadedCombinedData);
            
            Assert.Equal(certificateData.PublicCertificate, restoredCertificateData.PublicCertificate);
            Assert.Equal(certificateData.PrivateKey, restoredCertificateData.PrivateKey);
            Console.WriteLine("[CertificateData E2E] ✓ Integralność danych potwierdzona");

            // Krok 6: Utworzenie certyfikatu TYLKO z częścią publiczną (bez klucza prywatnego)
            Console.WriteLine("[CertificateData E2E] Krok 6: Tworzenie certyfikatu TYLKO z częścią publiczną...");
            
            // Używamy PemToDer z CertificateService zamiast ręcznej konwersji
            byte[] certOnlyBytes = certificateService.PemToDer(restoredCertificateData.PublicCertificate, "CERTIFICATE");
            var publicOnlyCert = new X509Certificate2(certOnlyBytes);
            
            Assert.False(publicOnlyCert.HasPrivateKey, "Certyfikat NIE powinien mieć klucza prywatnego");
            Console.WriteLine($"[CertificateData E2E] ✓ Certyfikat utworzony BEZ klucza prywatnego");
            Console.WriteLine($"[CertificateData E2E] Subject: {publicOnlyCert.Subject}");
            Console.WriteLine($"[CertificateData E2E] Serial: {publicOnlyCert.SerialNumber}");
            Console.WriteLine($"[CertificateData E2E] Ważny do: {publicOnlyCert.NotAfter:yyyy-MM-dd}");

            // Krok 7: Wczytanie XML faktury i obliczenie hash
            Console.WriteLine("[CertificateData E2E] Krok 7: Obliczanie hash faktury...");
            byte[] xmlBytes = File.ReadAllBytes(xmlPath);
            string invoiceHashBase64 = cryptographyService.GetHashData(xmlBytes);
            Assert.False(string.IsNullOrWhiteSpace(invoiceHashBase64));
            Console.WriteLine($"[CertificateData E2E] Hash faktury: {invoiceHashBase64}");

            // Krok 8: Generowanie linku weryfikacji faktury (offline)
            Console.WriteLine("[CertificateData E2E] Krok 8: Generowanie linku weryfikacji faktury...");
            DateTime issueDate = DateTime.Today;
            string invoiceVerificationUrl = verificationLinkService.BuildInvoiceVerificationUrl(
                nip: SellerNip,
                issueDate: issueDate,
                invoiceHash: invoiceHashBase64
            );
            
            Assert.StartsWith($"{KsefEnvironmentUris.TEST}/invoice/", invoiceVerificationUrl);
            Assert.Contains(SellerNip, invoiceVerificationUrl);
            Console.WriteLine($"[CertificateData E2E] URL faktury: {invoiceVerificationUrl}");

            // Krok 9: Generowanie QR kodu dla linku faktury
            Console.WriteLine("[CertificateData E2E] Krok 9: Generowanie QR kodu dla faktury...");
            byte[] invoiceQrPng = qrCodeService.GenerateQrCode(invoiceVerificationUrl, pixelsPerModule: 16, qrCodeResolutionInPx: 300);
            string invoiceQrPath = Path.Combine(DataDir, "certificatedata_invoice_qr.png");
            File.WriteAllBytes(invoiceQrPath, invoiceQrPng);
            Assert.True(File.Exists(invoiceQrPath), $"QR kod faktury nie został utworzony: {invoiceQrPath}");
            Console.WriteLine($"[CertificateData E2E] ✓ QR kod faktury zapisany: {invoiceQrPath}");

            // Krok 10: KLUCZOWY MOMENT - Generowanie linku z zaszyfrowanym kluczem prywatnym
            Console.WriteLine("[CertificateData E2E] Krok 10: Generowanie linku z zewnętrznym zaszyfrowanym kluczem...");
            Console.WriteLine("[CertificateData E2E] UWAGA: Klucz prywatny jest zaszyfrowany i hasło podajemy dopiero podczas podpisywania!");
            
            // Symulacja scenariusza, gdzie:
            // 1. Mamy certyfikat bez klucza prywatnego
            // 2. Klucz prywatny jest przechowywany osobno (może być zaszyfrowany)
            // 3. Hasło podajemy dopiero podczas wywołania VerificationLinkService
            string certificateVerificationUrl = verificationLinkService.BuildCertificateVerificationUrl(
                sellerNip: SellerNip,
                contextIdentifierType: QRCodeContextIdentifierType.Nip,
                contextIdentifierValue: SellerNip,
                certificateSerial: publicOnlyCert.SerialNumber, // Serial z certyfikatu bez klucza
                invoiceHash: invoiceHashBase64,
                signingCertificate: publicOnlyCert, // Certyfikat BEZ klucza prywatnego!
                privateKey: restoredCertificateData.PrivateKey, // Zewnętrzny klucz z CertificateData
                privateKeyPassword: PfxPassword // Hasło podawane dopiero tutaj!
            );
            
            Assert.StartsWith($"{KsefEnvironmentUris.TEST}/certificate/", certificateVerificationUrl);
            Assert.Contains(SellerNip, certificateVerificationUrl);
            Assert.Contains(publicOnlyCert.SerialNumber, certificateVerificationUrl);
            Console.WriteLine($"[CertificateData E2E] ✓ URL certyfikatu z zewnętrznym kluczem: {certificateVerificationUrl}");

            // Krok 11: Generowanie QR kodu dla linku certyfikatu
            Console.WriteLine("[CertificateData E2E] Krok 11: Generowanie QR kodu dla certyfikatu...");
            byte[] certificateQrPng = qrCodeService.GenerateQrCode(certificateVerificationUrl, pixelsPerModule: 16, qrCodeResolutionInPx: 300);
            string certificateQrPath = Path.Combine(DataDir, "certificatedata_certificate_qr.png");
            File.WriteAllBytes(certificateQrPath, certificateQrPng);
            Assert.True(File.Exists(certificateQrPath), $"QR kod certyfikatu nie został utworzony: {certificateQrPath}");
            Console.WriteLine($"[CertificateData E2E] ✓ QR kod certyfikatu zapisany: {certificateQrPath}");

            // Krok 12: Demonstracja różnicy między podejściami
            Console.WriteLine("[CertificateData E2E] Krok 12: Porównanie podejść do zarządzania kluczami...");
            
            Console.WriteLine("  PODEJŚCIE 1: Certyfikat z wbudowanym kluczem prywatnym");
            Console.WriteLine("  - Klucz prywatny jest częścią obiektu X509Certificate2");
            Console.WriteLine("  - Ryzyko: klucz jest zawsze dostępny w pamięci");
            Console.WriteLine("  - Użycie: privateKey = \"\" (pusty string)");
            
            Console.WriteLine("  PODEJŚCIE 2: Zewnętrzny zaszyfrowany klucz prywatny (CertificateData)");
            Console.WriteLine("  - Certyfikat zawiera TYLKO część publiczną");
            Console.WriteLine("  - Klucz prywatny przechowywany osobno, może być zaszyfrowany");
            Console.WriteLine("  - Hasło podawane dopiero podczas podpisywania");
            Console.WriteLine("  - Użycie: privateKey = string z kluczem, privateKeyPassword = hasło");
            Console.WriteLine("  ✓ BEZPIECZNIEJSZE - klucz dostępny tylko podczas podpisywania");

            // Podsumowanie końcowe
            Console.WriteLine("\n[CertificateData E2E] === PODSUMOWANIE TESTU ===");
            Console.WriteLine($"✓ Połączone dane zapisane: {combinedDataPath}");
            Console.WriteLine($"✓ QR kod faktury: {invoiceQrPath}");
            Console.WriteLine($"✓ QR kod certyfikatu: {certificateQrPath}");
            Console.WriteLine($"✓ Certyfikat bez klucza prywatnego: {publicOnlyCert.Subject}");
            Console.WriteLine($"✓ Długość połączonych danych: {combinedData.Length} znaków");
            Console.WriteLine($"✓ Integralność danych zachowana: TAK");
            Console.WriteLine($"✓ Detekcja PEM granic działa: TAK");
            Console.WriteLine($"✓ Podpis z zewnętrznym zaszyfrowanym kluczem: TAK");
            Console.WriteLine($"✓ Bezpieczeństwo: Klucz prywatny dostępny TYLKO podczas podpisywania");
            Console.WriteLine("[CertificateData E2E] Test pomyślnie zakończony!");
        }
    }
}