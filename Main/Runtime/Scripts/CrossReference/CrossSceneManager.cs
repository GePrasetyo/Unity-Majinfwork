using UnityEngine;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

namespace Majinfwork.CrossRef {
    public class CrossSceneManager : MonoBehaviour {
        // Cache for FieldInfo to make Reflection near-native speed
        private static readonly Dictionary<string, FieldInfo> _fieldCache = new Dictionary<string, FieldInfo>();
        // Cache for Database Assets so we don't Resources.Load every time
        private static readonly Dictionary<string, CrossSceneDB> _dbCache = new Dictionary<string, CrossSceneDB>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap() {
            var go = new GameObject("[Cross Scene Manager]");
            go.AddComponent<CrossSceneManager>();
            DontDestroyOnLoad(go);
        }

        private void OnEnable() => SceneManager.sceneLoaded += OnSceneLoaded;
        private void OnDisable() => SceneManager.sceneLoaded -= OnSceneLoaded;

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
            // 1. Only resolve the Partial DB for the scene that just arrived
            ResolveDatabase($"{scene.name}_Refs");

            // 2. Always resolve the Global DB (for ScriptableObjects/Assets)
            ResolveDatabase("Global_Asset_Refs");
        }

        public void ResolveDatabase(string dbName) {
            // Try cache first, then Resources
            if (!_dbCache.TryGetValue(dbName, out CrossSceneDB db)) {
                db = Resources.Load<CrossSceneDB>($"CrossSceneData/{dbName}");
                if (db != null) _dbCache[dbName] = db;
            }

            if (db == null) return;

            // Surgical Injection: Only loop through this specific scene's links
            foreach (var link in db.links) {
                if (link.host == null || string.IsNullOrEmpty(link.targetGuid)) continue;

                GameObject targetGo = CrossSceneAnchor.Find(link.targetGuid);
                if (targetGo == null) continue;

                InjectFast(link.host, link.fieldName, targetGo);
            }
        }

        private void InjectFast(Object host, string fieldName, GameObject targetGo) {
            string key = $"{host.GetType().FullName}_{fieldName}";

            if (!_fieldCache.TryGetValue(key, out FieldInfo field)) {
                field = host.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                _fieldCache[key] = field;
            }

            if (field != null) {
                Component targetComponent = targetGo.GetComponent(field.FieldType);
                field.SetValue(host, (field.FieldType == typeof(GameObject)) ? (object)targetGo : targetComponent);
            }
        }
    }
}