using System;
using UnityEngine;

namespace Majinfwork.CrossRef {
    /// <summary>The component type a cross-scene reference points at, for its drawer.</summary>
    public interface ICrossSceneRef {
        Type TargetType { get; }
    }

    /// <summary>
    /// A scene object that an asset can point at. The field keeps the id of the target's CrossSceneAnchor,
    /// so the link lives with its owner and resolves while the target's scene is loaded.
    /// It also keeps the asset id of that scene, so the editor can name it and check the link while it isn't open.
    /// </summary>
    [Serializable]
    public struct CrossSceneRef<T> : ICrossSceneRef where T : Component {
        [SerializeField] private string anchorId;
        [SerializeField] private string sceneId;

        public bool IsSet => !string.IsNullOrEmpty(anchorId);

        public T Resolve() {
            GameObject target = IsSet ? CrossSceneAnchor.Find(anchorId) : null;
            return target != null ? target.GetComponent<T>() : null;
        }

        Type ICrossSceneRef.TargetType => typeof(T);
    }
}
