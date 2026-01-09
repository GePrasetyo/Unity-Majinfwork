using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Majinfwork.CrossRef {
    public static class CrossSceneCloneExtension {
        private const string GlobalDbName = "CrossSceneData/Global_Asset_Refs";

        // Cache: original host -> list of (fieldName, targetGuid)
        private static readonly Dictionary<Object, List<(string fieldName, string targetGuid)>> _linkCache = new();

        // Cache: (Type, fieldName) -> FieldInfo
        private static readonly Dictionary<(Type, string), FieldInfo> _fieldCache = new();

        // All registered clones - kept until clone is destroyed
        // Re-resolves null fields on every scene load to handle unload/reload
        private static readonly List<(Object clone, Object original)> _registeredClones = new(256);
        private static bool _subscribedToSceneLoad;

        private static void EnsureSceneLoadSubscription() {
            if (_subscribedToSceneLoad) return;
            _subscribedToSceneLoad = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            int count = _registeredClones.Count;
            if (count == 0) return;

            // Compact list while re-resolving null fields
            int writeIndex = 0;
            for (int i = 0; i < count; i++) {
                var (clone, original) = _registeredClones[i];

                // Remove destroyed clones
                if (clone == null) {
                    continue;
                }

                // Try to resolve any null fields (handles unload/reload)
                TryResolveNullFields(clone, original);

                // Keep in list
                if (writeIndex != i) {
                    _registeredClones[writeIndex] = _registeredClones[i];
                }
                writeIndex++;
            }

            // Trim destroyed clones
            if (writeIndex < count) {
                _registeredClones.RemoveRange(writeIndex, count - writeIndex);
            }
        }

        private static List<(string fieldName, string targetGuid)> GetLinksForHost(Object host) {
            if (_linkCache.TryGetValue(host, out var cached))
                return cached;

            var db = Resources.Load<CrossSceneDB>(GlobalDbName);
            if (db == null) {
                _linkCache[host] = null;
                return null;
            }

            List<(string, string)> links = null;
            foreach (var link in db.links) {
                if (ReferenceEquals(link.host, host)) {
                    links ??= new List<(string, string)>();
                    links.Add((link.fieldName, link.targetGuid));
                }
            }

            _linkCache[host] = links;
            return links;
        }

        private static FieldInfo GetFieldCached(Type type, string fieldName) {
            var key = (type, fieldName);
            if (_fieldCache.TryGetValue(key, out var cached))
                return cached;

            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _fieldCache[key] = field;
            return field;
        }

        private static bool InjectField(Object target, string fieldName, GameObject sourceGo) {
            var field = GetFieldCached(target.GetType(), fieldName);
            if (field == null) return false;

            object value = field.FieldType == typeof(GameObject)
                ? sourceGo
                : sourceGo.GetComponent(field.FieldType);

            if (value == null) return false;

            field.SetValue(target, value);
            return true;
        }

        /// <summary>
        /// Tries to resolve fields that are currently null.
        /// Used on scene load to handle unload/reload scenarios.
        /// </summary>
        private static void TryResolveNullFields(Object clone, Object original) {
            var links = GetLinksForHost(original);
            if (links == null) return;

            foreach (var (fieldName, targetGuid) in links) {
                var field = GetFieldCached(clone.GetType(), fieldName);
                if (field == null) continue;

                // Only resolve if field is currently null
                var currentValue = field.GetValue(clone);
                if (currentValue != null && !(currentValue is Object obj && obj == null)) {
                    continue;
                }

                var targetGo = CrossSceneAnchor.Find(targetGuid);
                if (targetGo == null) continue;

                InjectField(clone, fieldName, targetGo);
            }
        }

        /// <summary>
        /// Tries to resolve all links for a clone.
        /// </summary>
        private static void TryResolveAllLinks(Object clone, Object original) {
            var links = GetLinksForHost(original);
            if (links == null) return;

            foreach (var (fieldName, targetGuid) in links) {
                var targetGo = CrossSceneAnchor.Find(targetGuid);
                if (targetGo == null) continue;

                InjectField(clone, fieldName, targetGo);
            }
        }

        /// <summary>
        /// Resolves CrossSceneReference fields for a cloned object.
        /// Works with ScriptableObjects, MonoBehaviours, and any UnityEngine.Object.
        /// Automatically re-resolves when scenes load/unload/reload.
        /// </summary>
        public static void ResolveCrossSceneReferences(this Object clone, Object original) {
            if (clone == null || original == null) return;

            EnsureSceneLoadSubscription();

            // Try immediate resolution
            TryResolveAllLinks(clone, original);

            // Register for scene load handling (unload/reload support)
            _registeredClones.Add((clone, original));
        }

        /// <summary>
        /// Resolves CrossSceneReference fields for a batch of cloned objects.
        /// Works with ScriptableObjects, MonoBehaviours, and any UnityEngine.Object.
        /// Automatically re-resolves when scenes load/unload/reload.
        /// </summary>
        public static void ResolveCrossSceneReferences<T>(this Dictionary<T, T> cloneMap) where T : Object {
            foreach (var (original, clone) in cloneMap) {
                clone.ResolveCrossSceneReferences(original);
            }
        }

        /// <summary>
        /// Resolves CrossSceneReference fields for all MonoBehaviours on an instantiated GameObject.
        /// Call after Instantiate() on a prefab that has components with [CrossSceneReference] fields.
        /// Automatically re-resolves when scenes load/unload/reload.
        /// </summary>
        public static void ResolveCrossSceneReferences(this GameObject clone, GameObject original, bool includeChildren = true) {
            if (clone == null || original == null) return;

            var cloneComponents = includeChildren
                ? clone.GetComponentsInChildren<MonoBehaviour>(true)
                : clone.GetComponents<MonoBehaviour>();

            var originalComponents = includeChildren
                ? original.GetComponentsInChildren<MonoBehaviour>(true)
                : original.GetComponents<MonoBehaviour>();

            // Match components by type and index
            var originalByType = new Dictionary<Type, List<MonoBehaviour>>();
            foreach (var comp in originalComponents) {
                if (comp == null) continue;
                var type = comp.GetType();
                if (!originalByType.TryGetValue(type, out var list)) {
                    list = new List<MonoBehaviour>();
                    originalByType[type] = list;
                }
                list.Add(comp);
            }

            var cloneByType = new Dictionary<Type, List<MonoBehaviour>>();
            foreach (var comp in cloneComponents) {
                if (comp == null) continue;
                var type = comp.GetType();
                if (!cloneByType.TryGetValue(type, out var list)) {
                    list = new List<MonoBehaviour>();
                    cloneByType[type] = list;
                }
                list.Add(comp);
            }

            // Resolve for matching pairs
            foreach (var (type, originals) in originalByType) {
                if (!cloneByType.TryGetValue(type, out var clones)) continue;

                int count = Math.Min(originals.Count, clones.Count);
                for (int i = 0; i < count; i++) {
                    clones[i].ResolveCrossSceneReferences(originals[i]);
                }
            }
        }

        /// <summary>
        /// Manually unregister a clone when you're done with it.
        /// Optional - clones are automatically cleaned up when destroyed.
        /// </summary>
        public static void UnregisterCrossSceneClone(this Object clone) {
            for (int i = _registeredClones.Count - 1; i >= 0; i--) {
                if (ReferenceEquals(_registeredClones[i].clone, clone)) {
                    // Swap with last and remove (O(1))
                    int lastIndex = _registeredClones.Count - 1;
                    if (i != lastIndex) {
                        _registeredClones[i] = _registeredClones[lastIndex];
                    }
                    _registeredClones.RemoveAt(lastIndex);
                    break;
                }
            }
        }
    }
}
