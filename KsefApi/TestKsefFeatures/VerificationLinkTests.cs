using CertificateManager;
using CertificateManager.Interfaces;
using CertificateManager.Models; // RuntimeCert
using CertificateManager.Models.QRCode;
using CertificateManager.Services;
using System;
using System.Security.Cryptography.X509Certificates;

namespace TestKsefFeatures
{
    // Prosty stub do testów podpisu w linku certyfikatu
    internal class StubCertificateService : ICertificateService
    {
        public X509Certificate2 CreateCertificateFromPem(string certPem, string keyPem) => throw new NotImplementedException();
        public X509Certificate2 CreateCertificateFromPem(string certPem, string keyPem, string privateKeyPassword) => throw new NotImplementedException();
        public X509Certificate2 CreateCertificateFromPem(string pathKeyPem, string pathCertPem, string pfxPassword, bool nonExportable = true, StoreLocation storeLocation = StoreLocation.CurrentUser) => throw new NotImplementedException();
        public X509Certificate2 FindExistingCertificate(string pathCert, StoreLocation storeLocation = StoreLocation.LocalMachine) => null;
        public RuntimeCert GetCertificate(string name) => null;
        public string GetCertificatePassword(string name) => null;
        public string ImportCertificateToStore(X509Certificate2 cert, StoreLocation storeLocation = StoreLocation.CurrentUser) => cert?.Thumbprint;
        public string ImportPemKeyAndCertToStore(string pathKeyPem, string pathCertPem, string pfxPassword, StoreLocation storeLocation = StoreLocation.CurrentUser, bool nonExportable = true) => string.Empty;
        public bool IsCertificateLoaded(string name) => false;
        public X509Certificate2 LoadCertificateFromFiles(string crtPath, string keyPath, string password) => throw new NotImplementedException();
        public void RegisterCertificate(string name, X509Certificate2 cert, string password) { }
        
        // New methods implementations - now use proper PEM concatenation
        public string CombineCertificateAndKey(string publicCertificate, string privateKey)
        {
            var certificateData = new CertificateData(publicCertificate, privateKey);
            return certificateData.ToCombinedString();
        }

        public CertificateData SeparateCertificateAndKey(string combinedData)
        {
            return CertificateData.FromCombinedString(combinedData);
        }

        public CertificateInfo ExtractCertificateInfo(X509Certificate2 certificate)
        {
            return new CertificateInfo(certificate);
        }

        public CertificateInfo ExtractCertificateInfoFromPem(string certificatePem)
        {
            // For testing, return a mock certificate info
            return new CertificateInfo(new X509Certificate2());
        }
        
        public string ComputeUrlEncodedSignedSignature(string pathToSign, X509Certificate2 cert, string privateKey = "", string privateKeyPassword = "")
        {
            // Zwracamy deterministyczny podpis (base64url) z sha256(pathToSign)
            var bytes = System.Text.Encoding.UTF8.GetBytes(pathToSign);
            using (var sha = System.Security.Cryptography.SHA256.Create())
            {
                var hash = sha.ComputeHash(bytes);
                return CertificateManager.Extensions.Base64UrlExtensions.EncodeBase64UrlToString(hash);
            }
        }
    }

    public class VerificationLinkTests
    {
        [Fact]
        public void BuildInvoiceVerificationUrl_ReturnsExpectedFormat()
        {
            // Arrange
            var certService = new StubCertificateService();
            var svc = new VerificationLinkService(KsefEnvironmentUris.TEST, certService);
            string nip = "1234567890";
            DateTime issueDate = new DateTime(2025, 1, 31);
            byte[] data = System.Text.Encoding.UTF8.GetBytes("invoice-bytes");
            string invoiceHashBase64 = Convert.ToBase64String(data);

            // Act
            string url = svc.BuildInvoiceVerificationUrl(nip, issueDate, invoiceHashBase64);

            // Assert
            Assert.StartsWith($"{KsefEnvironmentUris.TEST}/client-app/invoice/", url);
            Assert.Contains(nip, url);
            Assert.Contains("31-01-2025", url);
        }

        [Fact]
        public void BuildCertificateVerificationUrl_AppendsSignature()
        {
            // Arrange
            var certService = new StubCertificateService();
            var svc = new VerificationLinkService(KsefEnvironmentUris.TEST, certService);
            var dummyCert = new X509Certificate2(); // public only; stub nie wymaga klucza
            string sellerNip = "1234567890";
            string ctxValue = sellerNip;
            string certSerial = "ABCDEF1234";
            byte[] data = System.Text.Encoding.UTF8.GetBytes("invoice-bytes");
            string invoiceHashBase64 = Convert.ToBase64String(data);

            // Act
            string url = svc.BuildCertificateVerificationUrl(
                sellerNip,
                QRCodeContextIdentifierType.Nip,
                ctxValue,
                certSerial,
                invoiceHashBase64,
                dummyCert
            );

            // Assert: powinien zawierać część podpisu na końcu
            Assert.StartsWith($"{KsefEnvironmentUris.TEST}/client-app/certificate/", url);
            Assert.Contains(sellerNip, url);
            Assert.Contains(certSerial, url);
            Assert.Contains("/certificate/", url);
            // podpis powinien być ostatnim segmentem
            var parts = url.Split('/');
            Assert.True(parts.Length >= 8);
            string lastSegment = parts[parts.Length - 1];
            Assert.False(string.IsNullOrWhiteSpace(lastSegment));
        }
    }
}
