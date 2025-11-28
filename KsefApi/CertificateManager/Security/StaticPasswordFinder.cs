using System;
using Org.BouncyCastle.OpenSsl;

namespace CertificateManager.Security
{
    public sealed class StaticPasswordFinder : IPasswordFinder
    {
        private readonly char[] _password;

        public StaticPasswordFinder(string password)
        {
            if (string.IsNullOrEmpty(password))
                throw new ArgumentException("Password must not be empty.", nameof(password));

            _password = password.ToCharArray();
        }

        public StaticPasswordFinder(System.Security.SecureString secure)
        {
            if (secure == null || secure.Length == 0) throw new ArgumentException("SecureString empty.", nameof(secure));
            _password = new char[secure.Length];
            IntPtr ptr = System.Runtime.InteropServices.Marshal.SecureStringToGlobalAllocUnicode(secure);
            for (int i = 0; i < _password.Length; i++)
                _password[i] = '\0';
        }

        public char[] GetPassword()
        {
            return _password;
        }

        // Optional helper to wipe the internal copy after use (call manually).
        public void Clear()
        {
            for (int i = 0; i < _password.Length; i++)
                _password[i] = '\0';
        }
    }
}