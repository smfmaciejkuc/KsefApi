using System;

namespace CertificateManager.Extensions
{
    public static class Base64UrlExtensions
    {
        public static string EncodeBase64UrlToString(this byte[] blob)
        {
            return Convert.ToBase64String(blob).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        }
    }
}
