using CertificateManager.Extensions;
using CertificateManager.Models;
using CertificateManager.Security;
using Org.BouncyCastle.Asn1;
using Org.BouncyCastle.Asn1.Pkcs;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Signers;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace CertificateManager
{
    public class CertificateService : ICertificateService
    {
        private readonly Dictionary<string, RuntimeCert> _loadedCerts =
            new Dictionary<string, RuntimeCert>(StringComparer.OrdinalIgnoreCase);

        // -------------------------
        // Tworzenie certyfikatu z plików (KEY + CRT) -> X509Certificate2 (bez importu do Store)
        // -------------------------
        public X509Certificate2 CreateCertificateFromPem(string pathKeyPem, string pathCertPem, string pfxPassword,
            bool nonExportable = true, StoreLocation storeLocation = StoreLocation.CurrentUser)
        {
            if (!File.Exists(pathKeyPem)) throw new FileNotFoundException(nameof(pathKeyPem));
            if (!File.Exists(pathCertPem)) throw new FileNotFoundException(nameof(pathCertPem));

            AsymmetricKeyParameter privateKey;
            using (var reader = File.OpenText(pathKeyPem))
            {
                var passwordFinder = new StaticPasswordFinder(pfxPassword);
                var pemReader = new PemReader(reader, passwordFinder);
                var obj = pemReader.ReadObject();
                passwordFinder.Clear();
                if (obj is AsymmetricCipherKeyPair kp)
                    privateKey = kp.Private;
                else if (obj is AsymmetricKeyParameter akp)
                    privateKey = akp;
                else
                    throw new InvalidOperationException("Nieprawidłowy format pliku .key (PEM).");
            }

            X509Certificate bcCert;
            using (var reader = File.OpenText(pathCertPem))
            {
                var pemReader = new PemReader(reader);
                var obj = pemReader.ReadObject();
                if (obj is X509Certificate c)
                    bcCert = c;
                else
                    bcCert = obj as X509Certificate;
                if (bcCert == null) throw new InvalidOperationException("Nieprawidłowy format .crt PEM.");
            }

            var pkcs12Store = new Pkcs12StoreBuilder().Build();
            string friendlyName = bcCert.SubjectDN.ToString();
            var certEntry = new X509CertificateEntry(bcCert);
            var keyEntry = new AsymmetricKeyEntry(privateKey);
            pkcs12Store.SetKeyEntry(friendlyName, keyEntry, new[] { certEntry });

            byte[] pfxBytes;
            using (var ms = new MemoryStream())
            {
                pkcs12Store.Save(ms, pfxPassword.ToCharArray(), new SecureRandom());
                pfxBytes = ms.ToArray();
            }

            var flags = X509KeyStorageFlags.PersistKeySet;
            if (!nonExportable)
                flags |= X509KeyStorageFlags.Exportable;
            flags |= (storeLocation == StoreLocation.LocalMachine) ? X509KeyStorageFlags.MachineKeySet : X509KeyStorageFlags.UserKeySet;

            var cert = new X509Certificate2(pfxBytes, pfxPassword, flags);
            Array.Clear(pfxBytes, 0, pfxBytes.Length);
            return cert;
        }

        // -------------------------
        // Import certyfikatu do Windows Store
        // -------------------------
        public string ImportCertificateToStore(X509Certificate2 cert,
            StoreLocation storeLocation = StoreLocation.CurrentUser)
        {
            if (cert == null) throw new ArgumentNullException(nameof(cert));
            using (var storeWin = new X509Store(StoreName.My, storeLocation))
            {
                storeWin.Open(OpenFlags.ReadWrite);
                storeWin.Add(cert);
                storeWin.Close();
            }
            return cert.Thumbprint;
        }

        // -------------------------
        // Tworzenie certyfikatu z plików (KEY + CRT) i import certyfikatu do Windows Store 
        // -------------------------
        public string ImportPemKeyAndCertToStore(string pathKeyPem, string pathCertPem, string pfxPassword,
            StoreLocation storeLocation = StoreLocation.CurrentUser, bool nonExportable = true)
        {
            var cert = CreateCertificateFromPem(pathKeyPem, pathCertPem, pfxPassword, nonExportable, storeLocation);
            return ImportCertificateToStore(cert, storeLocation);
        }

        // -------------------------
        // Sprawdzenie czy cert z pliku istnieje w Store
        // -------------------------
        public X509Certificate2 FindExistingCertificate(string pathCert,
            StoreLocation storeLocation = StoreLocation.LocalMachine)
        {
            if (!File.Exists(pathCert))
                throw new FileNotFoundException("Brak certyfikatu", pathCert);

            var raw = LoadCertPemOrDer(pathCert);
            var certToCheck = new X509Certificate2(raw);

            using (var store = new X509Store(StoreName.My, storeLocation))
            {
                store.Open(OpenFlags.ReadOnly);

                var found = store.Certificates
                    .Find(X509FindType.FindByThumbprint, certToCheck.Thumbprint, validOnly: false)
                    .OfType<X509Certificate2>()
                    .FirstOrDefault();
                if (found != null) return found;

                found = store.Certificates
                    .Find(X509FindType.FindBySerialNumber, certToCheck.SerialNumber, validOnly: false)
                    .OfType<X509Certificate2>()
                    .FirstOrDefault();
                if (found != null) return found;

                foreach (var c in store.Certificates)
                {
                    if (c.RawData.SequenceEqual(certToCheck.RawData))
                        return c;
                }
            }
            return null;
        }

        // -------------------------
        // Tworzenie cert z zawartości PEM w stringach (certPem + keyPem)
        // -------------------------
        public X509Certificate2 CreateCertificateFromPem(string certPem, string keyPem)
        {
            var certBytes = PemToDer(certPem, "CERTIFICATE");
            var keyBytes = PemToDer(keyPem, "PRIVATE KEY");
            using (var cert = new X509Certificate2(certBytes))
            {
                var pfxBytes = CreatePfx(certBytes, keyBytes);
                return new X509Certificate2(pfxBytes, (string)null, X509KeyStorageFlags.Exportable);
            }
        }

        // -------------------------
        // Tworzenie cert z zawartości PEM w stringach (certPem + keyPem) z hasłem do klucza prywatnego
        // -------------------------
        public X509Certificate2 CreateCertificateFromPem(string certPem, string keyPem, string privateKeyPassword)
        {
            var certBytes = PemToDer(certPem, "CERTIFICATE");
            var keyBytes = PemToDer(keyPem, "PRIVATE KEY");
            var pfxBytes = CreatePfx(certBytes, keyBytes, privateKeyPassword ?? "");
            return new X509Certificate2(pfxBytes, (string)null, X509KeyStorageFlags.Exportable);
        }

        // -------------------------
        // Ładowanie CRT + KEY (wykorzystuje wersję stringową)
        // -------------------------
        public X509Certificate2 LoadCertificateFromFiles(string crtPath, string keyPath, string password)
        {
            if (!File.Exists(crtPath)) throw new FileNotFoundException("CRT not found");
            if (!File.Exists(keyPath)) throw new FileNotFoundException("KEY not found");
            var certPem = File.ReadAllText(crtPath);
            var keyPem = File.ReadAllText(keyPath);
            return CreateCertificateFromPem(certPem, keyPem);
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

        public bool IsCertificateLoaded(string name) => _loadedCerts.ContainsKey(name);

        public RuntimeCert GetCertificate(string name)
        {
            if (_loadedCerts.TryGetValue(name, out var cert)) return cert;
            return null;
        }

        public string GetCertificatePassword(string name)
        {
            if (!_loadedCerts.TryGetValue(name, out var cert)) return null;
            return DecryptPassword(cert.EncryptedPassword, cert.AesKey, cert.AesIV);
        }

        // Podpisuje ścieżkę zgodnie z wcześniejszym działającym schematem (RSA-PSS / ECDSA, pojedyncze hashowanie)
        public string ComputeUrlEncodedSignedSignature(
            string pathToSign,
            X509Certificate2 cert,
            string privateKey = "",
            string privateKeyPassword = ""
        )
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(pathToSign);

            // Jeśli mamy jawny klucz prywatny PEM -> podpisz BouncyCastle
            if (!string.IsNullOrEmpty(privateKey))
            {
                var bcKey = ParseBcPrivateKey(privateKey, privateKeyPassword);
                var signatureBytes = SignDataWithBc(bcKey, dataBytes);
                return signatureBytes.EncodeBase64UrlToString();
            }

            // W przeciwnym razie użyj klucza z certyfikatu (.NET)
            var dotNetSignature = SignDataWithDotNet(cert, dataBytes);
            if (dotNetSignature != null)
                return dotNetSignature.EncodeBase64UrlToString();

            throw new InvalidOperationException("Certyfikat nie posiada klucza prywatnego ani nie dostarczono privateKey.");
        }

        // -------------------------
        // Private helpers (PEM parsing + PFX creation stubs)
        // -------------------------
        private AsymmetricKeyParameter ParseBcPrivateKey(string pem, string password)
        {
            using (var reader = new StringReader(pem))
            {
                var pemReader = string.IsNullOrEmpty(password)
                    ? new PemReader(reader)
                    : new PemReader(reader, new Security.StaticPasswordFinder(password));
                object obj = pemReader.ReadObject();
                if (obj is AsymmetricCipherKeyPair kp)
                    return kp.Private;
                if (obj is AsymmetricKeyParameter akp)
                    return akp;
                throw new InvalidOperationException("Nieprawidłowy format klucza prywatnego (PEM).");
            }
        }

        private byte[] SignDataWithBc(AsymmetricKeyParameter privateKey, byte[] data)
        {
            // RSA-PSS lub ECDSA, pojedyncze hashowanie SHA-256 zgodnie z dotychczasowym zachowaniem
            if (privateKey is Org.BouncyCastle.Crypto.Parameters.RsaPrivateCrtKeyParameters ||
                privateKey is Org.BouncyCastle.Crypto.Parameters.RsaKeyParameters)
            {
                var pss = new PssSigner(new RsaEngine(), new Sha256Digest(), 32);
                pss.Init(true, privateKey);
                pss.BlockUpdate(data, 0, data.Length);
                return pss.GenerateSignature();
            }
            else
            {
                var ecdsaSigner = SignerUtilities.GetSigner("SHA256withECDSA");
                ecdsaSigner.Init(true, privateKey);
                ecdsaSigner.BlockUpdate(data, 0, data.Length);
                return ecdsaSigner.GenerateSignature();
            }
        }

        private byte[] SignDataWithDotNet(X509Certificate2 cert, byte[] data)
        {
            // RSA-PSS z pojedynczym SHA-256
            var rsa = cert.GetRSAPrivateKey();
            if (rsa != null)
            {
                using (var sha256 = SHA256.Create())
                {
                    var hash = sha256.ComputeHash(data);
                    return rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pss);
                }
            }

            // ECDSA z pojedynczym SHA-256
            var ecdsa = cert.GetECDsaPrivateKey();
            if (ecdsa != null)
            {
                using (var sha256 = SHA256.Create())
                {
                    var hash = sha256.ComputeHash(data);
                    return ecdsa.SignHash(hash);
                }
            }

            return null;
        }

        private byte[] LoadCertPemOrDer(string path)
        {
            var text = File.ReadAllText(path);
            if (text.Contains("-----BEGIN CERTIFICATE-----"))
            {
                var base64 = text
                    .Replace("-----BEGIN CERTIFICATE-----", "")
                    .Replace("-----END CERTIFICATE-----", "")
                    .Replace("\r", "")
                    .Replace("\n", "")
                    .Trim();
                return Convert.FromBase64String(base64);
            }
            return File.ReadAllBytes(path);
        }

        private byte[] PemToDer(string pem, string section)
        {
            var header = $"-----BEGIN {section}-----";
            var footer = $"-----END {section}-----";
            var start = pem.IndexOf(header, System.StringComparison.Ordinal);
            var end = pem.IndexOf(footer, System.StringComparison.Ordinal);
            if (start < 0 || end < 0) throw new ArgumentException("Invalid PEM format");
            var base64 = pem.Substring(start + header.Length, end - (start + header.Length)).Replace("\r", "").Replace("\n", "");
            return Convert.FromBase64String(base64);
        }

        private byte[] CreatePfx(byte[] certBytes, byte[] keyBytes, string privateKeyPassword = "")
        {
            if (certBytes == null || certBytes.Length == 0) throw new ArgumentException("Brak danych certyfikatu", nameof(certBytes));
            if (keyBytes == null || keyBytes.Length == 0) throw new ArgumentException("Brak danych klucza prywatnego", nameof(keyBytes));

            // 1) Parsuj certyfikat DER -> BC X509Certificate
            var certParser = new Org.BouncyCastle.X509.X509CertificateParser();
            var bcCert = certParser.ReadCertificate(certBytes);
            if (bcCert == null) throw new InvalidOperationException("Nie można odczytać certyfikatu.");

            // 2) Parsuj klucz prywatny (PKCS#8 / RSA / EC). Jeśli zaszyfrowany i podano hasło – odszyfruj.
            AsymmetricKeyParameter privateKey;
            try
            {
                // Próba bez hasła (zwykły niezabezpieczony klucz PKCS#8/SEC1)
                privateKey = PrivateKeyFactory.CreateKey(keyBytes);
            }
            catch
            {
                if (string.IsNullOrWhiteSpace(privateKeyPassword)) throw;
                // Spróbuj jako zaszyfrowany PKCS#8
                try
                {
                    var asnObj = Asn1Object.FromByteArray(keyBytes);
                    var encInfo = EncryptedPrivateKeyInfo.GetInstance(asnObj);
                    privateKey = PrivateKeyFactory.DecryptKey(privateKeyPassword.ToCharArray(), encInfo);
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Nie udało się odszyfrować klucza prywatnego.", ex);
                }
            }

            // 3) Zbuduj PKCS#12 store
            var store = new Pkcs12StoreBuilder().Build();
            string friendlyName = bcCert.SubjectDN.ToString();
            var certEntry = new X509CertificateEntry(bcCert);
            var keyEntry = new AsymmetricKeyEntry(privateKey);
            store.SetKeyEntry(friendlyName, keyEntry, new[] { certEntry });

            // 4) Eksportuj do byte[] (używamy tego samego hasła co do klucza; jeśli brak – pusty ciąg)
            char[] exportPassword = string.IsNullOrEmpty(privateKeyPassword) ? new char[0] : privateKeyPassword.ToCharArray();
            using (var ms = new MemoryStream())
            {
                store.Save(ms, exportPassword, new SecureRandom());
                return ms.ToArray();
            }
        }

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
    }
}
