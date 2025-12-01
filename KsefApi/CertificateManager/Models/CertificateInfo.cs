using System;
using System.Security.Cryptography.X509Certificates;

namespace CertificateManager.Models
{
    /// <summary>
    /// Value object representing certificate metadata and information
    /// </summary>
    public class CertificateInfo
    {
        public string Subject { get; }
        public string Issuer { get; }
        public string SerialNumber { get; }
        public string Thumbprint { get; }
        public DateTime NotBefore { get; }
        public DateTime NotAfter { get; }
        public bool HasPrivateKey { get; }
        public string SignatureAlgorithm { get; }
        public int KeySize { get; }
        public string Version { get; }
        
        public CertificateInfo(X509Certificate2 certificate)
        {
            if (certificate == null)
                throw new ArgumentNullException(nameof(certificate));
                
            Subject = certificate.Subject;
            Issuer = certificate.Issuer;
            SerialNumber = certificate.SerialNumber;
            Thumbprint = certificate.Thumbprint;
            NotBefore = certificate.NotBefore;
            NotAfter = certificate.NotAfter;
            HasPrivateKey = certificate.HasPrivateKey;
            SignatureAlgorithm = certificate.SignatureAlgorithm?.FriendlyName ?? "Unknown";
            Version = $"V{certificate.Version}";
            
            // Try to get key size
            try
            {
                var publicKey = certificate.PublicKey;
                if (publicKey.Oid.FriendlyName == "RSA")
                {
                    using (var rsa = certificate.GetRSAPublicKey())
                    {
                        KeySize = rsa?.KeySize ?? 0;
                    }
                }
                else if (publicKey.Oid.FriendlyName == "ECC")
                {
                    using (var ecdsa = certificate.GetECDsaPublicKey())
                    {
                        KeySize = ecdsa?.KeySize ?? 0;
                    }
                }
                else
                {
                    KeySize = publicKey.Key?.KeySize ?? 0;
                }
            }
            catch
            {
                KeySize = 0;
            }
        }
        
        /// <summary>
        /// Checks if the certificate is currently valid (not expired)
        /// </summary>
        public bool IsValid => DateTime.Now >= NotBefore && DateTime.Now <= NotAfter;
        
        /// <summary>
        /// Checks if the certificate is expired
        /// </summary>
        public bool IsExpired => DateTime.Now > NotAfter;
        
        /// <summary>
        /// Gets the number of days until expiration (negative if already expired)
        /// </summary>
        public int DaysUntilExpiration => (int)(NotAfter - DateTime.Now).TotalDays;
        
        /// <summary>
        /// Gets the common name from the subject
        /// </summary>
        public string CommonName
        {
            get
            {
                var cnStart = Subject.IndexOf("CN=", StringComparison.OrdinalIgnoreCase);
                if (cnStart == -1) return null;
                
                cnStart += 3; // Skip "CN="
                var cnEnd = Subject.IndexOf(',', cnStart);
                if (cnEnd == -1) cnEnd = Subject.Length;
                
                return Subject.Substring(cnStart, cnEnd - cnStart).Trim();
            }
        }
        
        public override string ToString()
        {
            return $"CN: {CommonName}, Serial: {SerialNumber}, Valid: {NotBefore:yyyy-MM-dd} - {NotAfter:yyyy-MM-dd}";
        }
    }
}