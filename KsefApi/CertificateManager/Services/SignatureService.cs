using System;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

namespace CertificateManager.Services
{
    public class SignatureService
    {
        public static byte[] SignWithThumbprint(string thumbprint, byte[] data, StoreLocation storeLocation = StoreLocation.CurrentUser)
        {
            using (var store = new X509Store(StoreName.My, storeLocation))
            {
                store.Open(OpenFlags.ReadOnly);
                var cert = store.Certificates.Find(X509FindType.FindByThumbprint, thumbprint, validOnly: false)
                              .OfType<X509Certificate2>().FirstOrDefault();
                if (cert == null) throw new InvalidOperationException("\"Certyfikat nieznaleziony\"");

                using (var rsa = cert.GetRSAPrivateKey())
                {
                    if (rsa == null) throw new InvalidOperationException("\"Brak klucza prywatnego\"");
                    return rsa.SignData(data, System.Security.Cryptography.HashAlgorithmName.SHA256, System.Security.Cryptography.RSASignaturePadding.Pkcs1);
                }
            }
        }

    }
}
