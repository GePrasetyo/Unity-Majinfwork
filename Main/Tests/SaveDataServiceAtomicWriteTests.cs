using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using Majinfwork.SaveSystem;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Majinfwork.Tests {
    /// <summary>
    /// A save must never be lost because a write was interrupted (the app killed, the headset taken off, power lost).
    /// The service writes to a temporary file and swaps it in, so the previous save stays intact until the new one
    /// is complete.
    /// </summary>
    public class SaveDataServiceAtomicWriteTests {
        private string tempDir;

        [SetUp]
        public void SetUp() {
            tempDir = Path.Combine(Path.GetTempPath(), "MajinfworkSaveTests", Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown() {
            try {
                if (Directory.Exists(tempDir)) Directory.Delete(tempDir, true);
            }
            catch (IOException) { /* best effort */ }
        }

        [UnityTest]
        public IEnumerator Failed_Write_Keeps_Previous_Save() => Run(FailedWriteKeepsPreviousSaveAsync);

        [UnityTest]
        public IEnumerator Overwrite_Replaces_Save_And_Leaves_No_Temp_File() => Run(OverwriteAsync);

        [UnityTest]
        public IEnumerator Leftover_Temp_File_Is_Not_A_Save() => Run(LeftoverTempFileAsync);

        private static IEnumerator Run(Func<Task> test) {
            var task = test();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception;
        }

        private async Task FailedWriteKeepsPreviousSaveAsync() {
            var serializer = new FlakySerializer();
            var service = await OpenAsync(serializer);
            Assert.IsTrue(await service.SaveAsync(new ProbeSave { score = 100 }), "first save failed");

            // The write dies halfway: some bytes are out, then the process "crashes".
            serializer.FailNextWrite = true;
            LogAssert.Expect(LogType.Error, new Regex("simulated crash mid-write"));
            Assert.IsFalse(await service.SaveAsync(new ProbeSave { score = 200 }), "an interrupted save reported success");

            var loaded = await (await OpenAsync(new BinarySaveSerializer())).LoadAsync<ProbeSave>(ProbeSave.Name);
            Assert.IsNotNull(loaded, "the previous save was destroyed by the interrupted write");
            Assert.AreEqual(100, loaded.score);
        }

        private async Task OverwriteAsync() {
            var service = await OpenAsync(new BinarySaveSerializer());
            Assert.IsTrue(await service.SaveAsync(new ProbeSave { score = 1 }));
            Assert.IsTrue(await service.SaveAsync(new ProbeSave { score = 2 }));

            var loaded = await (await OpenAsync(new BinarySaveSerializer())).LoadAsync<ProbeSave>(ProbeSave.Name);
            Assert.AreEqual(2, loaded.score);
            Assert.IsEmpty(Directory.GetFiles(tempDir, "*.tmp", SearchOption.AllDirectories), "a temporary file was left behind");
        }

        private async Task LeftoverTempFileAsync() {
            var service = await OpenAsync(new BinarySaveSerializer());
            Assert.IsTrue(await service.SaveAsync(new ProbeSave { score = 7 }));

            // What an interrupted write leaves: a partial temporary file next to the real save.
            string slotDir = Path.Combine(tempDir, "0");
            Assert.IsTrue(File.Exists(Path.Combine(slotDir, ProbeSave.Name + ".dat")), "after save: " + Listing());
            File.WriteAllBytes(Path.Combine(slotDir, ProbeSave.Name + ".dat.tmp"), new byte[] { 1, 2, 3 });

            CollectionAssert.AreEquivalent(new[] { ProbeSave.Name }, service.GetSlotFiles());
            Assert.IsTrue(File.Exists(Path.Combine(slotDir, ProbeSave.Name + ".dat")), "before reload: " + Listing());
            var loaded = await (await OpenAsync(new BinarySaveSerializer())).LoadAsync<ProbeSave>(ProbeSave.Name);
            Assert.AreEqual(7, loaded.score);
        }

        private string Listing() => string.Join(" | ", Directory.GetFiles(tempDir, "*", SearchOption.AllDirectories));

        private async Task<SaveDataService> OpenAsync(ISaveSerializer serializer) {
            var service = new SaveDataService(tempDir, 1, serializer);
            await service.InitializeAsync();
            service.SetCurrentSlot(0);
            return service;
        }

        /// <summary>Writes like the binary serializer, but can die halfway through a write.</summary>
        private sealed class FlakySerializer : ISaveSerializer {
            private readonly BinarySaveSerializer inner = new BinarySaveSerializer();

            public bool FailNextWrite;

            public string FileExtension => inner.FileExtension;

            public void Serialize<T>(Stream stream, T data) where T : class {
                if (FailNextWrite) {
                    FailNextWrite = false;
                    stream.Write(new byte[] { 0x4D, 0x4A }, 0, 2);
                    throw new IOException("simulated crash mid-write");
                }

                inner.Serialize(stream, data);
            }

            public T Deserialize<T>(Stream stream) where T : class => inner.Deserialize<T>(stream);

            public object Deserialize(Stream stream, Type type) => inner.Deserialize(stream, type);
        }

        [Serializable]
        public class ProbeSave : SaveData {
            public const string Name = "AtomicProbeSave";

            public override string FileName => Name;

            public int score;

            public ProbeSave() { }
        }
    }
}
