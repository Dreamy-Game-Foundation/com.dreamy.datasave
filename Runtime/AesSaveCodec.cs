using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Dreamy.Datasave
{
    public sealed class AesSaveCodec : ISaveCodec
    {
        private const int SaltLength = 16;
        private const int IvLength = 16;
        private const int KeyLength = 32;
        private const int DerivationIterations = 10000;

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
            byte[] key = DeriveKey(salt);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var output = new MemoryStream();
            output.Write(salt, 0, salt.Length);
            output.Write(iv, 0, iv.Length);

            using (var crypto = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write))
            using (var writer = new StreamWriter(crypto, Encoding.UTF8))
            {
                writer.Write(plainText);
            }

            return Convert.ToBase64String(output.ToArray());
        }

        public string Decode(string encodedText)
        {
            byte[] bytes = Convert.FromBase64String(encodedText);
            byte[] salt = new byte[SaltLength];
            byte[] iv = new byte[IvLength];

            Buffer.BlockCopy(bytes, 0, salt, 0, SaltLength);
            Buffer.BlockCopy(bytes, SaltLength, iv, 0, IvLength);

            byte[] key = DeriveKey(salt);

            using var aes = Aes.Create();
            aes.Key = key;
            aes.IV = iv;

            using var input = new MemoryStream(bytes, SaltLength + IvLength, bytes.Length - SaltLength - IvLength);
            using var crypto = new CryptoStream(input, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var reader = new StreamReader(crypto, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        private byte[] DeriveKey(byte[] salt)
        {
            using var deriveBytes = new Rfc2898DeriveBytes(password, salt, DerivationIterations, HashAlgorithmName.SHA256);
            return deriveBytes.GetBytes(KeyLength);
        }

        private static byte[] RandomBytes(int count)
        {
            byte[] bytes = new byte[count];
            RandomNumberGenerator.Fill(bytes);
            return bytes;
        }
    }
}
