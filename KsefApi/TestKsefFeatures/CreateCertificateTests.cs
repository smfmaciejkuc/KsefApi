using CertificateManager; // CertificateService lives in CertificateManager namespace
using System;
using System.Security.Cryptography.X509Certificates;

namespace TestKsefFeatures
{
    public class CreateCertificateTests
    {
        [Fact]
        public void CreatePfxCertificate_FromPemStrings_ThrowsOnInvalidData()
        {
            // Arrange
            string badCert = "-----BEGIN CERTIFICATE-----\nINVALID\n-----END CERTIFICATE-----";
            string badKey = "-----BEGIN PRIVATE KEY-----\nINVALID\n-----END PRIVATE KEY-----";
            var svc = new CertificateService();

            // Act & Assert
            Assert.Throws<FormatException>(new Action(() => svc.CreateCertificateFromPem(badCert, badKey)));
        }

        [Fact]
        public void CreatePfxCertificate_FromFiles_InvalidFileType_Throws()
        {
            var svc = new CertificateService();
            Assert.Throws<System.ArgumentException>(new Action(() =>
                svc.CreateCertificateFromFile("not_exists.key", "not_exists.crt", "pwd")));
        }

        [Fact]
        public void CreatePfxCertificate_FromInvalidPem_Throws()
        {

            string invalidPemCert = "-----BEGIN CERTIFICATE-----\nINVALID\n-----END CERTIFICATE-----";
            string invalidPemKey = "-----BEGIN PRIVATE KEY-----\nINVALID\n-----END PRIVATE KEY-----";
            var svc = new CertificateService();


            Assert.Throws<FormatException>(new Action(() => svc.CreateCertificateFromPem(invalidPemCert, invalidPemKey)));
        }
    }
}
