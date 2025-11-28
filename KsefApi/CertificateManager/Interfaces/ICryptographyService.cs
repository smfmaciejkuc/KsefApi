namespace CertificateManager.Services
{
    public interface ICryptographyService
    {
        string GetHashData(byte[] file);
    }
}