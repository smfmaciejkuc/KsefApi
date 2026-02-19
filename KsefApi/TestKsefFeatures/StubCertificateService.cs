using CertificateManager;
using CertificateManager.Models; // RuntimeCert
using System.Security.Cryptography.X509Certificates;

namespace TestKsefFeatures
{
    // Prosty stub do testów podpisu w linku certyfikatu
    internal class StubCertificateService : ICertificateService
    {
        public X509Certificate2 CreateCertificateFromPem(string certPem, string keyPem) => throw new NotImplementedException();
        public X509Certificate2 CreateCertificateFromPem(string certPem, string keyPem, string privateKeyPassword, bool nonExportable, StoreLocation storeLocation) => throw new NotImplementedException();
        public X509Certificate2 CreateCertificateFromFile(string pathKeyPem, string pathCertPem, string pfxPassword, bool nonExportable = false, StoreLocation storeLocation = StoreLocation.CurrentUser) => throw new NotImplementedException();
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
        
        public byte[] PemToDer(string pem, string section)
        {
            var header = $"-----BEGIN {section}-----";
            var footer = $"-----END {section}-----";
            var start = pem.IndexOf(header, System.StringComparison.Ordinal);
            var end = pem.IndexOf(footer, System.StringComparison.Ordinal);
            if (start < 0 || end < 0) throw new ArgumentException($"Invalid PEM format for section '{section}'");
            var base64 = pem.Substring(start + header.Length, end - (start + header.Length))
                .Replace("\r", "").Replace("\n", "").Replace(" ", "");
            return Convert.FromBase64String(base64);
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
}
