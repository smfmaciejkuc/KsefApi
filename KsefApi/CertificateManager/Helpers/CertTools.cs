using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.OpenSsl;
using Org.BouncyCastle.Pkcs;
using Org.BouncyCastle.Security;
using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using X509Certificate = Org.BouncyCastle.X509.X509Certificate;

namespace CertificateManager
{
    public static class CertTools
    {
        // Wczytuje niezaszyfrowany PEM private key oraz PEM cert i tworzy PFX (byte[]),
        // następnie importuje go do Windows Certificate Store (non-exportable zalecane).
        public static string ImportPemKeyAndCertToStore(string pathKeyPem, string pathCertPem, string pfxPassword,
            StoreLocation storeLocation = StoreLocation.CurrentUser, bool nonExportable = true)
        {
            if (!File.Exists(pathKeyPem)) throw new FileNotFoundException(nameof(pathKeyPem));
            if (!File.Exists(pathCertPem)) throw new FileNotFoundException(nameof(pathCertPem));

            // 1) Wczytaj PEM private key
            AsymmetricKeyParameter privateKey;
            using (var reader = File.OpenText(pathKeyPem))
            {
                var pemReader = new PemReader(reader);
                var obj = pemReader.ReadObject();
                if (obj is AsymmetricCipherKeyPair kp)
                    privateKey = kp.Private;
                else if (obj is AsymmetricKeyParameter akp)
                    privateKey = akp;
                else
                    throw new InvalidOperationException("Nieprawidłowy format pliku .key (PEM).");
            }

            // 2) Wczytaj PEM certificate (może być chain; obsłuż pierwszy cert jako główny)
            X509Certificate bcCert;
            using (var reader = File.OpenText(pathCertPem))
            {
                var pemReader = new PemReader(reader);
                var obj = pemReader.ReadObject();
                // może zwracać X509Certificate lub X509CertificateEntry/collection
                if (obj is X509Certificate c)
                    bcCert = c;
                else if (obj is Org.BouncyCastle.X509.X509CertificateParser)
                    throw new InvalidOperationException("Nieobsługiwany obiekt w cert PEM.");
                else
                    bcCert = obj as X509Certificate;
                if (bcCert == null) throw new InvalidOperationException("Nieprawidłowy format .crt PEM.");
            }

            // 3) Utwórz PKCS12 store i dodaj pair
            var store = new Pkcs12StoreBuilder().Build();
            string friendlyName = bcCert.SubjectDN.ToString();
            var certEntry = new X509CertificateEntry(bcCert);
            // we need key entry
            var keyEntry = new AsymmetricKeyEntry(privateKey);
            // add to store
            store.SetKeyEntry(friendlyName, keyEntry, new[] { certEntry });

            // 4) Eksportuj do PFX byte[]
            byte[] pfxBytes;
            using (var ms = new MemoryStream())
            {
                store.Save(ms, pfxPassword.ToCharArray(), new SecureRandom());
                pfxBytes = ms.ToArray();
            }

            // 5) Import do Windows Store
            var flags = X509KeyStorageFlags.PersistKeySet;
            if (!nonExportable)
                flags |= X509KeyStorageFlags.Exportable;
            flags |= (storeLocation == StoreLocation.LocalMachine) ? X509KeyStorageFlags.MachineKeySet : X509KeyStorageFlags.UserKeySet;

            var cert = new X509Certificate2(pfxBytes, pfxPassword, flags);

            using (var storeWin = new X509Store(StoreName.My, storeLocation))
            {
                storeWin.Open(OpenFlags.ReadWrite);
                storeWin.Add(cert);
                storeWin.Close();
            }

            // 6) Wyzeruj/wyczyść pfxBytes i usuń wszelkie tymczasowe pliki jeśli były.
            Array.Clear(pfxBytes, 0, pfxBytes.Length);

            // 7) Zwróć thumbprint nowo zaimportowanego certyfikatu
            return cert.Thumbprint;
        }

        /// <summary>
        /// Sprawdza czy certyfikat z pliku .crt (PEM lub DER) jest już zainstalowany
        /// w Windows Certificate Store (My).
        /// Zwraca obiekt X509Certificate2 jeśli istnieje, inaczej null.
        /// </summary>
        public static X509Certificate2 FindExistingCertificate(string pathCert,
            StoreLocation storeLocation = StoreLocation.LocalMachine)
        {
            if (!File.Exists(pathCert))
                throw new FileNotFoundException("Brak certyfikatu", pathCert);

            // Obsługa PEM i DER
            var raw = LoadCertPemOrDer(pathCert);
            var certToCheck = new X509Certificate2(raw);

            using (var store = new X509Store(StoreName.My, storeLocation))
            {
                store.Open(OpenFlags.ReadOnly);

                // Po thumbprint
                var found = store.Certificates
                    .Find(X509FindType.FindByThumbprint, certToCheck.Thumbprint, validOnly: false)
                    .OfType<X509Certificate2>()
                    .FirstOrDefault();

                if (found != null)
                    return found;

                // Dodatkowa walidacja: serial number
                found = store.Certificates
                    .Find(X509FindType.FindBySerialNumber, certToCheck.SerialNumber, validOnly: false)
                    .OfType<X509Certificate2>()
                    .FirstOrDefault();

                if (found != null)
                    return found;

                // Ostateczne porównanie byte-to-byte (bardzo rzadko potrzebne)
                foreach (var c in store.Certificates)
                {
                    if (c.RawData.SequenceEqual(certToCheck.RawData))
                        return c;
                }
            }

            return null; // brak certyfikatu
        }

        /// <summary>
        /// Wczytuje certyfikat z PEM (.crt) lub z DER-binary.
        /// </summary>
        internal static byte[] LoadCertPemOrDer(string path)
        {
            var text = File.ReadAllText(path);

            // Jeśli PEM
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

            // DER (binarny)
            return File.ReadAllBytes(path);
        }

        // Helper: Extract DER bytes from PEM
        internal static byte[] PemToDer(string pem, string section)
        {
            var header = $"-----BEGIN {section}-----";
            var footer = $"-----END {section}-----";
            var start = pem.IndexOf(header, StringComparison.Ordinal);
            var end = pem.IndexOf(footer, StringComparison.Ordinal);
            if (start < 0 || end < 0) throw new ArgumentException("Invalid PEM format");
            var base64 = pem.Substring(start + header.Length, end - (start + header.Length)).Replace("\r", "").Replace("\n", "");
            return Convert.FromBase64String(base64);
        }

        // Helper: Create PFX from cert and key (requires BouncyCastle or similar library)
        // This is a stub. You must implement this using a library like BouncyCastle.
        internal static byte[] CreatePfx(byte[] certBytes, byte[] keyBytes)
        {
            throw new NotImplementedException("PEM to PFX conversion requires BouncyCastle or similar library.");
        }
    }
}
