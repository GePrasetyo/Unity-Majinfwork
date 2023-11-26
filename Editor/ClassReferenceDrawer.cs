using System.Linq;
using System;
using UnityEditor;
using UnityEngine;
using System.Reflection;

namespace Majingari.Framework {
    [CustomPropertyDrawer(typeof(ClassReferenceAttribute))]
    public class ClassReferenceDrawer : PropertyDrawer {
        private Type[] types;
        private string[] typeNames;
        private Rect dropDownRect;
        private Rect fieldRect;
        GUIContent labelEmpty = new GUIContent("");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            dropDownRect = position;
            //dropDownRect.xMin += 14f;
            dropDownRect.height = EditorGUIUtility.singleLineHeight;

            fieldRect = position;
            fieldRect.height = position.height - EditorGUIUtility.singleLineHeight;

            EditorGUI.BeginProperty(position, label, property);

            if(property.propertyType != SerializedPropertyType.ManagedReference) {
                return;
            }

            if (types == null) {
                Type fieldType = GetTypeOfField(property);
                if (fieldType != null) {
                    types = TypeCache.GetTypesDerivedFrom(fieldType)
                        .Where(t => t.IsClass && !t.IsAbstract)
                        .ToArray();

                    typeNames = types.Select(t => t.Name).ToArray();
                }
            }

            if(types.Length == 0) {
                return;
            }

            int selectedIndex = 0;
            if (property.managedReferenceValue != null) {
                selectedIndex = Array.IndexOf(types, property.managedReferenceValue.GetType());
            }
            else {
                property.managedReferenceValue = Activator.CreateInstance(types[0]);
                selectedIndex = 0;
                property.serializedObject.ApplyModifiedProperties();
            }

            EditorGUI.BeginChangeCheck();
            selectedIndex = EditorGUI.Popup(dropDownRect, label.text, selectedIndex, typeNames);
            if (EditorGUI.EndChangeCheck()) {
                if (selectedIndex >= 0) {
                    property.managedReferenceValue = Activator.CreateInstance(types[selectedIndex]);
                    
                    
                    bool lalalal = property.serializedObject.ApplyModifiedProperties();
                    Debug.LogError(lalalal);
                }
            }

            EditorGUI.PropertyField(fieldRect, property, labelEmpty, true);
            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUI.GetPropertyHeight(property);
        }

        private Type GetTypeOfField(SerializedProperty property) {
            string[] propertyPath = property.managedReferenceFieldTypename.Split(' ');
            string className = propertyPath.Last();

            if (propertyPath.Length > 1) {
                string assemblyName = propertyPath[0];

                Assembly assembly = Assembly.Load(assemblyName);
                return assembly.GetType(className);
            }
            else {
                return Type.GetType(className);
            }            
        }
    }
}