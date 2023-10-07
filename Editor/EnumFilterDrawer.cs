using UnityEngine;
using UnityEditor;

[CustomPropertyDrawer(typeof(EnumFilterAttribute))]
public class EnumFilterDrawer : PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        EnumFilterAttribute enumAttribute = (EnumFilterAttribute)attribute;

        if (!enumAttribute.displayedOptions[0].GetType().IsEnum) {
            EditorGUI.PropertyField(position, property, label);
            return;
        }

        int currentIndex = System.Array.IndexOf(enumAttribute.optionsString, property.enumNames[property.enumValueIndex]);
        int newIndex = EditorGUI.Popup(position, label.text, currentIndex, enumAttribute.optionsString);

        if (newIndex != currentIndex) {
            property.enumValueIndex = System.Array.IndexOf(property.enumNames, enumAttribute.optionsString[newIndex]);
        }
    }
}