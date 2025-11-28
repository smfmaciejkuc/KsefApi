using CertificateManager.Services;
using System;

namespace TestKsefFeatures
{
    public class SignDataTests
    {
        [Fact]
        public void SignDataWithCertificate()
        {

        }

        [Fact]
        public void SignWithThumbprint_ThrowsWhenCertMissing()
        {
            var thumb = "0000000000000000000000000000000000000000"; // nieistniejący
            byte[] data = System.Text.Encoding.UTF8.GetBytes("hello");
            Assert.Throws<InvalidOperationException>(() => SignatureService.SignWithThumbprint(thumb, data));
        }
    }
}
