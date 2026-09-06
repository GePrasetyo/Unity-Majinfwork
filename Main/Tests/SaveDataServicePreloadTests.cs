using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Majinfwork.SaveSystem;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace Majinfwork.Tests {
    /// <summary>
    /// End-to-end cover for the path HighScoreSaveData actually travels: save through
    /// SaveDataService, then reload it in a fresh service via PreloadAllAsync.
    ///
    /// This is the flow that broke when the serializer moved off BinaryFormatter - the
    /// serializer's own unit tests all passed while this was failing, because they only ever
    /// deserialized a concrete type directly.
    /// </summary>
    public class SaveDataServicePreloadTests {
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
        public IEnumerator Preloaded_Save_Survives_Service_Restart() {
            var task = RunRestartAsync();
            while (!task.IsCompleted) yield return null;
            if (task.IsFaulted) throw task.Exception;
        }

        private async Task RunRestartAsync() {
            var serializer = new BinarySaveSerializer();

            // First run: write a save.
            var writer = new PreloadOnlyService(tempDir, serializer, typeof(ProbeSave));
            await writer.InitializeAsync();
            writer.SetCurrentSlot(0);

            var saved = await writer.SaveAsync(new ProbeSave { score = 12345, combo = 78 });
            Assert.IsTrue(saved, "SaveAsync reported failure");

            // Second run: fresh service over the same directory, as on game restart.
            var reader = new PreloadOnlyService(tempDir, serializer, typeof(ProbeSave));
            await reader.InitializeAsync();
            reader.SetCurrentSlot(0);
            await reader.PreloadAllAsync();

            var loaded = reader.GetPreloaded<ProbeSave>();

            Assert.IsNotNull(loaded, "preloaded save was null - the score would silently reset");
            Assert.AreEqual(12345, loaded.score, "high score did not survive the round trip");
            Assert.AreEqual(78, loaded.combo);
        }

        /// <summary>
        /// Pins the preload set instead of reflecting over every loaded assembly, so the test
        /// does not depend on - or pollute - global SaveData discovery.
        /// </summary>
        private sealed class PreloadOnlyService : SaveDataService {
            private readonly Type[] preloadTypes;

            public PreloadOnlyService(string baseDirectory, ISaveSerializer serializer, params Type[] preloadTypes)
                : base(baseDirectory, 3, serializer) {
                this.preloadTypes = preloadTypes;
            }

            protected override Type[] GetPreloadSaveDataTypes() => preloadTypes;
        }

        /// <summary>
        /// Public with a public parameterless constructor so Activator.CreateInstance can build
        /// it, mirroring how the service instantiates real SaveData types.
        /// </summary>
        [Serializable]
        public class ProbeSave : SaveData {
            public override string FileName => "ProbeSave";
            public override bool PreloadOnInit => true;

            public int score;
            public int combo;

            public ProbeSave() { }
        }
    }
}
