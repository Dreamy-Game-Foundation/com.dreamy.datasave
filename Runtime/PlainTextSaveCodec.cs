namespace Dreamy.Datasave
{
    public sealed class PlainTextSaveCodec : ISaveCodec
    {
        public string Encode(string plainText)
        {
            return plainText;
        }

        public string Decode(string encodedText)
        {
            return encodedText;
        }
    }
}
