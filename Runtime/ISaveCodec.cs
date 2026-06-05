namespace Dreamy.Datasave
{
    public interface ISaveCodec
    {
        string Encode(string plainText);
        string Decode(string encodedText);
    }
}
