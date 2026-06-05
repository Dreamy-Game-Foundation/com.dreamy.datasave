using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Dreamy.Datasave
{
    public sealed class DatasaveService : IDatasaveService
    {
        private const int CurrentFormatVersion = 1;
        private const string TempExtension = ".tmp";
        private const string BackupExtension = ".bak";

        private readonly DatasaveOptions options;
        private readonly Dictionary<string, SaveData> loadedData = new();
        private readonly JsonSerializerSettings serializerSettings;

        public DatasaveService(DatasaveOptions options = null)
        {
            this.options = options ?? new DatasaveOptions();
            serializerSettings = new JsonSerializerSettings
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace
            };
        }

        public T Load<T>(string key = null) where T : SaveData, new()
        {
            T data;
            string saveKey = ResolveKey<T>(key);
            string path = GetPath(saveKey);
            RestoreBackupIfNeeded(path);

            if (!File.Exists(path))
            {
                data = new T();
                loadedData[saveKey] = data;
                data.OnAfterLoad();
                return data;
            }

            string encodedEnvelope = File.ReadAllText(path);
            string envelopeJson = options.Codec.Decode(encodedEnvelope);
            var envelope = JsonConvert.DeserializeObject<SaveEnvelope>(envelopeJson, serializerSettings);

            data = JsonConvert.DeserializeObject<T>(envelope.Payload, serializerSettings) ?? new T();
            if (envelope.DataVersion != data.Version)
            {
                data.Migrate(envelope.DataVersion);
            }

            loadedData[saveKey] = data;
            data.OnAfterLoad();
            return data;
        }

        public void Save<T>(T data, string key = null) where T : SaveData
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            string saveKey = ResolveKey<T>(key ?? data.SaveKey);
            data.OnBeforeSave();

            string payload = JsonConvert.SerializeObject(data, Formatting.None, serializerSettings);
            var envelope = new SaveEnvelope
            {
                FormatVersion = CurrentFormatVersion,
                DataType = typeof(T).AssemblyQualifiedName,
                DataVersion = data.Version,
                SavedAtUtcTicks = DateTime.UtcNow.Ticks,
                Payload = payload
            };

            Formatting formatting = options.PrettyPrint ? Formatting.Indented : Formatting.None;
            string envelopeJson = JsonConvert.SerializeObject(envelope, formatting, serializerSettings);
            string encodedEnvelope = options.Codec.Encode(envelopeJson);

            string path = GetPath(saveKey);
            AtomicWrite(path, encodedEnvelope);
            loadedData[saveKey] = data;
        }

        public void SaveAll()
        {
            foreach (KeyValuePair<string, SaveData> pair in loadedData)
            {
                SaveDynamic(pair.Key, pair.Value);
            }
        }

        public bool Exists(string key)
        {
            return File.Exists(GetPath(key));
        }

        public void Delete(string key)
        {
            string path = GetPath(key);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            loadedData.Remove(key);
        }

        public void DeleteAll()
        {
            string root = DatasavePaths.GetSaveRootPath(options.DirectoryName);
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }

            loadedData.Clear();
        }

        private void SaveDynamic(string key, SaveData data)
        {
            Type serviceType = typeof(DatasaveService);
            var method = serviceType.GetMethod(nameof(Save))?.MakeGenericMethod(data.GetType());
            method?.Invoke(this, new object[] { data, key });
        }

        private string ResolveKey<T>(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                return DatasavePaths.SanitizeFileName(key);
            }

            return DatasavePaths.SanitizeFileName(typeof(T).Name);
        }

        private string GetPath(string key)
        {
            string root = DatasavePaths.GetSaveRootPath(options.DirectoryName);
            Directory.CreateDirectory(root);
            return Path.Combine(root, DatasavePaths.SanitizeFileName(key) + options.FileExtension);
        }

        private static void AtomicWrite(string path, string content)
        {
            string tempPath = path + TempExtension;
            string backupPath = path + BackupExtension;

            File.WriteAllText(tempPath, content);

            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }

            if (File.Exists(path))
            {
                File.Move(path, backupPath);
            }

            File.Move(tempPath, path);

            if (File.Exists(backupPath))
            {
                File.Delete(backupPath);
            }
        }

        private static void RestoreBackupIfNeeded(string path)
        {
            string backupPath = path + BackupExtension;
            if (!File.Exists(path) && File.Exists(backupPath))
            {
                File.Move(backupPath, path);
            }
        }
    }
}
