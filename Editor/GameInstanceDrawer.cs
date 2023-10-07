using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Majingari.Framework {
    [CustomPropertyDrawer(typeof(GameInstance))]
    public class GameInstanceDrawer : PropertyDrawer {
        private Type[] types;
        private string[] typeNames;
        private Rect dropDownRect;
        private Rect fieldRect;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            dropDownRect = position;
            dropDownRect.height = EditorGUIUtility.singleLineHeight;
            fieldRect = position;
            fieldRect.y += EditorGUIUtility.singleLineHeight;
            fieldRect.height = position.height + EditorGUIUtility.singleLineHeight;

            EditorGUI.BeginProperty(position, label, property);
            if (types == null) {
                types = typeof(GameInstance).Assembly.GetTypes()
                    .Where(t => typeof(GameInstance).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract)
                    .ToArray();
                typeNames = types.Select(t => t.Name).ToArray();
            }

            int selectedIndex;
            if (property.managedReferenceValue != null) {
                selectedIndex = Array.IndexOf(types, property.managedReferenceValue.GetType());
            }
            else {
                property.managedReferenceValue = new GameInstance();
                selectedIndex = Array.IndexOf(types, property.managedReferenceValue.GetType());
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUI.Popup(dropDownRect, "Game Instance", selectedIndex, typeNames);
            if (EditorGUI.EndChangeCheck()) {
                property.managedReferenceValue = Activator.CreateInstance(types[selectedIndex]);
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.PropertyField(fieldRect, property, label, true);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUI.GetPropertyHeight(property) + EditorGUIUtility.singleLineHeight;
        }
    }
}