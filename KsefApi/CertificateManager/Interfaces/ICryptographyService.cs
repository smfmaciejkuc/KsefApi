namespace CertificateManager.Services
{
    public interface ICryptographyService
    {
        string GetHashData(byte[] file);
        //byte[] GetByteHashData(byte[] file);
    }
}