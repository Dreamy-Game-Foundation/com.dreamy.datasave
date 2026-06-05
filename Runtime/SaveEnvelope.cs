namespace Dreamy.Datasave
{
    internal sealed class SaveEnvelope
    {
        public int FormatVersion;
        public string DataType;
        public int DataVersion;
        public long SavedAtUtcTicks;
        public string Payload;
    }
}
