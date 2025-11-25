using CertificateManager.Interfaces;
using QRCoder;
using SkiaSharp;
using System;

namespace CertificateManager.Services
{
    public class QrCodeService : IQrCodeService
    {
        public byte[] GenerateQrCode(string payloadUrl, int pixelsPerModule = 20, int qrCodeResolutionInPx = 300)
        {
            if (string.IsNullOrWhiteSpace(payloadUrl))
                throw new ArgumentException("Payload URL cannot be null or empty.", nameof(payloadUrl));

            using (var generator = new QRCodeGenerator())
            using (var qrData = generator.CreateQrCode(payloadUrl, QRCodeGenerator.ECCLevel.Q))
            {
                var pngQr = new PngByteQRCode(qrData);
                var pngBytes = pngQr.GetGraphic(pixelsPerModule);

                // If a specific resolution is requested, resize with SkiaSharp
                if (qrCodeResolutionInPx > 0)
                {
                    using (var srcBitmap = SKBitmap.Decode(pngBytes))
                    {
                        if (srcBitmap.Width != qrCodeResolutionInPx || srcBitmap.Height != qrCodeResolutionInPx)
                        {
                            var info = new SKImageInfo(qrCodeResolutionInPx, qrCodeResolutionInPx);
                            using (var surface = SKSurface.Create(info))
                            {
                                var canvas = surface.Canvas;
                                canvas.Clear(SKColors.White);
                                var destRect = new SKRect(0, 0, qrCodeResolutionInPx, qrCodeResolutionInPx);
                                canvas.DrawBitmap(srcBitmap, destRect);
                                using (var image = surface.Snapshot())
                                using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                                {
                                    return data.ToArray();
                                }
                            }
                        }
                    }
                }

                return pngBytes;
            }
        }

        public byte[] AddLabelToQrCode(byte[] qrCodePng, string label, int fontSizePx = 14)
        {
            if (qrCodePng == null || qrCodePng.Length == 0)
                throw new ArgumentException("QR code PNG bytes cannot be null or empty.", nameof(qrCodePng));

            label = label ?? string.Empty;

            using (var qrBitmap = SKBitmap.Decode(qrCodePng))
            {
                int width = qrBitmap.Width;
                int height = qrBitmap.Height;

                using (var paint = new SKPaint())
                {
                    paint.IsAntialias = true;
                    paint.Color = SKColors.Black;
                    paint.Typeface = SKTypeface.FromFamilyName("Arial");
                    paint.TextSize = fontSizePx;

                    var textBounds = new SKRect();
                    paint.MeasureText(label, ref textBounds);
                    int labelHeight = (int)Math.Ceiling(textBounds.Height) + 4;

                    var info = new SKImageInfo(width, height + labelHeight);
                    using (var surface = SKSurface.Create(info))
                    {
                        var canvas = surface.Canvas;
                        canvas.Clear(SKColors.White);

                        // Draw QR
                        canvas.DrawBitmap(qrBitmap, new SKPoint(0, 0));

                        // Draw label centered
                        float centerX = width / 2f;
                        float labelBaselineY = height + (labelHeight / 2f) + (textBounds.Height / 2f) - textBounds.Bottom;
                        var textAlign = SKTextAlign.Center;
                        paint.TextAlign = textAlign;
                        canvas.DrawText(label, centerX, labelBaselineY, paint);

                        using (var image = surface.Snapshot())
                        using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
                        {
                            return data.ToArray();
                        }
                    }
                }
            }
        }
    }
}