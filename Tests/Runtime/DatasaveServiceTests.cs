using System;
using System.IO;
using System.Security.Cryptography;
using NUnit.Framework;

namespace Dreamy.Datasave.Tests
{
    public sealed class DatasaveServiceTests
    {
        private const string SaveKey = "test-save";

        private DatasaveOptions options;

        [SetUp]
        public void SetUp()
        {
            options = new DatasaveOptions
            {
                DirectoryName = "DreamyDatasaveTests-" + Guid.NewGuid().ToString("N"),
                CreateFileOnFirstLoad = false
            };
        }

        [TearDown]
        public void TearDown()
        {
            string root = DatasavePaths.GetSaveRootPath(options.DirectoryName);
            if (Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }

        [Test]
        public void Load_WhenMainFileIsCorrupted_RecoversLastKnownGoodBackup()
        {
            DatasaveService service = new DatasaveService(options);
            service.Save(new TestSaveData { Value = 10 }, SaveKey);
            service.Save(new TestSaveData { Value = 20 }, SaveKey);

            string path = GetSavePath();
            File.WriteAllText(path, "corrupted");

            DatasaveService reloadedService = new DatasaveService(options);
            TestSaveData recovered = reloadedService.Load<TestSaveData>(SaveKey);

            Assert.That(recovered.Value, Is.EqualTo(10));
            Assert.That(File.ReadAllText(path), Is.Not.EqualTo("corrupted"));
        }

        [Test]
        public void Load_WhenEnvelopeIsInvalid_ThrowsDatasaveException()
        {
            string path = GetSavePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, "{}");

            DatasaveService service = new DatasaveService(options);

            DatasaveException exception = Assert.Throws<DatasaveException>(
                () => service.Load<TestSaveData>(SaveKey));
            Assert.That(exception.Stage, Is.EqualTo("Envelope"));
        }

        [Test]
        public void AesCodec_WhenPayloadIsModified_RejectsIt()
        {
            AesSaveCodec codec = new AesSaveCodec("test-password");
            string encoded = codec.Encode("important-data");
            const string prefix = "DAS2:";
            byte[] bytes = Convert.FromBase64String(encoded.Substring(prefix.Length));
            bytes[bytes.Length / 2] ^= 0x01;
            string modified = prefix + Convert.ToBase64String(bytes);

            Assert.Throws<CryptographicException>(() => codec.Decode(modified));
        }

        private string GetSavePath()
        {
            return Path.Combine(
                DatasavePaths.GetSaveRootPath(options.DirectoryName),
                SaveKey + options.FileExtension);
        }

        public sealed class TestSaveData : SaveData
        {
            public int Value;
        }
    }
}
