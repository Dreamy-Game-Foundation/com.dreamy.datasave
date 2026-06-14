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
            if (this.options.Codec == null)
            {
                throw new ArgumentException("Datasave codec cannot be null.", nameof(options));
            }

            serializerSettings = new JsonSerializerSettings
            {
                ObjectCreationHandling = ObjectCreationHandling.Replace
            };
        }

        public T Load<T>(string key = null) where T : SaveData, new()
        {
            T data;
            string saveKey = ResolveKey<T>(key);
            string path = GetPath(saveKey, false);
            RestoreBackupIfNeeded(path);

            if (!File.Exists(path))
            {
                data = new T();
                loadedData[saveKey] = data;
                data.OnAfterLoad();
                if (options.CreateFileOnFirstLoad)
                {
                    Save(data, saveKey);
                }

                return data;
            }

            try
            {
                data = LoadFromPath<T>(saveKey, path);
            }
            catch (Exception mainException)
            {
                string backupPath = path + BackupExtension;
                if (!File.Exists(backupPath))
                {
                    throw WrapLoadException(saveKey, path, mainException);
                }

                try
                {
                    data = LoadFromPath<T>(saveKey, backupPath);
                    File.Copy(backupPath, path, true);
                    Debug.LogWarning(
                        $"Recovered save '{saveKey}' from the last-known-good backup.");
                }
                catch (Exception backupException)
                {
                    throw new DatasaveException(
                        saveKey,
                        path,
                        "Recovery",
                        $"Save '{saveKey}' and its backup could not be loaded.",
                        new AggregateException(mainException, backupException));
                }
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
            SaveInternal(saveKey, data, typeof(T));
        }

        public void SaveAll()
        {
            KeyValuePair<string, SaveData>[] snapshot =
                new KeyValuePair<string, SaveData>[loadedData.Count];
            int index = 0;
            foreach (KeyValuePair<string, SaveData> pair in loadedData)
            {
                snapshot[index++] = pair;
            }

            foreach (KeyValuePair<string, SaveData> pair in snapshot)
            {
                if (pair.Value == null)
                {
                    Debug.LogWarning(
                        $"Skipped null save data for key '{pair.Key}'.");
                    continue;
                }

                SaveDynamic(pair.Key, pair.Value);
            }
        }

        public bool Exists(string key)
        {
            return File.Exists(GetPath(key, false));
        }

        public void Delete(string key)
        {
            string saveKey = DatasavePaths.SanitizeFileName(key);
            string path = GetPath(saveKey, false);
            if (File.Exists(path))
            {
                File.Delete(path);
            }

            DeleteIfExists(path + BackupExtension);
            DeleteIfExists(path + TempExtension);
            loadedData.Remove(saveKey);
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
            SaveInternal(key, data, data.GetType());
        }

        private string ResolveKey<T>(string key)
        {
            if (!string.IsNullOrEmpty(key))
            {
                return DatasavePaths.SanitizeFileName(key);
            }

            return DatasavePaths.SanitizeFileName(typeof(T).Name);
        }

        private string GetPath(string key, bool createDirectory)
        {
            string root = DatasavePaths.GetSaveRootPath(options.DirectoryName);
            if (createDirectory)
            {
                Directory.CreateDirectory(root);
            }

            return Path.Combine(root, DatasavePaths.SanitizeFileName(key) + options.FileExtension);
        }

        private T LoadFromPath<T>(string saveKey, string path) where T : SaveData, new()
        {
            string encodedEnvelope = File.ReadAllText(path);
            string envelopeJson = options.Codec.Decode(encodedEnvelope);
            SaveEnvelope envelope = JsonConvert.DeserializeObject<SaveEnvelope>(
                envelopeJson,
                serializerSettings);
            ValidateEnvelope<T>(saveKey, path, envelope);

            T data = JsonConvert.DeserializeObject<T>(envelope.Payload, serializerSettings);
            if (data == null)
            {
                throw new DatasaveException(
                    saveKey,
                    path,
                    "Payload",
                    $"Save '{saveKey}' produced null data.");
            }

            if (envelope.DataVersion > data.Version)
            {
                throw new DatasaveException(
                    saveKey,
                    path,
                    "Migration",
                    $"Save '{saveKey}' uses data version {envelope.DataVersion}, " +
                    $"but this client supports version {data.Version}.");
            }

            if (envelope.DataVersion < data.Version)
            {
                data.Migrate(envelope.DataVersion);
            }

            return data;
        }

        private void SaveInternal(string saveKey, SaveData data, Type dataType)
        {
            data.OnBeforeSave();

            string payload = JsonConvert.SerializeObject(
                data,
                dataType,
                Formatting.None,
                serializerSettings);
            SaveEnvelope envelope = new SaveEnvelope
            {
                FormatVersion = CurrentFormatVersion,
                DataType = dataType.AssemblyQualifiedName,
                DataVersion = data.Version,
                SavedAtUtcTicks = DateTime.UtcNow.Ticks,
                Payload = payload
            };

            Formatting formatting = options.PrettyPrint ? Formatting.Indented : Formatting.None;
            string envelopeJson = JsonConvert.SerializeObject(
                envelope,
                formatting,
                serializerSettings);
            string encodedEnvelope = options.Codec.Encode(envelopeJson);
            string path = GetPath(saveKey, true);
            AtomicWrite(path, encodedEnvelope);
            loadedData[saveKey] = data;
        }

        private static void ValidateEnvelope<T>(
            string saveKey,
            string path,
            SaveEnvelope envelope)
            where T : SaveData
        {
            if (envelope == null)
            {
                throw new DatasaveException(
                    saveKey,
                    path,
                    "Envelope",
                    $"Save '{saveKey}' does not contain a valid envelope.");
            }

            if (envelope.FormatVersion != CurrentFormatVersion)
            {
                throw new DatasaveException(
                    saveKey,
                    path,
                    "Envelope",
                    $"Save '{saveKey}' uses unsupported format version " +
                    $"{envelope.FormatVersion}. Supported version: {CurrentFormatVersion}.");
            }

            if (string.IsNullOrWhiteSpace(envelope.Payload))
            {
                throw new DatasaveException(
                    saveKey,
                    path,
                    "Envelope",
                    $"Save '{saveKey}' has an empty payload.");
            }

            string storedTypeName = GetTypeFullName(envelope.DataType);
            if (string.IsNullOrWhiteSpace(storedTypeName))
            {
                throw new DatasaveException(
                    saveKey,
                    path,
                    "Envelope",
                    $"Save '{saveKey}' does not declare its data type.");
            }

            if (!string.Equals(storedTypeName, typeof(T).FullName, StringComparison.Ordinal))
            {
                throw new DatasaveException(
                    saveKey,
                    path,
                    "Envelope",
                    $"Save '{saveKey}' contains '{storedTypeName}', " +
                    $"but '{typeof(T).FullName}' was requested.");
            }

            if (envelope.DataVersion < 0)
            {
                throw new DatasaveException(
                    saveKey,
                    path,
                    "Envelope",
                    $"Save '{saveKey}' has invalid data version {envelope.DataVersion}.");
            }
        }

        private static string GetTypeFullName(string assemblyQualifiedName)
        {
            if (string.IsNullOrWhiteSpace(assemblyQualifiedName))
            {
                return null;
            }

            int separatorIndex = assemblyQualifiedName.IndexOf(',');
            return separatorIndex >= 0
                ? assemblyQualifiedName.Substring(0, separatorIndex).Trim()
                : assemblyQualifiedName.Trim();
        }

        private static DatasaveException WrapLoadException(
            string saveKey,
            string path,
            Exception exception)
        {
            return exception as DatasaveException ?? new DatasaveException(
                saveKey,
                path,
                "Load",
                $"Save '{saveKey}' could not be loaded.",
                exception);
        }

        private static void AtomicWrite(string path, string content)
        {
            string tempPath = path + TempExtension;
            string backupPath = path + BackupExtension;

            File.WriteAllText(tempPath, content);

            if (File.Exists(path))
            {
                if (File.Exists(backupPath))
                {
                    File.Delete(backupPath);
                }

                File.Move(path, backupPath);
            }

            File.Move(tempPath, path);
        }

        private static void RestoreBackupIfNeeded(string path)
        {
            string backupPath = path + BackupExtension;
            if (!File.Exists(path) && File.Exists(backupPath))
            {
                File.Move(backupPath, path);
            }
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
