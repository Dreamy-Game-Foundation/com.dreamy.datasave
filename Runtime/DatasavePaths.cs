using System.IO;
using UnityEngine;

namespace Dreamy.Datasave
{
    public static class DatasavePaths
    {
        private const string DefaultDirectoryName = "DreamySaves";

        public static string GetSaveRootPath(string directoryName = DefaultDirectoryName)
        {
            return Path.Combine(Application.persistentDataPath, directoryName);
        }

        public static string SanitizeFileName(string key)
        {
            foreach (char invalid in Path.GetInvalidFileNameChars())
            {
                key = key.Replace(invalid, '_');
            }

            return key;
        }
    }
}
