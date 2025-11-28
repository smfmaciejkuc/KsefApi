using CertificateManager.Extensions;
using CertificateManager.Interfaces;
using CertificateManager.Models.QRCode;
using System;
using System.Security.Cryptography.X509Certificates;

namespace CertificateManager.Services
{
    public class VerificationLinkService : IVerificationLinkService
    {
        private readonly string BaseUrl;
        private readonly ICertificateService _certificateService;

        public VerificationLinkService(string baseUrl, ICertificateService certificateService)
        {
            BaseUrl = $"{baseUrl}/client-app" ?? KsefEnvironmentUris.TEST;
            _certificateService = certificateService;
        }

        public string BuildInvoiceVerificationUrl(string nip, DateTime issueDate, string invoiceHash)
        {
            string date = issueDate.ToString("dd-MM-yyyy");
            byte[] bytes = Convert.FromBase64String(invoiceHash);
            string urlEncoded = bytes.EncodeBase64UrlToString();
            return $"{BaseUrl}/invoice/{nip}/{date}/{urlEncoded}";
        }

        public string BuildCertificateVerificationUrl(
            string sellerNip,
            QRCodeContextIdentifierType contextIdentifierType,
            string contextIdentifierValue,
            string certificateSerial,
            string invoiceHash,
            X509Certificate2 signingCertificate,
            string privateKey = "",
            string privateKeyPassword = ""
        )
        {
            byte[] bytes = Convert.FromBase64String(invoiceHash);
            string invoiceHashUrlEncoded = bytes.EncodeBase64UrlToString();

            string pathToSign = $"{BaseUrl}/certificate/{contextIdentifierType}/{contextIdentifierValue}/{sellerNip}/{certificateSerial}/{invoiceHashUrlEncoded}".Replace("https://", "");
            string signedHash = _certificateService.ComputeUrlEncodedSignedSignature(pathToSign, signingCertificate, privateKey, privateKeyPassword);

            return $"{BaseUrl}/certificate/{contextIdentifierType}/{contextIdentifierValue}/{sellerNip}/{certificateSerial}/{invoiceHashUrlEncoded}/{signedHash}";
        }
    }
}
