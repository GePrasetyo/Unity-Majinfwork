using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace Majinfwork.CrossRef {
    /// <summary>
    /// Picks a scene object for a CrossSceneRef: gives the object a CrossSceneAnchor and stores the anchor's id
    /// and the scene's id in the field. The anchor is part of the link, so the scene is saved with it
    /// when that writes nothing else; otherwise the field asks for the save.
    /// </summary>
    [CustomPropertyDrawer(typeof(CrossSceneRef<>))]
    public sealed class CrossSceneRefDrawer : PropertyDrawer {
        private const string AnchorIdPath = "anchorId";
        private const string SceneIdPath = "sceneId";

        // Unity's style class for the text inside an ObjectField; there is no API to set that text.
        private const string ObjectFieldDisplayLabel = "unity-object-field-display__label";

        public override VisualElement CreatePropertyGUI(SerializedProperty property) {
            SerializedProperty anchorId = property.FindPropertyRelative(AnchorIdPath);
            SerializedProperty sceneId = property.FindPropertyRelative(SceneIdPath);
            Type targetType = ((ICrossSceneRef)property.boxedValue).TargetType;

            var root = new VisualElement();
            var field = new ObjectField(preferredLabel) { objectType = targetType, allowSceneObjects = true };
            field.AddToClassList(ObjectField.alignedFieldUssClassName);
            Label display = field.Q<Label>(className: ObjectFieldDisplayLabel);
            var status = new HelpBox();
            root.Add(field);
            root.Add(status);

            // The scene whose save the new anchor still waits for, if it couldn't be saved with the pick.
            string unsavedScene = null;

            void Show(string message, HelpBoxMessageType type) {
                status.text = message;
                status.messageType = type;
                status.style.display = string.IsNullOrEmpty(message) ? DisplayStyle.None : DisplayStyle.Flex;
            }

            // What the closed scene's file says about the anchor, read again only when the file or the link changes.
            string readKey = null;
            ClosedSceneObjects.Anchor inFile = ClosedSceneObjects.Anchor.Unreadable;
            string objectName = null;

            void ReadClosedScene(string scenePath) {
                string key = $"{scenePath}|{File.GetLastWriteTimeUtc(scenePath).Ticks}|{anchorId.stringValue}";
                if (key == readKey) return;

                readKey = key;
                inFile = ClosedSceneObjects.FindAnchored(scenePath, anchorId.stringValue, out objectName);
            }

            void Refresh() {
                Component target = FindTarget(anchorId.stringValue, targetType);
                field.SetValueWithoutNotify(target);

                string scenePath = AssetDatabase.GUIDToAssetPath(sceneId.stringValue);
                string sceneName = Path.GetFileNameWithoutExtension(scenePath);
                if (string.IsNullOrEmpty(anchorId.stringValue)) Show(null, HelpBoxMessageType.None);
                else if (target != null) Show(unsavedScene != null ? $"Save {sceneName} to keep this link." : null, HelpBoxMessageType.Warning);
                else if (SceneManager.GetSceneByPath(scenePath).isLoaded) Show($"Link broken: its anchor isn't in {sceneName} any more. Pick the target again.", HelpBoxMessageType.Error);
                else if (string.IsNullOrEmpty(scenePath)) Show($"Linked to anchor {anchorId.stringValue.Substring(0, 8)}, in a scene that isn't open.", HelpBoxMessageType.Info);
                else {
                    ReadClosedScene(scenePath);
                    if (inFile == ClosedSceneObjects.Anchor.NotSaved) {
                        Show($"Link broken: its anchor isn't saved in {sceneName}. Pick the target again.", HelpBoxMessageType.Error);
                    }
                    else {
                        // The field can't hold an object from a closed scene: name it in the field's text instead.
                        if (display != null) display.text = objectName != null ? $"{objectName} ({sceneName})" : $"In {sceneName}";
                        string what = objectName ?? "an object";
                        Show($"Linked to {what} in {sceneName}, which isn't open.", HelpBoxMessageType.Info);
                    }
                }
            }

            field.RegisterValueChangedCallback(change => {
                Link(change.newValue as Component, anchorId, sceneId, out unsavedScene);
                anchorId.serializedObject.ApplyModifiedProperties();
                Refresh();
            });

            // The target comes and goes with its scene, or with its object: look it up again when either changes.
            void OnSceneOpened(Scene scene, OpenSceneMode mode) => Refresh();
            void OnSceneClosed(Scene scene) => Refresh();
            void OnSceneSaved(Scene scene) {
                if (scene.path == unsavedScene) unsavedScene = null;
                Refresh();
            }

            root.RegisterCallback<AttachToPanelEvent>(_ => {
                EditorSceneManager.sceneOpened += OnSceneOpened;
                EditorSceneManager.sceneClosed += OnSceneClosed;
                EditorSceneManager.sceneSaved += OnSceneSaved;
                EditorApplication.hierarchyChanged += Refresh;
            });
            root.RegisterCallback<DetachFromPanelEvent>(_ => {
                EditorSceneManager.sceneOpened -= OnSceneOpened;
                EditorSceneManager.sceneClosed -= OnSceneClosed;
                EditorSceneManager.sceneSaved -= OnSceneSaved;
                EditorApplication.hierarchyChanged -= Refresh;
            });

            Refresh();
            return root;
        }

        // Points the field at the target. A new anchor is saved with its scene unless the scene holds other unsaved
        // edits, which the pick mustn't write: then unsavedScene names the scene still to save.
        private static void Link(Component target, SerializedProperty anchorId, SerializedProperty sceneId, out string unsavedScene) {
            unsavedScene = null;
            anchorId.stringValue = string.Empty;
            sceneId.stringValue = string.Empty;
            if (target == null) return;

            Scene scene = target.gameObject.scene;
            if (!scene.IsValid() || EditorSceneManager.IsPreviewScene(scene)) {
                Debug.LogWarning($"{target.name} isn't in a scene: a cross-scene reference points at scene objects.");
                return;
            }

            if (string.IsNullOrEmpty(scene.path)) {
                Debug.LogWarning($"Save {target.name}'s scene first: a scene that was never saved has no id to link to.");
                return;
            }

            var anchor = target.GetComponent<CrossSceneAnchor>();
            if (anchor == null) {
                bool hadOtherEdits = scene.isDirty;
                anchor = Undo.AddComponent<CrossSceneAnchor>(target.gameObject);
                if (hadOtherEdits) unsavedScene = scene.path;
                else EditorSceneManager.SaveScene(scene);
            }

            anchorId.stringValue = anchor.Guid;
            sceneId.stringValue = AssetDatabase.AssetPathToGUID(scene.path);
        }

        private static Component FindTarget(string id, Type targetType) {
            if (string.IsNullOrEmpty(id)) return null;

            foreach (CrossSceneAnchor anchor in Object.FindObjectsByType<CrossSceneAnchor>(FindObjectsInactive.Include)) {
                if (anchor.Guid == id) return anchor.GetComponent(targetType);
            }

            return null;
        }
    }
}
