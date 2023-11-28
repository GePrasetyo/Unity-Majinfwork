using System;
using UnityEngine;

namespace Majingari.Toolkit {
    [AttributeUsage(AttributeTargets.Field)]
    public class ReadOnlyAttribute : PropertyAttribute {
    }
}

#if UNITY_EDITOR
namespace Majingari.Toolkit.Inspector {
    using Majingari.Toolkit;
    using UnityEditor;

    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyAttributeDrawer : PropertyDrawer {
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUI.GetPropertyHeight(property, label, true);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (!(attribute is ReadOnlyAttribute conditional)) return;

            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, label, true);
        }
    }
}
#endif