using System;
using System.Security.Cryptography;

namespace CertificateManager.Services
{
    public class CryptographyService : ICryptographyService
    {
        public string GetHashData(byte[] file)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                byte[] hash = sha256.ComputeHash(file);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
