using CertificateManager.Models;
using System.Security.Cryptography.X509Certificates;

namespace CertificateManager
{
    public interface ICertificateService
    {
        X509Certificate2 CreateCertificateFromPem(string certPem, string keyPem);
        X509Certificate2 CreateCertificateFromPem(string certPem, string keyPem, string privateKeyPassword);
        X509Certificate2 CreateCertificateFromPem(string pathKeyPem, string pathCertPem, string pfxPassword, bool nonExportable = true, StoreLocation storeLocation = StoreLocation.CurrentUser);
        X509Certificate2 FindExistingCertificate(string pathCert, StoreLocation storeLocation = StoreLocation.LocalMachine);
        RuntimeCert GetCertificate(string name);
        string GetCertificatePassword(string name);
        string ImportCertificateToStore(X509Certificate2 cert, StoreLocation storeLocation = StoreLocation.CurrentUser);
        string ImportPemKeyAndCertToStore(string pathKeyPem, string pathCertPem, string pfxPassword, StoreLocation storeLocation = StoreLocation.CurrentUser, bool nonExportable = true);
        bool IsCertificateLoaded(string name);
        X509Certificate2 LoadCertificateFromFiles(string crtPath, string keyPath, string password);
        void RegisterCertificate(string name, X509Certificate2 cert, string password);
    }
}