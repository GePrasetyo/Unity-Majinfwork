using System;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;

namespace Majinfwork.World {
    public enum SceneLoadStatus { Unloaded, Loading, Loaded, Unloading }

    /// <summary>
    /// One Addressable scene streamed additively by <see cref="LevelStreamer"/>: the scene to stream, and the state
    /// of its stream. The state is runtime only, never serialized, and only the streamer changes it.
    /// </summary>
    [Serializable]
    public class AddressableSceneHandler {
        public AssetReference sceneAddressable;

        [NonSerialized] internal AsyncOperationHandle<SceneInstance> loadHandle;
        [NonSerialized] internal SceneLoadStatus status;
        /// <summary>The last load or unload asked of this scene: the next one starts after it.</summary>
        [NonSerialized] internal Task pending;

        public SceneLoadStatus Status {
            get {
                Sync();
                return status;
            }
        }

        public bool IsLoaded => Status == SceneLoadStatus.Loaded;

        /// <summary>The loaded scene, or an invalid one while it isn't loaded.</summary>
        public Scene Scene => IsLoaded ? loadHandle.Result.Scene : default;

        /// <summary>
        /// Catches up with an unload the streamer didn't make: a Single-mode load takes every additive scene with
        /// it, and Addressables releases the scene's handle then.
        /// </summary>
        internal void Sync() {
            if (status == SceneLoadStatus.Loaded && (!loadHandle.IsValid() || !loadHandle.Result.Scene.isLoaded)) {
                loadHandle = default;
                status = SceneLoadStatus.Unloaded;
            }
        }

        /// <summary>Forgets a stream from a previous play session (the handler lives on in a config asset).</summary>
        internal void ResetState() {
            loadHandle = default;
            status = SceneLoadStatus.Unloaded;
            pending = null;
        }

        public override string ToString() {
            return sceneAddressable != null ? sceneAddressable.RuntimeKey.ToString() : "(no scene)";
        }
    }
}
