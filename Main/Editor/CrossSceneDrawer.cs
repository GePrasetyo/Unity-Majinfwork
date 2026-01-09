using Majinfwork.CrossRef;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(CrossSceneReferenceAttribute))]
public class CrossSceneDrawer : PropertyDrawer {
    private const string DB_PATH = "Assets/Resources/CrossSceneData";

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        Object host = property.serializedObject.targetObject;
        CrossSceneDB db = GetDatabaseFor(host);

        // 1. Check if we have a saved link in the DB
        string savedGuid = GetSavedGuid(db, host, property.name);
        bool hasLink = !string.IsNullOrEmpty(savedGuid);

        // 2. Visual Style Setup
        Rect fieldRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        // If linked, we change the background color slightly to indicate success
        if (hasLink && property.objectReferenceValue == null) {
            Color originalColor = GUI.backgroundColor;
            GUI.backgroundColor = new Color(0.7f, 1f, 0.7f); // Light Green

            // Show the field. It will look like "None", but the green indicates it's handled.
            DrawObjectField(fieldRect, property, label, db, host);

            GUI.backgroundColor = originalColor;

            // 3. Draw a secondary label showing the "Virtual" link status
            Rect infoRect = new Rect(position.x + EditorGUIUtility.labelWidth, position.y + EditorGUIUtility.singleLineHeight, position.width - EditorGUIUtility.labelWidth, EditorGUIUtility.singleLineHeight);
            GUIStyle miniLabel = new GUIStyle(EditorStyles.miniLabel) { normal = { textColor = new Color(0.2f, 0.6f, 0.2f) } };
            EditorGUI.LabelField(infoRect, $"Cross Linked via GUID: {savedGuid.Substring(0, 8)}...", miniLabel);
        }
        else {
            DrawObjectField(fieldRect, property, label, db, host);
        }
    }

    private void DrawObjectField(Rect rect, SerializedProperty property, GUIContent label, CrossSceneDB db, Object host) {
        EditorGUI.BeginChangeCheck();
        Object current = EditorGUI.ObjectField(rect, label, property.objectReferenceValue, fieldInfo.FieldType, true);

        if (EditorGUI.EndChangeCheck()) {
            property.objectReferenceValue = current;

            if (current is Component target) {
                bool isCrossScene = IsCrossSceneReference(host, target);

                if (isCrossScene) {
                    var anchor = target.GetComponent<CrossSceneAnchor>() ?? Undo.AddComponent<CrossSceneAnchor>(target.gameObject);
                    db.Register(host, property.name, anchor.Guid);
                } else {
                    // Same scene - clear any existing DB entry, keep direct reference
                    db.Register(host, property.name, null);
                }
                EditorUtility.SetDirty(db);
                AssetDatabase.SaveAssets();
            }
            else {
                db.Register(host, property.name, null);
                EditorUtility.SetDirty(db);
            }
        }
    }

    private bool IsCrossSceneReference(Object host, Component target) {
        // ScriptableObjects/Prefabs always need cross-scene system (they can't serialize scene refs)
        if (!(host is MonoBehaviour hostMb))
            return true;

        // Same scene check for MonoBehaviours
        return hostMb.gameObject.scene != target.gameObject.scene;
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        // Add extra height if we need to show the "Linked" text
        Object host = property.serializedObject.targetObject;
        CrossSceneDB db = GetDatabaseFor(host);
        return (!string.IsNullOrEmpty(GetSavedGuid(db, host, property.name)) && property.objectReferenceValue == null)
            ? EditorGUIUtility.singleLineHeight * 2
            : EditorGUIUtility.singleLineHeight;
    }

    private string GetSavedGuid(CrossSceneDB db, Object host, string fieldName) {
        if (db == null) return null;
        var link = db.links.Find(l => l.host == host && l.fieldName == fieldName);
        return link?.targetGuid;
    }

    private CrossSceneDB GetDatabaseFor(Object host) {
        if (!Directory.Exists(DB_PATH)) Directory.CreateDirectory(DB_PATH);
        string dbName = (host is MonoBehaviour mb) ? $"{mb.gameObject.scene.name}_Refs" : "Global_Asset_Refs";
        string assetPath = $"{DB_PATH}/{dbName}.asset";

        var db = AssetDatabase.LoadAssetAtPath<CrossSceneDB>(assetPath);
        if (db == null) {
            db = ScriptableObject.CreateInstance<CrossSceneDB>();
            AssetDatabase.CreateAsset(db, assetPath);
        }
        return db;
    }
}