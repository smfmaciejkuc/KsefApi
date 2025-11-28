using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CertificateManager.Services;

namespace TestKsefFeatures
{
    public class QRCodetests
    {
        [Fact]
        public void GenerateAndReadQRCode()
        {

        }

        [Fact]
        public void GenerateQRCode_ReturnsPngBytes()
        {
            var svc = new QrCodeService();
            var png = svc.GenerateQrCode("https://example.com/test", pixelsPerModule: 8, qrCodeResolutionInPx: 256);
            Assert.NotNull(png);
            Assert.True(png.Length > 100); // co najmniej kilkaset bajtów dla małego QR
            // PNG magic header 89 50 4E 47
            Assert.Equal(0x89, png[0]);
            Assert.Equal(0x50, png[1]);
            Assert.Equal(0x4E, png[2]);
            Assert.Equal(0x47, png[3]);
        }
    }
}
