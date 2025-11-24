using System.Security.Cryptography.X509Certificates;

namespace CertyficateManager
{
    public class RuntimeCert
    {
        public string Thumbprint { get; set; }
        public X509Certificate2 Certificate { get; set; }
        public byte[] EncryptedPassword { get; set; } // AES encrypted
        public byte[] AesKey { get; set; }            // Protected with DPAPI
        public byte[] AesIV { get; set; }             // Protected with DPAPI
    }
}
