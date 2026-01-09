using System.Collections.Generic;
using UnityEngine;

namespace Majinfwork.CrossRef {
    [AddComponentMenu("")]
    [DisallowMultipleComponent]
    public class CrossSceneAnchor : MonoBehaviour {
        [SerializeField, HideInInspector] private string _guid = System.Guid.NewGuid().ToString();
        public string Guid => _guid;

        private static Dictionary<string, GameObject> _registry = new Dictionary<string, GameObject>();

        private void OnEnable() { if (!_registry.ContainsKey(_guid)) _registry.Add(_guid, gameObject); }

        private void OnDisable() => _registry.Remove(_guid);

        public static GameObject Find(string id) => _registry.TryGetValue(id, out var go) ? go : null;
    }
}