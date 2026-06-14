using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Dreamy.Datasave
{
    public sealed class AesSaveCodec : ISaveCodec
    {
        private const string AuthenticatedPrefix = "DAS2:";
        private const int SaltLength = 16;
        private const int IvLength = 16;
        private const int EncryptionKeyLength = 32;
        private const int AuthenticationKeyLength = 32;
        private const int AuthenticationTagLength = 32;
        private const int LegacyDerivationIterations = 10000;
        private const int DerivationIterations = 100000;

        private readonly string password;

        public AesSaveCodec(string password)
        {
            if (string.IsNullOrEmpty(password))
            {
                throw new ArgumentException("AES password cannot be null or empty.", nameof(password));
            }

            this.password = password;
        }

        public string Encode(string plainText)
        {
            byte[] salt = RandomBytes(SaltLength);
            byte[] iv = RandomBytes(IvLength);
            DeriveKeys(
                salt,
                DerivationIterations,
                out byte[] encryptionKey,
                out byte[] authenticationKey);

            using var aes = Aes.Create();
            aes.Key = encryptionKey;
            aes.IV = iv;

            byte[] cipherText;
            using (var cipherOutput = new MemoryStream())
            {
                using (var crypto = new CryptoStream(
                           cipherOutput,
                           aes.CreateEncryptor(),
                           CryptoStreamMode.Write))
                using (var writer = new StreamWriter(crypto, Encoding.UTF8))
                {
                    writer.Write(plainText);
                }

                cipherText = cipherOutput.ToArray();
            }

            byte[] authenticatedData = Combine(salt, iv, cipherText);
            byte[] authenticationTag;
            using (var hmac = new HMACSHA256(authenticationKey))
            {
                authenticationTag = hmac.ComputeHash(authenticatedData);
            }

            return AuthenticatedPrefix + Convert.ToBase64String(
                Combine(authenticatedData, authenticationTag));
        }

        public string Decode(string encodedText)
        {
            if (encodedText == null)
            {
                throw new ArgumentNullException(nameof(encodedText));
            }

            return encodedText.StartsWith(AuthenticatedPrefix, StringComparison.Ordinal)
                ? DecodeAuthenticated(encodedText.Substring(AuthenticatedPrefix.Length))
                : DecodeLegacy(encodedText);
        }

        private string DecodeAuthenticated(string encodedText)
        {
            byte[] bytes = Convert.FromBase64String(encodedText);
            int minimumLength = SaltLength + IvLength + AuthenticationTagLength + 1;
            if (bytes.Length < minimumLength)
            {
                throw new CryptographicException("Authenticated AES payload is truncated.");
            }

            byte[] salt = new byte[SaltLength];
            byte[] iv = new byte[IvLength];
            int cipherTextLength = bytes.Length - SaltLength - IvLength - AuthenticationTagLength;
            byte[] cipherText = new byte[cipherTextLength];
            byte[] storedTag = new byte[AuthenticationTagLength];

            Buffer.BlockCopy(bytes, 0, salt, 0, SaltLength);
            Buffer.BlockCopy(bytes, SaltLength, iv, 0, IvLength);
            Buffer.BlockCopy(bytes, SaltLength + IvLength, cipherText, 0, cipherTextLength);
            Buffer.BlockCopy(
                bytes,
                bytes.Length - AuthenticationTagLength,
                storedTag,
                0,
                AuthenticationTagLength);

            DeriveKeys(
                salt,
                DerivationIterations,
                out byte[] encryptionKey,
                out byte[] authenticationKey);
            byte[] authenticatedData = Combine(salt, iv, cipherText);
            byte[] computedTag;
            using (var hmac = new HMACSHA256(authenticationKey))
            {
                computedTag = hmac.ComputeHash(authenticatedData);
            }

            if (!CryptographicOperations.FixedTimeEquals(storedTag, computedTag))
            {
                throw new CryptographicException(
                    "Authenticated AES payload failed integrity validation.");
            }

            return Decrypt(cipherText, encryptionKey, iv);
        }

        private string DecodeLegacy(string encodedText)
        {
            byte[] bytes = Convert.FromBase64String(encodedText);
            if (bytes.Length <= SaltLength + IvLength)
            {
                throw new CryptographicException("Legacy AES payload is truncated.");
            }

            byte[] salt = new byte[SaltLength];
            byte[] iv = new byte[IvLength];
            byte[] cipherText = new byte[bytes.Length - SaltLength - IvLength];
            Buffer.BlockCopy(bytes, 0, salt, 0, SaltLength);
            Buffer.BlockCopy(bytes, SaltLength, iv, 0, IvLength);
            Buffer.BlockCopy(bytes, SaltLength + IvLength, cipherText, 0, cipherText.Length);

            using var deriveBytes = new Rfc2898DeriveBytes(
                password,
                salt,
                LegacyDerivationIterations,
                HashAlgorithmName.SHA256);
            byte[] encryptionKey = deriveBytes.GetBytes(EncryptionKeyLength);
            return Decrypt(cipherText, encryptionKey, iv);
        }

        private static string Decrypt(byte[] cipherText, byte[] encryptionKey, byte[] iv)
        {
            using var aes = Aes.Create();
            aes.Key = encryptionKey;
            aes.IV = iv;

            using var input = new MemoryStream(cipherText);
            using var crypto = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var reader = new StreamReader(crypto, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private void DeriveKeys(
            byte[] salt,
            int iterations,
            out byte[] encryptionKey,
            out byte[] authenticationKey)
        {
            using var deriveBytes = new Rfc2898DeriveBytes(
                password,
                salt,
                iterations,
                HashAlgorithmName.SHA256);
            byte[] keyMaterial = deriveBytes.GetBytes(
                EncryptionKeyLength + AuthenticationKeyLength);
            encryptionKey = new byte[EncryptionKeyLength];
            authenticationKey = new byte[AuthenticationKeyLength];
            Buffer.BlockCopy(keyMaterial, 0, encryptionKey, 0, EncryptionKeyLength);
            Buffer.BlockCopy(
                keyMaterial,
                EncryptionKeyLength,
                authenticationKey,
                0,
                AuthenticationKeyLength);
        }

        private static byte[] RandomBytes(int count)
        {
            byte[] bytes = new byte[count];
            RandomNumberGenerator.Fill(bytes);
            return bytes;
        }

        private static byte[] Combine(params byte[][] arrays)
        {
            int totalLength = 0;
            foreach (byte[] array in arrays)
            {
                totalLength += array.Length;
            }

            byte[] result = new byte[totalLength];
            int offset = 0;
            foreach (byte[] array in arrays)
            {
                Buffer.BlockCopy(array, 0, result, offset, array.Length);
                offset += array.Length;
            }

            return result;
        }
    }
}
