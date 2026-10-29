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
        [Tooltip("A scene's GameMode, run when whatever loads the scene names none (the loader's GameMode comes first).")]
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

            if (mapList.Length != 0) {
                for (int i = 0; i < mapList.Length; i++) {
                    MapConfigList[mapList[i].mapName] = mapList[i];
                }
            }

            levelStreamDictionary.Clear();

            if (levelStreamCollection.Length != 0) {
                for (int i = 0; i < levelStreamCollection.Length; i++) {
                    var stream = levelStreamCollection[i];
                    if (stream?.sceneAddressable == null || !stream.sceneAddressable.RuntimeKeyIsValid()) {
                        Debug.LogWarning($"[WorldConfig] '{name}' level stream {i} has no scene; skipped.");
                        continue;
                    }

                    stream.ResetState();
                    levelStreamDictionary[stream.sceneAddressable.AssetGUID] = stream;
                }
            }
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
    }

    [Serializable]
    public class SceneReference {
#if UNITY_EDITOR
        public SceneAsset Map;
#endif
        public string mapName;
    }
}
