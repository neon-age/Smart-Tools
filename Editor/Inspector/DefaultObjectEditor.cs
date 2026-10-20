
using System.Reflection;
using Unity.Scripting.LifecycleManagement;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Neonage.SmartTools
{
    [NoAutoStaticsCleanup]
    [CustomEditor(typeof(UnityEngine.Object), editorForChildClasses: true, isFallback = true)]
    class DefaultObjectEditor : Editor
    {
        static PropertyInfo R_inspectorUseIMGUIDefaultInspector = typeof(EditorSettings).GetProperty("inspectorUseIMGUIDefaultInspector", BindingFlags.NonPublic | BindingFlags.Static);

        public override VisualElement CreateInspectorGUI()
        {
            if ((bool)R_inspectorUseIMGUIDefaultInspector.GetValue(null))
                return null;
            var prefs = SmartToolsPrefs.Instance.inspector;
            var hideScriptField = prefs.hideScriptField;

            var root = new VisualElement();
            var property = serializedObject.GetIterator();
            if (property.NextVisible(true))
            {
                do
                {
                    if (property.name == "m_Script")
                    {
                        if (!hideScriptField)
                            root.Add(new PropertyField(property) { enabledSelf = false });
                    }
                    else
                    {
                        root.Add(new PropertyField(property));
                    }
                } 
                while (property.NextVisible(false));
            }
            return root;
        }

        public override void OnInspectorGUI()
        {
            var prefs = SmartToolsPrefs.Instance.inspector;
            var hideScriptField = prefs.hideScriptField;

            var property = serializedObject.GetIterator();
            if (property.NextVisible(true))
            {
                do
                {
                    if (property.name == "m_Script")
                    {
                        if (!hideScriptField)
                        {
                            EditorGUI.BeginDisabledGroup(true);
                            EditorGUILayout.PropertyField(property);
                            EditorGUI.EndDisabledGroup();
                        }
                    }
                    else
                    {
                        EditorGUILayout.PropertyField(property);
                    }
                } 
                while (property.NextVisible(false));
            }
        }
    }
}