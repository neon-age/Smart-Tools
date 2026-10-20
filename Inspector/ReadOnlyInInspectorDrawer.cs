
#if UNITY_EDITOR
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;

#endif

namespace UnityEngine // silly, but why not?
{
    public class ReadOnlyInInspectorAttribute : UnityEngine.PropertyAttribute {}
}

namespace Neonage.SmartTools
{
    // TODO: make solution that doesn't use PropertyDrawer
    // TODO: InspectorName support for fields?
#if UNITY_EDITOR 
    [CustomPropertyDrawer(typeof(UnityEngine.ReadOnlyInInspectorAttribute))]
    class ReadOnlyInInspectorDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            return new PropertyField(property) { enabledSelf = false };
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(property, label);
        }
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginDisabledGroup(true);
            EditorGUI.PropertyField(position, property, label);
            EditorGUI.EndDisabledGroup();
        }
    }
#endif
}
