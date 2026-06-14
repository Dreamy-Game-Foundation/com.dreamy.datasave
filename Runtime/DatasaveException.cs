using System;

namespace Dreamy.Datasave
{
    public sealed class DatasaveException : Exception
    {
        public DatasaveException(
            string key,
            string path,
            string stage,
            string message,
            Exception innerException = null)
            : base(message, innerException)
        {
            Key = key;
            Path = path;
            Stage = stage;
        }

        public string Key { get; }

        public string Path { get; }

        public string Stage { get; }
    }
}
