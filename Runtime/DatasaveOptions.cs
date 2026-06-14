namespace Dreamy.Datasave
{
    public sealed class DatasaveOptions
    {
        public string DirectoryName { get; set; } = "DreamySaves";
        public string FileExtension { get; set; } = ".json";
        public bool PrettyPrint { get; set; }
        public bool CreateFileOnFirstLoad { get; set; } = true;
        public ISaveCodec Codec { get; set; } = new PlainTextSaveCodec();
    }
}
