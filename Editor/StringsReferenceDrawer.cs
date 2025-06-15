using UnityEngine;
using UnityEditor;
using System;

// Register the drawer for the new MultiStringReferenceAttribute
[CustomPropertyDrawer(typeof(StringsReferenceAttribute), true)]
public class StringsReferenceDrawer : PropertyDrawer {
    public string[] options;
    private Rect dropDownRect;
    private Rect fieldRect;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        var stringReferenceAttribute = attribute as StringsReferenceAttribute;

        if (options == null) {
            options = stringReferenceAttribute.GetOptions();
        }

        if (options.Length == 0) {
            return;
        }

        dropDownRect = position;
        dropDownRect.height = EditorGUIUtility.singleLineHeight;
        fieldRect = position;
        fieldRect.height = position.height - EditorGUIUtility.singleLineHeight;

        EditorGUI.BeginProperty(position, label, property);

        int selectedIndex = 0;
        if (!String.IsNullOrEmpty(property.stringValue)) {
            selectedIndex = Array.IndexOf(options, property.stringValue);
        }
        else {
            selectedIndex = 0;
        }

        EditorGUI.BeginChangeCheck();
        selectedIndex = EditorGUI.Popup(position, label.text, selectedIndex, options);

        if (EditorGUI.EndChangeCheck()) {
            if (selectedIndex >= 0) {
                property.stringValue = options[selectedIndex];
                property.serializedObject.ApplyModifiedProperties();
            }
        }

        EditorGUI.PropertyField(fieldRect, property, label);
        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
        return EditorGUI.GetPropertyHeight(property);
    }
}