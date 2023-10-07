using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Majingari.Framework.World {
    [CreateAssetMenu(fileName = "Default World Config", menuName = "Config Object/World Config")]
    public class WorldConfig : ScriptableObject {
        [SerializeField] private WorldAssetConfig[] mapList = new WorldAssetConfig[0];
        [SerializeField] private AddressableSceneHandler[] levelStreamCollection = Array.Empty<AddressableSceneHandler>();

        public Dictionary<string, WorldAssetConfig> MapConfigList = new Dictionary<string, WorldAssetConfig>();
        public Dictionary<string, AddressableSceneHandler> levelStreamDictionary = new Dictionary<string, AddressableSceneHandler>();

        public void SetupDictionary() {
            MapConfigList.Clear();

            if (mapList.Length != 0) {
                for (int i = 0; i < mapList.Length; i++) {
                    MapConfigList[mapList[i].mapName] = mapList[i];
                }
            }

            levelStreamDictionary.Clear();

            if (levelStreamCollection.Length != 0) {
                for (int i = 0; i < levelStreamCollection.Length; i++) {
                    levelStreamCollection[i].status = SceneLoadStatus.Unloaded;
                    levelStreamDictionary[levelStreamCollection[i].sceneAddressable.AssetGUID] = levelStreamCollection[i];
                }
            }
        }
    }

    [Serializable]
    public class WorldAssetConfig {
#if UNITY_EDITOR
        public SceneAsset Map;
#endif
        public string mapName;
        public GameModeManager TheGameMode;
    }

    [Serializable]
    public class SceneReference {
#if UNITY_EDITOR
        public SceneAsset Map;
#endif
        public string mapName;
    }

    [Serializable]
    public class AddressableSceneHandler {
        public string sceneName;
        public AssetReference sceneAddressable;
        internal AsyncOperationHandle<SceneInstance> streamHandler;
        internal Action<string> streamHandlerCompleted;
        internal SceneLoadStatus status = SceneLoadStatus.Unloaded;

        public void UpdateHandler(AsyncOperationHandle<SceneInstance> obj) {
            if (obj.Status == AsyncOperationStatus.Failed) {
                goto Reset;
            }

            LightProbes.TetrahedralizeAsync();
            streamHandler = obj;
            streamHandlerCompleted?.Invoke(obj.Result.Scene.path);

            Reset:
            streamHandlerCompleted = null;
            status = obj.Result.Scene.isLoaded? SceneLoadStatus.Loaded:SceneLoadStatus.Unloaded;
        }
    }

    internal enum SceneLoadStatus { Unloaded, Loading, Loaded }
}
