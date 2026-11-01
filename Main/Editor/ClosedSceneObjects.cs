using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Majinfwork.CrossRef {
    /// <summary>
    /// Reads names of objects in a scene that isn't open, from the scene file itself (text serialization).
    /// </summary>
    public static class ClosedSceneObjects {
        public enum Anchor { Unreadable, NotSaved, Saved }

        /// <summary>
        /// Whether the scene file holds the anchor with this id, and the name of its object when the file tells it.
        /// </summary>
        public static Anchor FindAnchored(string scenePath, string anchorId, out string objectName) {
            objectName = null;
            if (EditorSettings.serializationMode != SerializationMode.ForceText || !File.Exists(scenePath)) return Anchor.Unreadable;

            string scene = File.ReadAllText(scenePath);
            int at = scene.IndexOf("_guid: " + anchorId, StringComparison.Ordinal);
            if (at < 0) return Anchor.NotSaved;

            objectName = NameOfOwner(scene, at);
            return Anchor.Saved;
        }

        private static string NameOfOwner(string scene, int anchorAt) {
            // The anchor names its GameObject.
            Match owner = Regex.Match(DocumentFrom(scene, scene.LastIndexOf("\n--- ", anchorAt, StringComparison.Ordinal) + 1), @"m_GameObject: \{fileID: (-?\d+)\}");
            if (!owner.Success) return null;

            string gameObject = Document(scene, "1", owner.Groups[1].Value, out bool stripped);
            if (gameObject == null) return null;
            if (!stripped) return Field(gameObject, "m_Name");

            // A prefab instance's object: its name is the instance's override, or the prefab's own.
            Match source = Regex.Match(gameObject, @"m_CorrespondingSourceObject: \{fileID: (-?\d+), guid: ([0-9a-f]+)");
            Match instance = Regex.Match(gameObject, @"m_PrefabInstance: \{fileID: (-?\d+)\}");
            if (!source.Success || !instance.Success) return null;

            string sourceId = source.Groups[1].Value;
            string prefabGuid = source.Groups[2].Value;
            string prefabInstance = Document(scene, "1001", instance.Groups[1].Value, out _);
            if (prefabInstance != null) {
                Match renamed = Regex.Match(prefabInstance,
                    @"- target: \{fileID: " + sourceId + ", guid: " + prefabGuid + @", type: 3\}[ \t]*\r?\n[ \t]*propertyPath: m_Name[ \t]*\r?\n[ \t]*value: (.*)$",
                    RegexOptions.Multiline);
                if (renamed.Success) return renamed.Groups[1].Value.Trim();
            }

            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(prefabGuid))) {
                if (asset is GameObject prefabObject
                    && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefabObject, out _, out long localId)
                    && localId.ToString() == sourceId) {
                    return prefabObject.name;
                }
            }

            return null;
        }

        private static string Document(string scene, string classId, string fileId, out bool stripped) {
            Match header = Regex.Match(scene, @"^--- !u!" + classId + " &" + fileId + @"( stripped)?[ \t]*\r?$", RegexOptions.Multiline);
            stripped = header.Success && header.Groups[1].Success;
            return header.Success ? DocumentFrom(scene, header.Index) : null;
        }

        // From a document's header to the next one.
        private static string DocumentFrom(string scene, int start) {
            int end = scene.IndexOf("\n--- ", start + 1, StringComparison.Ordinal);
            return end < 0 ? scene.Substring(start) : scene.Substring(start, end - start);
        }

        private static string Field(string document, string name) {
            Match field = Regex.Match(document, @"^[ \t]*" + name + @": (.*?)\r?$", RegexOptions.Multiline);
            return field.Success ? field.Groups[1].Value.Trim() : null;
        }
    }
}
