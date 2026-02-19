using CertificateManager;
using CertificateManager.Interfaces;
using CertificateManager.Models.QRCode;
using CertificateManager.Services;
using System;
using System.Security.Cryptography.X509Certificates;

namespace TestKsefFeatures
{
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
            Assert.StartsWith($"{KsefEnvironmentUris.TEST}/invoice/", url);
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
            Assert.StartsWith($"{KsefEnvironmentUris.TEST}/certificate/", url);
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
