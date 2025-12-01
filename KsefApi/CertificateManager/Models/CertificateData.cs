using System;
using System.Text.RegularExpressions;

namespace CertificateManager.Models
{
    /// <summary>
    /// Value object representing combined certificate and private key data
    /// </summary>
    public class CertificateData
    {
        public string PublicCertificate { get; }
        public string PrivateKey { get; }
        
        public CertificateData(string publicCertificate, string privateKey)
        {
            if (string.IsNullOrWhiteSpace(publicCertificate))
                throw new ArgumentException("Public certificate cannot be null or empty", nameof(publicCertificate));
            if (string.IsNullOrWhiteSpace(privateKey))
                throw new ArgumentException("Private key cannot be null or empty", nameof(privateKey));
                
            PublicCertificate = publicCertificate.Trim();
            PrivateKey = privateKey.Trim();
        }
        
        /// <summary>
        /// Combines certificate and private key into a single string
        /// </summary>
        public string ToCombinedString()
        {
            // Simply concatenate with a newline - PEM format is self-delimiting
            return $"{PublicCertificate}\n{PrivateKey}";
        }
        
        /// <summary>
        /// Creates CertificateData from a combined string using PEM boundary detection
        /// </summary>
        public static CertificateData FromCombinedString(string combinedData)
        {
            if (string.IsNullOrWhiteSpace(combinedData))
                throw new ArgumentException("Combined data cannot be null or empty", nameof(combinedData));

            var (certificate, privateKey) = ExtractCertificateAndKey(combinedData);
            
            if (string.IsNullOrWhiteSpace(certificate))
                throw new ArgumentException("No valid certificate found in combined data", nameof(combinedData));
            if (string.IsNullOrWhiteSpace(privateKey))
                throw new ArgumentException("No valid private key found in combined data", nameof(combinedData));
                
            return new CertificateData(certificate, privateKey);
        }
        
        /// <summary>
        /// Extracts certificate and private key from PEM data using boundary markers
        /// </summary>
        private static (string certificate, string privateKey) ExtractCertificateAndKey(string pemData)
        {
            // Certificate patterns
            var certPattern = @"-----BEGIN CERTIFICATE-----.*?-----END CERTIFICATE-----";
            var certMatch = Regex.Match(pemData, certPattern, RegexOptions.Singleline);
            
            // Private key patterns - support multiple formats
            var privateKeyPatterns = new[]
            {
                @"-----BEGIN PRIVATE KEY-----.*?-----END PRIVATE KEY-----",           // PKCS#8
                @"-----BEGIN RSA PRIVATE KEY-----.*?-----END RSA PRIVATE KEY-----",   // PKCS#1 RSA
                @"-----BEGIN EC PRIVATE KEY-----.*?-----END EC PRIVATE KEY-----",     // SEC1 EC
                @"-----BEGIN ENCRYPTED PRIVATE KEY-----.*?-----END ENCRYPTED PRIVATE KEY-----" // Encrypted PKCS#8
            };
            
            Match privateKeyMatch = null;
            foreach (var pattern in privateKeyPatterns)
            {
                privateKeyMatch = Regex.Match(pemData, pattern, RegexOptions.Singleline);
                if (privateKeyMatch.Success)
                    break;
            }
            
            string certificate = certMatch.Success ? certMatch.Value.Trim() : null;
            string privateKey = privateKeyMatch?.Success == true ? privateKeyMatch.Value.Trim() : null;
            
            return (certificate, privateKey);
        }
        
        /// <summary>
        /// Validates if the string contains a valid certificate PEM block
        /// </summary>
        public static bool ContainsCertificate(string pemData)
        {
            if (string.IsNullOrWhiteSpace(pemData))
                return false;
                
            return pemData.Contains("-----BEGIN CERTIFICATE-----") && 
                   pemData.Contains("-----END CERTIFICATE-----");
        }
        
        /// <summary>
        /// Validates if the string contains a valid private key PEM block
        /// </summary>
        public static bool ContainsPrivateKey(string pemData)
        {
            if (string.IsNullOrWhiteSpace(pemData))
                return false;
                
            var privateKeyMarkers = new[]
            {
                "-----BEGIN PRIVATE KEY-----",
                "-----BEGIN RSA PRIVATE KEY-----",
                "-----BEGIN EC PRIVATE KEY-----",
                "-----BEGIN ENCRYPTED PRIVATE KEY-----"
            };
            
            foreach (var marker in privateKeyMarkers)
            {
                if (pemData.Contains(marker))
                    return true;
            }
            
            return false;
        }
    }
}