using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Majinfwork.SaveSystem;
using NUnit.Framework;

namespace Majinfwork.Tests {
    /// <summary>
    /// Covers the GZip+JSON binary save format that replaced BinaryFormatter in the Unity 6.6
    /// upgrade: the DateTime round-trip that plain JsonUtility drops, and the migration path
    /// that keeps pre-6.6 BinaryFormatter save files loading.
    /// </summary>
    public class BinarySaveSerializerTests {
        private BinarySaveSerializer serializer;

        [SetUp]
        public void SetUp() {
            serializer = new BinarySaveSerializer();
        }

        private T RoundTrip<T>(T value) where T : class {
            using (var stream = new MemoryStream()) {
                serializer.Serialize(stream, value);
                stream.Position = 0;
                return serializer.Deserialize<T>(stream);
            }
        }

        // ---------- current format ----------

        [Test]
        public void RoundTrip_Preserves_Container_Slots_And_Metadata() {
            var container = new SaveContainer(3);
            var slot = container.GetSlot(1);
            slot.isValid = true;
            slot.displayName = "Second Run";
            slot.totalPlayTimeSeconds = 4321.5;
            slot.SetMetadata("stage", "3");

            var result = RoundTrip(container);

            Assert.AreEqual(3, result.SlotCount);
            var restored = result.GetSlot(1);
            Assert.IsTrue(restored.isValid);
            Assert.AreEqual("Second Run", restored.displayName);
            Assert.AreEqual(4321.5, restored.totalPlayTimeSeconds, 0.0001);
            Assert.AreEqual("3", restored.GetMetadata("stage"));
        }

        [Test]
        public void RoundTrip_Preserves_DateTime_Fields() {
            var container = new SaveContainer(1);
            var saved = new DateTime(2026, 9, 6, 12, 34, 56, DateTimeKind.Utc);
            container.lastModified = saved;
            container.GetSlot(0).lastSaveTime = saved;

            var result = RoundTrip(container);

            Assert.AreEqual(saved, result.lastModified);
            Assert.AreEqual(saved, result.GetSlot(0).lastSaveTime, "nested slot DateTime");
        }

        [Test]
        public void Default_DateTime_Stays_Default() {
            var container = new SaveContainer(1);
            container.GetSlot(0).lastSaveTime = default;

            var result = RoundTrip(container);

            Assert.AreEqual(default(DateTime), result.GetSlot(0).lastSaveTime);
            Assert.AreEqual("Never", result.GetSlot(0).FormattedLastSaveTime);
        }

        [Test]
        public void Serialize_Writes_Magic_Header() {
            using (var stream = new MemoryStream()) {
                serializer.Serialize(stream, new SaveContainer(1));
                var bytes = stream.ToArray();

                Assert.Greater(bytes.Length, 5);
                Assert.AreEqual("MJSV", Encoding.ASCII.GetString(bytes, 0, 4));
                Assert.AreEqual(1, bytes[4], "format version");
            }
        }

        [Test]
        public void Deserialize_Rejects_Empty_Stream() {
            using (var stream = new MemoryStream(Array.Empty<byte>())) {
                Assert.Throws<InvalidDataException>(() => serializer.Deserialize<SaveContainer>(stream));
            }
        }

        // ---------- polymorphic load (the real SaveDataService path) ----------

        /// <summary>
        /// Mirrors how SaveDataService loads a save file: it knows the concrete type from
        /// GetAllSaveDataTypes() and passes it in, rather than deserializing into the abstract
        /// SaveData base. This is the path HighScoreSaveData travels on preload.
        /// </summary>
        [Test]
        public void Deserialize_With_Runtime_Type_Recovers_Concrete_SaveData() {
            var data = new ProbeSaveData { score = 4242 };

            using (var stream = new MemoryStream()) {
                serializer.Serialize(stream, data);
                stream.Position = 0;

                var loaded = serializer.Deserialize(stream, typeof(ProbeSaveData)) as SaveData;

                Assert.IsNotNull(loaded, "runtime-type load returned null");
                Assert.IsInstanceOf<ProbeSaveData>(loaded, "concrete type was not recovered");
                Assert.AreEqual(4242, ((ProbeSaveData)loaded).score);
                Assert.AreEqual("1", loaded.version, "inherited base field survived");
            }
        }

        /// <summary>
        /// The abstract base cannot be materialised by JsonUtility, which is exactly why the
        /// runtime-type overload exists. Pinned so nobody reintroduces Deserialize&lt;SaveData&gt;.
        /// </summary>
        [Test]
        public void Deserialize_Into_Abstract_Base_Is_Not_Supported() {
            using (var stream = new MemoryStream()) {
                serializer.Serialize(stream, new ProbeSaveData { score = 1 });
                stream.Position = 0;

                Assert.Throws<ArgumentException>(
                    () => serializer.Deserialize<SaveData>(stream));
            }
        }

        [Serializable]
        private sealed class ProbeSaveData : SaveData {
            public override string FileName => "ProbeSave";
            public int score;
        }

        // ---------- legacy BinaryFormatter migration ----------

        [Test]
        public void Reads_Legacy_BinaryFormatter_Save() {
            var savedAt = new DateTime(2025, 12, 25, 9, 30, 0, DateTimeKind.Utc);
            var legacy = new LegacyContainer {
                version = "1",
                lastModified = savedAt,
                slots = new List<LegacySlot> {
                    new LegacySlot {
                        index = 0,
                        isValid = true,
                        displayName = "Old Save",
                        lastSaveTime = savedAt,
                        totalPlayTimeSeconds = 987.5,
                        metadata = new List<MetadataEntry> { new MetadataEntry("stage", "7") },
                    },
                },
            };

            using (var stream = WriteLegacy(legacy)) {
                var result = serializer.Deserialize<SaveContainer>(stream);

                Assert.IsNotNull(result, "legacy container should load");
                Assert.AreEqual("1", result.version);
                Assert.AreEqual(savedAt, result.lastModified, "container DateTime survived migration");
                Assert.AreEqual(1, result.SlotCount);

                var slot = result.GetSlot(0);
                Assert.IsTrue(slot.isValid);
                Assert.AreEqual("Old Save", slot.displayName);
                Assert.AreEqual(savedAt, slot.lastSaveTime, "slot DateTime survived migration");
                Assert.AreEqual(987.5, slot.totalPlayTimeSeconds, 0.0001);
                Assert.AreEqual("7", slot.GetMetadata("stage"));
            }
        }

        [Test]
        public void Migrated_Save_Rewrites_In_Current_Format() {
            var savedAt = new DateTime(2025, 12, 25, 9, 30, 0, DateTimeKind.Utc);
            var legacy = new LegacyContainer {
                version = "1",
                lastModified = savedAt,
                slots = new List<LegacySlot> {
                    new LegacySlot { index = 0, isValid = true, displayName = "Old Save", lastSaveTime = savedAt },
                },
            };

            SaveContainer migrated;
            using (var stream = WriteLegacy(legacy)) {
                migrated = serializer.Deserialize<SaveContainer>(stream);
            }

            // Re-saving must produce the new format and still preserve the timestamps.
            var reloaded = RoundTrip(migrated);

            Assert.AreEqual(savedAt, reloaded.lastModified);
            Assert.AreEqual(savedAt, reloaded.GetSlot(0).lastSaveTime);
            Assert.AreEqual("Old Save", reloaded.GetSlot(0).displayName);
        }

        /// <summary>
        /// Serializes old-shaped types under the production type names, reproducing a save file
        /// written before the 6.6 upgrade - crucially without the tick fields added since.
        /// </summary>
        private static MemoryStream WriteLegacy(LegacyContainer container) {
            var stream = new MemoryStream();
#pragma warning disable UAC0023, SYSLIB0011
            var formatter = new BinaryFormatter { Binder = new WriteAsProductionTypes() };
            formatter.Serialize(stream, container);
#pragma warning restore UAC0023, SYSLIB0011
            stream.Position = 0;
            return stream;
        }

        private sealed class WriteAsProductionTypes : SerializationBinder {
            private static readonly string ProdAssembly = typeof(SaveContainer).Assembly.FullName;
            private static readonly string ListAssembly = typeof(List<int>).Assembly.FullName;

            public override void BindToName(Type serializedType, out string assemblyName, out string typeName) {
                if (serializedType == typeof(LegacyContainer)) {
                    assemblyName = ProdAssembly;
                    typeName = "Majinfwork.SaveSystem.SaveContainer";
                }
                else if (serializedType == typeof(LegacySlot)) {
                    assemblyName = ProdAssembly;
                    typeName = "Majinfwork.SaveSystem.SaveSlot";
                }
                else if (serializedType == typeof(List<LegacySlot>)) {
                    assemblyName = ListAssembly;
                    typeName = "System.Collections.Generic.List`1[[Majinfwork.SaveSystem.SaveSlot, "
                               + ProdAssembly + "]]";
                }
                else {
                    assemblyName = serializedType.Assembly.FullName;
                    typeName = serializedType.FullName;
                }
            }

            public override Type BindToType(string assemblyName, string typeName) => null;
        }

        // Field-for-field mirrors of the pre-6.6 SaveContainer/SaveSlot.
        [Serializable]
        private sealed class LegacyContainer {
            public List<LegacySlot> slots = new List<LegacySlot>();
            public string version = "1";
            public DateTime lastModified;
        }

        [Serializable]
        private sealed class LegacySlot {
            public int index;
            public bool isValid;
            public string displayName;
            public DateTime lastSaveTime;
            public double totalPlayTimeSeconds;
            public List<MetadataEntry> metadata = new List<MetadataEntry>();
        }
    }
}
