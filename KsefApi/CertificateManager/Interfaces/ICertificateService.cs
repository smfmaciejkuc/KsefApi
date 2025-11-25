using CertificateManager.Models;
using System.Security.Cryptography.X509Certificates;

namespace CertificateManager.Interfaces
{
    public interface ICertificateService
    {
        X509Certificate2 CreateCertificateFromPem(string certPem, string keyPem);
        RuntimeCert GetCertificate(string name);
        string GetCertificatePassword(string name);
        bool IsCertificateLoaded(string name);
        X509Certificate2 LoadCertificateFromFiles(string crtPath, string keyPath, string password);
        void RegisterCertificate(string name, X509Certificate2 cert, string password);
    }
}