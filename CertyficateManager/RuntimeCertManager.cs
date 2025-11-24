using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;

namespace CertyficateManager
{
    public class RuntimeCertManager
    {
        private readonly Dictionary<string, RuntimeCert> _loadedCerts =
            new Dictionary<string, RuntimeCert>(StringComparer.OrdinalIgnoreCase);

        // -------------------------
        // AES + DPAPI
        // -------------------------
        private (byte[] key, byte[] iv) GenerateAesKey()
        {
            using (var aes = Aes.Create())
            {
                aes.GenerateKey();
                aes.GenerateIV();

                return (
                    ProtectedData.Protect(aes.Key, null, DataProtectionScope.CurrentUser),
                    ProtectedData.Protect(aes.IV, null, DataProtectionScope.CurrentUser)
                );
            }
        }

        private byte[] EncryptPassword(string password, byte[] protectedKey, byte[] protectedIv)
        {
            var key = ProtectedData.Unprotect(protectedKey, null, DataProtectionScope.CurrentUser);
            var iv = ProtectedData.Unprotect(protectedIv, null, DataProtectionScope.CurrentUser);

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (var encryptor = aes.CreateEncryptor(aes.Key, aes.IV))
                {
                    var plainBytes = Encoding.UTF8.GetBytes(password);

                    return encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
                }
            }
        }

        private string DecryptPassword(byte[] encrypted, byte[] protectedKey, byte[] protectedIv)
        {
            var key = ProtectedData.Unprotect(protectedKey, null, DataProtectionScope.CurrentUser);
            var iv = ProtectedData.Unprotect(protectedIv, null, DataProtectionScope.CurrentUser);

            using (var aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;

                using (var decryptor = aes.CreateDecryptor(aes.Key, aes.IV))
                {
                    var decryptedBytes = decryptor.TransformFinalBlock(encrypted, 0, encrypted.Length);

                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }

        // -------------------------
        // Ładowanie CRT + KEY
        // -------------------------

        public X509Certificate2 LoadCertificateFromFiles(string crtPath, string keyPath, string password)
        {
            if (!File.Exists(crtPath)) throw new FileNotFoundException("CRT not found");
            if (!File.Exists(keyPath)) throw new FileNotFoundException("KEY not found");

            var certPem = File.ReadAllText(crtPath);
            var keyPem = File.ReadAllText(keyPath);

            return CreateCertificateFromPem(certPem, keyPem);
        }

        public X509Certificate2 CreateCertificateFromPem(string certPem, string keyPem)
        {
            // .NET Framework and older .NET versions do not have X509Certificate2.CreateFromPem.
            // Use BouncyCastle or similar library, or convert PEM to PFX and load.
            // Here is a workaround using temporary PFX creation:

            // Convert PEM to byte[] (DER) for certificate and private key
            var certBytes = CertTools.PemToDer(certPem, "CERTIFICATE");
            var keyBytes = CertTools.PemToDer(keyPem, "PRIVATE KEY");

            using (var cert = new X509Certificate2(certBytes))
            {
                // Combine cert and key into a PFX
                var pfxBytes = CertTools.CreatePfx(certBytes, keyBytes);
                return new X509Certificate2(pfxBytes, (string)null, X509KeyStorageFlags.Exportable);
            }
        }

        // -------------------------
        // Rejestracja certyfikatu w pamięci aplikacji
        // -------------------------

        public void RegisterCertificate(string name, X509Certificate2 cert, string password)
        {
            var (key, iv) = GenerateAesKey();
            var encryptedPassword = EncryptPassword(password, key, iv);

            _loadedCerts[name] = new RuntimeCert
            {
                Thumbprint = cert.Thumbprint,
                Certificate = cert,
                EncryptedPassword = encryptedPassword,
                AesKey = key,
                AesIV = iv
            };
        }

        // -------------------------
        // Sprawdzanie, czy cert jest już załadowany
        // -------------------------

        public bool IsCertificateLoaded(string name)
        {
            return _loadedCerts.ContainsKey(name);
        }

        public RuntimeCert GetCertificate(string name)
        {
            if (_loadedCerts.TryGetValue(name, out var cert))
                return cert;
            return null;
        }

        public string GetCertificatePassword(string name)
        {
            if (!_loadedCerts.TryGetValue(name, out var cert))
                return null;

            return DecryptPassword(cert.EncryptedPassword, cert.AesKey, cert.AesIV);
        }

    }
}
