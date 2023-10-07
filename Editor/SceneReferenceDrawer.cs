using UnityEditor;
using UnityEngine;

namespace Majingari.Framework.World {
    [CustomPropertyDrawer(typeof(SceneReference))]
    public class SceneReferenceDrawer : PropertyDrawer {
        private static SceneReference instance;
        private Rect fieldRect;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            fieldRect = position;
            fieldRect.height = position.height - EditorGUIUtility.singleLineHeight;

            var startMapField = property.FindPropertyRelative(nameof(instance.Map));
            var startMapNameField = property.FindPropertyRelative(nameof(instance.mapName));

            EditorGUI.BeginProperty(position, label, property);

            if (startMapField.objectReferenceValue != null) {
                var mapAsset = (SceneAsset)startMapField.objectReferenceValue;
                startMapNameField.stringValue = mapAsset.name;
                startMapNameField.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.PropertyField(fieldRect, property, label, true);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUI.GetPropertyHeight(property);
        }
    }
}