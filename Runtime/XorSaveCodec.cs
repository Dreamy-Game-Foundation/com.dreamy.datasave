using System;
using System.Text;

namespace Dreamy.Datasave
{
    public sealed class XorSaveCodec : ISaveCodec
    {
        private readonly byte[] keyBytes;

        public XorSaveCodec(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new ArgumentException("Save codec key cannot be null or empty.", nameof(key));
            }

            keyBytes = Encoding.UTF8.GetBytes(key);
        }

        public string Encode(string plainText)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(plainText);
            Transform(bytes);
            return Convert.ToBase64String(bytes);
        }

        public string Decode(string encodedText)
        {
            byte[] bytes = Convert.FromBase64String(encodedText);
            Transform(bytes);
            return Encoding.UTF8.GetString(bytes);
        }

        private void Transform(byte[] bytes)
        {
            for (int i = 0; i < bytes.Length; i++)
            {
                bytes[i] ^= keyBytes[i % keyBytes.Length];
            }
        }
    }
}
