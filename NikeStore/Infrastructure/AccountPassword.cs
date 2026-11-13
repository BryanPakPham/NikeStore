using System;
using System.Security.Cryptography;

namespace NikeStore.Infrastructure
{
    internal static class AccountPassword
    {
        private const string Prefix = "PBKDF2-SHA256$";
        private const int Iterations = 600000;
        private const int SaltSize = 16;
        private const int KeySize = 32;

        public static string Hash(string password)
        {
            var salt = new byte[SaltSize];
            using (var random = RandomNumberGenerator.Create())
                random.GetBytes(salt);
            using (var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                return Prefix + Convert.ToBase64String(salt) + "$" +
                       Convert.ToBase64String(derive.GetBytes(KeySize));
            }
        }

        public static bool Matches(string stored, string password)
        {
            if (string.IsNullOrEmpty(stored) || string.IsNullOrEmpty(password)) return false;
            // Các tài khoản đã có trong phiên bản đầu lưu mật khẩu dạng văn bản.
            if (!stored.StartsWith(Prefix, StringComparison.Ordinal))
                return string.Equals(stored, password, StringComparison.Ordinal);

            var pieces = stored.Substring(Prefix.Length).Split('$');
            if (pieces.Length != 2) return false;
            try
            {
                var salt = Convert.FromBase64String(pieces[0]);
                var expected = Convert.FromBase64String(pieces[1]);
                if (salt.Length != SaltSize || expected.Length != KeySize) return false;
                using (var derive = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                {
                    var actual = derive.GetBytes(KeySize);
                    int difference = 0;
                    for (int i = 0; i < KeySize; i++) difference |= actual[i] ^ expected[i];
                    return difference == 0;
                }
            }
            catch (FormatException)
            {
                return false;
            }
        }
    }
}
