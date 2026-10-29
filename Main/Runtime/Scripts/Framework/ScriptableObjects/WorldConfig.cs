using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Majinfwork.World {
    [CreateAssetMenu(fileName = "Default World Config", menuName = "MFramework/Config Object/World Config")]
    public class WorldConfig : ScriptableObject {
        [Header("Scene Configuration")]
        [Tooltip("Each map's streaming levels, and its GameMode for when whatever loads the map names none (the loader's GameMode comes first).")]
        [SerializeField] private WorldAssetConfig[] mapList = new WorldAssetConfig[0];
        [SerializeField] private AddressableSceneHandler[] levelStreamCollection = Array.Empty<AddressableSceneHandler>();

        [Header("Default Fallback")]
        [Tooltip("GameMode used when neither the loader nor the scene names one.")]
        [SerializeField, FormerlySerializedAs("builtInDefaultGameMode")] private GameModeManager defaultMode;

        // Runtime lookup caches, rebuilt from mapList/levelStreamCollection in SetupSceneConfiguration().
        // Unity 6.6 can serialize dictionaries, so mark these explicitly non-serialized.
        [NonSerialized] public Dictionary<string, WorldAssetConfig> MapConfigList = new Dictionary<string, WorldAssetConfig>();
        [NonSerialized] public Dictionary<string, AddressableSceneHandler> levelStreamDictionary = new Dictionary<string, AddressableSceneHandler>();

        [SerializeReference, ClassReference] private LoadingStreamer loadingHandler;

        [Tooltip("Optional additional loading streamers, addressable by key via " +
                 "LoadingStreamerRegistry.Get(\"<key>\"). A caller asking for an unknown " +
                 "key falls back to the default loadingHandler above.")]
        [SerializeField] private KeyedLoadingStreamer[] keyedLoadingStreamers = System.Array.Empty<KeyedLoadingStreamer>();

        public GameModeManager DefaultGameMode => defaultMode;

        /// <summary>The Addressable scenes streamed with <paramref name="sceneName"/> (none when the map list doesn't name it).</summary>
        public IReadOnlyList<AddressableSceneHandler> StreamingLevelsOf(string sceneName) {
            foreach (WorldAssetConfig map in mapList) {
                if (map.mapName == sceneName) {
                    return map.streamingLevels;
                }
            }

            return Array.Empty<AddressableSceneHandler>();
        }

        public void SetupSceneConfiguration() {
            loadingHandler.Initialize();
            ServiceLocator.Register<LoadingStreamer>(loadingHandler);

            var registry = new LoadingStreamerRegistry(loadingHandler);
            if (keyedLoadingStreamers != null) {
                for (int i = 0; i < keyedLoadingStreamers.Length; i++) {
                    var entry = keyedLoadingStreamers[i];
                    if (entry.streamer == null) continue;
                    entry.streamer.Initialize();
                    registry.Register(entry.key, entry.streamer);
                }
            }
            ServiceLocator.Register<LoadingStreamerRegistry>(registry);

            MapConfigList.Clear();

            levelStreamDictionary.Clear();

            for (int i = 0; i < mapList.Length; i++) {
                MapConfigList[mapList[i].mapName] = mapList[i];
                foreach (AddressableSceneHandler stream in mapList[i].streamingLevels) {
                    RegisterStream(stream, mapList[i].mapName);
                }
            }

            for (int i = 0; i < levelStreamCollection.Length; i++) {
                RegisterStream(levelStreamCollection[i], $"level stream {i}");
            }
        }

        // Every stream, a map's or the collection's, is found by its scene's GUID, starting each play unloaded.
        private void RegisterStream(AddressableSceneHandler stream, string owner) {
            if (stream?.sceneAddressable == null || !stream.sceneAddressable.RuntimeKeyIsValid()) {
                Debug.LogWarning($"[WorldConfig] '{name}' {owner} has a stream with no scene; skipped.");
                return;
            }

            stream.ResetState();
            levelStreamDictionary[stream.sceneAddressable.AssetGUID] = stream;
        }
    }

    [Serializable]
    public class KeyedLoadingStreamer {
        [Tooltip("Lookup key — callers do LoadingStreamerRegistry.Get(key).")]
        public string key;
        [SerializeReference, ClassReference] public LoadingStreamer streamer;
    }

    [Serializable]
    public class WorldAssetConfig {
#if UNITY_EDITOR
        public SceneAsset Map;
#endif
        public string mapName;
        [FormerlySerializedAs("BuiltInGameMode")] public GameModeManager gameMode;
        [Tooltip("Addressable scenes streamed in on top of the map: what is seen, lit and collided with around it.")]
        public AddressableSceneHandler[] streamingLevels = Array.Empty<AddressableSceneHandler>();
    }

    [Serializable]
    public class SceneReference {
#if UNITY_EDITOR
        public SceneAsset Map;
#endif
        public string mapName;
    }
}
